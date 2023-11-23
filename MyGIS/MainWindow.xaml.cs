using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Rasters;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Offline;
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
        public GraphicsOverlay GraphicsOverlay { get; set; }
    }


    enum TypeOperation
    {
        None = 0b00000000,
        Identify = 0b00000001,
        InfoAttributes = 0b00000010,
        Info   = 0b00000100,
        Clear  = 0b00001000,
        Move   = 0b00010000,
        Rotate = 0b00100000,
        Resize = 0b01000000
    }

    enum TypeGraphic
    {
        None = 0b0000,
        Rectangle = 0b0001,
        Polygon = 0b0010,
        Line = 0b0100,
        Ellipse = 0b1000
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

        TypeOperation Operation { get; set; } = TypeOperation.None;
        TypeGraphic TypeGraphic { get; set; } = TypeGraphic.None;
        Graphic? movableGraphic = null;
        MapPoint LastMousePosition;
        int numberOfAddedPoints = 0;

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

            AddDrawingOverlayToGraphicsOverlays();

            this.Loaded += MainWindow_Loaded;

            StackLayers.ItemsSource = Layers;

            ImageVisibilityLayer = new Image() { Source = new BitmapImage(new Uri("/images/Eye.png", UriKind.Relative)) };
            ImageInvisibilityLayer = new Image() { Source = new BitmapImage(new Uri("/images/Invisible.png", UriKind.Relative)) };
            ImageEditLayer = new Image() { Source = new BitmapImage(new Uri("/images/Edit.png", UriKind.Relative)) };
            ImageUnEditLayer = new Image() { Source = new BitmapImage(new Uri("/images/UnEdit.png", UriKind.Relative)) };

            MapView.MouseLeftButtonDown += MapView_MouseLeftButtonDown;
            MapView.MouseDoubleClick += MapView_MouseDoubleClick;
            MapView.MouseMove += MapView_MouseMove;
            MapView.MouseRightButtonDown += MapView_MouseRightButtonDown;
        }

        private void MapView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (movableGraphic != null)
                movableGraphic.IsSelected = false;
            movableGraphic = null;

        }

        private void MapView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (movableGraphic != null)
                movableGraphic.IsSelected = false;
            movableGraphic = null;


            if (CurrentEditLayer is null)
                return;
            GraphicsOverlay? drawing = graphicsOverlays["DrawingGraphicOverlay"];
            GraphicsOverlay? editOverlay = graphicsOverlays[CurrentEditLayer.Name];

            if (drawing.Graphics.Count < 2)
                return;

            if (TypeGraphic == TypeGraphic.Rectangle)
            {
                List<MapPoint> points = new List<MapPoint>();
                foreach (var graphic in drawing.Graphics)
                {
                    points.Add((MapPoint)graphic.Geometry);
                }

                MapPoint a = points[0],
                    b = points[1];

                MapPoint point1 = new MapPoint(b.X, a.Y),
                    point2 = new MapPoint(a.X, b.Y);
                points.Insert(1, point1);
                points.Add(point2);

                Esri.ArcGISRuntime.Geometry.Polygon multipoint = new Esri.ArcGISRuntime.Geometry.Polygon(points);

                var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 5);
                var fillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Cross, System.Drawing.Color.Crimson, lineSymbol);

                var polygon = new Graphic(multipoint, fillSymbol);

                drawing.Graphics.Clear();
                numberOfAddedPoints = 0;

                editOverlay.Graphics.Add(polygon);
            }

            if (TypeGraphic == TypeGraphic.Line)
            {
                List<MapPoint> points = new List<MapPoint>();
                foreach (var graphic in drawing.Graphics)
                {
                    points.Add((MapPoint)graphic.Geometry);
                }
                Esri.ArcGISRuntime.Geometry.Polyline multipoint = new Esri.ArcGISRuntime.Geometry.Polyline(points);
                var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 5);

                var line = new Graphic(multipoint, lineSymbol);
                line.ZIndex = 1;

                drawing.Graphics.Clear();
                numberOfAddedPoints = 0;

                editOverlay.Graphics.Add(line);
            }

            if (TypeGraphic == TypeGraphic.Polygon)
            {
                List<MapPoint> points = new List<MapPoint>();
                foreach (var graphic in drawing.Graphics)
                {
                    points.Add((MapPoint)graphic.Geometry);
                }
                Esri.ArcGISRuntime.Geometry.Polygon multipoint = new Esri.ArcGISRuntime.Geometry.Polygon(points);

                var lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 5);
                var fillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Cross, System.Drawing.Color.Crimson, lineSymbol);

                var polygon = new Graphic(multipoint, fillSymbol);

                drawing.Graphics.Clear();
                numberOfAddedPoints = 0;

                editOverlay.Graphics.Add(polygon);
            }

            if (TypeGraphic == TypeGraphic.Ellipse)
            {
                List<MapPoint> points = new List<MapPoint>();
                foreach (var graphic in drawing.Graphics)
                {
                    points.Add((MapPoint)graphic.Geometry);
                }

                double a = Math.Abs(points[1].X - points[0].X),
                    b = Math.Abs(points[1].Y - points[0].Y);

                double rotationAngle = Math.Atan2(points[1].X - points[0].X, points[1].Y - points[0].Y) == double.NaN ? 0 : Math.Atan2(a, b);

                double temp;
                if (a > b)
                {
                    temp = a;
                    a = b;
                    b = temp;
                }

                var ellipseArcSegment = new EllipticArcSegment(points[0], 0, b, a / b, 0, 2 * Math.PI, spatialReference);
                
                var list = new List<Segment>()
                { (Segment)ellipseArcSegment};

                SimpleLineSymbol simpleLine = new SimpleLineSymbol(SimpleLineSymbolStyle.ShortDash, System.Drawing.Color.DarkOrange, 3);
                SimpleFillSymbol polygonFillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Honeydew, simpleLine);

                var ellipse = new Esri.ArcGISRuntime.Geometry.Polygon(list);
                //Esri.ArcGISRuntime.Geometry.Geometry geo = ellipse.Rotate(rotationAngle * 180 / Math.PI);

                var ellipseGraphic = new Graphic(ellipse, polygonFillSymbol);

                drawing.Graphics.Clear();
                numberOfAddedPoints = 0;

                editOverlay.Graphics.Add(ellipseGraphic);
            }

        }

        private void MapView_MouseMove(object sender, MouseEventArgs e)
        {
            var screenPoint = e.GetPosition(MapView);

            MapPoint? mapMousePosition = MapView.ScreenToLocation(screenPoint);

            if (mapMousePosition == null)
                return;

            double x = (mapMousePosition.X - pointsMapBinding[0].PixelX) / (pointsMapBinding[1].PixelX - pointsMapBinding[0].PixelX),
                y = (mapMousePosition.Y - pointsMapBinding[3].PixelY) / (pointsMapBinding[0].PixelY - pointsMapBinding[3].PixelY);

            double geoX = pointsMapBinding[0].GeoX + x * (pointsMapBinding[1].GeoX - pointsMapBinding[0].GeoX),
                geoY = pointsMapBinding[3].GeoY + y * (pointsMapBinding[0].GeoY - pointsMapBinding[3].GeoY);

            GeoCoordsTextBlock.Text = $"Координаты {geoX}; {geoY};";
            
            if (movableGraphic != null)
            {
                var geometry = movableGraphic.Geometry;
                MapPoint MapLocation = MapView.ScreenToLocation(screenPoint);

                double offSetX = MapLocation.X - LastMousePosition.X,
                    offSetY = MapLocation.Y - LastMousePosition.Y;

                LastMousePosition = MapLocation;
                var newGeometry = GeometryEngine.Move(geometry, offSetX, offSetY);

                CurrentEditLayer.GraphicsOverlay.Graphics.First((
                    el)=> el == movableGraphic
                ).Geometry = newGeometry;
            }
        }

        private async void MapView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            movableGraphic = null;
            var screenPoint = e.GetPosition(MapView);

            foreach (var graphicsOverlay in graphicsOverlays)
            {
                if (graphicsOverlay is null) continue;

                foreach (var graphic in graphicsOverlay.Graphics)
                {
                    if (graphic is null) continue;

                    graphic.IsSelected = false;
                }
            }

            if (Operation == TypeOperation.Identify)
            {
                var result = await MapView.IdentifyGraphicsOverlaysAsync(screenPoint, 10, false);

                if (result is null || result.Count <= 0)
                    return;


                var layer = result.First((el) => el.GraphicsOverlay.IsVisible);

                if (layer is null) return;

                layer.Graphics[0].IsSelected = true;
            }

            if (Operation == TypeOperation.Info)
            {
                var result = await MapView.IdentifyGraphicsOverlaysAsync(screenPoint, 10, false);

                if (result is null || result.Count <= 0)
                    return;


                var layer = result.First((el) => el.GraphicsOverlay.IsVisible);

                if (layer is null) return;

                Graphic figure = layer.Graphics[0];

                Esri.ArcGISRuntime.Geometry.Geometry? _geo = figure.Geometry;

                double area = _geo.Area(),
                    length = _geo.Length();

                double areaGeo = _geo.AreaGeodetic(AreaUnits.SquareKilometers),
                    lengthGeo = _geo.LengthGeodetic(LinearUnits.Kilometers);

                

                MessageBox.Show(area + " - " + areaGeo, length + " - " + lengthGeo);
            }

            if (Operation == TypeOperation.Clear)
            {
                if (CurrentEditLayer is null)
                    return;

                var result = await MapView.IdentifyGraphicsOverlayAsync(CurrentEditLayer.GraphicsOverlay, screenPoint, 10, false);

                if (result is null || result.Graphics.Count <= 0)
                    return;

                var graphic = result.Graphics[0];

                graphicsOverlays[CurrentEditLayer.Name].Graphics.Remove(graphic);
            }

            if (Operation == TypeOperation.Move)
            {
                
                if (CurrentEditLayer is null)
                    return;

                var result = await MapView.IdentifyGraphicsOverlayAsync(CurrentEditLayer.GraphicsOverlay, screenPoint, 10, false);

                if (result is null || result.Graphics.Count <= 0)
                    return;

                var graphic = result.Graphics[0];
                graphic.IsSelected = true;

                movableGraphic = graphic;

                LastMousePosition = MapView.ScreenToLocation(screenPoint);
            }

            
            if (CurrentEditLayer is null)
                return;
            GraphicsOverlay? drawing = graphicsOverlays["DrawingGraphicOverlay"];
            GraphicsOverlay? editOverlay = graphicsOverlays[CurrentEditLayer.Name];


            if (TypeGraphic == TypeGraphic.Rectangle)
            {
                

                if (numberOfAddedPoints < 2)
                {
                    MapPoint? mapPoint = MapView.ScreenToLocation(screenPoint);

                    var marker = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.DarkGreen, 5);

                    Graphic point = new Graphic(mapPoint, marker);

                    drawing.Graphics.Add(point);
                    numberOfAddedPoints++;
                }
                else
                {
                    
                }
            }

            if (TypeGraphic == TypeGraphic.Line)
            {

                if (numberOfAddedPoints < 2)
                {
                    MapPoint? mapPoint = MapView.ScreenToLocation(screenPoint);

                    var marker = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.DarkGreen, 5);

                    Graphic point = new Graphic(mapPoint, marker);

                    drawing.Graphics.Add(point);
                    numberOfAddedPoints++;
                }
            }

            if (TypeGraphic == TypeGraphic.Polygon)
            {

                MapPoint? mapPoint = MapView.ScreenToLocation(screenPoint);

                var marker = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.DarkGreen, 5);

                Graphic point = new Graphic(mapPoint, marker);

                drawing.Graphics.Add(point);
                numberOfAddedPoints++;

            }

            if (TypeGraphic == TypeGraphic.Ellipse)
            {

                if (numberOfAddedPoints < 2)
                {
                    MapPoint? mapPoint = MapView.ScreenToLocation(screenPoint);

                    var marker = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.DarkGreen, 5);

                    Graphic point = new Graphic(mapPoint, marker);

                    drawing.Graphics.Add(point);
                    numberOfAddedPoints++;
                }
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await GetProjectInformation();

            await GetAllLayers();

            await rasterLayer.LoadAsync();

            await Task.Run(()=> {
                _map.Basemap = new Basemap(rasterLayer);
                spatialReference = rasterLayer.FullExtent.SpatialReference;
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

        private void AddDrawingOverlayToGraphicsOverlays()
        {
            GraphicsOverlay drawingOverlay = new GraphicsOverlay() { 
                Id = "DrawingGraphicOverlay"
            };
            graphicsOverlays.Add(drawingOverlay);
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
                    Name = nameTable
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
            GraphicsOverlay graphicsOverlay = new GraphicsOverlay() { Id = nameOfLayer };

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
                if (data[countColumns - 1] == null)
                    continue;

                graphicsOverlay.Graphics.Add((Graphic)data[countColumns - 1]);
            }

            layer.Header = header;
            layer.TypesHeader = typesTable;
            layer.Data = data;
            layer.GraphicsOverlay = graphicsOverlay;

            graphicsOverlays.Add(graphicsOverlay);

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

            Layer? editLayer = Layers.FirstOrDefault((layer) => layer.Id == button.Uid);

            if (editLayer is null)
                return;

            if (CurrentEditLayer == editLayer)
            {
                CurrentEditLayerTextBlock.Text = "";
                CurrentEditLayer = null;
                return;
            }

            CurrentEditLayer = editLayer;
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

                MapPoint leftTop = new MapPoint(mapBindings[0].PixelX + envelope.XMin, mapBindings[0].PixelY + envelope.YMin, spatialReference);

                MapPoint rightTop = new MapPoint(mapBindings[1].PixelX + envelope.XMin, mapBindings[1].PixelY + envelope.YMin, spatialReference);

                MapPoint rightBottom = new MapPoint(mapBindings[2].PixelX + envelope.XMin, mapBindings[2].PixelY + envelope.YMin, spatialReference);

                MapPoint leftBottom = new MapPoint(mapBindings[3].PixelX + envelope.XMin, mapBindings[3].PixelY + envelope.YMin, spatialReference);

                GraphicsOverlay graphicsMapBindingsOverlay = new GraphicsOverlay();
                graphicsMapBindingsOverlay.Id = $"PointsMapBinding{_info.IdProject}";

                

                var diamondSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Diamond, System.Drawing.Color.Blue, 10);

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
                //new Graphic(line, lineStyle),
                //polygonGraphic,
                //ellipseGraphic

            };


                graphicsMapBindingsOverlay.Graphics.AddRange(graphics);

                Layers[0].GraphicsOverlay = graphicsMapBindingsOverlay;

                graphicsOverlays.Add(graphicsMapBindingsOverlay);

            });
        }

        private void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            Operation = TypeOperation.Identify;
            TypeGraphic = TypeGraphic.None;
            HideInfoAttributes();
            ClearOverlay();
        }

        private void InfoAttributesButton_Click(object sender, RoutedEventArgs e)
        {
            Operation = TypeOperation.InfoAttributes;
            TypeGraphic = TypeGraphic.None;
            ClearOverlay();
            LastGridColumn.Width = new GridLength(67, GridUnitType.Star);
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            Operation = TypeOperation.Info;
            TypeGraphic = TypeGraphic.None;
            HideInfoAttributes();
            ClearOverlay();
        }

        private void ClearGraphicButton_Click(object sender, RoutedEventArgs e)
        {
            Operation = TypeOperation.Clear;
            TypeGraphic = TypeGraphic.None;
            HideInfoAttributes();
            ClearOverlay();
        }

        private void DrawRectangleButton_Click(object sender, RoutedEventArgs e)
        {
            TypeGraphic = TypeGraphic.Rectangle;
            Operation = TypeOperation.None;

            ClearOverlay();
            numberOfAddedPoints = 0;
            HideInfoAttributes();
        }

        private void DrawPolygon_Click(object sender, RoutedEventArgs e)
        {
            TypeGraphic = TypeGraphic.Polygon;
            Operation = TypeOperation.None;

            ClearOverlay();
            numberOfAddedPoints = 0;
            HideInfoAttributes();
        }

        private void DrawLineButton_Click(object sender, RoutedEventArgs e)
        {
            TypeGraphic = TypeGraphic.Line;
            Operation = TypeOperation.None;

            ClearOverlay();
            numberOfAddedPoints = 0;

            HideInfoAttributes();
        }

        private void DrawEllipseButton_Click(object sender, RoutedEventArgs e)
        {
            TypeGraphic = TypeGraphic.Ellipse;
            Operation = TypeOperation.None;

            ClearOverlay();
            numberOfAddedPoints = 0;

            HideInfoAttributes();
        }

        private void MoveGraphicButton_Click(object sender, RoutedEventArgs e)
        {
            Operation = TypeOperation.Move;
            TypeGraphic = TypeGraphic.None;
            HideInfoAttributes();
            ClearOverlay();
        }

        private void HideInfoAttributes()
        {
            LastGridColumn.Width = new GridLength(0, GridUnitType.Star);
        }

        private void OpenSQLWindow(object sender, RoutedEventArgs e)
        {
            TypeGraphic = TypeGraphic.None;
            HideInfoAttributes();
            ClearOverlay();
        }

        private void ClearOverlay(string name = "DrawingGraphicOverlay")
        {
            GraphicsOverlay? overlay = graphicsOverlays[name];
            overlay.Graphics.Clear();
        }
    }
}
