using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyGIS
{
    public class Layer
    {
        private int _id;
        private string _name;
        List<string> header;
        List<Type> typesHeader;
        List<object> data;

        public Layer()
        {
            _name = string.Empty;
            header = new List<string>();
            typesHeader = new List<Type>();
            data = new List<object>();
        }

        public Layer(int id, string name, List<string> header, List<Type> typesHeader, List<object> data)
        {
            Id = id;
            Name = name;
            this.Header = header;
            this.TypesHeader = typesHeader;
            this.Data = data;
        }

        public int Id { get => _id; set => _id = value; }
        public string Name { get => _name; set => _name = value; }
        public List<string> Header { get => header; set => header = value; }
        public List<Type> TypesHeader { get => typesHeader; set => typesHeader = value; }
        public List<object> Data { get => data; set => data = value; }
    }
    

    public partial class MainWindow : Window
    {
        InfoProject _info = new InfoProject(4, "4 project");

        System.Drawing.Image imageMap;

        ObservableCollection<PointsMapBinding>? pointsMapBinding;
        double CurrentScale = 128_000;

        ObservableCollection<Layer> Layers = new ObservableCollection<Layer>();

        List<Layer> VisibleLayers = new List<Layer>();
        Layer? CurrentEditLayer = null;


        private Map _map;
        RasterLayer rasterLayer;

        public MainWindow()
        {
            InitializeComponent();
            

            SetMap(_info);

            this.Loaded += MainWindow_Loaded;

            StackLayers.ItemsSource = Layers;

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await GetProjectInformation();

            await GetAllLayers();

            await rasterLayer.LoadAsync();

            await Task.Run(()=>_map.Basemap = new Basemap(rasterLayer));
        }

        private void SetMap(InfoProject infoProject)
        {
            _map = new Map();

            MapView.Map = _map;
        }

        private async Task GetAllLayers()
        {
            Layers.Clear();
            await DbManager.OpenConnection();

            string command = "select nameTableOfLayer from project_layers where idProject = @id;";

            List<MySqlParameter> mySqlParameters = new List<MySqlParameter>() {
                new MySqlParameter("@id", MySqlDbType.Int32)
                {
                    Value = _info.IdProject
                },
            };

            var reader = await DbManager.ExecuteCommand(command, mySqlParameters);

            // read all data
            while (await reader.ReadAsync())
            {
                Layers.Add(new Layer() { Name = reader.GetString(0) });
            }

            await reader.CloseAsync();

            foreach (var layer in Layers)
            {
                await GetAllFromLayer(layer);
            }

            await DbManager.CloseConnection();
        }

        private async Task GetProjectInformation()
        {
            await DbManager.OpenConnection();

            string command = "select dataOffSet, ProjectImage, PointsBinding from project_images where idProject = @id;";

            List<MySqlParameter> parameters = new List<MySqlParameter>() {
                new MySqlParameter("@id", MySqlDbType.Int32)
                {
                    Value = _info.IdProject
                }

            };

            var reader = await DbManager.ExecuteCommand(command, parameters);

            try
            {

                while (await reader.ReadAsync())
                {
                    int dataOffSet = reader.GetInt32(0);

                    byte[] buffer = new byte[dataOffSet];
                    reader.GetBytes(1, 0, buffer, 0, dataOffSet);

                    var jsonStringPointsBinding = reader.GetString(2);

                    pointsMapBinding = JsonSerializer.Deserialize(jsonStringPointsBinding, typeof(ObservableCollection<PointsMapBinding>))
                        as ObservableCollection<PointsMapBinding>;

                    using (MemoryStream stream = new MemoryStream())
                    {
                        stream.Write(buffer, 0, buffer.Length);

                        imageMap = System.Drawing.Image.FromStream(stream);

                        
                        imageMap.Save(@"local/"+_info.Name+""+_info.IdProject+".tif", System.Drawing.Imaging.ImageFormat.Tiff);
                    }

                }
            }
            catch { }

            await reader.CloseAsync();

            await Task.Run(() =>
            {
                var path = $"local/{_info.Name}{_info.IdProject}.tif";
                var image = new Raster(path);

                rasterLayer = new RasterLayer(image);
            });

            await DbManager.CloseConnection();
        }

        private async Task GetAllFromLayer(Layer layer)
        {
            string nameOfLayer = layer.Name;

            string command = $"select * from {nameOfLayer};";

            var reader = await DbManager.ExecuteCommand(command);

            List<string> header = new List<string>();
            List<Type> typesTable = new List<Type>();
            List<object> data = new List<object>();

            int countColumns = reader.FieldCount;

            for (int i = 0; i < countColumns; i++)
            {
                string _head = reader.GetName(i);
                Type type = reader.GetFieldType(i);

                header.Add(_head);
                typesTable.Add(type);
            }

            while (await reader.ReadAsync())
            {
                for (int i = 0; i < countColumns; i++)
                {
                    data.Add(reader.GetValue(i));
                }
            }

            layer.Header = header;
            layer.TypesHeader = typesTable;
            layer.Data = data;

            await reader.CloseAsync();

        }

    }
}
