using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyGIS
{
    /// <summary>
    /// Логика взаимодействия для ManageMap.xaml
    /// </summary>
    public partial class ManageMap : Page
    {
        InfoProject _info;

        System.Drawing.Image imageMap;

        ObservableCollection<PointsMapBinding>? pointsMapBinding;

        double CurrentScale = 100_000;

        Point ScreenCenter;

        public ManageMap(InfoProject info)
        {
            InitializeComponent();

            _info = info;

            DbManager.OpenConnection();

            ScreenCenter = new Point(MainMapView.Width / 2, MainMapView.Height / 2);

            MapPoint mapCenterPoint = new MapPoint(49.106815, 55.797930, SpatialReferences.Wgs84);
            MainMapView.SetViewpoint(new Viewpoint(mapCenterPoint, 100_000));
        }

        

        private void AddLayerButton_Click(object sender, RoutedEventArgs e)
        {
            new CreateNewDataLayer(_info).Show();
        }

        private async void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentScale = Math.Max(CurrentScale / 2, 3125);
            

            var center = GetCenterVisibleArea();

            await MainMapView.SetViewpointAsync(new Viewpoint(center, CurrentScale), TimeSpan.FromSeconds(0.25), Esri.ArcGISRuntime.UI.AnimationCurve.EaseOutQuad);
        }

        private async void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentScale *= 2;

            var center = GetCenterVisibleArea();

            await MainMapView.SetViewpointAsync(new Viewpoint(center, CurrentScale), TimeSpan.FromSeconds(0.5), Esri.ArcGISRuntime.UI.AnimationCurve.EaseInQuad);
        }

        private MapPoint? GetCenterVisibleArea()
        {
            var viewPoint = MainMapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry);

            var center = viewPoint.TargetGeometry.Extent.GetCenter();

            return center;
        }

        private async void GetAllLayers()
        {
            await DbManager.OpenConnection();

            string command = "select nameTableOfLayer from ProjectsLayers where idProject = @id;";

            List<MySqlParameter> mySqlParameters = new List<MySqlParameter>() { 
                new MySqlParameter("@id", MySqlDbType.Int32)
                {
                    Value = _info.IdProject
                },
            };

            var reader = await DbManager.ExecuteCommand(command, mySqlParameters);

            // read all data

            await reader.CloseAsync();

            await DbManager.CloseConnection();
        }

        private async void MainMapView_Initialized(object sender, EventArgs e)
        {
            await DbManager.OpenConnection();

            string command = "select dataOffSet, ProjectImage, PointsBinding where idProject = @id;";

            List<MySqlParameter> parameters = new List<MySqlParameter>() {
                new MySqlParameter("@id", MySqlDbType.Int32)
                {
                    Value = _info.IdProject
                }

            };

            var reader = await DbManager.ExecuteCommand(command, parameters);

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
                }
                    
            }

            await reader.CloseAsync();

            await DbManager.CloseConnection();
        }
    }
}
