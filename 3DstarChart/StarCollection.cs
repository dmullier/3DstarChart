// EXISTING CLASS DEFINITION IN PROJECT:
// public class Star { 
//     public int Id { get; set; } 
//     public string Name { get; set; } 
//     public double Distance { get; set; } 
//     public double X { get; set; } 
//     public double Y { get; set; } 
//     public double Z { get; set; } 
//     public double Magnitude { get; set; } 
//     public string SpectralType { get; set; } 
// }

// CREATE THE STARMAP CLASS BELOW:
// Create a class called StarMap to load the star data from a csv file and store it as objects of the PREEXISTING Star class defined above.
// It will need a List<Star> and a method to load the data from the file, it will also need a method to get the list of stars.
// The data is in the form: id, hip, hd, hr...
// CRITICAL: DO NOT generate an internal, nested, or duplicate class for Star. Use the existing container type.

//create a class called StarMap to load the star data from a csv file and store it as objects of the preexiting Star class, it will need a list of stars and a method to load the data from the file, it will also need a method to get the list of stars
// the data is in the form id	hip	hd	hr	gl	bf	name	ra	dec	distance	pmra	pmdec	rv	mag	absmag	spect	ci	x	y	z	vx	vy	vz	rarad	decrad	pmrarad	pmdecrad	bayer	flam	con	comp	comp_primary	base	lum	var	var_min	var_max
//ignore data this is not in the star class, we only need id, name, distance, x, y, z, magnitude, spect




using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _3DstarMap
{
    /// <summary>
    /// Represents a collection of stars.
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
        /// Loads data from a CSV file into the Star collection.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        public void LoadFromCsv(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string[] values = line.Split(',');

                int number;
                if (values.Length >= 37 && Int32.TryParse(values[0], out number) == true)
                {
                    Star star = new Star
                    {
                        Id = int.Parse(values[0]),
                        Name = values[6],
                        Distance = double.Parse(values[9]),
                        X = double.Parse(values[17]),
                        Y = double.Parse(values[18]),
                        Z = double.Parse(values[19]),
                        SpectralType = values[15],
                        Magnitude = double.Parse(values[13])
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
