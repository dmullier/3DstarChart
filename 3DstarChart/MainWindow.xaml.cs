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
        private ModelVisual3D TextContainer;
        private PointsVisual3D DustField;
        private TextBlock CurrentSystemText;
        private ItemsControl NeighborsTextList;
        private bool isAnimationFinished = false;
        private Point3D[] dustParticles;
        private const int ParticleCount = 800;
        private double lastCameraZ = -1.0;
        private Random rand = new Random();
        private Star homeStar = null;

        public MainWindow()
        {
            InitializeComponent();

            // Setup input gestures smoothly
            MainViewport.RotateGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control);
            MainViewport.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            MainViewport.ZoomGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Alt);

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextContainer = new ModelVisual3D();
            DustField = new PointsVisual3D { Color = Color.FromArgb(176, 255, 255, 255), Size = 2 };

            MainViewport.Children.Add(TextContainer);
            MainViewport.Children.Add(DustField);

            // 1. RUN DATA CATALOG LOADER FIRST so homeStar and neighbor list data exist
            PopulateStarMap();

            // 2. DYNAMIC RETRO HUD INJECTION (Now safely consumes data parsed above)
            var hudStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(20),
                Width = 260
            };

            var systemBorder = new Border
            {
                BorderBrush = Brushes.Cyan,
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var systemInnerStack = new StackPanel();
            systemInnerStack.Children.Add(new TextBlock { Text = "CURRENT SYSTEM", FontSize = 11, Foreground = Brushes.DarkCyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold });

            // Read directly from the loaded home star or default to SOL
            string homeName = homeStar != null ? homeStar.Name.ToUpper() : "SOL";
            CurrentSystemText = new TextBlock { Text = homeName, FontSize = 20, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 0) };
            systemInnerStack.Children.Add(CurrentSystemText);
            systemBorder.Child = systemInnerStack;
            hudStack.Children.Add(systemBorder);

            hudStack.Children.Add(new TextBlock { Text = "CLOSEST NEIGHBORS:", FontSize = 12, Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 0, 6) });

            NeighborsTextList = new ItemsControl();

            var rowTemplate = new DataTemplate();
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BorderBrushProperty, Brushes.Cyan);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(8, 6, 8, 6));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 6));

            var gridFactory = new FrameworkElementFactory(typeof(Grid));

            var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            nameFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
            nameFactory.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            nameFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
            nameFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);

            var distFactory = new FrameworkElementFactory(typeof(TextBlock));
            distFactory.SetBinding(TextBlock.TextProperty, new Binding("DistanceString"));
            distFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            distFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Yellow);
            distFactory.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            distFactory.SetValue(TextBlock.FontSizeProperty, 13.0);

            gridFactory.AppendChild(nameFactory);
            gridFactory.AppendChild(distFactory);
            borderFactory.AppendChild(gridFactory);
            rowTemplate.VisualTree = borderFactory;

            NeighborsTextList.ItemTemplate = rowTemplate;
            hudStack.Children.Add(NeighborsTextList);

            if (MainViewport.Parent is Grid rootGrid)
            {
                rootGrid.Children.Add(hudStack);
            }

            // 3. Initialize Core Animation Mechanics
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                helixCamera.NearPlaneDistance = 0.00001;
                helixCamera.FarPlaneDistance = 500.0;
                lastCameraZ = helixCamera.Position.Z;
            }

            InitializeDustField();
            CompositionTarget.Rendering += OnRenderFrame;

            // Run the updated dynamic path animation
            AnimateCameraToSun();
        }

        private void PopulateStarMap()
        {
            // 1. Clear previous sun geometry content from the SunGroup container
            SunGroup.Children.Clear();

            // 2. Clear out the dynamic text label container to prevent ghosting labels
            TextContainer.Children.Clear();

            // 3. Clear out any background star layers added from previous runs
            List<Visual3D> toRemove = new List<Visual3D>();
            foreach (var child in MainViewport.Children)
            {
                if (child is PointsVisual3D && child != DustField)
                {
                    toRemove.Add(child);
                }
            }
            foreach (var oldLayer in toRemove) MainViewport.Children.Remove(oldLayer);

            // =================================================================
            // GENERATE THE RETRO 'ELITE' RADIAL GLOW TEXTURE
            // =================================================================
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var glowGradient = new RadialGradientBrush();
                glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.60));
                glowGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, 240, 240, 220), 0.75));
                glowGradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0.95));

                drawingContext.DrawRectangle(glowGradient, null, new Rect(0, 0, 512, 512));
            }

            var renderTargetBitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            var imageBrush = new ImageBrush(renderTargetBitmap);

            // =================================================================
            // BUILD THE STANDARD FLAT 3D RECTANGLE (QUAD) FOR THE TEXTURE
            // =================================================================
            MeshGeometry3D quadMesh = new MeshGeometry3D();
            quadMesh.Positions.Add(new Point3D(-0.01, -0.01, 0));
            quadMesh.Positions.Add(new Point3D(0.01, -0.01, 0));
            quadMesh.Positions.Add(new Point3D(0.01, 0.01, 0));
            quadMesh.Positions.Add(new Point3D(-0.01, 0.01, 0));

            quadMesh.TextureCoordinates.Add(new Point(0, 1));
            quadMesh.TextureCoordinates.Add(new Point(1, 1));
            quadMesh.TextureCoordinates.Add(new Point(1, 0));
            quadMesh.TextureCoordinates.Add(new Point(0, 0));

            quadMesh.TriangleIndices.Add(0); quadMesh.TriangleIndices.Add(1); quadMesh.TriangleIndices.Add(2);
            quadMesh.TriangleIndices.Add(0); quadMesh.TriangleIndices.Add(2); quadMesh.TriangleIndices.Add(3);

            var sunMaterial = new DiffuseMaterial(imageBrush);
            var sunModel = new GeometryModel3D(quadMesh, sunMaterial);
            sunModel.BackMaterial = sunMaterial;
            SunGroup.Children.Add(sunModel);

            // =================================================================
            // LOAD DATA CATALOG, GROUP BY COLORS & GENERATE NEIGHBOR CALCULATIONS
            // =================================================================
            string filePath = "starchart.csv";
            StarCollection chart = new StarCollection(filePath);
            Dictionary<Color, Point3DCollection> colorGroups = new Dictionary<Color, Point3DCollection>();
            List<StarNeighborDisplay> neighborList = new List<StarNeighborDisplay>();

            // Capture the home star base reference from first entry row
            if (chart.Stars != null && chart.Stars.Count > 0)
            {
                homeStar = chart.Stars[0];
            }

            foreach (Star star in chart.Stars)
            {
                if (homeStar != null && star.Id == homeStar.Id) continue;

                Color starColor = StarModelFactory.GetColourFromSpectrum(star.SpectralType);

                if (!colorGroups.ContainsKey(starColor))
                {
                    colorGroups[starColor] = new Point3DCollection();
                }
                colorGroups[starColor].Add(new Point3D(star.X, star.Y, star.Z));

                // MATH: Track relative distance calculation from home system coordinates
                double dx = star.X - (homeStar?.X ?? 0);
                double dy = star.Y - (homeStar?.Y ?? 0);
                double dz = star.Z - (homeStar?.Z ?? 0);
                double distanceToHome = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                neighborList.Add(new StarNeighborDisplay { Name = star.Name, Distance = distanceToHome });

                if (distanceToHome < 15.0)
                {
                    var starLabel = new TextVisual3D
                    {
                        Text = star.Name,
                        Position = new Point3D(star.X + 0.15, star.Y + 0.15, star.Z),
                        Height = 0.18,
                        Foreground = Brushes.Cyan,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    TextContainer.Children.Add(starLabel);
                }
            }

            // Sort list elements ascending by proximity value metrics
            neighborList.Sort((s1, s2) => s1.Distance.CompareTo(s2.Distance));

            List<StarNeighborDisplay> top5Closest = new List<StarNeighborDisplay>();
            for (int i = 0; i < Math.Min(5, neighborList.Count); i++)
            {
                top5Closest.Add(neighborList[i]);
            }

            // Stash sorted list inside window storage resource key bounds
            this.Resources["CachedNeighbors"] = top5Closest;

            foreach (var kvp in colorGroups)
            {
                PointsVisual3D starLayer = new PointsVisual3D
                {
                    Points = kvp.Value,
                    Color = kvp.Key,
                    Size = 4
                };
                MainViewport.Children.Add(starLayer);
            }
        }

        private void AnimateCameraToSun()
        {
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                helixCamera.NearPlaneDistance = 0.0001;
                helixCamera.FarPlaneDistance = 1000.0;

                double targetX = 0;
                double targetY = 0;
                double targetZ = 10.0;

                if (homeStar != null)
                {
                    targetX = homeStar.X;
                    targetY = homeStar.Y;
                    targetZ = homeStar.Z + 10.0; // Stop safely 10 units away from home system coordinates
                }

                Point3D endPosition = new Point3D(targetX, targetY, targetZ);
                Vector3D lookDirection = new Vector3D(0, 0, -1);
                Vector3D upDirection = new Vector3D(0, 1, 0);

                MainViewport.Camera.LookAt(endPosition, lookDirection, upDirection, 8000);

                DoubleAnimation sunSwellAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 40.0,
                    Duration = new Duration(TimeSpan.FromSeconds(8)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                SunScale.BeginAnimation(ScaleTransform3D.ScaleXProperty, sunSwellAnimation);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleYProperty, sunSwellAnimation);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleZProperty, sunSwellAnimation);
            }
        }

        private void InitializeDustField()
        {
            dustParticles = new Point3D[ParticleCount];
            for (int i = 0; i < ParticleCount; i++)
            {
                double x = (rand.NextDouble() - 0.5) * 30.0;
                double y = (rand.NextDouble() - 0.5) * 30.0;
                double z = rand.NextDouble() * 40.0;

                dustParticles[i] = new Point3D(x, y, z);
            }
        }

        private void OnRenderFrame(object sender, EventArgs e)
        {
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                if (!isAnimationFinished)
                {
                    double currentCameraZ = helixCamera.Position.Z;

                    if (Math.Abs(currentCameraZ - lastCameraZ) < 0.00001)
                    {
                        isAnimationFinished = true;

                        // Populate neighbors template source smoothly when animation halts
                        if (this.Resources["CachedNeighbors"] is List<StarNeighborDisplay> cachedData)
                        {
                            NeighborsTextList.ItemsSource = cachedData;
                        }
                    }
                    else
                    {
                        lastCameraZ = currentCameraZ;
                        DustField.Points = new Point3DCollection();
                        return;
                    }
                }

                double speed = 0.15;
                Point3DCollection updatedPoints = new Point3DCollection(ParticleCount);

                for (int i = 0; i < ParticleCount; i++)
                {
                    double nextZ = dustParticles[i].Z + speed;

                    if (nextZ > 42.0)
                    {
                        nextZ = 0.0;
                        dustParticles[i] = new Point3D((rand.NextDouble() - 0.5) * 30.0, (rand.NextDouble() - 0.5) * 30.0, nextZ);
                    }
                    else
                    {
                        dustParticles[i] = new Point3D(dustParticles[i].X, dustParticles[i].Y, nextZ);
                    }

                    updatedPoints.Add(dustParticles[i]);
                }

                DustField.Points = updatedPoints;
            }
        }
    }

    public class StarNeighborDisplay
    {
        public string Name { get; set; }
        public double Distance { get; set; }
        public string DistanceString => $"{Distance:F2} ly";
    }
}