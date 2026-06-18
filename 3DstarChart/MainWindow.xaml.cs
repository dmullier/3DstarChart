using _3DstarMap;
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
            // Remap controls cleanly for a laptop trackpad:
            // Control + Left Click drag will now Rotate/Orbit
            MainViewport.RotateGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Control);

            // Shift + Left Click drag will now Pan the camera
            MainViewport.PanGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Shift);
            MainViewport.ZoomGesture = new MouseGesture(MouseAction.LeftClick, ModifierKeys.Alt);
            PopulateStarMap();
            AnimateCameraToSun();
        }

        private void PopulateStarMap()
        {
            // 1. Plot the Sun explicitly at the center using our new factory
            var sun = StarModelFactory.CreateStarCube(0, 0, 0, 0.4, Colors.Yellow);
            StarGroup.Children.Add(sun);

            // 2. Load the data catalog container
            string filePath = "starchart.csv";
            StarCollection chart = new StarCollection(filePath);

            // 3. Process the dataset items
            foreach (Star star in chart.Stars)
            {
                if (star.Id == 0) continue; // Skip Sun duplication

                // Ask the factory to figure out the proper color mapping
                Color starColor = StarModelFactory.GetColourFromSpectrum(star.SpectralType);
                double size = 0.15;

                // Ask the factory to construct the actual 3D visual component
                var starModel = StarModelFactory.CreateStarCube(star.X, star.Y, star.Z, size, starColor);

                // Mount the output mesh asset straight into the UI tree viewport
                StarGroup.Children.Add(starModel);
            }
        }
        private void AnimateCameraToSun()
        {
            // 1. Safely extract the active camera from the Helix Viewport container
            if (MainViewport.Camera is PerspectiveCamera helixCamera)
            {
                // 2. Define the starting point and destination coordinates
                Point3D startPosition = new Point3D(0, 0, 1000);
                Point3D endPosition = new Point3D(0, 0, 4); // Sits right in front of the Sun

                // 3. Create the 3D Point Animation
                Point3DAnimation cameraFlyIn = new Point3DAnimation
                {
                    From = startPosition,
                    To = endPosition,
                    Duration = new Duration(TimeSpan.FromSeconds(8)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                // 4. Run the animation on the extracted camera instance
                helixCamera.BeginAnimation(PerspectiveCamera.PositionProperty, cameraFlyIn);
            }
        }
    }

}