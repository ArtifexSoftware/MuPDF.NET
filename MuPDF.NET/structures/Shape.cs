using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Maui.Layouts;
using mupdf;
using MuPDF.NET;

namespace MuPDF.NET
{
    public class Shape
    {
        public MuPDFPage Page;

        public MuPDFDocument Doc;

        public float Height;

        public float Width;

        public float X;

        public float Y;

        public Matrix Pctm;

        public Matrix IPctm;

        public string DrawCont;

        public string TextCont;

        public string TotalCont;

        public Point LastPoint;

        public Rect Rect;

        public Shape(MuPDFPage page)
        {
            this.Page = page;
            this.Doc = page.Parent;

            if (!Doc.IsPDF)
                throw new Exception("is no PDF");
            Height = page.MediaBoxSize.Y;
            Width = page.MediaBoxSize.X;
            X = page.CropBoxPosition.X;
            Y = page.CropBoxPosition.Y;

            Pctm = page.TransformationMatrix;
            IPctm = ~page.TransformationMatrix;

            DrawCont = "";
            TextCont = "";
            TotalCont = "";
            LastPoint = null;
            Rect = null;
        }


        /// <summary>
        /// Insert text lines
        /// </summary>
        /// <param name="point">the bottom-left position of the first character of text in pixels</param>
        /// <param name="buffer"></param>
        /// <param name="fontSize"></param>
        /// <param name="lineHeight"></param>
        /// <param name="fontName"></param>
        /// <param name="fontFile"></param>
        /// <param name="setSimple"></param>
        /// <param name="encoding"></param>
        /// <param name="color"></param>
        /// <param name="fill"></param>
        /// <param name="renderMode"></param>
        /// <param name="borderWidth"></param>
        /// <param name="rotate"></param>
        /// <param name="morph"></param>
        /// <param name="strokeOpacity">set transparency for stroke colors (the border line of a character). Only 0 <= value <= 1 will be considered. Default is 1</param>
        /// <param name="fillOpacity">set transparency for fill colors. Default is 1</param>
        /// <param name="oc">the xref number of an OCG or OCMD to make this text conditionally displayable</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
            MorphStruct morph = new MorphStruct(),
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            List<string> text = new List<string>();
            if (buffer is null)
                return 0;
            if (!(buffer is List<string>) || !(buffer is Tuple<string>))
                text = new List<string>(Convert.ToString(buffer).Split("\n"));
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
            catch (Exception)
            {
                return 0;
            }

            string fName = fontName;
            if (fName.StartsWith("/"))
                fName = fName.Substring(1);

            int xref = Page.InsertFont(
                fontName: fName,
                fontFile: fontFile,
                encoding: encoding,
                setSimple: setSimple
                );

            FontStruct fontInfo = Utils.CheckFontInfo(Doc, xref);

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
                glyphs = Utils.GetCharWidths(Doc, xref: xref, limit: maxCode + 1);
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

            bool morphing = Utils.CheckMorph(morph);
            int rot = rotate;
            if (rot % 90 != 0)
                throw new Exception("bad rotate value");

            while (rot < 0)
                rot += 360;
            rot = rot % 360;

            string cmp90 = "0 1 -1 0 0 0 cm\n";// rotates 90 deg counter-clockwise
            string cmm90 = "0 -1 1 0 0 0 cm\n";// rotates 90 deg clockwise
            string cm180 = "-1 0 0 -1 0 0 cm\n";// rotates by 180 deg.
            float height = Height;
            float width = Width;
            string cm = "";

            if (morphing)
            {
                Matrix m1 = new Matrix(1, 0, 0, 1, morph.FixPoint.X + X, height - morph.FixPoint.Y - Y);
                Matrix mat = ~m1 * morph.Matrix * m1;
                cm = $"{mat.A} {mat.B} {mat.C} {mat.D} {mat.E} {mat.F} cm\n";
            }
            else cm = "";

            float top = height - point.Y - Y;
            float left = point.X + X;
            float space = top;
            if (rot == 90)
            {
                left = height - point.Y - Y;
                top = -point.X - X;
                cm += cmp90;
                space = width - Math.Abs(top);
            }
            else if (rot == 270)
            {
                left = -height + point.Y + Y;
                top = point.X + X;
                cm += cmm90;
                space = Math.Abs(top);
            }
            else if (rot == 180)
            {
                left = -point.X - X;
                top = -height + point.Y + Y;
                cm += cm180;
                space = Math.Abs(point.Y + Y);
            }

            string optCont = Page.GetOptionalContent(oc);
            string bdc;
            string emc;
            if (optCont != null)
            {
                bdc = $"/OC /{optCont} BDC\n";
                emc = "EMC\n";
            }
            else bdc = emc = "";

            string alpha = Page.SetOpacity(CA: strokeOpacity, ca: fillOpacity);
            if (alpha == null)
                alpha = "";
            else
                alpha = $"/{alpha} gs\n";

            string nres = $"\nq\n{bdc}{alpha}BT\n{cm}1 0 0 1 {left} {top} Tm\n/{fName} {fontSize} Tf ";
            if (renderMode > 0)
                nres += $"{renderMode} Tr ";
            if (borderWidth != 1)
                nres += $"{borderWidth} w ";
            if (color != null)
                nres += colorStr;
            if (fill != null)
                nres += fillStr;

            nres += text[0];
            int nLines = 1;
            if (text.Count > 1)
                nres += $"TJ\n0 -{lheight} TD\n";
            else nres += "TJ";

            for (int i = 1; i < text.Count; i ++)
            {
                if (space < lheight)
                    break;
                if (i > 1)
                    nres += "\nT* ";
                nres += text[i] + "TJ";
                space -= lheight;
                nLines += 1;
            }

            nres += $"\nET\n{emc}Q\n";

            TextCont += nres;
            return nLines;
        }

        public void Commit(int overlay)
        {
            TotalCont += this.TextCont;
            byte[] bTotal = Encoding.UTF8.GetBytes(TotalCont);
            if (TotalCont != "")
            {
                int xref = Utils.InsertContents(Page, bTotal, overlay);
                /*mupdf.mupdf.pdf_update_stream(Doc, xref, TotalCont);//issue*/
            }

            LastPoint = null;
            Rect = null;
            DrawCont = "";
            TextCont = "";
            TotalCont = "";
            return;
        }
    }
}
