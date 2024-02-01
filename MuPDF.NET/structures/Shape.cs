using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mupdf;
using MuPDF.NET;

namespace MuPDF.NET
{
    public class Shape
    {
        public MuPDFPage PAGE;

        public MuPDFDocument DOC;

        public float HEIGHT;

        public float WIDTH;

        public float X;

        public float Y;

        public Matrix PCTM;

        public Matrix IPCTM;

        public string DRAWCONT;

        public string TEXTCONT;

        public string TOTALCONT;

        public Point LASTPOINT;

        public Rect RECT;

        public Shape(MuPDFPage page)
        {
            this.PAGE = page;
            this.DOC = page.Parent;

            if (!DOC.IsPDF)
                throw new Exception("is no PDF");
            HEIGHT = page.MediaBoxSize.Y;
            WIDTH = page.MediaBoxSize.X;
            X = page.CropBoxPosition.X;
            Y = page.CropBoxPosition.Y;

            PCTM = page.TransformationMatrix;
            IPCTM = ~page.TransformationMatrix;

            DRAWCONT = "";
            TEXTCONT = "";
            TOTALCONT = "";
            LASTPOINT = null;
            RECT = null;
        }



        public int InsertText(
            Point point,
            dynamic buffer,
            float fontSize = 11,
            float lineHeight = 0,
            string fontName = "helv",
            string fontFile = null,
            bool setSimple = false,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int renderMode = 0,
            float borderWidth = 0.05f,
            int rotate = 0,
            float[] morph = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            List<string> text = null;
            if (buffer is null)
                return 0;
            if (!(buffer is List<string>) || !(buffer is Tuple<string>))
                text = Convert.ToString(buffer).Split("\n");
            else
                text = buffer;

            if (text.Count <= 0)
                return 0;

            FzPoint p = point.ToFzPoint();
            int maxCode = 0;
            try
            {
                foreach (char c in String.Join(" ", text))
                {
                    if (maxCode < Convert.ToInt32(c))
                        maxCode = Convert.ToInt32(c);
                }
            }
            catch (Exception e)
            {
                return 0;
            }

            string fName = fontName;
            if (fName.StartsWith("/"))
                fName = fName.Substring(1);

            int xref = PAGE.InsertFont(
                fontName: fName,
                fontFile: fontFile,
                encoding: encoding,
                setSimple: setSimple
                );

            FontStruct fontInfo = Utils.CheckFontInfo(DOC, xref);

            int ordering = fontInfo.Ordering;
            bool simple = fontInfo.Simple;
            string bfName = fontInfo.Name;
            float ascender = fontInfo.Ascender;
            float descender = fontInfo.Descender;
            float lheight = 0;

            if (lineHeight != 0)
                lineHeight = fontSize * lineHeight;
            else if (ascender - descender <= 1)
                lheight = fontSize * 1.2f;
            else
                lheight = fontSize * (ascender - descender);

            List<(int, double)> glyphs = new List<(int, double)>();
            if (maxCode > 255)
                glyphs = Utils.GetCharWidths(DOC, xref: xref, limit: maxCode + 1);
            else
                glyphs = fontInfo.Glyphs;

            List<string> tab = new List<string>();
            List<(int, double)> g = null;
            foreach (string t in text)
            {
                if (simple && (bfName != "Symbol" || bfName != "ZapfDingbats"))
                    g = null;
                else
                    g = new List<(int, double)>(glyphs);
                tab.Add(Utils.GetTJstr(t, g, simple, ordering));
            }

            text = tab;

            string colorStr = Utils.GetColorCode(color, "c");
            string fillStr = Utils.GetColorCode(fill, "f");
            if (fill == null && renderMode == 0)
            {
                fill = color;
                fillStr = Utils.GetColorCode(color, "f");
            }

            bool morphing = Utils.CheckMorph(new List<float>(morph));

        }

        public void Commit(int overlay)
        {
            TOTALCONT += this.TEXTCONT;
            byte[] bTotal = Encoding.UTF8.GetBytes(TOTALCONT);
            if (TOTALCONT != "")
            {
                int xref = Utils.InsertContents(PAGE, bTotal, overlay);
                mupdf.mupdf.pdf_update_stream(DOC.PDFDOCUMENT, xref, TOTALCONT);//issue
            }

            LASTPOINT = null;
            RECT = null;
            DRAWCONT = "";
            TEXTCONT = "";
            TOTALCONT = "";
            return;
        }
    }
}
