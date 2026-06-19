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
        private Random rand = new Random();
        private Star homeStar = null;
        private StarCollection masterChart = null;
        private TranslateTransform3D SunPositionTransform = new TranslateTransform3D(0, 0, 0);
        private Point3D cameraTargetPosition;

        // NEW GLOBAL HUD HANDLES FOR EXTRA METADATA ROWS
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

            var stableGroup = new Transform3DGroup();
            stableGroup.Children.Add(SunScale);
            stableGroup.Children.Add(SunPositionTransform);
            SunGroup.Transform = stableGroup;

            string filePath = "starchart.csv";
            masterChart = new StarCollection(filePath);

            if (masterChart.Stars != null && masterChart.Stars.Count > 0)
            {
                homeStar = masterChart.Stars[0];
            }

            PopulateStarMap();

            // DYNAMIC RETRO HUD INJECTION
            var hudStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20), Width = 260 };
            var systemBorder = new Border { BorderBrush = Brushes.Cyan, BorderThickness = new Thickness(2), Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 10) };
            var systemInnerStack = new StackPanel();

            systemInnerStack.Children.Add(new TextBlock { Text = "CURRENT SYSTEM", FontSize = 11, Foreground = Brushes.DarkCyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold });

            string homeName = homeStar != null ? homeStar.Name.ToUpper() : "SOL";
            CurrentSystemText = new TextBlock { Text = homeName, FontSize = 22, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 8) };
            systemInnerStack.Children.Add(CurrentSystemText);

            // Subtle HUD layout divider line
            systemInnerStack.Children.Add(new Border { BorderBrush = Brushes.DarkCyan, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(0, 0, 0, 8) });

            // Determine initial string readouts from the home system entry
            string spectral = homeStar != null ? homeStar.SpectralType : "G2V";
            string coords = homeStar != null ? $"X:{homeStar.X:F2} Y:{homeStar.Y:F2} Z:{homeStar.Z:F2}" : "X:0.00 Y:0.00 Z:0.00";
            string details = homeStar != null ? $"CATALOG ID: {homeStar.Id:D4}" : "CLASSIFICATION: STAR";

            // Allocate the dynamic elements to our global class fields
            HudSpectralText = new TextBlock { Text = $"CLASS: {spectral}", FontSize = 12, Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 2, 0, 2) };
            HudCoordsText = new TextBlock { Text = coords, FontSize = 11, Foreground = Brushes.Yellow, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 2, 0, 2) };
            HudDetailText = new TextBlock { Text = details, FontSize = 11, Foreground = Brushes.LightGray, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 2, 0, 0) };

            // Inject them straight into the control stack
            systemInnerStack.Children.Add(HudSpectralText);
            systemInnerStack.Children.Add(HudCoordsText);
            systemInnerStack.Children.Add(HudDetailText);

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

            if (MainViewport.Parent is Grid rootGrid) { rootGrid.Children.Add(hudStack); }

            // Set dynamic camera properties
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
            // 1. Fetch the true spectral system color for our active home star
            Color systemStarColor = Colors.White; // Safe default fallback
            if (homeStar != null)
            {
                systemStarColor = StarModelFactory.GetColourFromSpectrum(homeStar.SpectralType);
            }

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var glowGradient = new RadialGradientBrush();

                // 2. Map the true system color stops directly onto our radial mask layers
                glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));       // Hot white fusion core
                glowGradient.GradientStops.Add(new GradientStop(systemStarColor, 0.45));   // Core spectral body coloration

                // Blend nicely out to a semi-transparent atmosphere aura
                glowGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, systemStarColor.R, systemStarColor.G, systemStarColor.B), 0.70));
                glowGradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0.95)); // Outer void envelope

                drawingContext.DrawRectangle(glowGradient, null, new Rect(0, 0, 512, 512));
            }

            var renderTargetBitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            var imageBrush = new ImageBrush(renderTargetBitmap);
            // =====================================================================

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
                    var starLabel = new TextVisual3D { Text = star.Name, Position = new Point3D(star.X + 0.15, star.Y + 0.15, star.Z), Height = 0.18, Foreground = Brushes.Cyan, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
                    TextContainer.Children.Add(starLabel);
                }
            }

            neighborList.Sort((s1, s2) => s1.Distance.CompareTo(s2.Distance));
            List<StarNeighborDisplay> top5Closest = new List<StarNeighborDisplay>();
            for (int i = 0; i < Math.Min(5, neighborList.Count); i++) { top5Closest.Add(neighborList[i]); }

            this.Resources["CachedNeighbors"] = top5Closest;

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

                // Shift our star group asset to the absolute target vector position
                SunPositionTransform.OffsetX = targetX;
                SunPositionTransform.OffsetY = targetY;
                SunPositionTransform.OffsetZ = targetZ;

                Vector3D lookDirection = new Vector3D(0, 0, -1);
                Vector3D upDirection = new Vector3D(0, 1, 0);

                // Compute starting and destination points safely
                Point3D startPosition = new Point3D(targetX, targetY, targetZ + 40.0);
                cameraTargetPosition = new Point3D(targetX, targetY, targetZ + 10.0);

                // Instantly snap the static orientation properties before flight
                helixCamera.LookDirection = lookDirection;
                helixCamera.UpDirection = upDirection;

                // Force terminate previous rendering clocks to reset the baseline
                SunScale.BeginAnimation(ScaleTransform3D.ScaleXProperty, null);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleYProperty, null);
                SunScale.BeginAnimation(ScaleTransform3D.ScaleZProperty, null);

                SunScale.ScaleX = 1.0;
                SunScale.ScaleY = 1.0;
                SunScale.ScaleZ = 1.0;

                // =====================================================================
                // FIX: NATIVE WPF CAM GLIDE ANIMATION (Replaces AnimateCamera extension)
                // =====================================================================
                Point3DAnimation cameraFlight = new Point3DAnimation
                {
                    From = startPosition,
                    To = cameraTargetPosition,
                    Duration = new Duration(TimeSpan.FromSeconds(8)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                helixCamera.BeginAnimation(PerspectiveCamera.PositionProperty, cameraFlight);
                // =====================================================================

                // Swell up the core glow texture uniformly
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

                // =====================================================================
                // NEW: REWRITE HUD DATA STRINGS ON TRANSIT BEGIN
                // =====================================================================
                if (HudSpectralText != null)
                    HudSpectralText.Text = $"CLASS: {homeStar.SpectralType}";

                if (HudCoordsText != null)
                    HudCoordsText.Text = $"X:{homeStar.X:F2} Y:{homeStar.Y:F2} Z:{homeStar.Z:F2}";

                if (HudDetailText != null)
                    HudDetailText.Text = $"CATALOG ID: {homeStar.Id:D4}";
                // =====================================================================

                if (NeighborsTextList != null)
                {
                    NeighborsTextList.ItemsSource = null;
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

                    // Once camera settles near destination target position vector, reveal HUD elements
                    if (distanceToTarget < 0.1)
                    {
                        isAnimationFinished = true;

                        if (this.Resources["CachedNeighbors"] is List<StarNeighborDisplay> cachedData)
                        {
                            NeighborsTextList.ItemsSource = cachedData;
                        }
                    }
                    else
                    {
                        DustField.Points = new Point3DCollection();
                        return;
                    }
                }

                // 2. RUN ANIMATED SPACE DUST LAYER
                double speed = 0.15; // Velocity at which dust particles rush past the cockpit view
                Point3DCollection updatedPoints = new Point3DCollection(ParticleCount);

                // Bounding parameters calculated straight from the destination anchor coordinates
                double maxBufferZ = targetZ + 40.0;
                double resetFarZ = targetZ;

                for (int i = 0; i < ParticleCount; i++)
                {
                    // CHANGE: ADD speed to make the dust drift forward along the camera's viewpoint line
                    double nextZ = dustParticles[i].Z + speed;

                    // Loop reset: Once a particle flies behind your cockpit window boundary, wrap it back to deep space
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