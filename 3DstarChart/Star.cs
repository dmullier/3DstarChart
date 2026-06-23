using System;
using System.Collections.Generic;

namespace _3DstarMap
{
    /// <summary>
    /// Star from Hubble Yale Gliese (HYG) Database, representing a star with its properties.
    /// </summary>
    public class Star
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Magnitude { get; set; }
        public string SpectralType { get; set; }
     
        public string BaseSystemId { get; set; } 
        public int PrimaryComponent { get; set; }
        public double AbsoluteMagnitude { get; set; }
        public double Luminosity { get; set; }
        public double ColourIndex { get; set; }
        public string Constellation { get; set; }

        public string Bayer { get; set; }
        public string Flam { get; set; }
        public string Gliese { get; set; }
        public int Hd { get; set; }
        public int Hip { get; set; }
        /// <summary>
        /// Used to set and detrrmine if a star is part of a multi-star system
        /// </summary>
        public Boolean HasCompanions { get; set; } = false;
        /// <summary>
        /// IDs of any companion stars, null array if none exist. This is used to represent binary or multiple star systems.
        /// </summary>
        public int[] CompanionStars { get; set; } = Array.Empty<int>();

        /// <summary>
        /// Constructor to generate blank star.
        /// </summary>
        public Star()
        {
            Id = 0;
            Name = "";
            Distance = 0.0;
            X = 0.0;
            Y = 0.0;
            Z = 0.0;
            Magnitude = 0.0;
            SpectralType = "";
            PrimaryComponent = 0;
            BaseSystemId = "";
            AbsoluteMagnitude = 0.0;
            Luminosity = 0.0;
            ColourIndex = 0.0;
            Constellation = "";

            // Initialize new datafields
            Bayer = "";
            Flam = "";
            Gliese = "";
            Hd = 0;
            Hip = 0;
            
        }

        public void SetId(int id) { Id = id; }
        public void SetNAME(string name) { Name = name; }
        public void SetDistance(double distance) { Distance = distance; }
        public void SetX(double x) { X = x; }
        public void SetY(double y) { Y = y; }
        public void SetZ(double z) { Z = z; }
        public void SetMagnitude(double magnitude) { Magnitude = magnitude; }
        public void SetSPECT(string spect) { SpectralType = spect; }
        public void SetPrimaryComponent(int primaryComponent) { PrimaryComponent = primaryComponent; }
        public void SetAbsoluteMagnitude(double absoluteMagnitude) { AbsoluteMagnitude = absoluteMagnitude; }
        public void SetLuminosity(double luminosity) { Luminosity = luminosity; }
        public void SetColourIndex(double colourIndex) { ColourIndex = colourIndex; }
        public void SetConstellation(string constellation) { Constellation = constellation; }
        public void SetBayer(string bayer) { Bayer = bayer; }
        public void SetFlam(string flam) { Flam = flam; }
        public void SetGliese(string gliese) { Gliese = gliese; }
        public void SetHd(int hd) { Hd = hd; }
        public void SetHip(int hip) { Hip = hip; }
    }
}