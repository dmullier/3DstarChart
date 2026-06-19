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
            // 1. Clear previous sun geometry content from the SunGroup container
            SunGroup.Children.Clear();

            // 2. Clear out any background star layers added from previous runs
            List<Visual3D> toRemove = new List<Visual3D>();
            foreach (var child in MainViewport.Children)
            {
                if (child is PointsVisual3D) toRemove.Add(child);
            }
            foreach (var oldLayer in toRemove) MainViewport.Children.Remove(oldLayer);

            // =================================================================
            // GENERATE THE RETRO 'ELITE' RADIAL GLOW TEXTURE
            // =================================================================
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Define a sharp radial gradient to match the classic solid core and dithered edge
                var glowGradient = new RadialGradientBrush();
                glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));       // Hot white core
                glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.60));      // Edge of solid mass
                glowGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, 240, 240, 220), 0.75)); // Diffuse glow
                glowGradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0.95)); // Outer void boundary

                // Render the brush into a flat 512x512 canvas square
                drawingContext.DrawRectangle(glowGradient, null, new Rect(0, 0, 512, 512));
            }

            var renderTargetBitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            var imageBrush = new ImageBrush(renderTargetBitmap);

            // =================================================================
            // BUILD THE STANDARD FLAT 3D RECTANGLE (QUAD) FOR THE TEXTURE
            // =================================================================
            MeshGeometry3D quadMesh = new MeshGeometry3D();

            // Add the 4 corner coordinates of our sun disk face (Base size: 0.2 units wide)
            quadMesh.Positions.Add(new Point3D(-0.1, -0.1, 0)); // Bottom Left
            quadMesh.Positions.Add(new Point3D(0.1, -0.1, 0));  // Bottom Right
            quadMesh.Positions.Add(new Point3D(0.1, 0.1, 0));   // Top Right
            quadMesh.Positions.Add(new Point3D(-0.1, 0.1, 0));  // Top Left

            // Map the 2D texture coordinates onto those 3D corners smoothly
            quadMesh.TextureCoordinates.Add(new Point(0, 1));
            quadMesh.TextureCoordinates.Add(new Point(1, 1));
            quadMesh.TextureCoordinates.Add(new Point(1, 0));
            quadMesh.TextureCoordinates.Add(new Point(0, 0));

            // Define the triangle layout sequence (two triangles make up the square)
            quadMesh.TriangleIndices.Add(0); quadMesh.TriangleIndices.Add(1); quadMesh.TriangleIndices.Add(2);
            quadMesh.TriangleIndices.Add(0); quadMesh.TriangleIndices.Add(2); quadMesh.TriangleIndices.Add(3);

            // Wrap the texture around a native WPF DiffuseMaterial container
            var sunMaterial = new DiffuseMaterial(imageBrush);
            var sunModel = new GeometryModel3D(quadMesh, sunMaterial);

            // Display on both sides so it doesn't vanish if your view angles rotate or shift
            sunModel.BackMaterial = sunMaterial;

            // Inject our new custom diffuse Sun model right into your existing SunGroup
            SunGroup.Children.Add(sunModel);

            // =================================================================
            // LOAD DATA CATALOG & GROUP BY COLORS
            // =================================================================
            string filePath = "starchart.csv";
            StarCollection chart = new StarCollection(filePath);
            Dictionary<Color, Point3DCollection> colorGroups = new Dictionary<Color, Point3DCollection>();

            foreach (Star star in chart.Stars)
            {
                if (star.Id == 0) continue; // Skip Sun duplication

                Color starColor = StarModelFactory.GetColourFromSpectrum(star.SpectralType);

                if (!colorGroups.ContainsKey(starColor))
                {
                    colorGroups[starColor] = new Point3DCollection();
                }

                colorGroups[starColor].Add(new Point3D(star.X, star.Y, star.Z));
            }

            // Inject a single constant-pixel layer for each active color bucket
            foreach (var kvp in colorGroups)
            {
                PointsVisual3D starLayer = new PointsVisual3D
                {
                    Points = kvp.Value,
                    Color = kvp.Key,
                    Size = 4 // Keeps background stars locked to a constant screen-pixel scale
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
                Point3D endPosition = new Point3D(0, 0, 10.0); // Stop safely at 3 parsecs out
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