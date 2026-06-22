using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _3DstarMap
{
    /// <summary>
    /// Represents a collection of stars loaded from an interstellar mapping dataset.
    /// </summary>
    public class StarCollection
    {
        /// <summary>
        /// A collection of Star objects.
        /// </summary>
        public List<Star> Stars { get; set; } = new List<Star>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StarCollection"/> class.
        /// Loads star data from a CSV file at the specified location.
        /// </summary>
        /// <param name="filePath">Path to the CSV file containing star data.</param>
        public StarCollection(string filePath)
        {
            LoadFromCsv(filePath);
        }

        /// <summary>
        /// Loads data from a CSV file into the Star collection, safely parsing metric properties and constellations.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        public void LoadFromCsv(string filePath)
        {
            if (!File.Exists(filePath)) return;

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string[] values = line.Split(',');

                int idNum;
                // Verify row validity by parsing the leading ID field and confirming required column length
                if (values.Length >= 37 && Int32.TryParse(values[0], out idNum))
                {
                    Star star = new Star
                    {
                        Id = idNum,
                        Name = values[6],
                        Distance = double.TryParse(values[9], out double d) ? d : 0.0,
                        X = double.TryParse(values[17], out double x) ? x : 0.0,
                        Y = double.TryParse(values[18], out double y) ? y : 0.0,
                        Z = double.TryParse(values[19], out double z) ? z : 0.0,
                        SpectralType = values[15],
                        Magnitude = double.TryParse(values[13], out double m) ? m : 0.0,

                        // EXTRACED PROPERTIES WITH FALLBACKS
                        AbsoluteMagnitude = double.TryParse(values[14], out double absMag) ? absMag : 0.0,
                        ColourIndex = double.TryParse(values[16], out double ci) ? ci : 0.0,
                        PrimaryComponent = int.TryParse(values[31], out int compPrimary) ? compPrimary : idNum,
                        Luminosity = double.TryParse(values[33], out double lum) ? lum : 0.0,

                        // READ CONSTELLATION FROM EXCEL COLUMN "AD" (INDEX 29)
                        Constellation = values.Length > 29 ? values[29] : ""
                    };

                    Stars.Add(star);
                }
            }
        }

        /// <summary>
        /// Retrieves the list of stars.
        /// </summary>
        /// <returns>A list of stars.</returns>
        public List<Star> GetStars()
        {
            return this.Stars;
        }
    }
}