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
        private ItemsControl DistantTextList;
        private bool isAnimationFinished = false;
        private Point3D[] dustParticles;
        private const int ParticleCount = 800;
        private Random rand = new Random();
        private Star homeStar = null;
        private StarCollection masterChart = null;
        private TranslateTransform3D SunPositionTransform = new TranslateTransform3D(0, 0, 0);
        private Point3D cameraTargetPosition;

        // GLOBAL HUD HANDLES FOR EXTRA METADATA ROWS
        private TextBlock HudSpectralText;
        private TextBlock HudCoordsText;
        private TextBlock HudDetailText;

        public MainWindow()
        {
            InitializeComponent();

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

            // =====================================================================
            // BUILD ONE PERMANENT HIERARCHY TREE TO IMMUNIZE ANIMATIONS
            // =====================================================================
            var stableGroup = new Transform3DGroup();
            stableGroup.Children.Add(SunScale);             // Layer 1: Sizing animations
            stableGroup.Children.Add(SunPositionTransform);  // Layer 2: Spatial location shifts
            SunGroup.Transform = stableGroup;

            string filePath = "starchart.csv";
            masterChart = new StarCollection(filePath);

            if (masterChart.Stars != null && masterChart.Stars.Count > 0)
            {
                homeStar = masterChart.Stars[0];
            }

            PopulateStarMap();

            // =====================================================================
            // DYNAMIC RETRO HUD INJECTION
            // =====================================================================
            var hudStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(20),
                Width = 260
            };

            // A. CURRENT SYSTEM READOUT PROFILE BOX
            var systemBorder = new Border
            {
                BorderBrush = Brushes.Cyan,
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var systemInnerStack = new StackPanel();

            systemInnerStack.Children.Add(new TextBlock { Text = "CURRENT SYSTEM", FontSize = 11, Foreground = Brushes.DarkCyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold });

            string homeName = homeStar != null ? homeStar.Name.ToUpper() : "SOL";
            CurrentSystemText = new TextBlock { Text = homeName, FontSize = 22, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 8) };
            systemInnerStack.Children.Add(CurrentSystemText);

            // Subtle HUD alignment divider rule line
            systemInnerStack.Children.Add(new Border { BorderBrush = Brushes.DarkCyan, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(0, 0, 0, 8) });

            // Extract textual parameters for initialization readout rows
            string spectral = homeStar != null ? homeStar.SpectralType : "G2V";
            string coords = homeStar != null ? $"X:{homeStar.X:F2} Y:{homeStar.Y:F2} Z:{homeStar.Z:F2}" : "X:0.00 Y:0.00 Z:0.00";
            string details = homeStar != null ? $"CATALOG ID: {homeStar.Id:D4}" : "CLASSIFICATION: STAR";

            // Bind instance metrics layout to the global tracking field identifiers
            HudSpectralText = new TextBlock { Text = $"CLASS: {spectral}", FontSize = 12, Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 2, 0, 2) };
            HudCoordsText = new TextBlock { Text = coords, FontSize = 11, Foreground = Brushes.Yellow, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 2, 0, 2) };
            HudDetailText = new TextBlock { Text = details, FontSize = 11, Foreground = Brushes.LightGray, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 2, 0, 0) };

            systemInnerStack.Children.Add(HudSpectralText);
            systemInnerStack.Children.Add(HudCoordsText);
            systemInnerStack.Children.Add(HudDetailText);
            systemBorder.Child = systemInnerStack;
            hudStack.Children.Add(systemBorder);

            // B. PRIMARY SYSTEM NAVIGATION LIST LABELS (INTERACTIVE TOP 5)
            hudStack.Children.Add(new TextBlock { Text = "CLOSEST NEIGHBOURS:", FontSize = 12, Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 0, 6) });

            NeighborsTextList = new ItemsControl();
            var rowTemplate = new DataTemplate();

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BorderBrushProperty, Brushes.Cyan);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(8, 6, 8, 6));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 6));
            borderFactory.SetValue(Border.CursorProperty, Cursors.Hand);
            borderFactory.AddHandler(Border.MouseLeftButtonDownEvent, new MouseButtonEventHandler(NeighborRow_Click));

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

            // C. DEEP SCAN TRACKING PANELS (NON-INTERACTIVE BACKUP 5)
            hudStack.Children.Add(new TextBlock { Text = "DEEP RANGE SCAN:", FontSize = 11, Foreground = Brushes.DarkCyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(4, 6, 0, 6) });

            DistantTextList = new ItemsControl();
            var secondaryTemplate = new DataTemplate();

            var baseBorder = new FrameworkElementFactory(typeof(Border));
            baseBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0, 100, 100)));
            baseBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            baseBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)));
            baseBorder.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));
            baseBorder.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 4));

            var baseGrid = new FrameworkElementFactory(typeof(Grid));

            var subNameText = new FrameworkElementFactory(typeof(TextBlock));
            subNameText.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            subNameText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            subNameText.SetValue(TextBlock.ForegroundProperty, Brushes.Gray);
            subNameText.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            subNameText.SetValue(TextBlock.FontSizeProperty, 12.0);

            var subDistText = new FrameworkElementFactory(typeof(TextBlock));
            subDistText.SetBinding(TextBlock.TextProperty, new Binding("DistanceString"));
            subDistText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            subDistText.SetValue(TextBlock.ForegroundProperty, Brushes.DarkGoldenrod);
            subDistText.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            subDistText.SetValue(TextBlock.FontSizeProperty, 12.0);

            baseGrid.AppendChild(subNameText);
            baseGrid.AppendChild(subDistText);
            baseBorder.AppendChild(baseGrid);
            secondaryTemplate.VisualTree = baseBorder;
            DistantTextList.ItemTemplate = secondaryTemplate;
            hudStack.Children.Add(DistantTextList);

            // Inject compiled dashboard assembly overlay into parent grid viewport container layers
            if (MainViewport.Parent is Grid rootGrid)
            {
                rootGrid.Children.Add(hudStack);
            }

            // Initialize tracking mechanisms and trigger initial runtime loops
            AnimateToStar();
            InitializeDustField();

            CompositionTarget.Rendering += OnRenderFrame;
        }

        private void PopulateStarMap()
        {
            if (masterChart == null) return;

            SunGroup.Children.Clear();
            TextContainer.Children.Clear();

            List<Visual3D> toRemove = new List<Visual3D>();
            foreach (var child in MainViewport.Children)
            {
                if (child is PointsVisual3D && child != DustField) { toRemove.Add(child); }
            }
            foreach (var oldLayer in toRemove) MainViewport.Children.Remove(oldLayer);

            // =====================================================================
            // DYNAMIC RETRO 'ELITE' RADIAL GLOW TEXTURE GENERATION
            // =====================================================================
            Color systemStarColor = Colors.White;
            if (homeStar != null)
            {
                systemStarColor = StarModelFactory.GetColourFromSpectrum(homeStar.SpectralType);
            }

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var glowGradient = new RadialGradientBrush();

                glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));       // Hot white fusion core
                glowGradient.GradientStops.Add(new GradientStop(systemStarColor, 0.45));   // Core spectral body coloration

                glowGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, systemStarColor.R, systemStarColor.G, systemStarColor.B), 0.70));
                glowGradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0.95)); // Outer void envelope

                drawingContext.DrawRectangle(glowGradient, null, new Rect(0, 0, 512, 512));
            }

            var renderTargetBitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            var imageBrush = new ImageBrush(renderTargetBitmap);

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

            Dictionary<Color, Point3DCollection> colorGroups = new Dictionary<Color, Point3DCollection>();
            List<StarNeighborDisplay> neighborList = new List<StarNeighborDisplay>();

            foreach (Star star in masterChart.Stars)
            {
                if (homeStar != null && star.Id == homeStar.Id) continue;

                Color starColor = StarModelFactory.GetColourFromSpectrum(star.SpectralType);
                if (!colorGroups.ContainsKey(starColor)) { colorGroups[starColor] = new Point3DCollection(); }
                colorGroups[starColor].Add(new Point3D(star.X, star.Y, star.Z));

                double dx = star.X - homeStar.X;
                double dy = star.Y - homeStar.Y;
                double dz = star.Z - homeStar.Z;
                double distanceToHome = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                neighborList.Add(new StarNeighborDisplay { AssociatedStar = star, Name = star.Name, Distance = distanceToHome });
                if (distanceToHome < 15.0)
                {
                    var starLabel = new BillboardTextVisual3D
                    {
                        Text = star.Name,
                        Position = new Point3D(star.X + 0.15, star.Y + 0.15, star.Z), // Keep offset position
                        Height = 11,               // Changed from scene geometry units to uniform device-independent font pixels
                        Foreground = Brushes.Cyan,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Consolas")
                    };
                    TextContainer.Children.Add(starLabel);
                }
            }

            neighborList.Sort((s1, s2) => s1.Distance.CompareTo(s2.Distance));

            // =====================================================================
            // FIXED: EXTRACT ACTIVE NAVIGATION TARGETS AND BACKUP SCAN TARGETS
            // =====================================================================
            List<StarNeighborDisplay> clickableStars = new List<StarNeighborDisplay>();
            List<StarNeighborDisplay> backgroundStars = new List<StarNeighborDisplay>();

            for (int i = 0; i < neighborList.Count; i++)
            {
                if (i < 5)
                {
                    clickableStars.Add(neighborList[i]);
                }
                else if (i < 10)
                {
                    backgroundStars.Add(neighborList[i]);
                }
                else
                {
                    break;
                }
            }

            this.Resources["CachedNeighbors"] = clickableStars;
            this.Resources["CachedDeepRange"] = backgroundStars;
            // =====================================================================

            foreach (var kvp in colorGroups)
            {
                PointsVisual3D starLayer = new PointsVisual3D { Points = kvp.Value, Color = kvp.Key, Size = 4 };
                MainViewport.Children.Add(starLayer);
            }
        }

        private void AnimateToStar()
        {
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                helixCamera.NearPlaneDistance = 0.0001;
                helixCamera.FarPlaneDistance = 1000.0;

                double targetX = 0;
                double targetY = 0;
                double targetZ = 0;

                if (homeStar != null)
                {
                    targetX = homeStar.X;
                    targetY = homeStar.Y;
                    targetZ = homeStar.Z;
                }

                SunPositionTransform.OffsetX = targetX;
                SunPositionTransform.OffsetY = targetY;
                SunPositionTransform.OffsetZ = targetZ;

                Vector3D lookDirection = new Vector3D(0, 0, -1);
                Vector3D upDirection = new Vector3D(0, 1, 0);

                Point3D startPosition = new Point3D(targetX, targetY, targetZ + 40.0);
                cameraTargetPosition = new Point3D(targetX, targetY, targetZ + 10.0);

                helixCamera.LookDirection = lookDirection;
                helixCamera.UpDirection = upDirection;

                SunScale.BeginAnimation(ScaleTransform3D.ScaleXProperty, null);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleYProperty, null);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleZProperty, null);

                SunScale.ScaleX = 1.0;
                SunScale.ScaleY = 1.0;
                SunScale.ScaleZ = 1.0;

                Point3DAnimation cameraFlight = new Point3DAnimation
                {
                    From = startPosition,
                    To = cameraTargetPosition,
                    Duration = new Duration(TimeSpan.FromSeconds(8)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                helixCamera.BeginAnimation(PerspectiveCamera.PositionProperty, cameraFlight);

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

        private void NeighborRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.DataContext is StarNeighborDisplay selectedData)
            {
                homeStar = selectedData.AssociatedStar;

                if (CurrentSystemText != null)
                {
                    CurrentSystemText.Text = homeStar.Name.ToUpper();
                }

                if (HudSpectralText != null)
                    HudSpectralText.Text = $"CLASS: {homeStar.SpectralType}";

                if (HudCoordsText != null)
                    HudCoordsText.Text = $"X:{homeStar.X:F2} Y:{homeStar.Y:F2} Z:{homeStar.Z:F2}";

                if (HudDetailText != null)
                    HudDetailText.Text = $"CATALOG ID: {homeStar.Id:D4}";

                if (NeighborsTextList != null)
                {
                    NeighborsTextList.ItemsSource = null;
                }

                if (DistantTextList != null)
                {
                    DistantTextList.ItemsSource = null;
                }

                isAnimationFinished = false;

                PopulateStarMap();
                InitializeDustField();
                AnimateToStar();
            }
        }

        private void InitializeDustField()
        {
            dustParticles = new Point3D[ParticleCount];

            double centerX = homeStar != null ? homeStar.X : 0.0;
            double centerY = homeStar != null ? homeStar.Y : 0.0;
            double centerZ = homeStar != null ? homeStar.Z : 0.0;

            for (int i = 0; i < ParticleCount; i++)
            {
                double x = (rand.NextDouble() - 0.5) * 30.0 + centerX;
                double y = (rand.NextDouble() - 0.5) * 30.0 + centerY;
                double z = (rand.NextDouble() * 40.0) + centerZ;

                dustParticles[i] = new Point3D(x, y, z);
            }
        }

        private void OnRenderFrame(object sender, EventArgs e)
        {
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                double targetX = homeStar != null ? homeStar.X : 0.0;
                double targetY = homeStar != null ? homeStar.Y : 0.0;
                double targetZ = homeStar != null ? homeStar.Z : 0.0;

                // 1. ARRIVAL DETECTION
                if (!isAnimationFinished)
                {
                    double dx = helixCamera.Position.X - cameraTargetPosition.X;
                    double dy = helixCamera.Position.Y - cameraTargetPosition.Y;
                    double dz = helixCamera.Position.Z - cameraTargetPosition.Z;
                    double distanceToTarget = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (distanceToTarget < 0.1)
                    {
                        isAnimationFinished = true;

                        // FIXED: Bind both the active neighbors list and deep scan tracking elements
                        if (this.Resources["CachedNeighbors"] is List<StarNeighborDisplay> cachedData)
                        {
                            NeighborsTextList.ItemsSource = cachedData;
                        }

                        if (this.Resources["CachedDeepRange"] is List<StarNeighborDisplay> deepRangeData)
                        {
                            DistantTextList.ItemsSource = deepRangeData;
                        }
                    }
                    else
                    {
                        DustField.Points = new Point3DCollection();
                        return;
                    }
                }

                // 2. RUN ANIMATED SPACE DUST LAYER
                double speed = 0.15;
                Point3DCollection updatedPoints = new Point3DCollection(ParticleCount);

                double maxBufferZ = targetZ + 40.0;
                double resetFarZ = targetZ;

                for (int i = 0; i < ParticleCount; i++)
                {
                    double nextZ = dustParticles[i].Z + speed;

                    if (nextZ > maxBufferZ)
                    {
                        nextZ = resetFarZ;
                        double x = (rand.NextDouble() - 0.5) * 30.0 + targetX;
                        double y = (rand.NextDouble() - 0.5) * 30.0 + targetY;
                        dustParticles[i] = new Point3D(x, y, nextZ);
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
        public Star AssociatedStar { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public string DistanceString => $"{Distance:F2} ly";
    }
}