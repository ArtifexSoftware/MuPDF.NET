using mupdf;

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

        public List<Font> UsedFonts;

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
            UsedFonts = new List<Font>();
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

        public (Rect, Point) Append(Point pos, string text, Font font, float fontSize = 11.0f, string language = null, int right2left = 0, int smallCaps = 0)
        {
            pos = pos * ICtm;
            if (font == null)
            {
                font = new Font("helv");
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

        public (Rect, Point) Appendv(Point pos, string text, Font font = null, float fontSiz = 11.0f,
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

        /*public void WriteText(MuPDFPage page, float[] color = null, float opacity = -1, float overlay = 1, Morph morph = null,
            Matrix matrix = null, int renderMode = 0, int oc = 0)
        {
            if ((Rect - page.Rect).Abs() > 1e-3)
                throw new Exception("incompatible page rect");

            if (opacity == -1)
                opacity = this.Opacity;
            if (color == null)
                color = this.Color;

            PdfPage pdfpage = page.GetPdfPage();
            FzColorspace colorSpace;

            float alpha = 1;
            if (opacity > 0 && opacity < 1)
                alpha = opacity;
            int nCol = 1;
            float[] devColor = new float[] { 0, 0, 0, 0 };
            if (color != null)
                devColor = MuPDFAnnotation.ColorFromSequence(color);
            if (devColor != null && devColor.Length == 3)
                colorSpace = mupdf.mupdf.fz_device_rgb();
            else if (devColor != null && devColor.Length == 4)
                colorSpace = mupdf.mupdf.fz_device_cmyk();
            else
                colorSpace = mupdf.mupdf.fz_device_gray();

            PdfObj resources = pdfpage.doc().pdf_new_dict(5);
            FzBuffer contents = mupdf.mupdf.fz_new_buffer(1024);
            FzDevice dev = pdfpage.doc().pdf_new_pdf_device(new FzMatrix(), resources, contents);

            IntPtr p_devColor = Marshal.AllocHGlobal(devColor.Length);
            Marshal.Copy(devColor, 0, p_devColor, devColor.Length);
            SWIGTYPE_p_float swig_devColor = new SWIGTYPE_p_float(p_devColor, true);
            mupdf.mupdf.fz_fill_text(dev, _nativeText, new FzMatrix(), colorSpace, swig_devColor, alpha, new FzColorParams(mupdf.mupdf.fz_default_color_params));
            mupdf.mupdf.fz_close_device(dev);

            int maxNums = Utils.MergeResources()

        }*/
    }
}