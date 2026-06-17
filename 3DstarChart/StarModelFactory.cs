using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DstarChart
{
    public static class StarModelFactory
    {
        // Generates a 3D visual cube representing a star
        public static Model3DGroup CreateStarCube(double x, double y, double z, double size, Color color)
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

        // Maps astronomical spectral classification string to a visual color representation
        public static Color GetColorFromSpectrum(string spectralType)
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
