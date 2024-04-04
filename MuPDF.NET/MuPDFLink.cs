using mupdf;

namespace MuPDF.NET
{
    public class MuPDFLink : IDisposable
    {

        public bool ThisOwn;

        private FzLink _nativeLink;

        public MuPDFPage Parent;

        public int Xref;

        public string Uri
        {
            get
            {
                return _nativeLink.m_internal != null ? _nativeLink.m_internal.uri : "";
            }
        }

        public string Id;

        public MuPDFLink(FzLink t) { _nativeLink = t; }

        public MuPDFLink() { }

        public Border Border
        {
            get
            {
                return _Border(Parent.Parent, Xref);
            }
        }

        public Color Colors
        {
            get { return _Colors(Parent.Parent, Xref); }
        }

        public LinkDest Dest
        {
            get
            {
                if (Parent is null)
                    throw new Exception("orphaned object: parent is None");
                if (Parent.Parent.IsClosed || Parent.Parent.IsEncrypted)
                    throw new Exception("document closed or encrypted");

                (dynamic, float, float) uri;
                if (IsExternal || Uri.StartsWith("#"))
                    uri = (null, 0, 0);
                else
                    uri = Parent.Parent.ResolveLink(Uri);

                return new LinkDest(this, uri, Parent.Parent);
            }
        }

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

        public bool IsExternal
        {
            get
            {
                if (_nativeLink == null || _nativeLink.m_internal.uri == null)
                    return false;
                return mupdf.mupdf.fz_is_external_link(_nativeLink.m_internal.uri) != 0;
            }
        }

        public MuPDFLink Next
        {
            get
            {
                if (_nativeLink.m_internal == null)
                    return null;

                FzLink fzVal = _nativeLink.next();
                if (fzVal == null)
                    return null;
                MuPDFLink val = new MuPDFLink(fzVal);
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
                        for (int i = 0; i < annotXrefs.Count; i++)
                        {
                            if (annotXrefs[i].AnnotType == PdfAnnotType.PDF_ANNOT_LINK)
                            {
                                linkXrefs.Add(annotXrefs[i].Xref);
                                linkIds.Add(annotXrefs[i].Id);
                            }
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

        public Rect Rect
        {
            get
            {
                if (_nativeLink == null || _nativeLink.m_internal == null)
                    throw new Exception("self.this.m_internal not available");
                return new Rect(new FzRect(_nativeLink.rect()));
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }

        private Border _Border(MuPDFDocument doc, int xref)
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(doc);
            if (pdf == null)
                return null;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                return null;

            Border b = Utils.GetAnnotBorder(linkObj);
            return b;
        }

        private Color _Colors(MuPDFDocument doc, int xref)
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(doc);
            if (pdf == null)
                return null;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                throw new Exception(Utils.ErrorMessages["MSG_BAD_XREF"]);

            return Utils.GetAnnotColors(linkObj);
        }

        public void Erase()
        {
            Parent = null;
            ThisOwn = false;
        }

        private void _SetBorder(Border border, MuPDFDocument doc, int xref)
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(doc);
            if (pdf == null)
                return;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                return;

            MuPDFAnnot.SetBorderAnnot(border, pdf, linkObj);
        }

        public void SetBorder(Border border, float width = 0, int[] dashes = null, string style = null)
        {
            if (border == null)
                border = new Border() { Width = width, Style = style, Dashes = dashes };

            _SetBorder(border, Parent.Parent, Xref);
        }

        public void SetColors(Color colors = null, float[] stroke = null, float[] fill = null)
        {
            MuPDFDocument doc = Parent.Parent;
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

        public void SetFlags(int flags)
        {
            MuPDFDocument doc = Parent.Parent;
            if (!doc.IsPDF)
                throw new Exception("is no PDF");
            doc.SetKeyXRef(Xref, "F", flags.ToString());
        }

        public void Dispose()
        {
            _nativeLink.Dispose();
        }
    }
}
