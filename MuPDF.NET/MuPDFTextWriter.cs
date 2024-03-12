using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        /*public (Rect, Point) Append(Point pos, string text, Font font, float fontSize = 11.0f, string language = null, int right2left = 0, int smallCaps = 0)
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

            }
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
                        
                    }
                }
            }
        }*/
    }
}