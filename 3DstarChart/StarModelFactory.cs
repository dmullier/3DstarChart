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
        public static Color GetColourFromSpectrum(string spectralType)
        {
            // 1. Safety check for null or empty data strings
            if (string.IsNullOrEmpty(spectralType)) return Colors.White;

            // 2. Convert to uppercase to handle entries like "dm6" or "g2v" cleanly
            string cleanType = spectralType.ToUpper();

            // 3. Handle White Dwarfs first (e.g., "DG", "DA", "DB") 
            // They are dead cores, so let's give them a unique hot-white/cyan tint
            if (cleanType.StartsWith("D") && cleanType.Length > 1)
            {
                return Colors.LightCyan;
            }

            // 4. Scan the string to find the first valid primary classification letter.
            // This safely skips prefixes like "sd" or "d" and jumps straight to the core class.
            foreach (char c in cleanType)
            {
                switch (c)
                {
                    case 'O':
                    case 'B': return Colors.LightSkyBlue; // Hottest blue/white stars
                    case 'A': return Colors.White;        // Pure white stars (e.g., Sirius)
                    case 'F': return Colors.LightYellow;   // Yellow-white stars (e.g., Procyon)
                    case 'G': return Colors.Yellow;        // Yellow stars (like our Sun)
                    case 'K': return Colors.Orange;        // Orange dwarfs/giants (e.g., Arcturus)
                    case 'M': return Colors.Red;           // Cool red dwarfs/giants (e.g., Proxima Centauri)
                }
            }

            // Default fall-back color if the string is unclassified or anomalous
            return Colors.White;
        }
    }
}