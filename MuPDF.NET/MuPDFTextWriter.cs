using mupdf;
using System.Runtime.InteropServices;
using System.Text;

namespace MuPDF.NET
{
    public class MuPDFTextWriter
    {

        private FzText _nativeText;

        public float Opacity;

        public Rect Rect;

        public Matrix Ctm;

        public Matrix ICtm;

        public Point LastPoint;

        public Rect TextRect;

        public List<MuPDFFont> UsedFonts;

        public bool ThisOwn;

        public float[] Color;

        public MuPDFTextWriter(Rect pageRect, float opacity = 1, float[] color = null)
        {
            _nativeText = mupdf.mupdf.fz_new_text();
            Opacity = opacity;
            Rect = pageRect;
            Ctm = new Matrix(1, 01, 0, -1, 0, Rect.Height);
            ICtm = ~Ctm;
            LastPoint = new Point();
            TextRect = new Rect();
            UsedFonts = new List<MuPDFFont>();
            Color = color;
            ThisOwn = false;
        }

        public Rect Bbox
        {
            get
            {
                Rect val = new Rect(mupdf.mupdf.fz_bound_text(_nativeText, new FzStrokeState(), new FzMatrix()));
                return val;
            }
        }

        public (Rect, Point) Append(Point pos, string text, MuPDFFont font, float fontSize = 11.0f, string language = null, int right2left = 0, int smallCaps = 0)
        {
            pos = pos * ICtm;
            if (font == null)
            {
                font = new MuPDFFont("helv");
            }

            if (!font.IsWriteable)
            {
                throw new Exception($"Unsupported font {font.Name}");
            }

            if (right2left != 0)
            {
                text = CleanRtl(text);
                text = String.Join("", text.Reverse().ToArray());
                right2left = 0;
            }

            fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
            FzMatrix trm = mupdf.mupdf.fz_make_matrix(fontSize, 0, 0, fontSize, pos.X, pos.Y);
            int markupDir = 0;
            int wmode = 0;
            if (smallCaps == 0)
                trm = _nativeText.fz_show_string(font.ToFzFont(), trm, text, wmode, right2left, (fz_bidi_direction)markupDir, lang);
            else
                trm = Utils.ShowStringCS(_nativeText, font, trm, text, wmode, (fz_bidi_direction)right2left, markupDir, lang);

            LastPoint = new Point(trm.e, trm.f) * Ctm;
            TextRect = Bbox * Ctm;
            (Rect, Point) ret = (TextRect, LastPoint);
            if (font.Flags["mono"] == 1)
                UsedFonts.Add(font);

            return ret;
        }

        public string CleanRtl(string text)
        {
            if (text == null || text == "")
                return null;
            string[] words = text.Split(" ");
            List<int> idx = new List<int>();
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];

                if (!(w.Length < 2 || w.ToCharArray().Any(c => (int)c > 255)))
                {
                    words[i] = w.ToCharArray().Reverse().ToString();
                    idx.Add(i);
                }
            }

            List<int> idx2 = new List<int>();
            foreach (int i in idx)
            {
                if (idx2.Count == 0)
                    idx2.Add(idx[i]);
                else if (idx[i] > idx2[idx2.Count - 1] + 1)
                {
                    if (idx2.Count > 1)
                    {
                        string[] part = words.Skip(idx2[0]).Take(idx2[idx2.Count - 1] + 1 - idx2[0]).ToArray();
                        part.Reverse();
                        for (int j = 0; j < part.Length; j++)
                            words[j + idx2[0]] = part[j];
                    }
                    idx2 = new List<int>() { idx[i] };
                }
                else if (idx[i] == idx2[idx2.Count - 1] + 1)
                    idx2.Add(idx[i]);
            }

