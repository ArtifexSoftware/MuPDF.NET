// For more information see https://aka.ms/fsharp-console-apps
open System
open System.Collections.Generic
open System.Linq

let FindFit (w: float) (h: float) =
    let PaperSizes = dict [
        ("A2", (1191.0f, 1684.0f));
        ("A3", (842.0f, 1191.0f));
        ("A4", (595.0f, 842.0f));
        ("A5", (420.0f, 595.0f));
        ("A6", (298.0f, 420.0f));
        ("A7", (210.0f, 298.0f));
        ("A8", (147.0f, 210.0f));
        ("A9", (105.0f, 147.0f));
        ("A10", (74.0f, 105.0f));
        ("B0", (2835.0f, 4008.0f));
        ("B1", (2004.0f, 2835.0f));
        ("B2", (1417.0f, 2004.0f));
        ("B3", (1001.0f, 1417.0f));
        ("B4", (709.0f, 1001.0f));
        ("B5", (499.0f, 709.0f));
        ("B6", (354.0f, 499.0f));
        ("B7", (249.0f, 354.0f));
        ("B8", (176.0f, 249.0f));
        ("B9", (125.0f, 176.0f));
        ("B10", (88.0f, 125.0f));
        ("C0", (2599.0f, 3677.0f));
        ("C1", (1837.0f, 2599.0f));
        ("C2", (1298.0f, 1837.0f));
        ("C3", (918.0f, 1298.0f));
        ("C4", (649.0f, 918.0f));
        ("C5", (459.0f, 649.0f));
        ("C6", (323.0f, 459.0f));
        ("C7", (230.0f, 323.0f));
        ("C8", (162.0f, 230.0f));
        ("C9", (113.0f, 162.0f));
        ("C10", (79.0f, 113.0f));
        ("Tabloid Extra", (864.0f, 1296.0f));
        ("Legal-13", (612.0f, 936.0f));
        ("Commercial", (297.0f, 684.0f));
        ("Monarch", (279.0f, 540.0f));
        ("Card-5x7", (360.0f, 504.0f));
        ("Card-4x6", (288.0f, 432.0f));
        ("Invoice", (396.0f, 612.0f));
        ("Executive", (522.0f, 756.0f));
        ("Letter", (612.0f, 792.0f));
        ("Legal", (612.0f, 1008.0f));
        ("Ledger", (792.0f, 1224.0f))
    ]

    let wi = int (Math.Round(w, 0))
    let hi = int (Math.Round(h, 0))
    let w1, h1 = if w <= h then (wi, hi) else (hi, wi)
    let sw = $"{w1}"
    let sh = $"{h1}"
    let stab = PaperSizes.Values |> Seq.map (fun size -> Math.Abs(float w1 - size.Item1) + Math.Abs(float h1 - size.Item2)) |> Seq.toList
    let small = Seq.min stab
    let idx = stab.IndexOf(small)
    let f = PaperSizes.Keys |> Seq.toList |> Seq.item idx
    let ff, ss = if w <= h then (f + "-P", $"{PaperSizes.[f].Item1}" + " x " + $"{PaperSizes.[f].Item2}")
                 else (f + "-L", $"{PaperSizes.[f].Item2}" + " x " + $"{PaperSizes.[f].Item1}")
