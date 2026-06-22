using System;
using System.Collections.Generic;

namespace _3DstarMap
{
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

        // EXTRACED PROPERTIES
        public int PrimaryComponent { get; set; }
        public double AbsoluteMagnitude { get; set; }
        public double Luminosity { get; set; }
        public double ColourIndex { get; set; }

        // NEW PROPERTY
        public string Constellation { get; set; }

        /// <summary>
        /// Represents a star with its properties.
        /// </summary>
        public Star()
        {
            /// <summary>
            /// Unique identifier for the star.
            /// </summary>
            Id = 0;

            /// <summary>
            /// Name of the star.
            /// </summary>
            Name = "";

            /// <summary>
            /// Distance from Earth to the star in astronomical units (AU).
            /// </summary>
            Distance = 0.0;

            /// <summary>
            /// X-coordinate of the star's position in space.
            /// </summary>
            X = 0.0;

            /// <summary>
            /// Y-coordinate of the star's position in space.
            /// </summary>
            Y = 0.0;

            /// <summary>
            /// Z-coordinate of the star's position in space.
            /// </summary>
            Z = 0.0;

            /// <summary>
            /// Apparent magnitude of the star, which is a measure of its brightness.
            /// </summary>
            Magnitude = 0.0;

            /// <summary>
            /// Spectral type of the star (e.g., O, B, A, F, G, K, M).
            /// </summary>
            SpectralType = "";

            /// <summary>
            /// ID of the primary star component if part of a multi-star system.
            /// </summary>
            PrimaryComponent = 0;

            /// <summary>
            /// The intrinsic brightness of the star as seen from a fixed distance of 10 parsecs.
            /// </summary>
            AbsoluteMagnitude = 0.0;

            /// <summary>
            /// Total energy output of the star relative to our Sun.
            /// </summary>
            Luminosity = 0.0;

            /// <summary>
            /// Numerical color metric (B-V value) indicating star temperature profiles.
            /// </summary>
            ColourIndex = 0.0;

            /// <summary>
            /// Three-letter abbreviation or full name of the astronomical constellation the star resides in.
            /// </summary>
            Constellation = "";
        }

        /// <summary>
        /// Sets the ID of this object.
        /// </summary>
        /// <param name="id">The ID to set.</param>
        public void SetId(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Sets the NAME property.
        /// </summary>
        /// <param name="name">The value to be set for the NAME property.</param>
        public void SetNAME(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Sets the distance.
        /// </summary>
        /// <param name="distance">The new distance.</param>
        public void SetDistance(double distance)
        {
            Distance = distance;
        }

        /// <summary>
        /// Sets the value of X.
        /// </summary>
        /// <param name="x">The new value for X.</param>
        public void SetX(double x)
        {
            X = x;
        }

        /// <summary>
        /// Sets the Y-coordinate of an object.
        /// </summary>
        /// <param name="y">The new value for Y.</param>
        public void SetY(double y)
        {
            Y = y;
        }

        /// <summary>
        /// Sets the value of Z.
        /// </summary>
        /// <param name="z">The new value of Z.</param>
        public void SetZ(double z)
        {
            Z = z;
        }

        /// <summary>
        /// Sets the magnitude value.
        /// </summary>
        /// <param name="magnitude">The magnitude to set.</param>
        public void SetMagnitude(double magnitude)
        {
            Magnitude = magnitude;
        }

        public void SetSPECT(string spect)
        {
            SpectralType = spect;
        }

        public void SetPrimaryComponent(int primaryComponent)
        {
            PrimaryComponent = primaryComponent;
        }

        public void SetAbsoluteMagnitude(double absoluteMagnitude)
        {
            AbsoluteMagnitude = absoluteMagnitude;
        }

        public void SetLuminosity(double luminosity)
        {
            Luminosity = luminosity;
        }

        public void SetColourIndex(double colourIndex)
        {
            ColourIndex = colourIndex;
        }
        /// <summary>
        /// Sets the constellation designation.
        /// </summary>
        /// <param name="constellation">The constellation string name or abbreviation.</param>
        public void SetConstellation(string constellation)
        {
            Constellation = constellation;
        }
    }
}