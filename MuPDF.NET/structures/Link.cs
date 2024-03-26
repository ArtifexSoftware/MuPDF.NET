using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class Link : IDisposable
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

        public Link(FzLink t) { _nativeLink = t; }

        public BorderStruct? Border
        {
            get
            {
                return _Border(Parent.Parent, Xref);
            }
        }

        public ColorStruct? Colors
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

        public Link Next
        {
            get
            {
                if (_nativeLink != null)
                    return null;

                FzLink fzVal = _nativeLink.next();
                if (fzVal == null)
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
                        List<(int, pdf_annot_type, string)> annotXrefs = Parent.GetAnnotXrefs();
                        for (int i = 0; i < annotXrefs.Count; i++)
                        {
                            if (annotXrefs[i].Item2 == pdf_annot_type.PDF_ANNOT_LINK)
                            {
                                linkXrefs.Add(annotXrefs[i].Item1);
                                linkIds.Add(annotXrefs[i].Item3);
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

        private BorderStruct? _Border(MuPDFDocument doc, int xref)
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(doc);
            if (pdf == null)
                return null;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                return null;

            BorderStruct b = Utils.GetAnnotBorder(linkObj);
            return b;
        }

        private ColorStruct? _Colors(MuPDFDocument doc, int xref)
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

        private void _SetBorder(BorderStruct border, MuPDFDocument doc, int xref)
        {
            PdfDocument pdf = MuPDFDocument.AsPdfDocument(doc);
            if (pdf == null)
                return;
            PdfObj linkObj = pdf.pdf_new_indirect(xref, 0);
            if (linkObj == null)
                return;

            MuPDFAnnotation.SetBorderAnnot(border, pdf, linkObj);
        }

        public dynamic SetBorder(dynamic border, float width = 0, int[] dashes = null, string style = null)
        {
            if (!(border is BorderStruct))
                border = new BorderStruct() { Width = width, Style = style, Dashes = dashes };
            return _SetBorder(border, Parent.Parent, Xref);
        }

        public void SetColors(dynamic colors = null, float[] stroke = null, float[] fill = null)
        {
            MuPDFDocument doc = Parent.Parent;
            if (!(colors is ColorStruct))
                colors = new ColorStruct() { Fill = fill, Stroke = stroke };

            fill = colors.Fill;
            stroke = colors.Stroke;
            if (fill != null)
                Console.WriteLine("warning: links have no fill color");
            if (stroke is float[] || stroke is Tuple || stroke is List<float>)
            {
                doc.SetKeyXRef(Xref, "C", "[]");
                return;
            }

            Utils.CheckColor(stroke);
            string s = null;
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
