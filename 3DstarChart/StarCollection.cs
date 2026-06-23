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
        public List<Star> Stars { get; set; } = new List<Star>();

        // Constellations mapped to their full Latin Genitives to resolve naming grammars accurately
        private readonly Dictionary<string, string> ConstellationGenitives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
                        PrimaryComponent = int.TryParse(values[31], out int compPrimary) ? compPrimary : idNum,
                        Luminosity = double.TryParse(values[33], out double lum) ? lum : 0.0,
                        Constellation = values.Length > 29 ? values[29]?.Trim() : "",

                        // Parse new datafields directly from their column positions
                        Hip = int.TryParse(values[1], out int hipId) ? hipId : 0,
                        Hd = int.TryParse(values[2], out int hdId) ? hdId : 0,
                        Gliese = values[4]?.Trim() ?? "",
                        Bayer = values[27]?.Trim() ?? "",
                        Flam = values[28]?.Trim() ?? ""
                    };

                    // Engine to automatically resolve missing names cleanly from extracted fields
                    if (string.IsNullOrWhiteSpace(star.Name))
                    {
                        star.Name = ResolveMissingStarName(star);
                    }

                    Stars.Add(star);
                }
            }
        }

        private string ResolveMissingStarName(Star star)
        {
            string baseConstellation = ConstellationGenitives.TryGetValue(star.Constellation, out var genitive)
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

        public List<Star> GetStars()
        {
            return this.Stars;
        }
    }
}