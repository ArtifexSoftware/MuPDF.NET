using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            this.DOC = page.PARENT;

            if (!DOC.IsPDF)
                throw new Exception("is no PDF");
            HEIGHT = page.MEDIABOX_SIZE.Y;
            WIDTH = page.MEDIABOX_SIZE.X;
            X = page.CROPBOX_POSITION.X;
            Y = page.CROPBOX_POSITION.Y;

            PCTM = page.transformationMatrix;
            IPCTM = ~page.transformationMatrix;

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
            string text = "";
            if (buffer is null)
                return 0;
            if (!(buffer is List<byte>) || !(buffer is Tuple<byte>))
                text = buffer.splitlines();
            else
                text = buffer;

            if (text.Length <= 0)
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
