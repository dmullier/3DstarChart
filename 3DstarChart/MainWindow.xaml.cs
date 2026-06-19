using _3DstarMap;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _3DstarChart
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 1. Trackpad bindings
            MainViewport.RotateGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control);
            MainViewport.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            MainViewport.ZoomGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Alt);

            // 2. Wait until the window is fully loaded before rendering stars and animating
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Now that Helix is fully alive, configure the data map
            PopulateStarMap();

            // Trigger the cinematic animation loop
            AnimateCameraToSun();
        }

        private void PopulateStarMap()
        {
            SunGroup.Children.Clear();

            List<Visual3D> toRemove = new List<Visual3D>();
            foreach (var child in MainViewport.Children)
            {
                if (child is PointsVisual3D) toRemove.Add(child);
            }
            foreach (var oldLayer in toRemove) MainViewport.Children.Remove(oldLayer);

            var sunMesh = StarModelFactory.CreateStarCube(0, 0, 0, 0.4, Colors.Yellow);
            SunGroup.Children.Add(sunMesh);

            string filePath = "starchart.csv";
            StarCollection chart = new StarCollection(filePath);

            Dictionary<Color, Point3DCollection> colorGroups = new Dictionary<Color, Point3DCollection>();

            foreach (Star star in chart.Stars)
            {
                if (star.Id == 0) continue;

                Color starColor = StarModelFactory.GetColourFromSpectrum(star.SpectralType);

                if (!colorGroups.ContainsKey(starColor))
                {
                    colorGroups[starColor] = new Point3DCollection();
                }

                colorGroups[starColor].Add(new Point3D(star.X, star.Y, star.Z));
            }

            foreach (var kvp in colorGroups)
            {
                Color layerColor = kvp.Key;
                Point3DCollection layerPoints = kvp.Value;

                PointsVisual3D starLayer = new PointsVisual3D
                {
                    Points = layerPoints,
                    Color = layerColor,
                    Size = 4
                };

                MainViewport.Children.Add(starLayer);
            }
        }

        private void AnimateCameraToSun()
        {
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                // 1. Manually set conservative clipping overrides first
                helixCamera.NearPlaneDistance = 0.001;
                helixCamera.FarPlaneDistance = 1000.0;

                // 2. Define our target destination coordinates right in front of the Sun
                Point3D targetDestination = new Point3D(0, 0, 0.45);
                Vector3D lookDirection = new Vector3D(0, 0, -1);
                Vector3D upDirection = new Vector3D(0, 1, 0);

                // 3. Use Helix's native, high-performance animation framework.
                // Parameters: (Target position, Look vector, Up vector, Animation duration in milliseconds)
                MainViewport.Camera.LookAt(targetDestination, lookDirection, upDirection, 8000);
            }
        }
    }
}