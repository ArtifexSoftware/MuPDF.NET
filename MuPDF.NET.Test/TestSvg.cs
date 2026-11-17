using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using MuPDF.NET;
namespace MuPDF.NET.Test
{
    [Collection("MuPDF.NET native")]
    public class TestSvg
    {
        private static string Resources =>
            Path.Combine(_Path.ResolveSolutionRoot(), "TestDocuments", "MuPDF.NET.Test", nameof(TestSvg));

        private static string Out(string fileName) => _Path.ForOutput(fileName, nameof(TestSvg));

        private static string Svg(string body)
        {
            return "<svg xmlns=\"http://www.w3.org/2000/svg\" "
                + "width=\"400\" height=\"200\" viewBox=\"0 0 400 200\">"
                + body
                + "</svg>";
        }

        [Fact]
        public void test_5118()
        {
            Console.WriteLine();
            string outDir = Path.GetDirectoryName(Out("placeholder.svg"))!;
            Console.WriteLine($"out={outDir}");
            Directory.CreateDirectory(outDir);

            string gradient = Svg(@"
      <defs>
        <linearGradient id=""bg"" x1=""0"" x2=""1"">
          <stop offset=""0"" stop-color=""#05f8ff""/>
          <stop offset=""1"" stop-color=""#eefbf6""/>
        </linearGradient>
      </defs>
      <rect x=""1"" y=""1"" width=""398"" height=""198""
            fill=""url(#bg)"" stroke=""#cbd6e4""/>
    ");

            const string textAttributes =
                "text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"20\"";
            var cases = new Dictionary<string, string>
            {
                ["gradient"] = gradient,
                ["gradient_percent_stops"] = gradient.Replace("offset=\"0\"", "offset=\"0%\"")
                    .Replace("offset=\"1\"", "offset=\"100%\""),
                ["gradient_percent_coordinates"] = gradient.Replace(
                    "x1=\"0\" x2=\"1\"", "x1=\"0%\" y1=\"0%\" x2=\"100%\" y2=\"0%\""),
                ["gradient_stop_style"] = gradient.Replace(
                    "stop-color=\"#f5f8ff\"", "style=\"stop-color:#f5f8ff\"")
                    .Replace("stop-color=\"#eefbf6\"", "style=\"stop-color:#eefbf6\""),
                ["gradient_user_space"] = gradient.Replace(
                    "x1=\"0\" x2=\"1\"",
                    "gradientUnits=\"userSpaceOnUse\" x1=\"1\" y1=\"1\" x2=\"399\" y2=\"1\""),
                ["solid_fill_control"] = gradient.Replace("url(#bg)", "#f5f8ff"),
                ["text_direct"] = Svg(
                    $"<text x=\"200\" y=\"60\" {textAttributes}>middle label</text>"),
                ["text_group"] = Svg(
                    $"<g {textAttributes}>"
                    + "<text x=\"200\" y=\"60\">middle label</text></g>"),
                ["text_inline"] = Svg(
                    "<text x=\"200\" y=\"60\" "
                    + "style=\"text-anchor:middle;font-family:Arial, sans-serif;font-size:20\">"
                    + "middle label</text>"),
                ["text_anchor_group_only"] = Svg(
                    "<g text-anchor=\"middle\">"
                    + "<text x=\"200\" y=\"60\" font-family=\"Arial, sans-serif\" font-size=\"20\">"
                    + "middle label</text></g>"),
                ["text_family_group_only"] = Svg(
                    "<g font-family=\"Arial, sans-serif\">"
                    + "<text x=\"200\" y=\"60\" text-anchor=\"middle\" font-size=\"20\">"
                    + "middle label</text></g>"),
                ["class_fill"] = Svg(
                    "<style>.c { fill: #ff0000; }</style>"
                    + "<rect class=\"c\" x=\"50\" y=\"50\" width=\"300\" height=\"100\"/>"),
                ["element_fill"] = Svg(
                    "<style>rect { fill: #ff0000; }</style>"
                    + "<rect x=\"50\" y=\"50\" width=\"300\" height=\"100\"/>"),
                ["inline_fill_control"] = Svg(
                    "<rect x=\"50\" y=\"50\" width=\"300\" height=\"100\" style=\"fill:#ff0000\"/>"),
            };
            cases["text_edge_direct"] = cases["text_direct"].Replace("x=\"200\"", "x=\"320\"");
            cases["text_edge_group"] = cases["text_anchor_group_only"].Replace("x=\"200\"", "x=\"320\"");

            var pngNames = new List<string>();

            foreach (var kv in cases)
            {
                string name = kv.Key;
                byte[] svgBytes = Encoding.UTF8.GetBytes(kv.Value);
                File.WriteAllBytes(Out($"{name}.svg"), svgBytes);
                using var doc = new Document(svgBytes, "svg");
                var page = doc[0];
                using var pix = page.GetPixmap(alpha: false);
                string pngName = $"{name}.png";
                pix.Save(Out(pngName));
                pngNames.Add(pngName);
                Console.WriteLine($"{name} center RGB {FormatPixel(pix.GetPixel(200, 100))}");
                var dict = (Dictionary<string, object>)page.GetText("dict");
                foreach (var block in (List<Dictionary<string, object>>)dict["blocks"])
                {
                    if (Convert.ToInt32(block["type"]) != 0)
                        continue;
                    foreach (var line in (List<Dictionary<string, object>>)block["lines"])
                    {
                        foreach (var span in (List<Dictionary<string, object>>)line["spans"])
                        {
                            Console.WriteLine(
                                "  text " + span["text"]
                                + " font " + span["font"]
                                + " size " + span["size"]
                                + " origin " + FormatOrigin(span["origin"]));
                        }
                    }
                }
            }

            // Check.png files.
            int numErrors = 0;
            foreach (string pngName in pngNames)
            {
                string expected = Path.Combine(Resources, $"test_5118_expected_{pngName}");
                string actual = Out(pngName);
                float rms;
                try
                {
                    rms = _Compare.PixmapsRms(expected, actual);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"{pngName}: {ex.Message}");
                    numErrors += 1;
                    continue;
                }
                Console.WriteLine($"png_name={pngName}: rms={rms}");
                if (rms > 1)
                    numErrors += 1;
            }

            // Confirm the same problems through Markdown-to-PDF conversion.
            foreach (string name in new[] { "gradient", "text_direct", "text_group", "class_fill" })
            {
                string markdownPath = Out($"{name}.md");
                File.WriteAllText(markdownPath, $"![SVG example]({name}.svg)\n", Encoding.UTF8);
                using var archive = new Archive(outDir);
                using var doc = new Document(
                    markdownPath,
                    rect: Utils.PaperRect("A4"),
                    archive: archive);
                doc.EzSave(Out($"{name}_from_markdown.pdf"));
            }

            Console.WriteLine($"num_errors={numErrors}");

            if (_Version.mupdf_version_tuple_at_least(1, 29, 0))
                Assert.True(numErrors == 0, $"num_errors={numErrors}");
            else
            {
                // 2026-10-09.
                Assert.NotEqual(0, numErrors);
            }
        }

        private static string FormatPixel(byte[] pixel)
        {
            if (pixel == null)
                return "";
            return "(" + string.Join(", ", pixel) + ")";
        }

        private static string FormatOrigin(object origin)
        {
            if (origin is Point p)
                return $"({Math.Round(p.X, 2)}, {Math.Round(p.Y, 2)})";
            if (origin is float[] f && f.Length >= 2)
                return $"({Math.Round(f[0], 2)}, {Math.Round(f[1], 2)})";
            return origin?.ToString() ?? "";
        }
    }
}
