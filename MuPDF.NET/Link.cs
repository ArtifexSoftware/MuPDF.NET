using mupdf;

namespace MuPDF.NET
{
    public class Link
    {
        static Link()
        {
            Utils.InitApp();
        }

        public bool ThisOwn { get; set; }

        private FzLink _nativeLink;

        public Page Parent { get; set; }

        public int Xref { get; set; }

        public int Page { get; set; }

        public string Uri
        {
            get
            { 
                return _nativeLink.m_internal != null ? _nativeLink.m_internal.uri : "";
            }
        }

        public string Id { get; set; }

        public Link(FzLink link)
        {
            _nativeLink = link;
        }

        public Link() { }

        public Border Border
        {
            get { return _Border(Parent.Parent, Xref); }
        }

        /// <summary>
        /// Meaningful for PDF only: A dictionary of two tuples of floats in range 0 <= float <= 1 specifying the stroke and the interior (fill) colors. If not a PDF, null is returned. As mentioned above, the fill color is always None for links. The stroke color is used for the border of the link rectangle. The length of the tuple implicitly determines the colorspace: 1 = GRAY, 3 = RGB, 4 = CMYK. So (1.0, 0.0, 0.0) stands for RGB color red. The value of each float f is mapped to the integer value i in range 0 to 255 via the computation f = i / 255.
        /// </summary>
        public Color Colors
        {
            get { return _Colors(Parent.Parent, Xref); }
        }

        /// <summary>
        /// The link destination details object.
        /// </summary>
        public LinkDest Dest
        {
            get
            {
                if (Parent is null)
                    throw new Exception("orphaned object: parent is None");
                if (Parent.Parent.IsClosed || Parent.Parent.IsEncrypted)
                    throw new Exception("document closed or encrypted");

                (List<int>, float, float) uri;
                if (IsExternal || Uri.StartsWith("#"))
                    uri = (null, 0, 0);
                else
                    uri = Parent.Parent.ResolveLink(Uri);
                return new LinkDest(this, uri, Parent.Parent);
            }
        }

        /// <summary>
        /// Return the link annotation flags, an integer (see Annot.flags for details). Zero if not a PDF.
        /// </summary>
        public int Flags
        {
            get
            {
                if (!Parent.Parent.IsPDF)
                    return 0;
                (string, string) f = Parent.Parent.GetKeyXref(Xref, "F");
                if (f.Item2 != "null")
                    return int.Parse(f.Item2);

                return 0;
            }
        }

        /// <summary>
        /// A bool specifying whether the link target is outside of the current document.
        /// </summary>
        public bool IsExternal
        {
            get
            {
                if (
                    _nativeLink.m_internal == null
                    || string.IsNullOrEmpty(_nativeLink.m_internal.uri)
                )
                    return false;
                
                return mupdf.mupdf.fz_is_external_link(_nativeLink.m_internal.uri) != 0;
            }
        }

        public Link Next
        {
            get
            {
                if (_nativeLink.m_internal == null)
                    return null;

                FzLink fzVal = _nativeLink.next();
                if (fzVal.m_internal == null)
                    return null;

                Link val = new Link(fzVal);
                if (val != null)
                {
                    val.ThisOwn = true;
                    val.Parent = Parent;
                    val.Parent.AnnotRefs.Add(val.GetHashCode(), val);
                    if (Xref > 0)
                    {
                        List<int> linkXrefs = new List<int>();
                        List<string> linkIds = new List<string>();
                        List<AnnotXref> annotXrefs = Parent.GetAnnotXrefs();
                        int i = 0;
                        for (i = 0; i < annotXrefs.Count; i++)
                        {
                            if (annotXrefs[i].AnnotType == PdfAnnotType.PDF_ANNOT_LINK)
                            {
                                linkXrefs.Add(annotXrefs[i].Xref);
                                linkIds.Add(annotXrefs[i].Id);
                            }
                        }

                        for (i = 0; i < annotXrefs.Count; i++)
                        {
                            int idx = linkXrefs.IndexOf(Xref);
                            val.Xref = linkXrefs[idx + 1];
                            val.Id = linkIds[idx + 1];
                        }
                    }
                    else
                    {
                        Xref = 0;
                        Id = "";
                    }
                }
                return val;
            }
        }