            text = string.Join(" ", words);
            return text;
        }

        public (Rect, Point) Appendv(Point pos, string text, MuPDFFont font = null, float fontSiz = 11.0f,
            string language = null, bool smallCaps = false)
        {
            float lheight = fontSiz * 1.2f;
            foreach (char c in text)
            {
                Append(pos, c.ToString(), font: font, fontSize: fontSiz, language: language, smallCaps: smallCaps ? 1 : 0);
                pos.Y += lheight;
            }
            return (TextRect, LastPoint);
        }

        public void WriteText(MuPDFPage page, float[] color = null, float opacity = -1, int overlay = 1, Morph morph = null,
            Matrix matrix = null, int renderMode = 0, int oc = 0)
        {
            if ((Rect - page.Rect).Abs() > 1e-3)
                throw new Exception("incompatible page rect");
            if (matrix != null && morph != null)
                throw new Exception("only one of matrix, m");
            if (opacity == -1)
                opacity = Opacity;
            if (color == null)
                color = Color;

            PdfPage pdfPage = page.GetPdfPage();
            float alpha = 1;
            FzColorspace colorSpace;

            if (opacity >= 0 && opacity <= 1)
                alpha = opacity;
            int nCol = 1;
            float[] devColor = { 0, 0, 0, 0 };
            if (color != null)
                devColor = MuPDFAnnot.ColorFromSequence(color);
            if (devColor.Length == 3)
                colorSpace = mupdf.mupdf.fz_device_rgb();
            else if (devColor.Length == 4)
                colorSpace = mupdf.mupdf.fz_device_cmyk();
            else
                colorSpace = mupdf.mupdf.fz_device_gray();

            PdfObj resources = pdfPage.doc().pdf_new_dict(5);
            FzBuffer contents = mupdf.mupdf.fz_new_buffer(1024);
            FzDevice dev = mupdf.mupdf.pdf_new_pdf_device(pdfPage.doc(), new FzMatrix(), resources, contents);

            IntPtr pDevColor = Marshal.AllocHGlobal(devColor.Length * sizeof(float));
            Marshal.Copy(devColor, 0, pDevColor, devColor.Length);
            SWIGTYPE_p_float swigDevColor = new SWIGTYPE_p_float(pDevColor, true);

            dev.fz_fill_text(_nativeText, new FzMatrix(), colorSpace, swigDevColor, alpha, new FzColorParams(mupdf.mupdf.fz_default_color_params));
            dev.fz_close_device();

            (int, int) maxNums = Utils.MergeResources(pdfPage, resources);
            string cont = Utils.EscapeStrFromBuffer(contents);
            (int maxAlp, int maxFont) = maxNums;
            string[] oldLines = cont.Split('\n');

            string optCont = page.GetOptionalContent(oc);
            string bdc = "";
            string emc = "";
            if (!string.IsNullOrEmpty(optCont))
            {
                bdc = $"/OC /{optCont} BDC";
                emc = "EMC";
            }
            List<string> newContLines = new List<string>();
            if (string.IsNullOrEmpty(bdc))
                newContLines.Add(bdc);

            Point cb = page.CropBoxPosition;
            float delta = 0;
            if (page.Rotation == 90 || page.Rotation == 270)
                delta = page.Rect.Height - page.Rect.Width;
            Rect mb = page.MediaBox;
            if (cb != null || mb.Y0 != 0 || delta != 0)
                newContLines.Add($"1 0 0 1 {cb.X} {cb.Y + mb.Y0 - delta} cm");

            Matrix matrix_ = new Matrix();
            if (morph != null)
            {
                Point p = morph.P * ICtm;
                Matrix matrixDelta = (new Matrix(1, 1)).Pretranslate(p.X, p.Y);
                matrix_ = ~matrixDelta * morph.M * matrixDelta;
            }

            if (morph != null || matrix != null)
                newContLines.Add($"{matrix_.A} {matrix_.B} {matrix_.C} {matrix_.D} {matrix_.E} {matrix_.F}");
            foreach (string line in newContLines)
            {
                string line_ = line;
                if (line_.EndsWith(" cm"))
                    continue;
                if (line_ == "BT")
                {
                    newContLines.Add(line_);
                    newContLines.Add($"{renderMode} Tr");
                    continue;
                }
                if (line_.EndsWith(" gs"))
                {
                    int alp = Convert.ToInt32(line_.Split(" ")[0].Substring(4)) + maxAlp;
                    line_ = $"/Alp{alp} gs";
                }
                else if (line_.EndsWith(" Tf"))
                {
                    string[] temp = line_.Split(" ");
                    float fSize = (float)Convert.ToDouble(temp[1]);
                    float w = 1;
                    if (renderMode != 0)
                        w = fSize * 0.05f;
                    newContLines.Add($"{w} w");
                    int font = Convert.ToInt32(temp[0].Substring(2)) + maxFont;
                    line_ = string.Join(" ", (new List<string>() { $"/F{font}" }).Concat(temp.Skip(1)));
                }
                else if (line_.EndsWith(" rg"))
                    newContLines.Add(line_.Replace("rg", "RG"));
                else if (line_.EndsWith(" g"))
                    newContLines.Add(line_.Replace(" g", " G"));
                else if (line_.EndsWith(" k"))
                    newContLines.Add(line_.Replace(" k", " K"));
                newContLines.Add(line_);
            }
            if (string.IsNullOrEmpty(emc))
                newContLines.Add(emc);
            newContLines.Add("Q\n");
            byte[] content = Encoding.UTF8.GetBytes(string.Join("\n", newContLines));
            Utils.InsertContents(page, content, overlay);
            foreach (MuPDFFont font in UsedFonts)
                Utils.RepairMonoFont(page, font);
        }
    }
}