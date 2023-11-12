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

        private RelayCommand addMapImage;

        public RelayCommand AddMapImage
        {
            get
            {
                return addMapImage ?? (
                 addMapImage = new RelayCommand(
                     (obj) =>
                     {
                         byte[] image = obj as byte[];

                         if (image != null) 
                         {
                             //var rTimeImage = new RuntimeImage(image);

                             //new ImageFrame(rTimeImage, )

                             //Map.OperationalLayers.Clear();
                         }
                     }
                  )
                ); 
            }
        }


        private void SetupMap()
        {

            

            this.Map = new Map();
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
