using _3DstarMap;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _3DstarChart
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PopulateStarMap();
        }

        private void PopulateStarMap()
        {
            // 1. Plot the Sun explicitly at the absolute center (0,0,0)
            var sun = CreateStarCube(0, 0, 0, 0.4, Colors.Yellow);
            StarGroup.Children.Add(sun);

            // 2. Initialize your existing StarChart object
            // This will automatically trigger its constructor and load your data
            string filePath = "starchart.csv";
            StarCollection chart = new StarCollection(filePath);

            // 3. Loop through your existing List<Star> collection
            foreach (Star star in chart.Stars)
            {
                // Safety skip: If the Sun is in your list, don't double-render it
                if (star.Id == 0) continue;

                // Color based on spectral class type
                Color starColor = GetColorFromSpectrum(star.SpectralType);
                double size = 0.15;

                // 4. Build the 3D model cube using the pre-calculated coordinates
                var starModel = CreateStarCube(star.X, star.Y, star.Z, size, starColor);

                // 5. Mount it directly into the XAML-defined container group
                StarGroup.Children.Add(starModel);
            }
        }

        // The geometric mesh engine that transforms point coordinates into a 3D surface
        private Model3DGroup CreateStarCube(double x, double y, double z, double size, Color color)
        {
            Model3DGroup meshGroup = new Model3DGroup();
            MeshGeometry3D mesh = new MeshGeometry3D();
            double half = size / 2.0;

            // Generate the 8 vertices of the cube centered on X, Y, Z
            mesh.Positions.Add(new Point3D(x - half, y - half, z - half));
            mesh.Positions.Add(new Point3D(x + half, y - half, z - half));
            mesh.Positions.Add(new Point3D(x + half, y + half, z - half));
            mesh.Positions.Add(new Point3D(x - half, y + half, z - half));
            mesh.Positions.Add(new Point3D(x - half, y - half, z + half));
            mesh.Positions.Add(new Point3D(x + half, y - half, z + half));
            mesh.Positions.Add(new Point3D(x + half, y + half, z + half));
            mesh.Positions.Add(new Point3D(x - half, y + half, z + half));

            // Index mapping for drawing structural triangles across the 6 faces
            int[] triangles = {
                0,2,1, 0,3,2, 1,6,5, 1,2,6, 5,7,4, 5,6,7,
                4,3,0, 4,7,3, 3,6,2, 3,7,6, 4,1,5, 4,0,1
            };
            foreach (int t in triangles) mesh.TriangleIndices.Add(t);

            // Wrap the geometric surface in a solid color material skin
            SolidColorBrush brush = new SolidColorBrush(color);
            GeometryModel3D model = new GeometryModel3D(mesh, new DiffuseMaterial(brush));

            meshGroup.Children.Add(model);
            return meshGroup;
        }

        private Color GetColorFromSpectrum(string spectralType)
        {
            if (string.IsNullOrEmpty(spectralType)) return Colors.White;
            char classification = char.ToUpper(spectralType[0]);

            switch (classification)
            {
                case 'O': case 'B': return Colors.LightSkyBlue;
                case 'A': return Colors.White;
                case 'F': return Colors.LightYellow;
                case 'G': return Colors.Yellow;
                case 'K': return Colors.Orange;
                case 'M': return Colors.Red;
                default: return Colors.White;
            }
        }
    }
}