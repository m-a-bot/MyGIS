using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Symbology;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Rasters;
using System.Windows.Input;
using System.Windows;

namespace MyGIS
{
    internal class MapViewModel : INotifyPropertyChanged
    {
        public MapViewModel() 
        {
            SetupMap();

            CreateGraphics();

        }


        // add points of binding
        public void AddMapImage(double x, double y, byte[] imageBytes)
        {
            GraphicsOverlay imageMapOverlay = new GraphicsOverlay();

            var image = new PictureMarkerSymbol(new RuntimeImage(imageBytes));

            var graphic = new Graphic(new MapPoint(x, y), image);

            imageMapOverlay.Graphics.Add(graphic);

            GraphicsOverlays.Add(imageMapOverlay);
        }

        //(49, 56) (78.5760942765971, 64.015213867277) Label "левый вверх",
        //  (49.5, 56) (1920.35387205418, 69.5963586484211) Label "правый вверх",
        //  (49, 55.66) (65.3467140546254, 2252.75415887265) Label "левый низ",
        //  (49.5, 55.66) (1924.07463524161, 2262.67619403913) Label "правый низ"

        private void SetupMap()
        {
            //new Raster()

            //this.Map = new Map(new Basemap(new RasterLayer();
        }

        private void Create()
        {
            //new RuntimeImage(byte[]);
        }

        private void CreateGraphics()
        {
            var kazanGraphicsOverlay = new GraphicsOverlay();

            GraphicsOverlayCollection overlays = new GraphicsOverlayCollection()
            {
                kazanGraphicsOverlay
            };

            this.GraphicsOverlays = overlays;

            var dumeBeachPoint = new MapPoint(49.106815, 55.797930, SpatialReferences.Wgs84);

            // Create a symbol to define how the point is displayed.
            var pointSymbol = new SimpleMarkerSymbol
            {
                Style = SimpleMarkerSymbolStyle.Circle,
                Color = System.Drawing.Color.Orange,
                Size = 10.0
            };

            // Add an outline to the symbol.
            pointSymbol.Outline = new SimpleLineSymbol
            {
                Style = SimpleLineSymbolStyle.Solid,
                Color = System.Drawing.Color.Blue,
                Width = 2.0
            };

            // Create a point graphic with the geometry and symbol.
            var pointGraphic = new Graphic(dumeBeachPoint, pointSymbol);

            // Add the point graphic to graphics overlay.
            kazanGraphicsOverlay.Graphics.Add(pointGraphic);
        }

        private Map? _map;

        public Map? Map
        {
            get { return _map; }
            set
            {
                _map = value;
                OnPropertyChanged();
            }
        }

        private GraphicsOverlayCollection? _graphicsOverlay;
        
        public GraphicsOverlayCollection? GraphicsOverlays
        {
            get { return _graphicsOverlay; }
            set
            {
                _graphicsOverlay = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
