using _3DstarMap;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
using Quaternion = System.Windows.Media.Media3D.Quaternion;
namespace _3DstarChart
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // CONFIGURATION VARIABLES & CONFIGURABLE CONSTANTS
        // =====================================================================
        private const int ParticleCount = 1800;                 // Total density of the interstellar warp field
        private const double WarpSpeed = 0.15;                  // Frame translation increment for dust streaming
        private const double WarpStreakLength = 0.3;            // Z-axis line length of the vector motion streaks
        private const double SpaceDustSpreadRadius = 30.0;     // Horizontal/Vertical bounds of coordinate particle generation
        private const double SpaceDustAheadBuffer = 40.0;      // Maximum ahead distance threshold for line recycling

        private const double BaseQuadDimension = 0.01;         // Micro-dimensions of unscaled focal plane textures
        private const double BaseCameraApproachDistance = 10.0;// Stationary camera offset focal depth from targeted system
        private const double MaxSwellAnimationScale = 40.0;    // Target destination magnification for primary stars
        private const double NearLabelVisibilityLimit = 15.0;  // Direct distance threshold to draw HUD billboard string labels
        private const double FlightAnimationSeconds = 8.0;     // System-to-system dynamic transition duration baseline

        // =====================================================================
        // SCENE GRAPH & CORE ENGINE FIELDS
        // =====================================================================
        private ModelVisual3D TextContainer;
        private LinesVisual3D DustField;
        private TextBlock CurrentSystemText;
        private ItemsControl NeighborsTextList;
        private ItemsControl DistantTextList;

        private bool isAnimationFinished = false;
        private Point3D[] dustParticles;
        private Random rand = new Random();
        private Star homeStar;
        private int StartStarID = 19799; // Catalogue ID of starting star (Keid)
        private StarCollection masterChart = null;

        private Point3D cameraTargetPosition;

        private TextBlock HudSpectralText;
        private TextBlock HudCoordsText;
        private TextBlock HudDetailText;
        private TextBlock HudConstellationText; // Dynamic overlay hook
        private TextBlock HudViewingVectorText;

        // =====================================================================
        // INITIALIZATION & LIFECYCLE
        // =====================================================================
        public MainWindow()
        {
            InitializeComponent();

            MainViewport.RotateGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control);
            MainViewport.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            MainViewport.ZoomGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Alt);

            this.KeyDown += MainWindow_KeyDown;
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextContainer = new ModelVisual3D();
            DustField = new LinesVisual3D { Color = Color.FromArgb(176, 255, 255, 255), Thickness = 1.5 };

            MainViewport.Children.Add(TextContainer);
            MainViewport.Children.Add(DustField);

            string filePath = "bigstarchart.csv";
            masterChart = new StarCollection(filePath);

            if (masterChart.Stars != null && masterChart.Stars.Count > 0)
            {
                homeStar = masterChart.Stars.FirstOrDefault(s => s.Id == StartStarID);
            }

            PopulateStarMap();

            // DYNAMIC RETRO HUD INJECTION
            var hudStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20), Width = 280 };
            var systemBorder = new Border { BorderBrush = Brushes.Cyan, BorderThickness = new Thickness(2), Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 10) };
            var systemInnerStack = new StackPanel();

            systemInnerStack.Children.Add(new TextBlock { Text = "CURRENT SYSTEM", FontSize = 11, Foreground = Brushes.DarkCyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold });
            string homeName = homeStar != null ? homeStar.Name.ToUpper() : "UNKNOWN SYSTEM / NO DATA";
            CurrentSystemText = new TextBlock { Text = homeName, FontSize = 22, Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 6) };
            systemInnerStack.Children.Add(CurrentSystemText);
            systemInnerStack.Children.Add(new Border { BorderBrush = Brushes.DarkCyan, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(0, 0, 0, 8) });

            string spectral = homeStar != null ? homeStar.SpectralType : "G2V";
            string coords = homeStar != null ? $"X:{homeStar.X:F2} Y:{homeStar.Y:F2} Z:{homeStar.Z:F2}" : "X:0.00 Y:0.00 Z:0.00";
            string details = homeStar != null ? $"CATALOG ID: {homeStar.Id:D4}" : "CLASSIFICATION: STAR";
            string constellation = homeStar != null && !string.IsNullOrEmpty(homeStar.Constellation) ? $"CONSTELLATION: {homeStar.Constellation.ToUpper()}" : "SECTOR: UNMAPPED SPEC";

            HudSpectralText = new TextBlock { Text = $"CLASS: {spectral}", FontSize = 12, Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 1, 0, 1) };
            HudConstellationText = new TextBlock { Text = constellation, FontSize = 11, Foreground = Brushes.MediumSpringGreen, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 1, 0, 1) };
            HudCoordsText = new TextBlock { Text = coords, FontSize = 11, Foreground = Brushes.Yellow, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 1, 0, 1) };
            HudDetailText = new TextBlock { Text = details, FontSize = 11, Foreground = Brushes.LightGray, FontFamily = new FontFamily("Consolas"), Margin = new Thickness(0, 1, 0, 0) };
            HudViewingVectorText = new TextBlock { Text = "VIEW BRG: 090° | PTH: +00°", FontSize = 11, Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 1) };

            systemInnerStack.Children.Add(HudSpectralText);
            systemInnerStack.Children.Add(HudConstellationText);
            systemInnerStack.Children.Add(HudCoordsText);
            systemInnerStack.Children.Add(HudDetailText);
            systemInnerStack.Children.Add(new Border { BorderBrush = Brushes.DarkCyan, BorderThickness = new Thickness(0, 0, 0, 1), Margin = new Thickness(0, 4, 0, 4) });
            systemInnerStack.Children.Add(HudViewingVectorText);
            systemBorder.Child = systemInnerStack;
            hudStack.Children.Add(systemBorder);

            hudStack.Children.Add(new TextBlock { Text = "CLOSEST NEIGHBOURS:", FontSize = 12, Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 0, 6) });

            // =====================================================================
            // REFACTORED: CLOSEST NEIGHBOURS LIST TEMPLATE (UNIFIED BRIGHT COLOURS)
            // =====================================================================
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

            var leftStackFactory = new FrameworkElementFactory(typeof(StackPanel));
            leftStackFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Left);

            var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Cyan); // Force bright Cyan
            nameFactory.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            nameFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
            nameFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);

            var vectorFactory = new FrameworkElementFactory(typeof(TextBlock));
            vectorFactory.SetBinding(TextBlock.TextProperty, new Binding("VectorTelemetryString"));
            vectorFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Cyan); // Force bright Cyan
            vectorFactory.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            vectorFactory.SetValue(TextBlock.FontSizeProperty, 10.0);

            leftStackFactory.AppendChild(nameFactory);
            leftStackFactory.AppendChild(vectorFactory);

            var distFactory = new FrameworkElementFactory(typeof(TextBlock));
            distFactory.SetBinding(TextBlock.TextProperty, new Binding("DistanceString"));
            distFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            distFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            distFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Cyan); // Force bright Cyan
            distFactory.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            distFactory.SetValue(TextBlock.FontSizeProperty, 13.0);

            gridFactory.AppendChild(leftStackFactory);
            gridFactory.AppendChild(distFactory);
            borderFactory.AppendChild(gridFactory);
            rowTemplate.VisualTree = borderFactory;
            NeighborsTextList.ItemTemplate = rowTemplate;
            hudStack.Children.Add(NeighborsTextList);

            hudStack.Children.Add(new TextBlock { Text = "DEEP RANGE SCAN:", FontSize = 11, Foreground = Brushes.DarkCyan, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Margin = new Thickness(4, 6, 0, 6) });

            // =====================================================================
            // REFACTORED: DEEP RANGE SCAN LIST TEMPLATE (UNIFIED BRIGHT COLOURS)
            // =====================================================================
            DistantTextList = new ItemsControl();
            var secondaryTemplate = new DataTemplate();
            var baseBorder = new FrameworkElementFactory(typeof(Border));
            baseBorder.SetValue(Border.BorderBrushProperty, Brushes.Cyan); // Fixed dark teal border to bright Cyan
            baseBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            baseBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)));
            baseBorder.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));
            baseBorder.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 4));

            var baseGrid = new FrameworkElementFactory(typeof(Grid));

            var rightStackFactory = new FrameworkElementFactory(typeof(StackPanel));
            rightStackFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Left);

            var subNameText = new FrameworkElementFactory(typeof(TextBlock));
            subNameText.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            subNameText.SetValue(TextBlock.ForegroundProperty, Brushes.Cyan); // Fixed dull gray to bright Cyan
            subNameText.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            subNameText.SetValue(TextBlock.FontSizeProperty, 12.0);

            var subVectorText = new FrameworkElementFactory(typeof(TextBlock));
            subVectorText.SetBinding(TextBlock.TextProperty, new Binding("VectorTelemetryString"));
            subVectorText.SetValue(TextBlock.ForegroundProperty, Brushes.Cyan); // Fixed faint dark teal to bright Cyan
            subVectorText.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            subVectorText.SetValue(TextBlock.FontSizeProperty, 9.0);

            rightStackFactory.AppendChild(subNameText);
            rightStackFactory.AppendChild(subVectorText);

            var subDistText = new FrameworkElementFactory(typeof(TextBlock));
            subDistText.SetBinding(TextBlock.TextProperty, new Binding("DistanceString"));
            subDistText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            subDistText.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            subDistText.SetValue(TextBlock.ForegroundProperty, Brushes.Cyan); // Fixed dark goldenrod to bright Cyan
            subDistText.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Consolas"));
            subDistText.SetValue(TextBlock.FontSizeProperty, 12.0);

            baseGrid.AppendChild(rightStackFactory);
            baseGrid.AppendChild(subDistText);
            baseBorder.AppendChild(baseGrid);
            secondaryTemplate.VisualTree = baseBorder;
            DistantTextList.ItemTemplate = secondaryTemplate;
            hudStack.Children.Add(DistantTextList);

            if (MainViewport.Parent is Grid rootGrid)
            {
                rootGrid.Children.Add(hudStack);
            }
            AnimateToStar();
            InitializeDustField();

            CompositionTarget.Rendering += OnRenderFrame;
        }

        private void PopulateStarMap()
        {
            if (masterChart == null) return;

            LocalSystemGroup.Children.Clear();
            TextContainer.Children.Clear();

            List<Visual3D> toRemove = new List<Visual3D>();
            foreach (var child in MainViewport.Children)
            {
                if (child is PointsVisual3D && child != DustField) { toRemove.Add(child); }
            }
            foreach (var oldLayer in toRemove) MainViewport.Children.Remove(oldLayer);

            Color systemStarColor = Colors.White;
            if (homeStar != null)
            {
                systemStarColor = StarModelFactory.GetColourFromSpectrum(homeStar.SpectralType);
            }

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                var glowGradient = new RadialGradientBrush();
                glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                glowGradient.GradientStops.Add(new GradientStop(systemStarColor, 0.45));
                glowGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, systemStarColor.R, systemStarColor.G, systemStarColor.B), 0.70));
                glowGradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0.95));
                drawingContext.DrawRectangle(glowGradient, null, new Rect(0, 0, 512, 512));
            }

            var renderTargetBitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            var imageBrush = new ImageBrush(renderTargetBitmap);

            System.Windows.Media.Media3D.MeshGeometry3D quadMesh = new System.Windows.Media.Media3D.MeshGeometry3D();
            quadMesh.Positions.Add(new Point3D(-BaseQuadDimension, -BaseQuadDimension, 0));
            quadMesh.Positions.Add(new Point3D(BaseQuadDimension, -BaseQuadDimension, 0));
            quadMesh.Positions.Add(new Point3D(BaseQuadDimension, BaseQuadDimension, 0));
            quadMesh.Positions.Add(new Point3D(-BaseQuadDimension, BaseQuadDimension, 0));

            quadMesh.TextureCoordinates.Add(new Point(0, 1));
            quadMesh.TextureCoordinates.Add(new Point(1, 1));
            quadMesh.TextureCoordinates.Add(new Point(1, 0));
            quadMesh.TextureCoordinates.Add(new Point(0, 0));

            quadMesh.TriangleIndices.Add(0); quadMesh.TriangleIndices.Add(1); quadMesh.TriangleIndices.Add(2);
            quadMesh.TriangleIndices.Add(0); quadMesh.TriangleIndices.Add(2); quadMesh.TriangleIndices.Add(3);

            var sunMaterial = new DiffuseMaterial(imageBrush);
            var sunModel = new GeometryModel3D(quadMesh, sunMaterial);
            sunModel.BackMaterial = sunMaterial;

            LocalSystemGroup.Children.Add(sunModel);

            if (homeStar != null && homeStar.HasCompanions && homeStar.CompanionStars != null)
            {
                double distanceScaleFactor = 3.0;
                double minimumVisualOffset = BaseQuadDimension * 2.5;
                double maximumVisualOffset = BaseCameraApproachDistance * 0.4;

                int companionIndex = 0;

                foreach (int companionId in homeStar.CompanionStars)
                {
                    Star companionStar = masterChart.Stars.FirstOrDefault(s => s.Id == companionId);
                    if (companionStar == null) continue;

                    double dx = companionStar.X - homeStar.X;
                    double dy = companionStar.Y - homeStar.Y;
                    double dz = companionStar.Z - homeStar.Z;
                    double actualDistance = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    double currentOffset = actualDistance * distanceScaleFactor;
                    if (currentOffset < minimumVisualOffset) currentOffset = minimumVisualOffset;
                    if (currentOffset > maximumVisualOffset) currentOffset = maximumVisualOffset;

                    if (actualDistance < 0.001) currentOffset = minimumVisualOffset * (companionIndex + 1);

                    double primaryLum = homeStar.Luminosity > 0 ? homeStar.Luminosity : 1.0;
                    double companionLum = companionStar.Luminosity > 0 ? companionStar.Luminosity : 0.1;
                    double relativeRadiusScale = Math.Sqrt(companionLum / primaryLum);

                    if (relativeRadiusScale < 0.35) relativeRadiusScale = 0.35;
                    if (relativeRadiusScale > 1.5) relativeRadiusScale = 1.5;

                    double size = BaseQuadDimension * relativeRadiusScale;

                    double localZOffset = (companionIndex % 2 == 0) ? (BaseQuadDimension * 0.5) : -(BaseQuadDimension * 0.5);
                    localZOffset += (companionIndex * 0.1 * BaseQuadDimension);

                    Color companionColor = StarModelFactory.GetColourFromSpectrum(companionStar.SpectralType);
                    var compDrawingVisual = new DrawingVisual();
                    using (var drawingContext = compDrawingVisual.RenderOpen())
                    {
                        var glowGradient = new RadialGradientBrush();
                        glowGradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                        glowGradient.GradientStops.Add(new GradientStop(companionColor, 0.45));
                        glowGradient.GradientStops.Add(new GradientStop(Color.FromArgb(160, companionColor.R, companionColor.G, companionColor.B), 0.70));
                        glowGradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0.95));
                        drawingContext.DrawRectangle(glowGradient, null, new Rect(0, 0, 512, 512));
                    }

                    var compRenderTarget = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
                    compRenderTarget.Render(compDrawingVisual);
                    var compImageBrush = new ImageBrush(compRenderTarget);

                    var compQuadMesh = new System.Windows.Media.Media3D.MeshGeometry3D();
                    compQuadMesh.Positions.Add(new Point3D(currentOffset - size, -size, localZOffset));
                    compQuadMesh.Positions.Add(new Point3D(currentOffset + size, -size, localZOffset));
                    compQuadMesh.Positions.Add(new Point3D(currentOffset + size, size, localZOffset));
                    compQuadMesh.Positions.Add(new Point3D(currentOffset - size, size, localZOffset));

                    compQuadMesh.TextureCoordinates.Add(new Point(0, 1));
                    compQuadMesh.TextureCoordinates.Add(new Point(1, 1));
                    compQuadMesh.TextureCoordinates.Add(new Point(1, 0));
                    compQuadMesh.TextureCoordinates.Add(new Point(0, 0));

                    compQuadMesh.TriangleIndices.Add(0); compQuadMesh.TriangleIndices.Add(1); compQuadMesh.TriangleIndices.Add(2);
                    compQuadMesh.TriangleIndices.Add(0); compQuadMesh.TriangleIndices.Add(2); compQuadMesh.TriangleIndices.Add(3);

                    var compMaterial = new DiffuseMaterial(compImageBrush);
                    var companionModel = new GeometryModel3D(compQuadMesh, compMaterial);
                    companionModel.BackMaterial = compMaterial;

                    LocalSystemGroup.Children.Add(companionModel);

                    double scaledOffset = currentOffset * MaxSwellAnimationScale;
                    double scaledYOffset = (size * 1.2) * MaxSwellAnimationScale;
                    double scaledZOffset = localZOffset * MaxSwellAnimationScale;

                    var compLabel = new BillboardTextVisual3D
                    {
                        Text = companionStar.Name.ToUpper(),
                        Position = new Point3D(homeStar.X + scaledOffset, homeStar.Y + scaledYOffset, homeStar.Z + scaledZOffset),
                        Height = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(180, 255, 255)),
                        FontFamily = new FontFamily("Consolas")
                    };
                    TextContainer.Children.Add(compLabel);

                    companionIndex++;
                }
            }

            Dictionary<Color, Point3DCollection> colorGroups = new Dictionary<Color, Point3DCollection>();
            List<StarNeighborDisplay> neighborList = new List<StarNeighborDisplay>();

            foreach (Star star in masterChart.Stars)
            {
                if (homeStar != null && star.Id == homeStar.Id) continue;

                double dx = star.X - homeStar.X;
                double dy = star.Y - homeStar.Y;
                double dz = star.Z - homeStar.Z;
                double distanceToHome = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                // Initialize tracking container with system origins passed straight down for bearing calculations
                neighborList.Add(new StarNeighborDisplay(star, homeStar, distanceToHome));
            }

            neighborList.Sort((s1, s2) => s1.Distance.CompareTo(s2.Distance));

            List<StarNeighborDisplay> clickableStars = new List<StarNeighborDisplay>();
            List<StarNeighborDisplay> backgroundStars = new List<StarNeighborDisplay>();

            for (int i = 0; i < neighborList.Count; i++)
            {
                if (i < 5) clickableStars.Add(neighborList[i]);
                else if (i < 10) backgroundStars.Add(neighborList[i]);
                else break;
            }

            this.Resources["CachedNeighbors"] = clickableStars;
            this.Resources["CachedDeepRange"] = backgroundStars;

            foreach (var record in neighborList)
            {
                Star star = record.AssociatedStar;
                double distanceToHome = record.Distance;

                bool isPinnedOnHud = clickableStars.Any(s => s.AssociatedStar.Id == star.Id) ||
                                     backgroundStars.Any(s => s.AssociatedStar.Id == star.Id);

                if (star.Name != null && (star.Name.StartsWith("Gliese") || star.Name.StartsWith("GJ")))
                {
                    if (star.Id == star.PrimaryComponent)
                    {
                        if (!isPinnedOnHud) continue;
                    }
                }

                Color starColor = StarModelFactory.GetColourFromSpectrum(star.SpectralType);
                if (!colorGroups.ContainsKey(starColor)) { colorGroups[starColor] = new Point3DCollection(); }
                colorGroups[starColor].Add(new Point3D(star.X, star.Y, star.Z));

                if (distanceToHome < NearLabelVisibilityLimit)
                {
                    var starLabel = new BillboardTextVisual3D
                    {
                        Text = star.Name,
                        Position = new Point3D(star.X + 0.15, star.Y + 0.15, star.Z),
                        Height = 11,
                        Foreground = Brushes.Cyan,
                        FontWeight = FontWeights.Bold,
                        FontFamily = new FontFamily("Consolas")
                    };
                    TextContainer.Children.Add(starLabel);
                }
            }

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

                double targetX = 0; double targetY = 0; double targetZ = 0;
                if (homeStar != null)
                {
                    targetX = homeStar.X; targetY = homeStar.Y; targetZ = homeStar.Z;
                }

                SystemPositionTransform.OffsetX = targetX;
                SystemPositionTransform.OffsetY = targetY;
                SystemPositionTransform.OffsetZ = targetZ;

                Vector3D lookDirection = new Vector3D(0, 0, -1);
                Vector3D upDirection = new Vector3D(0, 1, 0);

                Point3D startPosition = new Point3D(targetX, targetY, targetZ + SpaceDustAheadBuffer);
                cameraTargetPosition = new Point3D(targetX, targetY, targetZ + BaseCameraApproachDistance);

                helixCamera.LookDirection = lookDirection;
                helixCamera.UpDirection = upDirection;

                SystemScaleTransform.BeginAnimation(ScaleTransform3D.ScaleXProperty, null);
                SystemScaleTransform.BeginAnimation(ScaleTransform3D.ScaleYProperty, null);
                SystemScaleTransform.BeginAnimation(ScaleTransform3D.ScaleZProperty, null);

                SystemScaleTransform.ScaleX = 1.0; SystemScaleTransform.ScaleY = 1.0; SystemScaleTransform.ScaleZ = 1.0;

                SystemQuaternionRotation.Quaternion = new Quaternion(0, 0, 0, 1);

                Point3DAnimation cameraFlight = new Point3DAnimation
                {
                    From = startPosition,
                    To = cameraTargetPosition,
                    Duration = new Duration(TimeSpan.FromSeconds(FlightAnimationSeconds)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                cameraFlight.Completed += (s, args) =>
                {
                    helixCamera.BeginAnimation(PerspectiveCamera.PositionProperty, null);
                    helixCamera.Position = cameraTargetPosition;
                };

                helixCamera.BeginAnimation(PerspectiveCamera.PositionProperty, cameraFlight);

                DoubleAnimation sunSwellAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = MaxSwellAnimationScale,
                    Duration = new Duration(TimeSpan.FromSeconds(FlightAnimationSeconds)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                SystemScaleTransform.BeginAnimation(ScaleTransform3D.ScaleXProperty, sunSwellAnimation);
                SystemScaleTransform.BeginAnimation(ScaleTransform3D.ScaleYProperty, sunSwellAnimation);
                SystemScaleTransform.BeginAnimation(ScaleTransform3D.ScaleZProperty, sunSwellAnimation);
            }
        }

        private void NeighborRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.DataContext is StarNeighborDisplay selectedData)
            {
                homeStar = selectedData.AssociatedStar;

                if (CurrentSystemText != null) CurrentSystemText.Text = homeStar.Name.ToUpper();
                if (HudSpectralText != null) HudSpectralText.Text = $"CLASS: {homeStar.SpectralType}";
                if (HudCoordsText != null) HudCoordsText.Text = $"X:{homeStar.X:F2} Y:{homeStar.Y:F2} Z:{homeStar.Z:F2}";
                if (HudDetailText != null) HudDetailText.Text = $"CATALOG ID: {homeStar.Id:D4}";

                string constellation = homeStar != null && !string.IsNullOrEmpty(homeStar.Constellation) ? $"CONSTELLATION: {homeStar.Constellation.ToUpper()}" : "SECTOR: UNMAPPED SPEC";
                if (HudConstellationText != null) HudConstellationText.Text = constellation;

                if (NeighborsTextList != null) NeighborsTextList.ItemsSource = null;
                if (DistantTextList != null) DistantTextList.ItemsSource = null;

                isAnimationFinished = false;
                // Reset live screen vector readouts back to baseline approach metrics
                if (HudViewingVectorText != null) HudViewingVectorText.Text = "VIEW BRG: 090° | PTH: +00°";
                PopulateStarMap();
                InitializeDustField();
                AnimateToStar();
            }
        }

        /// <summary>
        /// Intercepts keyboard inputs to orbit endlessly around the target system.
        /// Rotates the camera matrix seamlessly to eliminate polar jerking artifacts.
        /// </summary>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                double angleStep = 0.03; // Velocity step size for looking around
                double zoomMultiplier = 0.90;

                // 1. Establish the current center configuration matrix
                double centerX = homeStar != null ? homeStar.X : 0.0;
                double centerY = homeStar != null ? homeStar.Y : 0.0;
                double centerZ = homeStar != null ? homeStar.Z : 0.0;

                // 2. EXTRACT LIVE BASIS VECTORS FROM THE CAMERA'S LENS FRAME
                Vector3D lookDir = helixCamera.LookDirection;
                lookDir.Normalize();

                Vector3D localUp = helixCamera.UpDirection;
                localUp.Normalize();

                // Compute the horizontal side-to-side vector relative to the camera lens
                Vector3D cameraRight = Vector3D.CrossProduct(lookDir, localUp);
                cameraRight.Normalize();

                Transform3DGroup lookRotation = new Transform3DGroup();

                switch (e.Key)
                {
                    // Look Left / Right (Rotate the viewing vector around the global Y axis)
                    case Key.Left:
                        lookRotation.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), angleStep * (180.0 / Math.PI))));
                        break;
                    case Key.Right:
                        lookRotation.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -angleStep * (180.0 / Math.PI))));
                        break;

                    // Look Up / Down (Rotate the viewing vector around the camera's own right-to-left axis)
                    case Key.Up:
                        lookRotation.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(cameraRight, angleStep * (180.0 / Math.PI))));
                        break;
                    case Key.Down:
                        lookRotation.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(cameraRight, -angleStep * (180.0 / Math.PI))));
                        break;

                    // Zoom / Advance controls (Physically slide the look anchor forward or backward)
                    case Key.Z:
                        helixCamera.Position += lookDir * (BaseCameraApproachDistance * (1 - zoomMultiplier));
                        break;
                    case Key.X:
                        helixCamera.Position -= lookDir * (BaseCameraApproachDistance * (1 - zoomMultiplier));
                        break;

                    default:
                        return;
                }

                // 3. APPLY EXPLICIT LOOK VECTOR TRANSFORMS (Camera position stays fixed!)
                Vector3D newLookDir = lookRotation.Transform(lookDir);
                newLookDir.Normalize();
                helixCamera.LookDirection = newLookDir;

                Vector3D newUpDir = lookRotation.Transform(localUp);
                newUpDir.Normalize();
                helixCamera.UpDirection = newUpDir;

                // =====================================================================
                // REAL-TIME MULTI-AXIS BILLBOARDING FOR STATIONARY CAMERA
                // =====================================================================
                // To keep the star quads volumetric while you look away, they need to 
                // match the camera's live position relative to the local system center.
                double dx = helixCamera.Position.X - centerX;
                double dy = helixCamera.Position.Y - centerY;
                double dz = helixCamera.Position.Z - centerZ;
                double currentRadius = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                Vector3D billboardLookVector = new Vector3D(dx, dy, dz);
                billboardLookVector.Normalize();

                double yawRadians = Math.Atan2(billboardLookVector.X, billboardLookVector.Z);
                double pitchRadians = -Math.Asin(billboardLookVector.Y);

                double yawDegrees = yawRadians * (180.0 / Math.PI);
                double pitchDegrees = pitchRadians * (180.0 / Math.PI);

                AxisAngleRotation3D horizontalRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), yawDegrees);
                AxisAngleRotation3D verticalRotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), pitchDegrees);

                System.Windows.Media.Media3D.Quaternion qHorizontal = new System.Windows.Media.Media3D.Quaternion(horizontalRotation.Axis, horizontalRotation.Angle);
                System.Windows.Media.Media3D.Quaternion qVertical = new System.Windows.Media.Media3D.Quaternion(verticalRotation.Axis, verticalRotation.Angle);

                SystemQuaternionRotation.Quaternion = qHorizontal * qVertical;
                // =====================================================================

                // =====================================================================
                // RADAR HUD TEXT UPDATE
                // =====================================================================
                // Calculate radar values based on where the camera is actually looking now
                double viewBrgRad = Math.Atan2(newLookDir.X, newLookDir.Z);
                double viewBrgDeg = viewBrgRad * (180.0 / Math.PI);
                if (viewBrgDeg < 0) viewBrgDeg += 360.0;

                double viewPthDeg = Math.Asin(newLookDir.Y) * (180.0 / Math.PI);

                if (HudViewingVectorText != null)
                {
                    HudViewingVectorText.Text = $"VIEW BRG: {viewBrgDeg:000}° | PTH: {viewPthDeg:+00;-00;00}°";
                }
                // =====================================================================

                e.Handled = true;
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
                double x = (rand.NextDouble() - 0.5) * SpaceDustSpreadRadius + centerX;
                double y = (rand.NextDouble() - 0.5) * SpaceDustSpreadRadius + centerY;
                double z = (rand.NextDouble() * SpaceDustAheadBuffer) + centerZ;

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

                if (!isAnimationFinished)
                {
                    double dx = helixCamera.Position.X - cameraTargetPosition.X;
                    double dy = helixCamera.Position.Y - cameraTargetPosition.Y;
                    double dz = helixCamera.Position.Z - cameraTargetPosition.Z;
                    double distanceToTarget = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (distanceToTarget < 0.1)
                    {
                        isAnimationFinished = true;

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

                Point3DCollection lineSegments = new Point3DCollection(ParticleCount * 2);
                double maxBufferZ = targetZ + SpaceDustAheadBuffer;
                double resetFarZ = targetZ;

                for (int i = 0; i < ParticleCount; i++)
                {
                    double nextZ = dustParticles[i].Z + WarpSpeed;

                    if (nextZ > maxBufferZ)
                    {
                        nextZ = resetFarZ;
                        double x = (rand.NextDouble() - 0.5) * SpaceDustSpreadRadius + targetX;
                        double y = (rand.NextDouble() - 0.5) * SpaceDustSpreadRadius + targetY;
                        dustParticles[i] = new Point3D(x, y, nextZ);
                    }
                    else
                    {
                        dustParticles[i] = new Point3D(dustParticles[i].X, dustParticles[i].Y, nextZ);
                    }

                    lineSegments.Add(dustParticles[i]);
                    lineSegments.Add(new Point3D(dustParticles[i].X, dustParticles[i].Y, dustParticles[i].Z + WarpStreakLength));
                }

                DustField.Points = lineSegments;
            }
        }
    }

    // =====================================================================
    // EXPANDED DATA DISPLAY MODEL WITH VECTOR CALCULATIONS
    // =====================================================================
    public class StarNeighborDisplay
    {
        public Star AssociatedStar { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Bearing { get; private set; }
        public double Pitch { get; private set; }

        public StarNeighborDisplay(Star target, Star origin, double baselineDistance)
        {
            AssociatedStar = target;
            Name = target.Name;
            Distance = baselineDistance;

            CalculateVectorMetrics(target, origin);
        }

        private void CalculateVectorMetrics(Star target, Star origin)
        {
            if (origin == null || target == null) return;

            // Extract delta vectors relative to system center root
            double dx = target.X - origin.X;
            double dy = target.Y - origin.Y;
            double dz = target.Z - origin.Z;

            // 1. Calculate Astronomical Bearing (Yaw) in horizontal XZ plane
            double bearingRad = Math.Atan2(dx, dz);
            double bearingDeg = bearingRad * (180.0 / Math.PI);
            if (bearingDeg < 0) bearingDeg += 360.0; // Wrap neatly to full circle
            Bearing = bearingDeg;

            // 2. Calculate Pitch (Inclination Angle relative to flat celestial horizon)
            if (Distance > 0.001)
            {
                double pitchRad = Math.Asin(dy / Distance);
                Pitch = pitchRad * (180.0 / Math.PI);
            }
            else
            {
                Pitch = 0.0;
            }
        }

        public string DistanceString => $"{Distance:F2} ly";
        public string VectorTelemetryString => $"BRG: {Bearing:000}° | PTH: {Pitch:+00;-00;00}°";
    }
}