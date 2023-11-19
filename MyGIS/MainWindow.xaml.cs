using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
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
        private string _id;
        private string _name;
        List<string> header;
        List<Type> typesHeader;
        List<object> data;

        public Layer()
        {
            _id = string.Empty;
            _name = string.Empty;
            header = new List<string>();
            typesHeader = new List<Type>();
            data = new List<object>();
        }

        public Layer(string id, string name, List<string> header, List<Type> typesHeader, List<object> data)
        {
            Id = id;
            Name = name;
            this.Header = header;
            this.TypesHeader = typesHeader;
            this.Data = data;
        }

        public string Id { get => _id; set => _id = value; }
        public string Name { get => _name; set => _name = value; }
        public List<string> Header { get => header; set => header = value; }
        public List<Type> TypesHeader { get => typesHeader; set => typesHeader = value; }
        public List<object> Data { get => data; set => data = value; }
    }
    

    public partial class MainWindow : Window
    {
        InfoProject _info = new InfoProject(4, "4 project");
        Image ImageVisibilityLayer { get; set; }
        Image ImageInvisibilityLayer { get; set; }
        Image ImageEditLayer { get; set; }
        Image ImageUnEditLayer { get; set; }

        System.Drawing.Image imageMap;

        ObservableCollection<PointsMapBinding>? pointsMapBinding;
        double CurrentScale = 128_000;

        ObservableCollection<Layer> Layers = new ObservableCollection<Layer>();

        List<Layer> VisibleLayers = new List<Layer>();
        Layer? CurrentEditLayer = null;


        private Map _map;
        private GraphicsOverlayCollection graphicsOverlays = new GraphicsOverlayCollection();
        RasterLayer rasterLayer;
        SpatialReference? spatialReference;

        public MainWindow()
        {
            InitializeComponent();
            
            SetMap(_info);

            SetGraphicsOverlayCollection();

            this.Loaded += MainWindow_Loaded;

            StackLayers.ItemsSource = Layers;

            ImageVisibilityLayer = new Image() { Source = new BitmapImage(new Uri("/images/Eye.png", UriKind.Relative)) };
            ImageInvisibilityLayer = new Image() { Source = new BitmapImage(new Uri("/images/Invisible.png", UriKind.Relative)) };
            ImageEditLayer = new Image() { Source = new BitmapImage(new Uri("/images/Edit.png", UriKind.Relative)) };
            ImageUnEditLayer = new Image() { Source = new BitmapImage(new Uri("/images/UnEdit.png", UriKind.Relative)) };

            MapView.GeoViewDoubleTapped += MapView_GeoViewDoubleTapped;
            MapView.GeoViewHolding += MapView_GeoViewHolding;
            MapView.GeoViewTapped += MapView_GeoViewTapped;
            
        }

        private void MapView_GeoViewTapped(object? sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MapView_GeoViewHolding(object? sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MapView_GeoViewDoubleTapped(object? sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await GetProjectInformation();

            await GetAllLayers();

            await rasterLayer.LoadAsync();

            await Task.Run(()=> { 
                _map.Basemap = new Basemap(rasterLayer);
                spatialReference = rasterLayer.SpatialReference;
            });

            await DrawPointsBinding(pointsMapBinding);
        }

        private void SetMap(InfoProject infoProject)
        {
            _map = new Map();

            MapView.Map = _map;
        }

        private void SetGraphicsOverlayCollection()
        {
            MapView.GraphicsOverlays = graphicsOverlays;
        }

        private async Task GetAllLayers()
        {
            SetDefaultImageLayer($"{_info.Name}{_info.IdProject}");

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
                var nameTable = reader.GetString(0);
                Layers.Add(new Layer() { 
                    Id = nameTable+_info.IdProject,
                    Name =  nameTable
                });
            }

            await reader.CloseAsync();

            for (int i=1; i < Layers.Count; i++) 
            {
                var layer = Layers[i];

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

        private void SetDefaultImageLayer(string name)
        {
            
            Layers.Clear();

            Layers.Add(
                new Layer()
                {
                    Id = $"basemap{_info.IdProject}",
                    Name = name
                }
            );

        }

        private void VisibilityLayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.Uid == $"basemap{_info.IdProject}")
            {
                rasterLayer.IsVisible = !rasterLayer.IsVisible;
            }

            string tag = (string)button.Tag;

            if (tag == "Visible")
            {
                button.Content = ImageInvisibilityLayer;
                button.Tag = "Invisible";
            }
            if (tag == "Invisible")
            {
                button.Content = ImageVisibilityLayer;
                button.Tag = "Visible";
            }
        }

        private void EditLayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            CurrentEditLayer = Layers.FirstOrDefault((layer) => layer.Id == button.Uid);

            if (CurrentEditLayer is null)
                return;

            CurrentEditLayerTextBlock.Text = CurrentEditLayer.Name;
        }

        private async Task DrawPointsBinding(ObservableCollection<PointsMapBinding>? mapBindings)
        {


            await Task.Run(()=>
            {
                if (mapBindings is null)
                    return;

                Envelope? envelope = rasterLayer.FullExtent;

                if (envelope == null)
                    return;

                MapPoint leftTop = new MapPoint(mapBindings[0].GeoX + envelope.XMin, mapBindings[0].GeoY + envelope.YMax);

                MapPoint rightTop = new MapPoint(mapBindings[1].GeoX + envelope.XMax, mapBindings[1].GeoY + envelope.YMax);

                MapPoint rightBottom = new MapPoint(mapBindings[2].GeoX + envelope.XMax, mapBindings[2].GeoY + envelope.YMin);

                MapPoint leftBottom = new MapPoint(mapBindings[3].GeoX + envelope.XMin, mapBindings[3].GeoY + envelope.YMin);

                GraphicsOverlay graphicsMapBindingsOverlay = new GraphicsOverlay();
                graphicsMapBindingsOverlay.Id = $"PointsMapBinding{_info.IdProject}";

                var diamondSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Blue, 20);

                var line = new Esri.ArcGISRuntime.Geometry.Polyline(new List<MapPoint>() {leftTop, rightTop });
                var lineStyle = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Orange, 3.0);


                List<MapPoint> polygonPoints = new List<MapPoint>
                {
                    new MapPoint(500, 590),
                    new MapPoint(550, 540),
                    new MapPoint(600, 530),
                    new MapPoint(550, 580),
                    new MapPoint(650, 600)
                };

                var polygon = new Esri.ArcGISRuntime.Geometry.Polygon(polygonPoints);

                var polygonSymbolOutline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Blue, 2.0);
                var polygonFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Orange, polygonSymbolOutline);

                var polygonGraphic = new Graphic(polygon, polygonFillSymbol);



                var ellipseArcSegment = new EllipticArcSegment(rightTop, 0, 20, 30, 0, 2 * Math.PI, spatialReference);

                var list = new List<Segment>()
                { (Segment)ellipseArcSegment};

                var ellipse = new Esri.ArcGISRuntime.Geometry.Polygon(list);
                var ellipseGraphic = new Graphic(ellipse, polygonFillSymbol);

                List<Graphic> graphics = new List<Graphic>()
            {
                new Graphic(leftTop, diamondSymbol),
                new Graphic(rightTop, diamondSymbol),
                new Graphic(rightBottom, diamondSymbol),
                new Graphic(leftBottom, diamondSymbol),
                new Graphic(line, lineStyle),
                polygonGraphic,
                ellipseGraphic

            };


                graphicsMapBindingsOverlay.Graphics.AddRange(graphics);

                graphicsOverlays.Add(graphicsMapBindingsOverlay);

            });
        }

        private void AddCircle()
        {

        }

        private void AddLine()
        {

        }
    }
}
