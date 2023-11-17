﻿using Esri.ArcGISRuntime.Geometry;
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
        InfoProject _info = new InfoProject(4, "4 project");

        System.Drawing.Image imageMap;

        ObservableCollection<PointsMapBinding>? pointsMapBinding;

        MapViewModel mapViewModel;

        double CurrentScale = 128_000;

        Point ScreenCenter;

        public ManageMap()
        {
            InitializeComponent();

            mapViewModel = new MapViewModel(_info, pointsMapBinding);

            SetMapBinding();

            MapPoint mapCenterPoint = mapViewModel.CenterMap;
            MainMapView.SetViewpoint(new Viewpoint(mapCenterPoint, 100_000));

        }

        private void SetMapBinding()
        {
            Binding bindingMap = new Binding();
            Binding bindingGraphicsOverlays = new Binding();

            bindingMap.Source = mapViewModel;
            bindingMap.Path = new PropertyPath(MapViewModel.MapProperty);

            bindingGraphicsOverlays.Source = mapViewModel;
            bindingGraphicsOverlays.Path = new PropertyPath(MapViewModel.GraphicsOverlaysProperty);

            MainMapView.SetBinding(MapView.MapProperty, bindingMap);
            MainMapView.SetBinding(MapView.GraphicsOverlaysProperty, bindingMap);
        }

        private async void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            var extent = mapViewModel.RasterLayer.FullExtent;

            CurrentScale = Math.Max(CurrentScale / 2, 125);
            

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
            
        }

        private async void ManageMapPage_Loaded(object sender, RoutedEventArgs e)
        {
            await DbManager.OpenConnection();

            string command = "select dataOffSet, ProjectImage, PointsBinding from ProjectImages where idProject = @id;";

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

                        imageMap.Save($"temp/{_info.Name}{_info.IdProject}.tif", System.Drawing.Imaging.ImageFormat.Tiff);
                    }

                }
            }
            catch { }

            await reader.CloseAsync();

            await DbManager.CloseConnection();

            
        }
    }
}
