using System;
using System.Collections.Generic;
namespace MuPDF.Fonts.MupdfFontsData {
    internal static class Registry {
        internal sealed class Entry { public bool Bold; public bool Italic; public Func<byte[]> Loader = () => Array.Empty<byte>(); }
        internal static readonly Dictionary<string, Entry> Map = new Dictionary<string, Entry>(StringComparer.Ordinal) {
            ["cascadia"] = new Entry { Bold = false, Italic = false, Loader = CascadiaMono_Regular_Data.GetBuffer },
            ["cascadiab"] = new Entry { Bold = true, Italic = false, Loader = CascadiaMono_Bold_Data.GetBuffer },
            ["cascadiabi"] = new Entry { Bold = true, Italic = true, Loader = CascadiaMono_BoldItalic_Data.GetBuffer },
            ["cascadiai"] = new Entry { Bold = false, Italic = true, Loader = CascadiaMono_Italic_Data.GetBuffer },
            ["figbi"] = new Entry { Bold = true, Italic = true, Loader = FiraGO_BoldItalic_Data.GetBuffer },
            ["figbo"] = new Entry { Bold = true, Italic = false, Loader = FiraGO_Bold_Data.GetBuffer },
            ["figit"] = new Entry { Bold = false, Italic = true, Loader = FiraGO_Italic_Data.GetBuffer },
            ["figo"] = new Entry { Bold = false, Italic = false, Loader = FiraGO_Regular_Data.GetBuffer },
            ["fimbo"] = new Entry { Bold = true, Italic = false, Loader = FiraMono_Bold_Data.GetBuffer },
            ["fimo"] = new Entry { Bold = false, Italic = false, Loader = FiraMono_Regular_Data.GetBuffer },
            ["math"] = new Entry { Bold = false, Italic = false, Loader = NotoSansMath_Regular_Data.GetBuffer },
            ["music"] = new Entry { Bold = false, Italic = false, Loader = NotoMusic_Regular_Data.GetBuffer },
            ["notos"] = new Entry { Bold = false, Italic = false, Loader = NotoSans_Regular_Data.GetBuffer },
            ["notosbi"] = new Entry { Bold = true, Italic = true, Loader = NotoSans_BoldItalic_Data.GetBuffer },
            ["notosbo"] = new Entry { Bold = true, Italic = false, Loader = NotoSans_Bold_Data.GetBuffer },
            ["notosit"] = new Entry { Bold = false, Italic = true, Loader = NotoSans_Italic_Data.GetBuffer },
            ["spacembi"] = new Entry { Bold = true, Italic = true, Loader = SpaceMono_BoldItalic_Data.GetBuffer },
            ["spacembo"] = new Entry { Bold = true, Italic = false, Loader = SpaceMono_Bold_Data.GetBuffer },
            ["spacemit"] = new Entry { Bold = false, Italic = true, Loader = SpaceMono_Italic_Data.GetBuffer },
            ["spacemo"] = new Entry { Bold = false, Italic = false, Loader = SpaceMono_Regular_Data.GetBuffer },
            ["symbol1"] = new Entry { Bold = false, Italic = false, Loader = NotoSansSymbols_Regular_Data.GetBuffer },
            ["symbol2"] = new Entry { Bold = false, Italic = false, Loader = NotoSansSymbols2_Regular_Data.GetBuffer },
            ["ubuntm"] = new Entry { Bold = false, Italic = false, Loader = UbuntuMono_Regular_Data.GetBuffer },
            ["ubuntmbi"] = new Entry { Bold = true, Italic = true, Loader = UbuntuMono_BoldItalic_Data.GetBuffer },
            ["ubuntmbo"] = new Entry { Bold = true, Italic = false, Loader = UbuntuMono_Bold_Data.GetBuffer },
            ["ubuntmit"] = new Entry { Bold = false, Italic = true, Loader = UbuntuMono_Italic_Data.GetBuffer },
            ["ubuntu"] = new Entry { Bold = false, Italic = false, Loader = Ubuntu_Regular_Data.GetBuffer },
            ["ubuntubi"] = new Entry { Bold = true, Italic = true, Loader = Ubuntu_BoldItalic_Data.GetBuffer },
            ["ubuntubo"] = new Entry { Bold = true, Italic = false, Loader = Ubuntu_Bold_Data.GetBuffer },
            ["ubuntuit"] = new Entry { Bold = false, Italic = true, Loader = Ubuntu_Italic_Data.GetBuffer },
        };
    }
}
