using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace _3DstarMap
{
    /// <summary>
    /// Represents a collection of stars loaded from an interstellar mapping dataset.
    /// </summary>
    public class StarCollection
    {
        private Dictionary<int, List<int>> _systemLookup = new Dictionary<int, List<int>>();
        public List<Star> Stars { get; set; } = new List<Star>();

        // Constellations mapped to their full Latin Genitives to resolve naming grammars accurately
        private readonly Dictionary<string, string> ConstellationNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "And", "Andomedae" }, { "Ant", "Antliae" }, { "Aps", "Apodis" }, { "Aqr", "Aquarii" },
            { "Aql", "Aquilae" }, { "Ara", "Arae" }, { "Ari", "Arietis" }, { "Aur", "Aurigae" },
            { "Boo", "Bootis" }, { "Cae", "Caeli" }, { "Cam", "Camelopardalis" }, { "Cnc", "Cancri" },
            { "CVn", "Canum Venaticorum" }, { "CMa", "Canis Majoris" }, { "CMi", "Canis Minoris" }, { "Cap", "Capricorni" },
            { "Car", "Carinae" }, { "Cas", "Cassiopeiae" }, { "Cen", "Centauri" }, { "Cep", "Cephei" },
            { "Cet", "Ceti" }, { "Cha", "Chamaeleontis" }, { "Cir", "Circini" }, { "Col", "Columbae" },
            { "Com", "Comae Berenices" }, { "CrA", "Coronae Australis" }, { "CrB", "Coronae Borealis" }, { "Crv", "Corvi" },
            { "Crt", "Crateris" }, { "Cru", "Crucis" }, { "Cyg", "Cygni" }, { "Del", "Delphini" },
            { "Dor", "Doradus" }, { "Dra", "Draconis" }, { "Equ", "Equulei" }, { "Eri", "Eridani" },
            { "For", "Fornacis" }, { "Gem", "Geminorum" }, { "Gru", "Gruis" }, { "Her", "Herculis" },
            { "Hor", "Horologii" }, { "Hya", "Hydrae" }, { "Hyi", "Hydri" }, { "Ind", "Indi" },
            { "Lac", "Lacertae" }, { "Leo", "Leonis" }, { "LMi", "Leonis Minoris" }, { "Lep", "Leporis" },
            { "Lib", "Librae" }, { "Lup", "Lupi" }, { "Lyn", "Lyncis" }, { "Lyr", "Lyrae" },
            { "Men", "Mensae" }, { "Mic", "Microscopii" }, { "Mon", "Monocerotis" }, { "Mus", "Muscae" },
            { "Nor", "Normae" }, { "Oct", "Octantis" }, { "Oph", "Ophiuchi" }, { "Ori", "Orionis" },
            { "Pav", "Pavonis" }, { "Peg", "Pegasi" }, { "Per", "Persei" }, { "Phe", "Phoenicis" },
            { "Pic", "Pictoris" }, { "Psc", "Piscium" }, { "PsA", "Piscis Austrini" }, { "Pup", "Puppis" },
            { "Pyx", "Pyxidis" }, { "Ret", "Reticuli" }, { "Sge", "Sagittae" }, { "Sgr", "Sagittarii" },
            { "Sco", "Scorpii" }, { "Scl", "Sculptoris" }, { "Sct", "Scuti" }, { "Ser", "Serpentis" },
            { "Sex", "Sextantis" }, { "Tau", "Tauri" }, { "Tel", "Telescopii" }, { "Tri", "Trianguli" },
            { "TrA", "Trianguli Australis" }, { "Tuc", "Tucanae" }, { "UMa", "Ursae Majoris" }, { "UMi", "Ursae Minoris" },
            { "Vel", "Velorum" }, { "Vir", "Virginis" }, { "Vol", "Volantis" }, { "Vul", "Vulpeculae" }
        };

        private readonly Dictionary<string, string> GreekLetters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Alp", "Alpha" },  { "Bet", "Beta" },    { "Gam", "Gamma" },
            { "Del", "Delta" },  { "Eps", "Epsilon" }, { "Zet", "Zeta" },
            { "Eta", "Eta" },    { "The", "Theta" },   { "Iot", "Iota" },
            { "Kap", "Kappa" },  { "Lam", "Lambda" },  { "Mu",  "Mu" },
            { "Nu",  "Nu" },     { "Xi",  "Xi" },      { "Omi", "Omicron" },
            { "Pi",  "Pi" },     { "Rho", "Rho" },     { "Sig", "Sigma" },
            { "Tau", "Tau" },    { "Ups", "Upsilon" }, { "Phi", "Phi" },
            { "Chi", "Chi" },    { "Psi", "Psi" },     { "Ome", "Omega" }
        };

        public StarCollection(string filePath)
        {
            LoadFromCsv(filePath);
        }

        public void LoadFromCsv(string filePath)
        {
            if (!File.Exists(filePath)) return;

            string[] lines = File.ReadAllLines(filePath);

            // FIRST PASS: Read the raw fields and populate the dictionary fully
            foreach (string line in lines)
            {
                string[] values = line.Split(',');

                if (values.Length >= 37 && Int32.TryParse(values[0], out int idNum))
                {
                    string rawProper = values[6]?.Trim();

                    Star star = new Star
                    {
                        Id = idNum,
                        Name = rawProper,
                        Distance = double.TryParse(values[9], out double d) ? d : 0.0,
                        X = double.TryParse(values[17], out double x) ? x : 0.0,
                        Y = double.TryParse(values[18], out double y) ? y : 0.0,
                        Z = double.TryParse(values[19], out double z) ? z : 0.0,
                        SpectralType = values[15]?.Trim(),
                        Magnitude = double.TryParse(values[13], out double m) ? m : 0.0,
                        AbsoluteMagnitude = double.TryParse(values[14], out double absMag) ? absMag : 0.0,
                        ColourIndex = double.TryParse(values[16], out double ci) ? ci : 0.0,
                        Luminosity = double.TryParse(values[33], out double lum) ? lum : 0.0,
                        Constellation = values.Length > 29 ? values[29]?.Trim() : "",

                        PrimaryComponent = int.TryParse(values[31], out int compPrimary) ? compPrimary : idNum,
                        BaseSystemId = values.Length > 32 ? values[32]?.Trim() : "",

                        Hip = int.TryParse(values[1], out int hipId) ? hipId : 0,
                        Hd = int.TryParse(values[2], out int hdId) ? hdId : 0,
                        Gliese = values[4]?.Trim() ?? "",
                        Bayer = values[27]?.Trim() ?? "",
                        Flam = values[28]?.Trim() ?? ""
                    };

                    // Build the lookup structure completely first
                    if (!_systemLookup.ContainsKey(star.PrimaryComponent))
                    {
                        _systemLookup[star.PrimaryComponent] = new List<int>();
                    }
                    _systemLookup[star.PrimaryComponent].Add(star.Id);

                    // Replace three-letter code with full name
                    string fullConstellationName = ConstellationNames.GetValueOrDefault(star.Constellation, "Unknown Constellation");
                    star.Constellation = fullConstellationName;

                    if (string.IsNullOrWhiteSpace(star.Name))
                    {
                        star.Name = ResolveMissingStarName(star);
                    }

                    Stars.Add(star);
                }
            }

            // SECOND PASS: Now that the dictionary is complete, map out the relationships
            foreach (Star star in Stars)
            {
                star.CompanionStars = GetSystemStarIds(star);

                if (star.CompanionStars != null && star.CompanionStars.Length > 0)
                {
                    star.HasCompanions = true;
                }
                else
                {
                    star.HasCompanions = false;
                }
            }
        }
        private string ResolveMissingStarName(Star star)
        {
            string baseConstellation = ConstellationNames.TryGetValue(star.Constellation, out var genitive)
                ? genitive
                : star.Constellation;

            // 1. Check Bayer Designation (e.g. "Tau Ceti" or "Alpha 1 Centauri")
            if (!string.IsNullOrWhiteSpace(star.Bayer) && !string.IsNullOrWhiteSpace(star.Constellation))
            {
                string[] bayerParts = star.Bayer.Split('-');
                string rootGreek = bayerParts[0];

                string readableGreek = GreekLetters.TryGetValue(rootGreek, out var fullGreek)
                    ? fullGreek
                    : rootGreek;

                if (bayerParts.Length > 1)
                {
                    return $"{readableGreek} {bayerParts[1]} {baseConstellation}";
                }
                return $"{readableGreek} {baseConstellation}";
            }

            // 2. Check Flamsteed Designation (e.g. "52 Ceti")
            if (!string.IsNullOrWhiteSpace(star.Flam) && !string.IsNullOrWhiteSpace(star.Constellation))
            {
                return $"{star.Flam} {baseConstellation}";
            }

            // 3. Check Gliese Designation (e.g. "Gl 65A")
            if (!string.IsNullOrWhiteSpace(star.Gliese))
            {
                return star.Gliese;
            }

            // 4. Catalog fallback deep index strings
            if (star.Hd > 0) return $"HD {star.Hd}";
            if (star.Hip > 0) return $"HIP {star.Hip}";

            // 5. Ultimate fallback if completely anonymous
            return $"HYG {star.Id}";
        }

        /// <summary>
        /// Return array of IDs of any companions stars to the current star, null if none.
        /// </summary>
        /// <param name="star">star.cs</param>
        /// <returns>int array of companion star ids</returns>
        public int[] GetSystemStarIds(Star star)
        {
            // 1. If the input star is null, return null immediately
            if (star == null)
            {
                return null;
            }

            // 2. Check if our high-speed dictionary contains this system's PrimaryComponent
            if (_systemLookup.TryGetValue(star.PrimaryComponent, out List<int> allSystemIds))
            {
                // 3. If the system only has 1 star, it's a single star. 
                // Per your requirement, we return null because there are "none" (no companions).
                if (allSystemIds.Count <= 1)
                {
                    return null;
                }

                List<int> companionIds = new List<int>();

                // 4. Loop ONLY through the stars inside this specific system
                foreach (int id in allSystemIds)
                {
                    // 5. If the ID belongs to a companion and is not the star itself, add it
                    if (id != star.Id)
                    {
                        companionIds.Add(id);
                    }
                }
                if (companionIds.Count >= 2)
                {
                    Console.WriteLine("more than double found");
                }
                // 6. Return the array of companion IDs
                return companionIds.ToArray();
            }

            // 7. Fallback safety if the ID wasn't found in the dictionary index
            return null;
        }

        public List<Star> GetStars()
        {
            return this.Stars;
        }
    }
}