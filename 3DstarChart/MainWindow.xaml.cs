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
            // 1. Generate the scene layout (which forces an internal Helix layout reset)
            PopulateStarMap();

            // 2. HARD-OVERRIDE CLIPPING THRESHOLDS DIRECTLY ON THE ACTIVE CAMERA
            // This stops Helix from rendering the Sun black when the camera gets close.
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                helixCamera.NearPlaneDistance = 0.0000001;
                helixCamera.FarPlaneDistance = 500.0;
            }

            // 3. Kick off your clean animation path down to 0.0045
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

            var sunMesh = StarModelFactory.CreateStarCube(0, 0, 0, 0.01, Colors.Yellow);
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
                // 1. Lock in healthy clipping planes right before the flight begins
                helixCamera.NearPlaneDistance = 0.001;
                helixCamera.FarPlaneDistance = 1000.0;

                // ==========================================
                // ANIMATION A: Move the Camera Through the Stars
                // ==========================================
                Point3D startPosition = new Point3D(0, 0, 40);
                Point3D endPosition = new Point3D(0, 0, 3.0); // Stop safely at 3 parsecs out
                Vector3D lookDirection = new Vector3D(0, 0, -1);
                Vector3D upDirection = new Vector3D(0, 1, 0);

                // Tell Helix to smoothly glide the camera over 8 seconds
                MainViewport.Camera.LookAt(endPosition, lookDirection, upDirection, 8000);

                // ==========================================
                // ANIMATION B: Simultaneously Scale up the Sun Mesh
                // ==========================================
                // This swells the Sun mesh from 1.0x up to 40.0x size over the exact same 8 seconds
                DoubleAnimation sunSwellAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 40.0,
                    Duration = new Duration(TimeSpan.FromSeconds(8)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                // Fire the scale animations right alongside the camera movement
                SunScale.BeginAnimation(ScaleTransform3D.ScaleXProperty, sunSwellAnimation);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleYProperty, sunSwellAnimation);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleZProperty, sunSwellAnimation);
            }
        }
    }
}