        /// <summary>
        /// The area that can be clicked in untransformed coordinates.
        /// </summary>
        public Rect Rect
        {
            get
            {
                if (_nativeLink == null || _nativeLink.m_internal == null)
                    throw new Exception("FzLink.m_internal not available");
                
                return new Rect(new FzRect(_nativeLink.rect()));
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }

        private Border _Border(Document doc, int xref)
        {
            PdfDocument pdf = Document.AsPdfDocument(doc);
            if (pdf.m_internal == null)
                return null;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                return null;

            Border b = Utils.GetAnnotBorder(linkObj);
            
            return b;
        }

        private Color _Colors(Document doc, int xref)
        {
            PdfDocument pdf = Document.AsPdfDocument(doc);
            if (pdf.m_internal == null)
                return null;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);

            return Utils.GetAnnotColors(linkObj);
        }

        /// <summary>
        ///
        /// </summary>
        public void Erase()
        {
            Parent = null;
            ThisOwn = false;
        }

        private void _SetBorder(Border border, Document doc, int xref)
        {
            PdfDocument pdf = Document.AsPdfDocument(doc);
            if (pdf.m_internal == null)
                return;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                return;

            Annot.SetBorderAnnot(border, pdf, linkObj);
        }

        /// <summary>
        /// PDF only: Change border width and dashing properties.
        /// </summary>
        /// <param name="border">a dictionary as returned by the border property, with keys “width” (float), “style” (str) and “dashes” (sequence). Omitted keys will leave the resp. property unchanged.</param>
        /// <param name="width"></param>
        /// <param name="dashes"></param>
        /// <param name="style"></param>
        public void SetBorder(
            Border border,
            float width = 0,
            int[] dashes = null,
            string style = null
        )
        {
            if (border == null)
                border = new Border()
                {
                    Width = width,
                    Style = style,
                    Dashes = dashes
                };

            _SetBorder(border, Parent.Parent, Xref);
        }

        /// <summary>
        /// PDF only: Changes the “stroke” color.
        /// </summary>
        /// <param name="colors">a dictionary containing color specifications. For accepted dictionary keys and values see below. The most practical way should be to first make a copy of the colors property and then modify this dictionary as required.</param>
        /// <param name="stroke"></param>
        /// <param name="fill"></param>
        public void SetColors(Color colors = null, float[] stroke = null, float[] fill = null)
        {
            Document doc = Parent.Parent;
            if (colors == null)
                colors = new Color() { Fill = fill, Stroke = stroke };

            fill = colors.Fill;
            stroke = colors.Stroke;
            if (fill != null)
                Console.WriteLine("warning: links have no fill color");
            if (stroke == null || stroke.Length == 0)
            {
                doc.SetKeyXRef(Xref, "C", "[]");
                return;
            }

            Utils.CheckColor(stroke);
            string s = "";
            if (stroke.Length == 1)
                s = $"[{stroke[0]}]";
            else if (stroke.Length == 3)
                s = $"[{stroke[0]} {stroke[1]} {stroke[2]}]";
            else
                s = $"[{stroke[0]} {stroke[1]} {stroke[2]} {stroke[3]}]";

            doc.SetKeyXRef(Xref, "C", s);
        }

        /// <summary>
        /// Set the PDF /F property of the link annotation.
        /// </summary>
        /// <param name="flags"></param>
        /// <exception cref="Exception"></exception>
        public void SetFlags(int flags)
        {
            Document doc = Parent.Parent;
            if (!doc.IsPDF)
                throw new Exception("is no PDF");
            doc.SetKeyXRef(Xref, "F", flags.ToString());
        }
    }
}
