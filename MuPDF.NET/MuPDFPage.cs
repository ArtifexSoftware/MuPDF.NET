using mupdf;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;

namespace MuPDF.NET
{
    public class MuPDFPage : IDisposable
    {
        private FzPage _nativePage;

        private PdfPage _pdfPage;

        public MuPDFDocument Parent { get; set; }

        private static FitResult fit;

        private List<Block> _imageInfo = null;

        public PdfObj PageObj
        {
            get
            {
                return _pdfPage.obj();
            }
        }

        public int Xref
        {
            get
            {
                return Parent.PageXref(Number);
            }
        }

        public int Number { get; set; }

        public bool ThisOwn { get; set; }

        public bool WasWrapped { get; set; }

        public Point MediaBoxSize
        {
            get
            {
                return new Point(MediaBox.X1, MediaBox.Y1);
            }
        }

        public bool IsWrapped
        {
            get
            {
                if (WasWrapped)
                    return true;

                byte[] cont = ReadContents();
                if (cont.Length == 0)
                {
                    WasWrapped = true;
                    return true;
                }
                if (cont[0] != Convert.ToByte('q') || cont.Last() != Convert.ToByte('Q'))
                    return false;
                WasWrapped = true;
                return true;
            }
        }

        public Point CropBoxPosition
        {
            get
            {
                return CropBox.TopLeft;
            }
        }

        public Rect Rect
        {
            get
            {
                return GetBound();
            }
        }

        public int Rotation
        {
            get
            {
                if (_pdfPage == null)
                    return 0;
                return Utils.PageRotation(_pdfPage);
            }
        }

        public MuPDFLink FristLink
        {
            get
            {
                return LoadLinks();
            }
        }

        public Dictionary<int, dynamic> AnnotRefs = new Dictionary<int, dynamic>();

        public Rect GetBound()
        {
            FzPage page = _pdfPage.super();
            Rect val = new Rect(page.fz_bound_page());

            if (val.IsInfinite && Parent.IsPDF)
            {
                Rect cb = CropBox;
                float w = cb.Width;
                float h = cb.Height;
                if (Rotation != 0 || Rotation != 180)
                    (w, h) = (h, w);
                val = new Rect(0, 0, w, h);
                // print warning message - __init__/8391
            }
            return val;
        }

        public static (Rect, Rect, IdentityMatrix) RectFunction(int rectN, Rect filled)
        {
            return (fit.Rect, fit.Rect, new IdentityMatrix());
        }

        public FzPage AsFzPage(dynamic page)
        {
            if (page is MuPDFPage)
                return (page as MuPDFPage).GetPdfPage().super();
            if (page is PdfPage)
                return (page as PdfPage).super();
            else if (page is FzPage)
                return page;

            return null;
        }

        public Matrix TransformationMatrix
        {
            get
            {
                FzMatrix ctm = new FzMatrix();
                PdfPage page = _pdfPage;
                if (page.m_internal == null)
                    return new Matrix(ctm);

                FzRect mediabax = new FzRect(FzRect.Fixed.Fixed_UNIT);
                page.pdf_page_transform(mediabax, ctm);

                if (Rotation % 360 == 0)
                    return new Matrix(ctm);
                else
                    return new Matrix(1, 0, 0, -1, 0, CropBox.Height);
            }
        }

        public Rect ArtBox
        {
            get
            {
                Rect rect = OtherBox("ArtBox");
                if (rect is null)
                    return CropBox;
                Rect mb = MediaBox;
                return new Rect(rect[0], mb.Y1 - rect[3], rect[2], mb.Y1 - rect[1]);
            }
        }

        public Rect MediaBox
        {
            get
            {
                PdfPage page = _pdfPage;
                Rect rect = null;
                if (page == null)
                    rect = new Rect(page.pdf_bound_page(fz_box_type.FZ_MEDIA_BOX));
                else
                    rect = Utils.GetMediaBox(page.obj());
                return rect;
            }
        }

        public Rect BleedBox
        {
            get
            {
                Rect rect = OtherBox("BLEEDBOX");
                if (rect is null)
                    return CropBox;
                Rect mb = MediaBox;
                return new Rect(rect[0], mb.Y1 - rect[3], rect[2], mb.Y1 - rect[1]);
            }
        }

        public Rect CropBox
        {
            get
            {
                PdfPage page = _pdfPage;
                Rect ret = null;
                if (page.m_internal == null)
                    ret = new Rect(_nativePage.fz_bound_page());
                else
                    ret = Utils.GetCropBox(page.obj());
                return ret;
            }
        }

        public Matrix DerotationMatrix
        {
            get
            {
                PdfPage page = _pdfPage;
                if (page == null)
                    return new Matrix(new Rect(new FzRect(FzRect.Fixed.Fixed_UNIT)));
                return new Matrix(Utils.DerotatePageMatrix(page));
            }
        }

        public MuPDFAnnot FirstAnnot
        {
            get
            {
                PdfPage page = _pdfPage;
                if (page == null)
                    return null;
                PdfAnnot annot = page.pdf_first_annot();
                if (annot == null)
                    return null;

                MuPDFAnnot ret = new MuPDFAnnot(annot);
                return ret;
            }
        }

        /// <summary>
        /// Reflects page rotation.
        /// </summary>
        public Matrix RotationMatrix
        {
            get
            {
                return Utils.GetRotateMatrix(this);
            }
        }

        /// <summary>
        /// First link on page
        /// </summary>
        public MuPDFLink FirstLink
        {
            get
            {
                return LoadLinks();
            }
        }

        public Widget FirstWidget
        {
            get
            {
                /*int annot = 0;*/
                PdfPage page = _pdfPage;
                if (page == null)
                    return null;
                PdfAnnot annot = page.pdf_first_widget();
                if (annot == null)
                    return null;

                MuPDFAnnot val = new MuPDFAnnot(annot);
                val.ThisOwn = true;
                val.Parent = this;
                AnnotRefs[val.GetHashCode()] = val;
                Widget widget = new Widget(this);
                Utils.FillWidget(val, widget);
                
                return widget;
            }
        }

        /// <summary>
        /// Search for a string on a page
        /// </summary>
        /// <param name="needle">string to be searched for</param>
        /// <param name="clip">restrict search to this rectangle</param>
        /// <param name="quads">return quads instead of rectangles</param>
        /// <param name="flags">bit switches, default: join hyphened words</param>
        /// <param name="stPage">a pre-created STextPage</param>
        /// <returns></returns>
        public List<Quad> SearchFor(
            string needle,
            Rect clip = null,
            bool quads = false,
            int flags = (int)(TextFlags.TEXT_DEHYPHENATE | TextFlags.TEXT_PRESERVE_WHITESPACE | TextFlags.TEXT_PRESERVE_LIGATURES | TextFlags.TEXT_MEDIABOX_CLIP),
            MuPDFTextPage stPage = null
            )
        {
            MuPDFTextPage tp = stPage;
            if (tp == null)
                tp = GetTextPage(clip, flags);
            List<Quad> ret = MuPDFTextPage.Search(tp, needle, quad: quads);
            if (stPage == null)
                tp.Dispose();

            return ret;
        }

        public Rect OtherBox(string boxtype)
        {
            FzRect rect = new FzRect(FzRect.Fixed.Fixed_INFINITE);
            PdfPage page = _pdfPage;
            if (page != null)
            {
                PdfObj obj = page.obj().pdf_dict_get(new PdfObj(boxtype));
                if (obj.pdf_is_array() != 0)
                    rect = obj.pdf_to_rect();
            }
            if (rect.fz_is_infinite_rect() != 0)
                return null;
            return new Rect(rect);
        }

        public override string ToString()
        {

            return base.ToString();
        }

        public MuPDFPage(PdfPage pdfPage, MuPDFDocument parent)
        {
            _pdfPage = pdfPage;
            _nativePage = pdfPage.super();
            Parent = parent;

            if (_pdfPage.m_internal == null)
                Number = 0;
            else
                Number = _pdfPage.m_internal.super.number;
        }

        public MuPDFPage(FzPage fzPage, MuPDFDocument parent)
        {
            _pdfPage = fzPage.pdf_page_from_fz_page();
            _nativePage = fzPage;
            Parent = parent;

            if (_pdfPage.m_internal == null)
                Number = 0;
            else
                Number = _pdfPage.m_internal.super.number;
        }

        /// <summary>
        /// PDF only: Add a caret icon. A caret annotation is a visual symbol normally used to indicate the presence of text edits on the page.
        /// </summary>
        /// <param name="point">the top left point of a 20 x 20 rectangle containing the MuPDF-provided icon.</param>
        /// <returns>the created annotation. Stroke color blue = (0, 0, 1), no fill color support.</returns>
        private PdfAnnot AddCaretAnnot(Point point)
        {
            PdfPage page = _pdfPage;
            PdfAnnot annot = null;
            try
            {
                annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_CARET);
                if (point != null)
                {
                    FzRect r = annot.pdf_annot_rect();
                    r = new FzRect(point.X, point.Y, point.X + r.x1 - r.x0, point.Y + r.y1 - r.y0);
                    annot.pdf_set_annot_rect(r);
                }
                annot.pdf_update_annot();
                Utils.AddAnnotId(annot, "A");
            }
            catch
            {
                annot = null;
            }

            return annot;
        }

        public MuPDFAnnot AddFileAnnot(
            Point point,
            byte[] buffer_,
            string filename,
            dynamic ufilename,
            string desc,
            string icon)
        {
            PdfPage page = _pdfPage;
            string uf = ufilename != null ? ufilename : filename;
            string d = desc != null ? desc : filename;
            FzBuffer fileBuf = Utils.BufferFromBytes(buffer_);

            if (fileBuf == null)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_BUFFER"]);
            }
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_FILE_ATTACHMENT);
            FzRect r = annot.pdf_annot_rect();
            r = mupdf.mupdf.fz_make_rect(point.X, point.Y, point.X + r.x1 - r.x0, point.Y + r.y1 - r.y0);

            annot.pdf_set_annot_rect(r);
            int flags = (int)PdfAnnotStatus.PDF_ANNOT_IS_PRINT;
            annot.pdf_set_annot_flags(flags);

            if (icon != null)
            {
                annot.pdf_set_annot_icon_name(icon);
            }

            PdfObj val = Utils.EmbedFile(page.doc(), fileBuf, filename, uf, d, 1);
            annot.pdf_annot_obj().pdf_dict_put(new PdfObj("FS"), val);
            annot.pdf_annot_obj().pdf_dict_put_text_string(new PdfObj("Contents"), filename);
            annot.pdf_update_annot();
            annot.pdf_set_annot_rect(r);
            annot.pdf_set_annot_flags(flags);
            Utils.AddAnnotId(annot, "A");

            return new MuPDFAnnot(annot);
        }

        public MuPDFAnnot AddFreeTextAnnot(
            Rect rect,
            string text,
            int fontSize = 11,
            string fontName = null,
            float[] textColor = null,
            float[] fillColor = null,
            float[] borderColor = null,
            int align = 0,
            int rotate = 0
            )
        {
            int oldRotation = AnnotPreProcess(this);
            MuPDFAnnot val;
            FzRect r = rect.ToFzRect();
            try
            {
                PdfPage page = _pdfPage;
                float[] fColor = MuPDFAnnot.ColorFromSequence(fillColor);
                float[] tColor = MuPDFAnnot.ColorFromSequence(textColor);
                if (r.fz_is_infinite_rect() != 0 && r.fz_is_empty_rect() != 0)
                {
                    throw new Exception(Utils.ErrorMessages["MSG_BAD_RECT"]);
                }

                PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_FREE_TEXT);
                PdfObj annotObj = annot.pdf_annot_obj();
                annot.pdf_set_annot_contents(text);
                annot.pdf_set_annot_rect(r);
                annotObj.pdf_dict_put_int(new PdfObj("Rotate"), rotate);
                annotObj.pdf_dict_put_int(new PdfObj("Q"), align);

                if (fColor != null && fColor.Length > 0)
                {
                    IntPtr fColorPtr = Marshal.AllocHGlobal(fColor.Length);
                    Marshal.Copy(fColor, 0, fColorPtr, fColor.Length);
                    SWIGTYPE_p_float swigFColor = new SWIGTYPE_p_float(fColorPtr, false);
                    annot.pdf_set_annot_color(fColor.Length, swigFColor);
                }

                Utils.MakeAnnotDA(annot, tColor == null ? -1 : tColor.Length, tColor, fontName, fontSize);
                annot.pdf_update_annot();
                Utils.AddAnnotId(annot, "A");
                val = new MuPDFAnnot(annot);

                byte[] ap = val.GetAP();
                int BT = Encoding.UTF8.GetString(ap).IndexOf("BT");
                int ET = Encoding.UTF8.GetString(ap).IndexOf("ET") + 2;
                ap = Utils.ToByte(Encoding.UTF8.GetString(ap).Substring(BT, ET - BT));

                float w = rect[2] - rect[0];
                float h = rect[3] - rect[1];
                if ((new List<float>() { 90, -90, 270 }).Contains(rotate))
                {
                    float t = w;
                    w = h;
                    h = t;
                }

                byte[] re = Utils.ToByte($"0 0 {w} {h} re");
                ap = MuPDFAnnot.MergeByte(MuPDFAnnot.MergeByte(re, Utils.ToByte($"\nW\nn\n")), ap);
                byte[] ope = null;
                byte[] bWidth = null;
                byte[] fillBytes = Utils.ToByte((MuPDFAnnot.ColorCode(fColor, "f")));
                if (fillBytes != null || fillBytes.Length != 0)
                {
                    fillBytes = MuPDFAnnot.MergeByte(fillBytes, Utils.ToByte("\n"));
                    ope = Utils.ToByte("f");
                }

                byte[] strokeBytes = Utils.ToByte(MuPDFAnnot.ColorCode(borderColor, "c"));
                if (strokeBytes != null || strokeBytes.Length != 0)
                {
                    strokeBytes = MuPDFAnnot.MergeByte(strokeBytes, Utils.ToByte("\n"));
                    bWidth = Utils.ToByte("1 w\n");
                    ope = Utils.ToByte("S");
                }

                if (fillBytes != null && strokeBytes != null)
                    ope = Utils.ToByte("B");
                if (ope != null)
                {
                    ap = MuPDFAnnot.MergeByte(
                        MuPDFAnnot.MergeByte(
                            MuPDFAnnot.MergeByte(
                                MuPDFAnnot.MergeByte(
                                    MuPDFAnnot.MergeByte(bWidth, fillBytes), strokeBytes), re), Utils.ToByte("\n")),
                        MuPDFAnnot.MergeByte(Utils.ToByte("\n"), ap));
                }

                val.SetAP(ap);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(oldRotation);
            }
            AnnotPostProcess(this, val);
            return val;
        }

        /// <summary>
        /// page load widget by xref
        /// </summary>
        /// <param name="xref"></param>
        /// <returns></returns>
        public Widget LoadWidget(int xref)
        {
            PdfPage page = _nativePage.pdf_page_from_fz_page();
            PdfAnnot annot = Utils.GetWidgetByXref(page, xref);
            MuPDFAnnot val = new MuPDFAnnot(annot);

            val.ThisOwn = true;
            val.Parent = this;
            AnnotRefs[val.GetHashCode()] = val;
            Widget widget = new Widget(this);
            Utils.FillWidget(val, widget);

            return widget;
        }

        /// <summary>
        /// Generator over the widgets of a page.
        /// </summary>
        /// <param name="types">field types to subselect from. If none, all fields are returned.E.g.types=[PDF_WIDGET_TYPE_TEXT] will only yield text fields.</param>
        /// <returns></returns>
        public IEnumerable<Widget> GetWidgets(int[] types = null)
        {
            List<AnnotXref> refs = GetAnnotXrefs();
            List<int> xrefs = refs.Where(a => a.AnnotType == PdfAnnotType.PDF_ANNOT_WIDGET).Select(a => a.Xref).ToList();
            foreach (int xref in xrefs)
            {
                Widget widget = LoadWidget(xref);
                if (types == null || types.Contains(widget.FieldType))
                    yield return widget;
            }
        }

        public MuPDFAnnot AddInkAnnot(List<List<Point>> list)
        {
            PdfPage page = _pdfPage;

            FzMatrix ctm = new FzMatrix();
            page.pdf_page_transform(new FzRect(0), ctm);
            FzMatrix invCtm = ctm.fz_invert_matrix();
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_INK);
            PdfObj annotObj = annot.pdf_annot_obj();
            int n0 = list.Count;
            PdfObj inkList = page.doc().pdf_new_array(n0);

            for (int j = 0; j < n0; j++)
            {
                dynamic subList = list[j];
                int n1 = subList.Count;
                PdfObj stroke = page.doc().pdf_new_array(n1 * 2);

                for (int i = 0; i < n1; i++)
                {
                    Point p = subList[i];
                    FzPoint point = mupdf.mupdf.fz_transform_point(p.ToFzPoint(), invCtm);
                    stroke.pdf_array_push_real(point.x);
                    stroke.pdf_array_push_real(point.y);
                }
                inkList.pdf_array_push(stroke);
            }
            annotObj.pdf_dict_put(new PdfObj("InkList"), inkList);
            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            
            return new MuPDFAnnot(annot);
        }

        public MuPDFAnnot AddLineAnnot(Point p1, Point p2)
        {
            PdfPage page = _pdfPage;
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_LINE);
            annot.pdf_set_annot_line(p1.ToFzPoint(), p2.ToFzPoint());
            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            return new MuPDFAnnot(annot);
        }

        public MuPDFAnnot AddMultiLine(List<Point> points, PdfAnnotType annotType)
        {
            PdfPage page = _pdfPage;
            if (points.Count < 2)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_ARG_POINTS"]);
            }
            PdfAnnot annot = page.pdf_create_annot((pdf_annot_type)annotType);
            foreach (Point p in points)
            {
                annot.pdf_add_annot_vertex(p.ToFzPoint());
            }

            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            return new MuPDFAnnot(annot);
        }

        public MuPDFAnnot AddRedactAnnot(Quad quad, string text = null, string dataStr = null, int align = 0, float[] fill = null, float[] textColr = null)
        {
            PdfPage page = _pdfPage;
            float[] fCol = new float[4] { 1, 1, 1, 0 };
            int nFCol = 0;
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_REDACT);
            Rect r = quad.Rect;
            annot.pdf_set_annot_rect(r.ToFzRect());

            if (fill != null)
            {
                fCol = MuPDFAnnot.ColorFromSequence(fill);
                nFCol = fCol.Length;
                PdfObj arr = page.doc().pdf_new_array(nFCol);
                for (int i = 0; i < nFCol; i++)
                {
                    arr.pdf_array_push_real(fCol[i]);
                }
                annot.pdf_annot_obj().pdf_dict_put(new PdfObj("IC"), arr);
            }
            if (text != null)
            {
                annot.pdf_annot_obj().pdf_dict_puts("OverlayText", mupdf.mupdf.pdf_new_text_string(text));
                annot.pdf_annot_obj().pdf_dict_put_text_string(new PdfObj("DA"), dataStr);
                annot.pdf_annot_obj().pdf_dict_put_int(new PdfObj("Q"), align);
            }

            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");

            SWIGTYPE_p_pdf_annot swigAnnot = mupdf.mupdf.ll_pdf_keep_annot(annot.m_internal);
            annot = new PdfAnnot(swigAnnot);
            return new MuPDFAnnot(annot);
        }

        private MuPDFAnnot AddSquareOrCircle(Rect rect, PdfAnnotType annotType)
        {
            PdfPage page = _pdfPage;
            FzRect r = rect.ToFzRect();
            if (r.fz_is_infinite_rect() != 0 || r.fz_is_empty_rect() != 0)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_RECT"]);
            }

            PdfAnnot annot = page.pdf_create_annot((pdf_annot_type)annotType);
            annot.pdf_set_annot_rect(r);
            annot.pdf_update_annot();
            return new MuPDFAnnot(annot);
        }

        private MuPDFAnnot AddStampAnnot(Rect rect, int stamp = 0)
        {
            PdfPage page = _pdfPage;
            List<PdfObj> stampIds = new List<PdfObj>()
            {
                new PdfObj("Approved"),
                new PdfObj("AsIs"),
                new PdfObj("Confidential"),
                new PdfObj("Departmental"),
                new PdfObj("Experimental"),
                new PdfObj("Expired"),
                new PdfObj("Final"),
                new PdfObj("ForComment"),
                new PdfObj("ForPublicRelease"),
                new PdfObj("NotApproved"),
                new PdfObj("NotForPublicRelease"),
                new PdfObj("Sold"),
                new PdfObj("TopSecret"),
                new PdfObj("Draft"),
            };
            int n = stampIds.Count;
            PdfObj name = stampIds[0];
            FzRect r = rect.ToFzRect();
            if (r.fz_is_infinite_rect() != 0 || r.fz_is_empty_rect() != 0)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_RECT"]);
            }
            if (Utils.INRANGE(stamp, 0, n - 1))
            {
                name = stampIds[stamp];
            }

            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_STAMP);
            annot.pdf_set_annot_rect(r);
            try
            {
                annot.pdf_annot_obj().pdf_dict_put(new PdfObj("Name"), name);
            }
            catch (Exception)
            {

            }

            annot.pdf_set_annot_contents(annot.pdf_annot_obj().pdf_dict_get_name(new PdfObj("Name")));
            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            return new MuPDFAnnot(annot);
        }

        /// <summary>
        /// PDF only: Add a comment icon (“sticky note”) with accompanying text. Only the icon is visible, the accompanying text is hidden and can be visualized by many PDF viewers by hovering the mouse over the symbol.
        /// </summary>
        /// <param name="point">the top left point of a 20 x 20 rectangle containing the MuPDF-provided “note” icon.</param>
        /// <param name="text">the top left point of a 20 x 20 rectangle containing the MuPDF-provided “note” icon.</param>
        /// <param name="icon">the top left point of a 20 x 20 rectangle containing the MuPDF-provided “note” icon.</param>
        /// <returns></returns>
        public MuPDFAnnot AddTextAnnot(Point point, string text, string icon = "Note")
        {
            PdfPage page = _pdfPage;
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_TEXT);
            FzRect r = annot.pdf_annot_rect();
            r = mupdf.mupdf.fz_make_rect(point.X, point.Y, point.X + r.x1 - r.x0, point.Y + r.y1 - r.y0);
            annot.pdf_set_annot_rect(r);
            annot.pdf_set_annot_contents(text);
            if (icon != null || icon != "")
                annot.pdf_set_annot_icon_name(icon);

            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            return new MuPDFAnnot(annot);
        }

        private MuPDFAnnot AddTextMarker(List<Quad> quads, pdf_annot_type annotType)
        {
            MuPDFAnnot ret = null;
            PdfAnnot annot = null;
            PdfPage page = new PdfPage(_pdfPage.m_internal);
            if (!Parent.IsPDF)
                throw new Exception("is not pdf");
            int rotation = Rotation;
            try
            {
                if (rotation != 0)
                    page.obj().pdf_dict_put_int(new PdfObj("Rotate"), rotation);
                try
                {
                    annot = page.pdf_create_annot(annotType);
                }
                catch (Exception)
                {
                    Console.WriteLine("message catched");
                    annot = new PdfAnnot();
                }
                foreach (Quad item in quads)
                {
                    annot.pdf_add_annot_quad_point(item.ToFzQuad());
                }
                annot.pdf_update_annot();
                Utils.AddAnnotId(annot, "A");
                if (rotation != 0)
                    page.obj().pdf_dict_put_int(new PdfObj("Rotate"), rotation);
                ret = new MuPDFAnnot(annot);
            }
            catch (Exception)
            {
                if (rotation != 0)
                    page.obj().pdf_dict_put_int(new PdfObj("Rotate"), rotation);
                ret = null;
            }
            if (ret == null)
                return null;
            AnnotRefs.Add(ret.GetHashCode(), ret);

            return ret;
        }

        /// <summary>
        /// PDF only: Add a rectangle, resp. circle annotation.
        /// </summary>
        /// <param name="rect">the rectangle in which the circle or rectangle is drawn, must be finite and not empty. If the rectangle is not equal-sided, an ellipse is drawn.</param>
        /// <returns></returns>
        public MuPDFAnnot AddCircleAnnot(Rect rect)
        {
            int oldRotation = AnnotPreProcess(this);
            MuPDFAnnot ret = null;
            try
            {
                ret = AddSquareOrCircle(rect, PdfAnnotType.PDF_ANNOT_CIRCLE);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(0);
            }
            AnnotPostProcess(this, ret);
            return ret;
        }

        public int AnnotPreProcess(MuPDFPage page)
        {
            if (!page.Parent.IsPDF)
                throw new Exception("is not PDF");
            int oldRotation = page.Rotation;
            if (oldRotation != 0)
                page.SetRotation(0);
            return oldRotation;
        }

        public void AnnotPostProcess(MuPDFPage page, MuPDFAnnot annot)
        {
            annot.Parent = page;
            if (page.AnnotRefs.Keys.Contains(annot.GetHashCode()))
                page.AnnotRefs[annot.GetHashCode()] = annot;
            else
                page.AnnotRefs.Add(annot.GetHashCode(), annot);
            annot.ThisOwn = true;
        }

        public MuPDFAnnot AddPolygonAnnot(List<Point> points)
        {
            int oldRotation = AnnotPreProcess(this);
            MuPDFAnnot annot;
            try
            {
                annot = AddMultiLine(points, PdfAnnotType.PDF_ANNOT_POLYGON);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(oldRotation);
            }
            AnnotPostProcess(this, annot);
            return annot;
        }

        public MuPDFAnnot AddPolylineAnnot(List<Point> points)
        {
            int oldRotation = AnnotPreProcess(this);
            MuPDFAnnot annot;
            try
            {
                annot = AddMultiLine(points, PdfAnnotType.PDF_ANNOT_POLY_LINE);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(oldRotation);
            }
            AnnotPostProcess(this, annot);
            return annot;
        }

        public MuPDFAnnot AddRectAnnot(Rect rect)
        {
            int oldRotation = AnnotPreProcess(this);
            MuPDFAnnot annot;
            try
            {
                annot = AddSquareOrCircle(rect, PdfAnnotType.PDF_ANNOT_SQUARE);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(oldRotation);
            }
            AnnotPostProcess(this, annot);
            return annot;
        }

        private List<Rect> GetHighlightSelection(List<Quad> quads, Point start, Point stop, Rect clip)
        {
            if (clip is null)
                clip = this.MediaBox;
            if (start is null)
                start = clip.TopLeft;
            if (stop is null)
                stop = clip.BottomRight;
            clip.Y0 = start.Y;
            clip.Y1 = stop.Y;

            if (clip.IsEmpty || clip.IsInfinite)
                return null;

            List<Block> blocks = GetText("dict", clip, 0).BLOCKS;

            List<Rect> lines = new List<Rect>();
            foreach (Block b in blocks)
            {
                Rect bbox = new Rect(b.Bbox);
                if (bbox.IsInfinite || bbox.IsEmpty)
                    continue;

                foreach (Line line in b.Lines)
                {
                    bbox = new Rect(line.Bbox);
                    if (bbox.IsInfinite || bbox.IsEmpty)
                        continue;
                    lines.Add(bbox);
                }
            }

            if (lines.Count == 0)
                return lines;

            lines.Sort((Rect bbox1, Rect bbox2) => bbox1.Y1.CompareTo(bbox2.Y1));
            Rect bboxf = new Rect(lines[0]);
            lines.RemoveAt(0);
            if (bboxf.Y0 - start.Y <= 0.1 * bboxf.Height)
            {
                Rect r = new Rect(start.X, bboxf.Y0, bboxf.BottomRight.X, bboxf.BottomRight.Y);
                if (!(r.IsEmpty || r.IsInfinite))
                    lines.Insert(0, r);
            }
            else
                lines.Insert(0, bboxf);

            if (lines.Count == 0)
                return lines;

            Rect bboxl = lines[lines.Count - 1];
            lines.RemoveAt(lines.Count - 1);
            if (stop.Y - bboxl.Y1 <= 0.1 * bboxl.Height)
            {
                Rect r = new Rect(bboxl.TopLeft.X, bboxl.TopLeft.Y, stop.X, bboxl.Y1);
                if (!(r.IsEmpty || r.IsInfinite))
                    lines.Add(r);
            }
            else
                lines.Add(bboxl);

            return lines;
        }

        public MuPDFAnnot AddHighlightAnnot(dynamic quads, Point start = null, Point stop = null, Rect clip = null)
        {
            List<Quad> q = new List<Quad>();
            if (quads is Rect)
                q.Add(quads.QUAD);
            else if (quads is Quad)
                q.Add(quads);
            else if (quads is null)
            {
                List<Rect> rs = GetHighlightSelection(q, start, stop, clip);
                foreach (Rect r in rs)
                    q.Add(r.Quad);
            }
            else
                q = quads;
            MuPDFAnnot ret = AddTextMarker(q, pdf_annot_type.PDF_ANNOT_HIGHLIGHT);
            return ret;
        }

        private MuPDFTextPage _GetTextPage(Rect clip = null, int flags = 0, Matrix matrix = null)
        {
            PdfPage page = _pdfPage;
            FzStextOptions options = new FzStextOptions(flags);
            FzRect rect = (clip == null) ? mupdf.mupdf.fz_bound_page(new FzPage(page)) : clip.ToFzRect();
            FzMatrix ctm = matrix.ToFzMatrix();
            FzStextPage stPage = new FzStextPage(rect);
            FzDevice dev = stPage.fz_new_stext_device(options);

            FzPage _page = null;
            if (page is PdfPage)
                _page = page.super();
            else
                Debug.Assert(false, "Unrecognised type");
            _page.fz_run_page(dev, ctm, new FzCookie());
            dev.fz_close_device();
            
            return new MuPDFTextPage(stPage);
        }

        public MuPDFTextPage GetTextPage(Rect clip = null, int flags = 0, Matrix matrix = null)
        {
            if (matrix == null)
                matrix = new Matrix(1, 1);

            if (clip == null)
                clip = new Rect(new FzRect(FzRect.Fixed.Fixed_INFINITE));

            int oldRotation = Rotation;
            if (oldRotation != 0) SetRotation(0);

            MuPDFTextPage stPage = null;
            try
            {
                stPage = _GetTextPage(clip, flags, matrix);
            }
            finally
            {
                if (oldRotation != 0)
                    SetRotation(oldRotation);
            }

            return stPage;
        }

        public void SetRotation(int rotation)
        {
            PdfPage page = _pdfPage;
            rotation = Utils.NormalizeRotation(rotation);
            page.obj().pdf_dict_put_int(new PdfObj("Rotate"), rotation);

        }

        public MuPDFAnnot AddUnderlineAnnot(dynamic quads = null, Point start = null, Point stop = null, Rect clip = null)
        {
            List<Quad> q = new List<Quad>();
            if (quads is Rect)
                q.Add(quads.QUAD);
            else if (quads is Quad)
                q.Add(quads);
            else if (quads is null)
            {
                List<Rect> rs = GetHighlightSelection(q, start, stop, clip);
                foreach (Rect r in rs)
                    q.Add(r.Quad);
            }
            else
                q = quads;

            return AddTextMarker(q, pdf_annot_type.PDF_ANNOT_UNDERLINE);
        }

        public MuPDFAnnot AddSquigglyAnnot(dynamic quads = null, Point start = null, Point stop = null, Rect clip = null)
        {
            List<Quad> q = new List<Quad>();
            if (quads is Rect)
                q.Add(quads.QUAD);
            else if (quads is Quad)
                q.Add(quads);
            else if (quads is null)
            {
                List<Rect> rs = GetHighlightSelection(q, start, stop, clip);
                foreach (Rect r in rs)
                    q.Add(r.Quad);
            }
            else
                q = quads;

            return AddTextMarker(q, pdf_annot_type.PDF_ANNOT_SQUIGGLY);
        }

        public MuPDFAnnot AddStrikeoutAnnot(dynamic quads = null, Point start = null, Point stop = null, Rect clip = null)
        {
            List<Quad> q = new List<Quad>();
            if (quads is Rect)
                q.Add(quads.QUAD);
            else if (quads is Quad)
                q.Add(quads);
            else if (quads is null)
            {
                List<Rect> rs = GetHighlightSelection(q, start, stop, clip);
                foreach (Rect r in rs)
                    q.Add(r.Quad);
            }
            else
                q = quads;

            return AddTextMarker(q, pdf_annot_type.PDF_ANNOT_STRIKE_OUT);
        }

        public void AddAnnotFromString(List<string> links)
        {
            PdfPage page = _pdfPage;
            int lCount = links.Count;
            if (lCount < 1)
                return;
            int i = -1;

            if (page.obj().pdf_dict_get(new PdfObj("Annots")).m_internal == null)
                page.obj().pdf_dict_put_array(new PdfObj("Annots"), lCount);
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            Debug.Assert(annots == null, $"{lCount} is {annots}");

            for (i = 0; i < lCount; i++)
            {
                string text = links[i];
                if (text == null)
                {
                    Console.WriteLine($"skipping bad link / annot item {i}.\\n");
                    continue;
                }

                try
                {
                    PdfObj annot = page.doc().pdf_add_object(Utils.PdfObjFromStr(page.doc(), text));
                    PdfObj indObj = page.doc().pdf_new_indirect(annot.pdf_to_num(), 0);
                    annots.pdf_array_push(indObj);
                }
                catch (Exception)
                {
                    Console.WriteLine($"skipping bad link / annot item {i}.");
                }
            }
        }

        public MuPDFAnnot AddWidget(PdfWidgetType fieldType, string fieldName)
        {
            PdfPage page = _pdfPage;
            PdfDocument pdf = page.doc();
            PdfAnnot annot = Utils.CreateWidget(pdf, page, fieldType, fieldName);

            if (annot == null)
                throw new Exception("cannot create widget");
            Utils.AddAnnotId(annot, "W");

            return new MuPDFAnnot(annot);
        }

        public int ApplyRedactions(int images)
        {
            PdfPage page = _pdfPage;
            PdfRedactOptions opts = new PdfRedactOptions();
            opts.black_boxes = 0;
            opts.image_method = images;
            int success = page.doc().pdf_redact_page(page, opts);

            return success;
        }

        private void ResetAnnotRefs()
        {
            AnnotRefs.Clear();
        }
        public void Erase()
        {
            this.ResetAnnotRefs();
            try
            {
                Parent.ForgetPage(this);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Parent = null;
            ThisOwn = false;
            Number = 0;
        }

        private List<(string, int)> GetResourceProperties()
        {
            PdfPage page = _pdfPage;
            List<(string, int)> rc = Utils.GetResourceProperties(page.obj());

            return rc;
        }

        private void SetResourceProperty(string mc, int xref)
        {
            PdfPage page = _pdfPage;
            Utils.SetResourceProperty(page.obj(), mc, xref);
        }

        public void InsertLink(Link link, bool mark = true)
        {
            string annot = Utils.GetLinkText(this, link);
            if (annot == "")
                throw new Exception("link kind not supported");
            AddAnnotFromString(new List<string>() { annot });
        }

        public int InsertImage(
            Rect rect = null, string filename = null, Pixmap pixmap = null, byte[] stream = null, byte[] imask = null,
            int overlay = 1, int rotate = 0, int keepProportion = 1, int oc = 0, int width = 0, int height = 0,
            int xref = 0, int alpha = -1, string imageName = null, byte[] mask = null)
        {
            MuPDFDocument doc = Parent;
            if (!Parent.IsPDF)
                throw new Exception("is no pdf");
            if (xref == 0 && (string.IsNullOrEmpty(filename) ? 0 : 1) + (stream == null ? 0 : 1) + (pixmap == null ? 0 : 1) != 1)
                throw new Exception("xref=0 needs exactly one of filename, pixmap, stream");

            if (!File.Exists(filename))
                throw new Exception($"No such file: {filename}");
            else if (mask != null && (stream == null || string.IsNullOrEmpty(filename)))
                throw new Exception("mask requires stream or filename");

            while (rotate < 0)
                rotate += 360;
            while (rotate >= 360)
                rotate -= 360;

            if (!(new int[] { 0, 90, 180, 270 }).Contains(rotate))
                throw new Exception("bad rotate value");

            if (rect.IsEmpty || rect.IsInfinite)
                throw new Exception("rect must be finite and not empty");
            Rect clip = rect * ~TransformationMatrix;

            List<string> iList = new List<string>();
            List<Entry> images = doc.GetPageImages(Number);
            foreach (Entry i in images)
                iList.Add(i.RefName);

            List<Entry> xobjects = doc.GetPageXObjects(Number);
            foreach (Entry i in xobjects)
                iList.Add(i.RefName);

            List<Entry> fonts = doc.GetPageFonts(Number);
            foreach (Entry i in fonts)
                iList.Add(i.RefName);
            
            string n = "fzImg";
            int j = 0;
            string imgName = n + "0";
            while (iList.Contains(imgName))
            {
                j += 1;
                imgName = n + $"{j}";
            }

            Dictionary<string, int> digests = doc.InsertedImages;

            FzBuffer maskBuf = new FzBuffer();
            PdfPage page = _pdfPage;
            PdfDocument pdf = page.doc();
            int w = width;
            int h = height;
            int imgXRef = xref;
            int rcDigest = 0;
            string template = "\nq\n{0} {1} {2} {3} {4} {5} cm\n/{6} Do\nQ\n";

            int do_process_pixmap = 1;
            int do_process_stream = 1;
            int do_have_imask = 1;
            int do_have_image = 1;
            int do_have_xref = 1;
            FzBuffer imgBuf = null;
            FzImage image = null;
            FzImage maskImage = null;
            byte[] md5 = null;
            PdfObj ref_ = new PdfObj();

            if (xref > 0)
            {
                PdfObj refer = pdf.pdf_new_indirect(xref, 0);
                w = refer.pdf_dict_geta(new PdfObj("Width"), new PdfObj("W")).pdf_to_int();
                h = refer.pdf_dict_geta(new PdfObj("Height"), new PdfObj("H")).pdf_to_int();

                if (w + h == 0)
                    throw new Exception(Utils.ErrorMessages["MSG_IS_NO_IMAGE"]);

                do_process_pixmap = 0;
                do_process_stream = 0;
                do_have_imask = 0;
                do_have_image = 0;
            }
            else
            {
                if (stream != null)
                {
                    imgBuf = Utils.BufferFromBytes(stream);
                    do_process_pixmap = 0;
                }
                else
                {
                    if (!string.IsNullOrEmpty(filename))
                    {
                        imgBuf = mupdf.mupdf.fz_read_file(filename);
                        do_process_pixmap = 0;
                    }
                }
            }

            if (do_process_pixmap != 0)
            {
                FzPixmap argPix = pixmap.ToFzPixmap();
                w = argPix.w();
                h = argPix.h();
                vectoruc digest = argPix.fz_md5_pixmap2();
                int temp = digests.GetValueOrDefault(Encoding.UTF8.GetString(digest.ToArray()), -1);
                if (temp != -1)
                {
                    imgXRef = temp;
                    PdfObj refer = page.doc().pdf_new_indirect(imgXRef, 0);
                    do_process_stream = 0;
                    do_have_imask = 0;
                    do_have_image = 0;
                }
                else
                {
                    if (argPix.alpha() == 0)
                        image = argPix.fz_new_image_from_pixmap(new FzImage());
                    else
                    {
                        FzPixmap pm = argPix.fz_convert_pixmap(
                            new FzColorspace(),
                            new FzColorspace(),
                            new FzDefaultColorspaces(),
                            new FzColorParams(),
                            1
                            );
                        pm.m_internal.alpha = 0;
                        pm.m_internal.colorspace = null;
                        maskImage = pm.fz_new_image_from_pixmap(new FzImage());
                        image = argPix.fz_new_image_from_pixmap(maskImage);
                    }
                    do_process_stream = 0;
                    do_have_imask = 0;
                }
            }

            if (do_process_stream != 0)
            {
                FzMd5 state = new FzMd5();
                state.fz_md5_update(imgBuf.m_internal.data, imgBuf.m_internal.len);

                if (imask != null)
                {
                    maskBuf = Utils.BufferFromBytes(imask);
                    state.fz_md5_update(maskBuf.m_internal.data, maskBuf.m_internal.len);
                }
                vectoruc digest = state.fz_md5_final2();
                md5 = digest.ToArray();
                int tmp = digests.GetValueOrDefault(Encoding.UTF8.GetString(md5), -1);
                if (tmp != -1)
                {
                    imgXRef = tmp;
                    ref_ = page.doc().pdf_new_indirect(imgXRef, 0);
                    w = ref_.pdf_dict_geta(new PdfObj("Width"), new PdfObj("W")).pdf_to_int();
                    h = ref_.pdf_dict_geta(new PdfObj("Height"), new PdfObj("H")).pdf_to_int();
                    do_have_imask = 0;
                    do_have_image = 0;
                }
                else
                {
                    image = imgBuf.fz_new_image_from_buffer();
                    w = image.w();
                    h = image.h();
                    if (imask == null)
                        do_have_imask = 0;
                }
            }

            if (do_have_imask != 0)
            {
                // version 1.24 later
                FzCompressedBuffer cbuf = image.fz_compressed_image_buffer();
                if (cbuf.m_internal == null)
                    throw new Exception("uncompressed image cannot have mask");
                byte bpc = image.bpc();
                FzColorspace colorspace = image.colorspace();
                (int xres, int yres) = image.fz_image_resolution();
                maskImage = maskBuf.fz_new_image_from_buffer();
                image = mupdf.mupdf.fz_new_image_from_compressed_buffer2(
                    w,
                    h,
                    bpc,
                    colorspace,
                    xres,
                    yres,
                    1,
                    0,
                    new vectorf(),
                    new vectori(),
                    cbuf,
                    maskImage
                );
            }
            
            if (do_have_image != 0)
            {
                ref_ = pdf.pdf_add_image(image);
                if (oc != 0)
                    Utils.AddOcObject(pdf, ref_, oc);
                imgXRef = ref_.pdf_to_num();
                digests[Encoding.UTF8.GetString(md5)] = imgXRef;
                rcDigest = 1;
            }

            if (do_have_xref != 0)
            {
                PdfObj resources = page.obj().pdf_dict_get_inheritable(new PdfObj("Resources"));
                if (resources.m_internal == null)
                    resources = page.obj().pdf_dict_put_dict(new PdfObj("Resources"), 2);
                PdfObj xobject = resources.pdf_dict_get(new PdfObj("XObject"));
                if (xobject.m_internal == null)
                    xobject = resources.pdf_dict_put_dict(new PdfObj("XObject"), 2);
                FzMatrix mat = Utils.CalcImageMatrix(w, h, clip, rotate, keepProportion != 0);
                xobject.pdf_dict_puts(imageName, ref_);
                FzBuffer nres = mupdf.mupdf.fz_new_buffer(50);
                nres.fz_append_string(string.Format(template, mat.a, mat.b, mat.c, mat.d, mat.e, mat.f, imageName));
                Utils.InsertContents(pdf, page.obj(), nres, overlay);
            }

            if (rcDigest != 0)
            {
                doc.InsertedImages = digests;
            }
            
            return imgXRef;
        }

        public int InsertText(
            Point point,
            dynamic text,
            float fontSize = 11,
            float lineHeight = 0,
            string fontName = "helv",
            string fontFile = null,
            int setSimple = 0,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            float borderWidth = 0.05f,
            int renderMode = 0,
            int rotate = 0,
            Morph morph = null,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            Shape img = new Shape(this);
            int rc = img.InsertText(
                point,
                text,
                fontSize: fontSize,
                fontFile: fontFile,
                lineHeight: lineHeight,
                fontName: fontName,
                setSimple: setSimple != 0,
                encoding: encoding,
                color: color,
                fill: fill,
                borderWidth: borderWidth,
                renderMode: renderMode,
                rotate: rotate,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc
                );

            if (rc >= 0)
                img.Commit(overlay);
            return rc;
        }

        public (float, float) InsertHtmlBox(
            Rect rect,
            dynamic text,
            string css = null,
            float opacity = 1,
            int rotate = 0,
            float scaleLow = 0,
            MuPDFArchive archive = null,
            int oc = 0,
            bool overlay = true
            )
        {
            if (rotate % 90 != 0)
                throw new Exception("bad rotation angle");
            while (rotate < 0)
                rotate += 360;
            while (rotate >= 360)
                rotate -= 360;

            if (!(0 <= scaleLow && scaleLow <= 1))
                throw new Exception("'scale_low' must be in [0, 1]");

            if (css == null)
                css = "";

            Rect tempRect = null;
            if (rotate == 90 || rotate == 270)
                tempRect = new Rect(0, 0, rect.Height, rect.Width);
            else
                tempRect = new Rect(0, 0, rect.Width, rect.Height);

            // use a small border by default
            string mycss = "body {margin:1px;}" + css;

            // either make a story or accept a given one
            MuPDFStory story = null;
            if (text is string)
                story = new MuPDFStory(text, mycss, archive: archive);
            else if (text is MuPDFStory)
                story = text;
            else
                throw new Exception($"{text} must be a string or a Story");

            float scaleMax = scaleLow == 0 ? 0.0f : 1 / scaleLow;
            fit = story.FitScale(tempRect, scaleMin: 1, scaleMax: scaleMax);

            if (fit.BigEnough == false)
                return (-1, scaleLow);

            var filled = fit.Filled;
            float scale = 1 / fit.Parameter;

            float spareHeight = fit.Rect.Y1 - filled[3];
            if (scale != 1 || spareHeight < 0)
                spareHeight = 0;

            MuPDFDocument doc = story.WriteWithLinks(RectFunction);
            
            if (0 <= opacity && opacity < 1)
            {
                MuPDFPage tpage = doc[0];
                string alpha = tpage.SetOpacity(CA: opacity, ca: opacity);
                string s = $"/{alpha} gs\n";
                Utils.InsertContents(tpage, Encoding.UTF8.GetBytes(s), 0);
            }
            ShowPdfPage(rect, doc, 0, rotate: rotate, oc: oc, overlay: overlay);
            Point mp1 = (fit.Rect.TopLeft + fit.Rect.BottomRight) / 2 * scale;
            Point mp2 = (rect.TopLeft + rect.BottomRight) / 2;
            
            Matrix mat = (new Matrix(scale, 0, 0, scale, -mp1.X, -mp1.Y) * new Matrix(-rotate) * new Matrix(1, 0, 0, 1, mp2.X, mp2.Y));
            
            foreach (Link link in doc[0].GetLinks())
            {
                Link t = link;
                t.From *= mat;
                InsertLink(t);
            }
            return (spareHeight, scale);
        }

        public List<Link> GetLinks()
        {
            MuPDFLink ln = FirstLink;
            List<Link> links = new List<Link>();
            while (ln != null)
            {
                Link nl = Utils.GetLinkDict(ln, Parent);
                links.Add(nl);
                ln = ln.Next;
            }

            if (links.Count != 0 && Parent.IsPDF)
            {
                List<AnnotXref> xrefs = GetAnnotXrefs();
                xrefs = xrefs.Where(xref => xref.AnnotType ==PdfAnnotType.PDF_ANNOT_LINK).ToList();
                if (xrefs.Count == links.Count)
                {
                    for (int i = 0; i < xrefs.Count; i++)
                    {
                        Link t = links[i];
                        t.Xref = xrefs[i].Xref;
                        t.Id = xrefs[i].Id;
                        links.Insert(i, t);
                    }
                }
            }

            return links;
        }

        public Matrix CalcMatrix(Rect sr, Rect tr, bool keep = true, int rotate = 0)
        {
            Point smp = (sr.TopLeft + sr.BottomRight) / 2.0f;
            Point tmp = (tr.TopLeft + tr.BottomRight) / 2.0f;

            Matrix m = new Matrix(1, 0, 0, 1, -smp.X, -smp.Y) * new Matrix(rotate);
            Rect sr1 = sr * m;

            float fw = tr.Width / sr1.Width;
            float fh = tr.Height / sr1.Height;
            if (keep)
                fw = fh = Math.Min(fw, fh);

            m *= new Matrix(fw, fh);
            m *= new Matrix(1, 0, 0, 1, tmp.X, tmp.Y);
            return m;
        }

        public int ShowPdfPage(
            Rect rect,
            MuPDFDocument src,
            int pno = 0,
            bool keepProportion = true,
            bool overlay = true,
            int oc = 0,
            int rotate = 0,
            Rect clip = null
            )
        {
            MuPDFDocument doc = Parent;
            if (!doc.IsPDF || !src.IsPDF)
            {
                throw new Exception("is not PDF");
            }

            if (rect.IsEmpty || rect.IsInfinite)
                throw new Exception("rect must be finite and not empty");

            while (pno < 0)
                pno += src.GetPageCount();

            MuPDFPage srcPage = src[pno];
            if (srcPage.GetContents().Count == 0)
            {
                throw new Exception("nothing to show - source page empty");
            }

            Rect tarRect = rect * ~TransformationMatrix;
            Rect srcRect = clip == null ? srcPage.Rect : srcPage.Rect & clip;
            if (srcRect.IsEmpty || srcRect.IsInfinite)
                throw new Exception("clip must be finite and not empty");
            srcRect = srcRect * ~srcPage.TransformationMatrix;

            Matrix matrix = CalcMatrix(srcRect, tarRect, keep: keepProportion, rotate: rotate);
            
            List<dynamic> iList = new List<dynamic>();
            List<Entry> res = doc.GetPageXObjects(Number);
            int i = 0;
            for (i = 0; i < res.Count; i++)
            {
                iList.Add(res[i].RefName);
            }
            
            res = doc.GetPageImages(Number);
            for (i = 0; i < res.Count; i++)
            {
                iList.Add(res[i].RefName);
            }

            res = doc.GetPageFonts(Number);
            for (i = 0; i < res.Count; i++)
                iList.Add(res[i].RefName);

            string n = "fzFrm";
            i = 0;
            string imgName = n + "0";
            while (iList.Contains(imgName))
            {
                i += 1;
                imgName = n + $"{i}";
            }

            int isrc = src.GraftID;
            if (doc.GraftID == isrc)
                throw new Exception("source document must not equal target");

            MuPDFGraftMap gmap = doc.GraftMaps.GetValueOrDefault(isrc, null);
            if (gmap == null)
            {
                gmap = new MuPDFGraftMap(doc);
                doc.GraftMaps[isrc] = gmap;
            }

            int xref = doc.ShownPages.GetValueOrDefault((isrc, pno), 0);
            xref = ShowPdfPage(srcPage, overlay, matrix, xref, oc, srcRect.ToFzRect(), gmap, imgName);
            
            doc.ShownPages[(isrc, pno)] = xref;

            return xref;
        }

        private int ShowPdfPage(MuPDFPage srcPage, bool overlay = true, Matrix matrix = null, int xref = 0, int oc = 0, FzRect clip = null, MuPDFGraftMap gmap = null, string imgName = null)
        {
            FzRect cropBox = new FzRect(FzRect.Fixed.Fixed_INFINITE);
            if (clip != null)
                cropBox = clip;
            FzMatrix mat = matrix != null ? matrix.ToFzMatrix() : new FzMatrix();
            int rcXref = xref;
            PdfPage tpage = _pdfPage;
            PdfObj tpageObj = tpage.obj();
            PdfDocument pdfOut = tpage.doc();
            Utils.EnsureOperations(pdfOut);

            PdfObj xobj1 = Utils.GetXObjectFromPage(pdfOut, srcPage.GetPdfPage(), rcXref, gmap);
            if (rcXref == 0)
                rcXref = xobj1.pdf_to_num();

            // create refereneing xobject (controls display on target page)
            PdfObj subRes1 = mupdf.mupdf.pdf_new_dict(pdfOut, 5);
            subRes1.pdf_dict_puts("fullpage", xobj1);
            PdfObj subRes = mupdf.mupdf.pdf_new_dict(pdfOut, 5);
            subRes.pdf_dict_put(new PdfObj("XObject"), subRes1);

            FzBuffer res = mupdf.mupdf.fz_new_buffer(20);
            res.fz_append_string("/fullpage Do");

            PdfObj xobj2 = pdfOut.pdf_new_xobject(cropBox, mat, subRes, res);
            if (oc > 0)
                Utils.AddOcObject(pdfOut, xobj2, oc);

            // update target page with xobj2
            PdfObj resources = tpageObj.pdf_dict_get_inheritable(new PdfObj("Resources"));
            subRes = resources.pdf_dict_get(new PdfObj("XObject"));
            if (subRes.m_internal == null)
                subRes = resources.pdf_dict_put_dict(new PdfObj("XObject"), 5);
            subRes.pdf_dict_puts(imgName, xobj2);

            FzBuffer nres = mupdf.mupdf.fz_new_buffer(50); // buffer for Do command
            nres.fz_append_string(" q /"); // Do command
            nres.fz_append_string(imgName);
            nres.fz_append_string(" Do Q ");

            Utils.InsertContents(pdfOut, tpageObj, nres, overlay ? 1 : 0);
            return rcXref;
        }

        public List<int> GetContents()
        {
            List<int> ret = new List<int>();
            PdfObj obj = _pdfPage.obj();
            PdfObj contents = obj.pdf_dict_get(new PdfObj("Contents"));
            if (contents.pdf_is_array() != 0)
            {
                int n = contents.pdf_array_len();
                for (int i = 0; i < n; i++)
                {
                    PdfObj icont = contents.pdf_array_get(i);
                    int xref = icont.pdf_to_num();
                    ret.Add(xref);
                }
            }
            else if (contents != null)
            {
                int xref = contents.pdf_to_num();
                ret.Add(xref);
            }
            return ret;
        }

        public int InsertFont(
            string fontName = "helv",
            string fontFile = null,
            byte[] fontBuffer = null,
            bool setSimple = false,
            int wmode = 0,
            int encoding = 0
            )
        {
            MuPDFDocument doc = Parent;
            int xref = 0;
            int idx = 0;

            if (doc is null)
                throw new Exception("orphaned object: parent is None");

            if (fontName.StartsWith("/"))
                fontName = fontName.Substring(1);

            HashSet<char> INVALID_NAME_CHARS = new HashSet<char>(" \t\n\r\v\f()<>[]{}/%" + '\0');
            INVALID_NAME_CHARS.IntersectWith(fontName);

            if (INVALID_NAME_CHARS.Count > 0)
                throw new Exception($"bad fontname chars {INVALID_NAME_CHARS.ToString()}");

            Font font = Utils.CheckFont(this, fontName);
            if (font != null)
            {
                xref = font.Xref;
                if (Utils.CheckFontInfo(doc, xref) != null)
                    return xref;

                Utils.GetCharWidths(doc, xref);
                return xref;
            }

            string bfName;
            try
            {
                bfName = Utils.Base14_fontdict.GetValueOrDefault(fontName.ToLower(), null);
            }
            catch
            {
                bfName = "";
            }
            int serif = 0;
            int CJK_number = -1;
            List<string> CJK_list_n = new List<string>() { "china-t", "china-s", "japan", "korea" };
            List<string> CJK_list_s = new List<string>() { "china-ts", "china-ss", "japan-s", "korea-s" };

            try
            {
                CJK_number = CJK_list_n.IndexOf(fontName);
                serif = 0;
            }
            catch (Exception) { }

            if (CJK_number < 0)
            {
                try
                {
                    CJK_number = CJK_list_n.IndexOf(fontName);
                    serif = 1;
                }
                catch (Exception) { }
            }

            Font val = _InsertFont(fontName, bfName, fontFile, fontBuffer, setSimple, idx,
                wmode, serif, encoding, CJK_number);

            if (val == null)
                return -1;

            Font fontDict = val;
            var _ = Utils.GetCharWidths(doc, xref: fontDict.Xref, fontDict: fontDict);
            return fontDict.Xref;
        }

        public List<Entry> GetFonts(bool full = false)
        {
            return Parent.GetPageFonts(Number, full);
        }

        public PdfPage GetPdfPage()
        {
            return _pdfPage;
        }

        private Font _InsertFont(
            string fontName,
            string bfName,
            string fontFile,
            byte[] fontBuffer,
            bool setSimple,
            int idx, int wmode,
            int serif, int encoding,
            int ordering
            )
        {
            PdfPage page = GetPdfPage();
            PdfDocument pdf = page.doc();

            Font value = Utils.InsertFont(pdf, bfName, fontFile, fontBuffer, setSimple, idx, wmode, serif, encoding, ordering);
            PdfObj resources = page.obj().pdf_dict_get_inheritable(new PdfObj("Resources"));

            PdfObj fonts = resources.pdf_dict_get(new PdfObj("Font"));

            if (fonts.pdf_dict_len() == 0)
            {
                fonts = pdf.pdf_new_dict(5);
                Utils.pdf_dict_putl(page.obj(), fonts, new string[2] { "Resources", "Font" });
            }

            if (value.Xref == 0)
                throw new Exception("cannot insert font");

            PdfObj fontObj = pdf.pdf_new_indirect(value.Xref, 0);

            fonts.pdf_dict_puts(fontName, fontObj);
            return value;
        }

        public string GetOptionalContent(int oc)
        {
            if (oc == 0)
                return null;
            MuPDFDocument doc = Parent;
            string check = doc.GetXrefObject(oc, 1);
            if (!(check.IndexOf("/Type/OCG") != -1 || check.IndexOf("/Type/OCMD") != -1))
                throw new Exception("bad optional content: 'oc'");

            Dictionary<int, string> propsDict = new Dictionary<int, string>();
            List<(string, int)> props = GetResourceProperties();
            foreach ((string p, int x) in props)
            {
                propsDict[x] = p;
            }

            if (propsDict.Keys.Contains(oc))
                return propsDict[oc];
            int i = 0;
            string mc = $"MC{i}";
            while (propsDict.Values.Contains(mc))
            {
                i += 1;
                mc = $"MC{i}";
            }
            SetResourceProperty(mc, oc);
            return mc;
        }

        public string SetOpacity(string gstate = null, float CA = 1, float ca = 1, string blendMode = null)
        {
            if (CA >= 1 && ca >= 1 && string.IsNullOrEmpty(blendMode))
                return null;
            int tCA = Convert.ToInt32(Math.Round(Math.Max(CA, 0) * 100));
            if (tCA >= 100)
                tCA = 99;
            int tca = Convert.ToInt32(Math.Round(Math.Max(ca, 0) * 100));
            if (tca >= 100)
                tca = 99;
            gstate = String.Format("fitzca{0:D2}{1:D2}", tCA, tca);

            if (gstate == null) return null;

            PdfObj resources = _pdfPage.obj().pdf_dict_get(new PdfObj("Resources"));
            if (resources.m_internal == null)
                resources = _pdfPage.obj().pdf_dict_put_dict(new PdfObj("Resources"), 2);
            PdfObj extg = resources.pdf_dict_get(new PdfObj("ExtGState"));
            if (extg.m_internal == null)
                extg = resources.pdf_dict_put_dict(new PdfObj("ExtGState"), 2);
            int n = extg.pdf_dict_len();

            for (int i = 0; i < n; i++)
            {
                PdfObj o1 = extg.pdf_dict_get_key(i);
                string name = o1.pdf_to_name();
                if (name == gstate)
                    return gstate;
            }

            PdfObj opa = _pdfPage.doc().pdf_new_dict(3);
            opa.pdf_dict_put_real(new PdfObj("CA"), CA);
            opa.pdf_dict_put_real(new PdfObj("ca"), ca);
            extg.pdf_dict_puts(gstate, opa);

            return gstate;
        }

        public List<string> GetAnnotNames()
        {
            PdfPage page = GetPdfPage();
            if (page == null)
                return null;
            return Utils.GetAnnotIdList(page);
        }

        public List<AnnotXref> GetAnnotXrefs()
        {
            PdfPage page = GetPdfPage();
            if (page == null)
                return null;
            return Utils.GetAnnotXrefList(page.obj());
        }

        public List<AnnotXref> GetUnusedAnnotXrefs()
        {
            PdfPage page = GetPdfPage();
            if (page == null)
                return null;
            return Utils.GetAnnotXrefList(page.obj());
        }

        public IEnumerable<MuPDFAnnot> GetAnnots(List<PdfAnnotType> types = null)
        {
            List<PdfAnnotType> skipTypes = new List<PdfAnnotType>() { PdfAnnotType.PDF_ANNOT_LINK, PdfAnnotType.PDF_ANNOT_POPUP, PdfAnnotType.PDF_ANNOT_WIDGET };
            List<int> annotXrefs = new List<int>();
            foreach (AnnotXref annot in GetAnnotXrefs())
            {
                if (types == null)
                {
                    if (!skipTypes.Contains(annot.AnnotType))
                        annotXrefs.Add(annot.Xref);
                }
                else
                {
                    if (!skipTypes.Contains(annot.AnnotType) && types.Contains(annot.AnnotType))
                        annotXrefs.Add(annot.Xref);
                }
            }

            foreach (int xref in annotXrefs)
            {
                MuPDFAnnot annot = LoadAnnot(xref);
                annot.Yielded = true;
                yield return annot;
            }
        }

        /// <summary>
        /// PDF only: return the annotation identified by ident. This may be its unique name (PDF /NM key), or its xref.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MuPDFAnnot LoadAnnot(string name)
        {
            MuPDFAnnot val = _LoadAnnot(name, 0);
            if (val == null)
                return null;
            val.ThisOwn = true;
            val.Parent = this;
            if (AnnotRefs.Keys.Contains(val.GetHashCode()))
                AnnotRefs[val.GetHashCode()] = val;
            else AnnotRefs.Add(val.GetHashCode(), val);

            return val;
        }

        public MuPDFAnnot LoadAnnot(int xref)
        {
            MuPDFAnnot val = _LoadAnnot(null, xref);
            if (val == null)
                return null;
            val.ThisOwn = true;
            val.Parent = this;
            if (AnnotRefs.Keys.Contains(val.GetHashCode()))
                AnnotRefs[val.GetHashCode()] = val;
            else AnnotRefs.Add(val.GetHashCode(), val);

            return val;
        }

        private MuPDFAnnot _LoadAnnot(string name, int xref)
        {
            PdfPage page = GetPdfPage();
            PdfAnnot annot;
            if (xref == 0)
                annot = Utils.GetAnnotByName(this, name);
            else
                annot = Utils.GetAnnotByXref(this, xref);
            return annot == null ? null : new MuPDFAnnot(annot);
        }

        /// <summary>
        /// Return the first link on a page. Synonym of property first_link.
        /// </summary>
        /// <returns>Return the first link on a page. Synonym of property first_link.</returns>
        public MuPDFLink LoadLinks()
        {
            FzLink _val = mupdf.mupdf.fz_load_links(AsFzPage(_pdfPage));
            if (_val.m_internal == null)
                return null;

            MuPDFLink val = new MuPDFLink(_val);
            val.ThisOwn = true;
            val.Parent = this;
            AnnotRefs.Add(val.GetHashCode(), val);

            val.Xref = 0;
            val.Id = "";
            if (Parent.IsPDF)
            {
                List<AnnotXref> xrefs = GetAnnotXrefs();
                xrefs = xrefs.Where(xref => xref.AnnotType == PdfAnnotType.PDF_ANNOT_LINK).ToList();
                if (xrefs.Count > 0)
                {
                    AnnotXref linkId = xrefs[0];
                    val.Xref = linkId.Xref;
                    val.Id = linkId.Id;
                }
            }
            else
            {
                val.Xref = 0;
                val.Id = "";
            }
            return val;
        }

        public MuPDFAnnot DeleteAnnot(MuPDFAnnot annot)
        {
            PdfPage page = GetPdfPage();
            while (true)
            {
                PdfAnnot irtAnnot = MuPDFAnnot.FindAnnotIRT(annot.ToPdfAnnot());
                if (irtAnnot == null)
                    break;
                page.pdf_delete_annot(irtAnnot);
            }
            PdfAnnot nextAnnot = annot.ToPdfAnnot().pdf_next_annot();
            page.pdf_delete_annot(annot.ToPdfAnnot());
            MuPDFAnnot val = new MuPDFAnnot(nextAnnot);

            if (val != null)
            {
                val.ThisOwn = true;
                val.Parent = this;
                if (AnnotRefs.Keys.Contains(val.GetHashCode()))
                    AnnotRefs[val.GetHashCode()] = val;
                else AnnotRefs.Add(val.GetHashCode(), val);
            }
            annot.Erase();
            return val;
        }

        public void DeleteLink(Link link)
        {
            void Finished()
            {
                if (link.Xref == 0) return;
                try
                {
                    string linkId = link.Id;
                    var linkObj = AnnotRefs[0];// MuPDFAnnotation or Link, Widget
                    linkObj.Erase();
                }
                catch (Exception)
                {
                    // pass
                }
            }

            PdfPage page = _pdfPage;
            if (page == null)
            {
                Finished();
                return;
            }

            int xref = link.Xref;
            if (xref < 1)
            {
                Finished();
                return;
            }

            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            if (annots == null)
            {
                Finished();
                return;
            }

            int len = annots.pdf_array_len();
            if (len == 0)
            {
                Finished();
                return;
            }

            int oxref = 0;
            int i;
            for (i = 0; i < len; i++)
            {
                oxref = annots.pdf_array_get(i).pdf_to_num();
                if (xref == oxref)
                    break;
            }

            if (xref != oxref)
            {
                Finished();
                return;
            }
            annots.pdf_array_delete(i);
            page.doc().pdf_delete_object(xref);
            page.obj().pdf_dict_put(new PdfObj("Annots"), annots);

            Utils.RefreshLinks(page);
            Finished();
        }

        public void Refresh()
        {
            MuPDFDocument doc = Parent;
            MuPDFPage page = doc.ReloadPage(this);

        }

        public void ExtendTextPage(MuPDFTextPage tpage, int flags = 0, Matrix m = null)
        {
            PdfPage page = _pdfPage;
            FzStextPage tp = tpage._nativeTextPage;
            FzStextOptions options = new FzStextOptions();
            options.flags = flags;

            FzDevice dev = new FzDevice(tp, options);
            mupdf.mupdf.fz_run_page(page.super(), dev, m.ToFzMatrix(), new FzCookie());
            mupdf.mupdf.fz_close_device(dev);
        }

        public List<Rect> GetBboxlog(bool layers = false)
        {
            int oldRotation = Rotation;
            if (oldRotation != 0)
                SetRotation(0);

            FzPage page = _pdfPage.super();
            List<Rect> rc = new List<Rect>();

            BoxDevice dev = new BoxDevice(rc, layers);
            page.fz_run_page(dev, new FzMatrix(), new FzCookie());
            dev.fz_close_device();

            if (oldRotation != 0)
                SetRotation(oldRotation);
            return rc;
        }

        public List<PathInfo> GetCDrawings(bool extended = false) // not complete
        {
            int oldRotation = Rotation;
            if (oldRotation != 0)
                SetRotation(0);

            FzPage page = new FzPage(_pdfPage);
            bool clips = extended ? true : false;
            FzRect prect = page.fz_bound_page();

            List<PathInfo> rc = new List<PathInfo>();
            LineartDevice dev = new LineartDevice(rc, clips);

            dev.Ptm = new FzMatrix(1, 0, 0, -1, 0, prect.y1);
            page.fz_run_page(dev, new FzMatrix(), new FzCookie());
            dev.fz_close_device();

            if (oldRotation != 0)
                SetRotation(oldRotation);
            return rc;
        }

        public void GetDrawings(bool extended = false) // not complete
        {
            List<string> allkeys = new List<string>() { "closePath", "fill", "color", "width", "lineCap", "lineJoin", "dashes", "stroke_opacity", "fill_opacity", "even_odd" };
            List<PathInfo> val = GetCDrawings(extended);

            /*foreach (Dictionary<string, dynamic> npath in val)
            {
                if (npath["Type"].StartsWith)
            }*/
        }

        public void GetImageBbox(string name, bool transform = false)
        {
            MuPDFDocument doc = Parent;
            if (doc.IsClosed || doc.IsEncrypted)
                throw new Exception("document closed or encrypted");

            Rect infRect = new Rect(1, 1, -1, -1);
            Matrix nullMat = new Matrix();
            (Rect, Matrix) rc;
            if (transform)
                rc = (infRect, nullMat);
        }

        public Pixmap GetPixmap(IdentityMatrix matrix = null, int dpi = 0, string colorSpace = null,
            Rect clip = null, bool alpha = false, bool annots = true)
        {
            if (matrix == null)
                matrix = new IdentityMatrix();

            float zoom;
            if (dpi != 0)
            {
                zoom = dpi / 72;
                matrix = new IdentityMatrix(zoom, zoom);
            }

            ColorSpace _colorSpace;
            if (string.IsNullOrEmpty(colorSpace))
                _colorSpace = new ColorSpace(Utils.CS_RGB);
            else if (colorSpace.ToUpper() == "GRAY")
                _colorSpace = new ColorSpace(Utils.CS_GRAY);
            else if (colorSpace.ToUpper() == "CMYK")
                _colorSpace = new ColorSpace(Utils.CS_CMYK);
            else _colorSpace = new ColorSpace(Utils.CS_RGB);

            if (!(new List<int>() { 1, 3, 4 }).Contains(_colorSpace.N))
                throw new Exception("unsupported colorspace");

            DisplayList dl = GetDisplayList(annots ? 1 : 0);
            Pixmap pix = dl.GetPixmap(matrix, colorSpace: _colorSpace, alpha: alpha ? 1 : 0, clip: clip);

            dl.Dispose();
            dl = null;
            if (dpi != 0)
                pix.SetDpi(dpi, dpi);
            return pix;
        }

        public DisplayList GetDisplayList(int annots = 1)
        {
            if (annots != 0)
                return new DisplayList(mupdf.mupdf.fz_new_display_list_from_page(_pdfPage.super()));
            else
                return new DisplayList(mupdf.mupdf.fz_new_display_list_from_page_contents(_pdfPage.super()));
        }

        /// <summary>
        /// Delete the image referred to by xef.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="xref"></param>
        public void DeleteImage(int xref)
        {
            Pixmap pix = new Pixmap(new ColorSpace(Utils.CS_GRAY), new IRect(0, 0, 1, 1), 1);
            pix.ClearWith();
            ReplaceImage(xref, pixmap: pix);
        }

        /// <summary>
        /// Replace the image referred to by xref.
        /// </summary>
        public void ReplaceImage(int xref, string filename = null, Pixmap pixmap = null, byte[] stream = null)
        {
            MuPDFDocument doc = Parent;
            if (!doc.XrefIsImage(xref))
                throw new Exception("xref not an image");
            if ((filename == null ? 0 : 1) + (pixmap == null ? 0 : 1) + (stream == null ? 0 : 1) != 1)
                throw new Exception("Exactly one of filename/stream/pixmap must be given");
            int newXref = InsertImage(Rect, filename: filename, stream: stream, pixmap: pixmap);
            doc.CopyXref(newXref, xref);
            List<int> xrefs = GetContents();
            int lastXref = xrefs[xrefs.Count - 1];
            doc.UpdateStream(lastXref, Encoding.UTF8.GetBytes(" "));
        }

        public void DeleteWidget()
        {

        }

        public Point DrawBezier(
            Point p1,
            Point p2,
            Point p3,
            Point p4,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            float width = 1,
            Morph morph = null,
            bool closePath = false,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
        )
        {
            Shape img = new Shape(this);
            Point ret = img.DrawBezier(p1, p2, p3, p4);
            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                closePath: closePath,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc
                );
            img.Commit(overlay);
            return ret;
        }

        public Point DrawSector(
            Point center,
            Point point,
            float beta,
            float[] color = null,
            float[] fill = null,
            string dashes = null,
            bool fullSector = true,
            Morph morph = null,
            float width = 1,
            bool closePath = false,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawSector(center, point, beta, fullSector);
            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                closePath: closePath,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawCircle(
            Point center,
            float radius,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawCircle(center, radius);

            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        /// <summary>
        /// Draw an oval given its containing rectangle or quad.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="fill"></param>
        /// <param name="morph"></param>
        /// <param name="dashes"></param>
        /// <param name="width"></param>
        /// <param name="lineCap"></param>
        /// <param name="lineJoin"></param>
        /// <param name="overlay"></param>
        /// <param name="strokeOpacity"></param>
        /// <param name="fillOpacity"></param>
        /// <param name="oc"></param>
        /// <returns></returns>
        public Point DrawOval(
            Rect rect,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawOval(rect);

            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawCurve(
            Point p1,
            Point p2,
            Point p3,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            bool closePath = false,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawCurve(p1, p2, p3);

            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                closePath: closePath,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawLine(
            Point p1,
            Point p2,
            float[] color = null,
            Morph morph = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawLine(p1, p2);

            img.Finish(
                color: color,
                dashes: dashes,
                width: width,
                closePath: false,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawPolyline(
            Point[] points,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawPolyline(points);

            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawQuad(
            Quad quad,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawQuad(quad);

            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawRect(
            Rect rect,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            string dashes = null,
            float width = 1.0f,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawRect(rect);

            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawSquiggle(
            Point p1,
            Point p2,
            float breadth = 2,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawSquiggle(p1, p2, breadth);
            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                closePath: false,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public Point DrawZigzag(
            Point p1,
            Point p2,
            float breadth = 2,
            float[] color = null,
            float[] fill = null,
            Morph morph = null,
            string dashes = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            Point ret = img.DrawZigzag(p1, p2, breadth);

            img.Finish(
                color: color,
                fill: fill,
                dashes: dashes,
                width: width,
                lineCap: lineCap,
                lineJoin: lineJoin,
                morph: morph,
                strokeOpacity: strokeOpacity,
                fillOpacity: fillOpacity,
                oc: oc);
            img.Commit(overlay);

            return ret;
        }

        public List<Block> GetImageInfo(bool hashes = false, bool xrefs = false)
        {
            MuPDFDocument doc = Parent;
            if (xrefs && doc.IsPDF)
                hashes = true;
            if (!doc.IsPDF)
                xrefs = false;
            List<Block> imgInfo = _imageInfo;
            if (imgInfo == null)
            {
                MuPDFTextPage textpage = GetTextPage(flags: (int)TextFlags.TEXT_PRESERVE_IMAGES);
                imgInfo = textpage.ExtractImageInfo(hashes ? 1 : 0);
                textpage = null;
                if (hashes)
                    _imageInfo = imgInfo;
            }

            if (!xrefs || !doc.IsPDF)
            {
                return imgInfo;
            }

            List<Entry> imgList = GetImages();
            Dictionary<string, int> digests = new Dictionary<string, int>();
            foreach (Entry entry in imgList)
            {
                int xref = entry.Xref;
                Pixmap pix = new Pixmap(MuPDFDocument.AsPdfDocument(Parent), xref);
                digests.Add(Encoding.UTF8.GetString(pix.Digest), xref);
                pix = null;
            }
            for (int i = 0; i < imgInfo.Count; i ++)
            {
                Block item = imgInfo[i];
                int xref = digests.GetValueOrDefault(Encoding.UTF8.GetString(item.Digest.ToArray()));
                item.Xref = xref;
                imgInfo[i] = item;
            }
            return imgInfo;
        }

        public List<Entry> GetImages(bool full = false)
        {
            return Parent.GetPageImages(Number, full);
        }

        public void GetImageRects(string name, bool transform = false)
        {
            List<Entry> imgs = GetImages();
            imgs = imgs.Where(img => img.RefName == name).ToList();
            if (imgs.Count == 0)
                throw new Exception("bad image name");
            else if (imgs.Count != 1)
                throw new Exception("multiple image names found");
        }

        public List<Box> GetImageRects(int name, bool transform = false)
        {
            int xref = name;
            Pixmap pix = new Pixmap(MuPDFDocument.AsPdfDocument(Parent), xref);
            byte[] digest = pix.Digest;
            pix.Dispose();

            List<Block> infos = GetImageInfo(hashes: true);
            List<Box> bboxes = new List<Box>();
            if (!transform)
            {
                foreach (Block im in infos)
                {
                    if (im.Digest.ToArray().SequenceEqual(digest))
                        bboxes.Add(new Box() { Rect = new Rect(im.Bbox), Matrix = null });
                }
            }
            else
            {// is it possible to include transform in Image
                /*foreach (Block im in infos)
                {
                    if (im.Digest.ToArray().SequenceEqual(digest))
                        bboxes.Add(new Box() { Rect = new Rect(im.Bbox), Matrix = im.t });
                }*/
            }

            return bboxes;
        }

        /// <summary>
        /// Return the label for this PDF page.
        /// </summary>
        /// <returns>The label (str) of the page. Errors return an empty string.</returns>
        public string GetLabel()
        {
            List<Label> labels = Parent.GetPageLabels();
            if (labels.Count == 0)
                return "";
            labels.Sort();
            return Utils.GetPageLabel(Number, labels);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="option"></param>
        /// <param name="clip"></param>
        /// <param name="flags"></param>
        /// <param name="textpage"></param>
        /// <param name="sort"></param>
        /// <param name="delimiters"></param>
        /// <returns>return value depends on option type.
        /// if option is "text", </returns>
        public dynamic GetText(
            string option = "text",
            Rect clip = null,
            int flags = 0,
            MuPDFTextPage textpage = null,
            bool sort = false,
            char[] delimiters = null
            )
        {
            return Utils.GetText(this, option, clip, flags, textpage, sort, delimiters);
        }

        /// <summary>
        /// Return the text blocks on a page
        /// </summary>
        /// <param name="flags">control the amount of data parsed into the textpage</param>
        /// <returns> A list of the blocks. Each item contains the containing rectangle coordinates, text lines, block type and running block number</returns>
        public List<TextBlock> GetTextBlocks(
            Rect clip = null,
            int flags = 0,
            MuPDFTextPage textPage = null,
            bool sort = false)
        {
            return Utils.GetTextBlocks(this, clip, flags, textPage, sort);
        }

        /// <summary>
        /// Return text selected between p1 and p2
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="clip"></param>
        /// <param name="textPage"></param>
        /// <returns></returns>
        public string GetTextSelection(
            Point p1,
            Point p2,
            Rect clip = null,
            MuPDFTextPage textPage = null)
        {
            return Utils.GetTextSelection(this, p1, p2, clip, textPage);
        }

        public List<WordBlock> GetTextWords(
            Rect clip = null,
            int flags = 0,
            MuPDFTextPage textPage = null,
            bool sort = false,
            char[] delimiters = null)
        {
            return Utils.GetTextWords(this, clip, flags, textPage, sort, delimiters);
        }

        public string GetTextBox(
            Rect rect = null,
            MuPDFTextPage textPage = null)
        {
            return Utils.GetTextBox(this, rect, textPage);
        }

        /// <summary>
        /// Create a Textpage from combined results of normal and OCR text parsing
        /// </summary>
        /// <param name="flags">control content becoming part of the result</param>
        /// <param name="language">specify expected language(s)</param>
        /// <param name="dpi">resolution in dpi, default 72</param>
        /// <param name="full">whether to OCR the full page image, or only its images (default)</param>
        /// <param name="tessdata"></param>
        /// <returns></returns>
        public MuPDFTextPage GetTextPageOcr(
            int flags = 0,
            string language = "eng",
            int dpi = 72,
            bool full = false,
            string tessdata = null)
        {
            if (string.IsNullOrEmpty(Utils.TESSDATA_PREFIX) && string.IsNullOrEmpty(tessdata))
                throw new Exception("No OCR support: TESSDATA_PREFIX not set");

            MuPDFTextPage FullOcr(MuPDFPage page, int dpi, string language, int flags)
            {
                float zoom = dpi / 72.0f;
                Matrix mat = new Matrix(zoom, zoom);
                Pixmap pix = page.GetPixmap(matrix: (IdentityMatrix)mat);
                MuPDFDocument ocrPdf = new MuPDFDocument("pdf", pix.PdfOcr2Bytes(0, language, tessdata));

                MuPDFPage ocrPage = ocrPdf.LoadPage(0);
                float unZoom = page.Rect.Width / ocrPage.Rect.Width;
                Matrix ctm = new Matrix(unZoom, unZoom) * page.DerotationMatrix;
                MuPDFTextPage tp = ocrPage.GetTextPage(flags: flags, matrix: ctm);
                ocrPdf.Close();

                pix = null;
                tp.Parent = this;
                return tp;
            }

            if (full)
                return FullOcr(this, dpi, language, flags);
            MuPDFTextPage tp = GetTextPage(flags: flags);
            foreach (Block block in (GetText("dict", flags: (int)TextFlags.TEXT_PRESERVE_IMAGES) as PageInfo).Blocks)
            {
                if (block.Type != 1)
                    continue;
                Rect bbox = new Rect(block.Bbox);
                if (bbox.Width <= 3 || bbox.Height <= 3)
                    continue;
                try
                {
                    Pixmap pix = new Pixmap(block.Image);
                    if (pix.N - pix.Alpha != 3)
                        pix = new Pixmap(new ColorSpace(Utils.CS_RGB), pix);
                    if (pix.Alpha != 0)
                        pix = new Pixmap(pix, 0);
                    MuPDFDocument imgDoc = new MuPDFDocument(
                        "pdf",
                        pix.PdfOcr2Bytes(language: language, tessdata: tessdata)
                        );
                    MuPDFPage imgPage = imgDoc.LoadPage(0);
                    pix = null;
                    Rect imgRect = imgPage.Rect;
                    Matrix shrink = new Matrix(1 / imgRect.Width, 1 / imgRect.Height);
                    Matrix mat = shrink; //* block.;

                    imgPage.ExtendTextPage(tp, flags: 0, m: mat);
                    imgDoc.Close();
                }
                catch (Exception)
                {
                    tp = null;
                    Console.WriteLine("Falling back to full page OCR");
                    return FullOcr(this, dpi, language, flags);
                }
            }
            return tp;
        }

        public float InsertTextbox(
            Rect rect,
            dynamic text,
            float fontSize = 11,
            float lineHeight = 0,
            string fontName = "helv",
            string fontFile = null,
            int setSimple = 0,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int expandTabs = 1,
            int align = 0,
            float borderWidth = 0.05f,
            int renderMode = 0,
            int rotate = 0,
            Morph morph = null,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Shape img = new Shape(this);
            float ret = img.InsertTextbox(
                rect,
                text,
                fontSize,
                lineHeight,
                fontName,
                fontFile,
                setSimple != 0,
                encoding,
                color,
                fill,
                expandTabs,
                align,
                renderMode,
                borderWidth,
                rotate,
                morph,
                strokeOpacity,
                fillOpacity,
                oc
                );
            if (ret >= 0)
                img.Commit(overlay);
            return ret;
        }

        public void UpdateLink(Link link)
        {
            Utils.UpdateLink(this, link);
        }

        public void CleanContetns(int sanitize = 1)
        {
            if (sanitize == 0 && IsWrapped)
                WrapContents();
            PdfPage page = _pdfPage;
            if (page.m_internal == null)
                return;
            PdfFilterOptions filter = Utils.MakePdfFilterOptions(recurse: 1, sanitize: sanitize);
            page.doc().pdf_filter_page_contents(page, filter);
        }

        public void WrapContents()
        {
            if (IsWrapped)
                return;
            Utils.InsertContents(this, Encoding.UTF8.GetBytes("q\n"), 0);
            Utils.InsertContents(this, Encoding.UTF8.GetBytes("\nQ"), 1);
            WasWrapped = true;
        }

        public byte[] ReadContents()
        {
            return Utils.GetAllContents(this);
        }

        public Shape NewShape()
        {
            return new Shape(this);
        }

        public MuPDFAnnot AddWidget(Widget widget)
        {
            MuPDFDocument doc = Parent;
            if (!doc.IsPDF)
                throw new Exception("is no PDF");
            widget.Validate();
            PdfPage page = GetPdfPage();
            PdfDocument pdf = page.doc();
            PdfAnnot annot = Utils.CreateWidget(pdf, page, (PdfWidgetType)widget.FieldType, widget.FieldName);
            if (annot.m_internal == null)
                throw new Exception("cannot create widget");
            Utils.AddAnnotId(annot, "W");
            
            MuPDFAnnot annot_ = new MuPDFAnnot(annot);
            annot_.ThisOwn = true;
            annot_.Parent = this;
            AnnotRefs[annot_.GetHashCode()] = annot_;

            widget.Parent = annot_.Parent;
            widget._annot = annot;
            widget.Update();

            return annot_;
        }

        public void Dispose()
        {
            _pdfPage.Dispose();
        }
    }

    public class BoxDevice : FzDevice2
    {
        public List<Rect> rc { get; set; }

        public bool layers { get; set; }
        public BoxDevice(List<Rect> rc, bool layers) : base()
        {
            this.rc = rc;
            this.layers = layers;

            use_virtual_fill_path();
            use_virtual_stroke_path();
            use_virtual_fill_text();
            use_virtual_stroke_text();
            use_virtual_ignore_text();
            use_virtual_fill_shade();
            use_virtual_fill_image();
            use_virtual_fill_image_mask();

            use_virtual_begin_layer();
            use_virtual_end_layer();
        }

        // not implemented some logics
    }

    public class LineartDevice : FzDevice2
    {
        public int SeqNo { get; set; }
        public int Depth { get; set; }
        public bool Clips { get; set; }
        public int Method { get; set; }

        public PathInfo PathDict { get; set; }
        public List<FzRect> Scissors { get; set; }
        public int LineWidth { get; set; }
        public FzMatrix Ptm { get; set; }
        public FzMatrix Ctm { get; set; }
        public FzMatrix Rot { get; set; }

        public FzPoint LastPoint { get; set; }
        public FzRect PathRect { get; set; }
        public float PathFactor { get; set; }
        public int LineCount { get; set; }
        public int PathType { get; set; }
        public string LayerName { get; set; }

        public List<PathInfo> Out { get; set; }

        public LineartDevice(List<PathInfo> rc, bool clips) : base()
        {
            use_virtual_fill_path();
            use_virtual_stroke_path();
            use_virtual_clip_path();
            use_virtual_clip_image_mask();
            use_virtual_clip_stroke_path();
            use_virtual_clip_stroke_text();
            use_virtual_clip_text();


            use_virtual_fill_text();
            use_virtual_stroke_text();
            use_virtual_ignore_text();


            use_virtual_fill_shade();
            use_virtual_fill_image();
            use_virtual_fill_image_mask();

            use_virtual_pop_clip();

            use_virtual_begin_group();
            use_virtual_end_group();

            use_virtual_begin_layer();
            use_virtual_end_layer();

            SeqNo = 0;
            Depth = 0;
            Clips = clips;
            Method = 0;
            Out = rc;

            PathDict = new PathInfo();
            Scissors = new List<FzRect>();
            LineWidth = 0;
            Ptm = new FzMatrix();
            Ctm = new FzMatrix();
            Rot = new FzMatrix();
            LastPoint = new FzPoint();
            PathRect = new FzRect();
            PathFactor = 0;
            LineCount = 0;
            PathType = 0;
            LayerName = "";
        }

        public override void clip_path(fz_context arg_0, SWIGTYPE_p_fz_path arg_2, int arg_3, fz_matrix arg_4, fz_rect arg_5)
        {
            if (!Clips)
                return;

            Ctm = new FzMatrix(arg_4);
            PathType = Utils.trace_device_CLIP_PATH;
            LineartPath(arg_0, arg_2);
            if (PathDict == null)
                return;

            PathDict.Type = "clip";
            PathDict.EvenOdd = Convert.ToBoolean(arg_3);
            PathDict.ClosePath = false;

            PathDict.Scissor = new Rect(Utils.ComputerScissor(this));
            PathDict.Level = Depth;
            PathDict.Layer = LayerName;
            AppendMerge();
            Depth += 1;
        }

        public override void stroke_path(fz_context ctx, SWIGTYPE_p_fz_path path, fz_stroke_state stroke, fz_matrix ctm, fz_colorspace cs, SWIGTYPE_p_float color, float alpha, fz_color_params colorparam)
        {
            PathFactor = 1;
            if (Math.Abs(Ctm.a) == Math.Abs(Ctm.d))
                PathFactor = Math.Abs(Ctm.a);
            Ctm = new FzMatrix(ctm);
            PathType = Utils.trace_device_CLIP_STROKE_PATH;

            LineartPath(ctx, path);
            if (PathDict == null)
                return;
            PathDict.Type = "s";
            PathDict.StrokeOpacity = alpha;
            PathDict.Color = LineartColor(cs, color);
            PathDict.Width = PathFactor * stroke.linewidth;
            PathDict.LineCap = new List<LineCapType>() { (LineCapType)stroke.start_cap, (LineCapType)stroke.dash_cap, (LineCapType)stroke.end_cap };
            PathDict.LineJoin = PathFactor * (int)stroke.linejoin;

            PathDict.ClosePath = false;

            if (stroke.dash_len != 0)
            {
                FzBuffer buff = mupdf.mupdf.fz_new_buffer(256);
                buff.fz_append_string("[ ");
                for (int i = 0; i < stroke.dash_len; i++)
                {
                    float value = mupdf.mupdf.floats_getitem(stroke.dash_list, (uint)i);
                    buff.fz_append_string($"{PathFactor * value} ");
                }
                buff.fz_append_string($"] {PathFactor * stroke.dash_phase}");
                PathDict.Dashes = Encoding.UTF8.GetString(Utils.BinFromBuffer(buff));
            }
            else
                PathDict.Dashes = "[] 0";
            PathDict.Rect = new Rect(PathRect);
            PathDict.Layer = LayerName;
            PathDict.SeqNo = SeqNo;
            if (Clips)
                PathDict.Level = Depth;
            AppendMerge();
            SeqNo += 1;
        }

        public override void fill_path(fz_context ctx, SWIGTYPE_p_fz_path path, int evenOdd, fz_matrix ctm, fz_colorspace cs, SWIGTYPE_p_float color, float alpha, fz_color_params colorParams)
        {
            bool bEvenOdoo = evenOdd != 0 ? true : false;
            try
            {
                Ctm = new FzMatrix(ctm);
                PathType = Utils.trace_device_FILL_PATH;
                LineartPath(ctx, path);
                if (PathDict == null)
                    return;

                PathDict.Type = "f";
                PathDict.EvenOdd = bEvenOdoo;
                PathDict.FillOpacity = alpha;
                PathDict.Fill = LineartColor(cs, color);
                PathDict.Rect = new Rect(PathRect);
                PathDict.SeqNo = SeqNo;
                PathDict.Layer = LayerName;
                
                if (Clips)
                    PathDict.Level = Depth;

                AppendMerge();
                SeqNo += 1;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public override void clip_image_mask(fz_context ctx, fz_image image, fz_matrix ctm, fz_rect scissors)
        {
            if (!Clips) return;
            Utils.ComputerScissor(this);
            Depth += 1;
        }

        public override void clip_stroke_path(fz_context ctx, SWIGTYPE_p_fz_path path, fz_stroke_state stroke, fz_matrix ctm, fz_rect scissors)
        {
            if (!Clips)
                return;
            Ctm = new FzMatrix(ctm);
            PathType = Utils.trace_device_CLIP_STROKE_PATH;
            LineartPath(ctx, path);
            if (PathDict == null)
                return;

            PathDict.Type = "clip";
            PathDict.EvenOdd = false;
            PathDict.ClosePath = false;
            PathDict.Scissor = new Rect(Utils.ComputerScissor(this));
            PathDict.Level = Depth;
            PathDict.Layer = LayerName;
            AppendMerge();
            Depth += 1;
        }

        public override void clip_stroke_text(fz_context arg_0, fz_text arg_2, fz_stroke_state arg_3, fz_matrix arg_4, fz_rect arg_5)
        {
            if (!Clips)
                return;
            Utils.ComputerScissor(this);
            Depth += 1;
        }

        public override void clip_text(fz_context ctx, fz_text text, fz_matrix ctm, fz_rect scissor)
        {
            if (!Clips)
                return;
            Utils.ComputerScissor(this);
            Depth += 1;
        }

        public override void fill_text(fz_context arg_0, fz_text arg_2, fz_matrix arg_3, fz_colorspace arg_4, SWIGTYPE_p_float arg_5, float arg_6, fz_color_params arg_7)
        {
            SeqNo++;
        }

        public override void stroke_text(fz_context arg_0, fz_text arg_2, fz_stroke_state arg_3, fz_matrix arg_4, fz_colorspace arg_5, SWIGTYPE_p_float arg_6, float arg_7, fz_color_params arg_8)
        {
            SeqNo++;
        }

        public override void ignore_text(fz_context arg_0, fz_text arg_2, fz_matrix arg_3)
        {
            SeqNo++;
        }

        public override void fill_shade(fz_context arg_0, fz_shade arg_2, fz_matrix arg_3, float arg_4, fz_color_params arg_5)
        {
            SeqNo++;
        }

        public override void fill_image(fz_context arg_0, fz_image arg_2, fz_matrix arg_3, float arg_4, fz_color_params arg_5)
        {
            SeqNo++;
        }

        public override void fill_image_mask(fz_context arg_0, fz_image arg_2, fz_matrix arg_3, fz_colorspace arg_4, SWIGTYPE_p_float arg_5, float arg_6, fz_color_params arg_7)
        {
            SeqNo++;
        }

        public override void pop_clip(fz_context arg_0)
        {
            if (!Clips || Scissors == null)
                return;
            int len = Scissors.Count;
            if (len < 1)
                return;
            Scissors.RemoveAt(Scissors.Count - 1);
            Depth -= 1;
        }

        public override void begin_group(fz_context ctx, fz_rect bbox, fz_colorspace cs, int isolated, int knockout, int blendmode, float alpha)
        {
            if (!Clips)
                return;
            PathDict = new PathInfo()
            {
                Type = "group",
                Rect = new Rect(new FzRect(bbox)),
                Isolated = Convert.ToBoolean(isolated),
                Knockout = Convert.ToBoolean(knockout),
                BlendMode = mupdf.mupdf.fz_blendmode_name(blendmode),
                Opacity = alpha,
                Level = Depth,
                Layer = LayerName
            };
            
            AppendMerge();
            Depth += 1;
        }

        public override void end_group(fz_context arg_0)
        {
            if (!Clips)
                return;
            Depth -= 1;
        }

        public override void begin_layer(fz_context arg_0, string name)
        {
            if (name == "" || name == null)
                LayerName = name;
            else
                LayerName = "";
        }

        public override void end_layer(fz_context arg_0)
        {
            LayerName = "";
        }

        public float[] LineartColor(fz_colorspace colorSpace, SWIGTYPE_p_float color)
        {
            if (colorSpace != null)
            {
                try
                {
                    FzColorspace cs = new FzColorspace(mupdf.FzColorspace.Fixed.Fixed_RGB);
                    FzColorParams cp = new FzColorParams();

                    IntPtr pColor = Marshal.AllocHGlobal(3 * sizeof(float));
                    SWIGTYPE_p_float swigColor = new SWIGTYPE_p_float(pColor, true);
                    mupdf.mupdf.ll_fz_convert_color(colorSpace, color, cs.m_internal, swigColor, null, cp.internal_());

                    float[] ret = new float[3];
                    Marshal.Copy(SWIGTYPE_p_float.getCPtr(swigColor).Handle, ret, 0, 3);
                    Marshal.FreeHGlobal(pColor);

                    return ret;
                }
                catch (Exception) { return null; }
            }
            return null;
        }

        public void LineartPath(fz_context arg0, SWIGTYPE_p_fz_path path)
        {
            try
            {
                PathRect = new FzRect(FzRect.Fixed.Fixed_INFINITE);
                LineCount = 0;
                LastPoint = new FzPoint(0, 0);
                PathDict = new PathInfo();
                PathDict.Items = new List<Item>();

                Walker walker = new Walker(this);

                FzPathWalker pathWalker = new FzPathWalker(walker.m_internal);
                SWIGTYPE_p_void swigArg = new SWIGTYPE_p_void(fz_path_walker.getCPtr(walker.m_internal).Handle, true);

                mupdf.mupdf.fz_walk_path(new FzPath(mupdf.mupdf.ll_fz_keep_path(path)), pathWalker, swigArg);

                if (PathDict.Items.Count == 0)
                    PathDict = null;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void AppendMerge()
        {
            void Append()
            {
                this.Out.Add(PathDict); // copy key & value
                PathDict = null;
            }

            int len = Out.Count;
            if (len == 0)
            {
                Append();
                return;
            }

            string type = PathDict.Type;
            if (type != "s")
            {
                Append();
                return;
            }

            PathInfo prev = Out[Out.Count - 1];
            string prevType = prev.Type;

            if (prevType != "f")
            {
                Append();
                return;
            }
            List<Item> prevItems = prev.Items;
            List<Item> thisItems = PathDict.Items;
            if (!Enumerable.SequenceEqual(prevItems, thisItems))
            {
                Append();
                return;
            }

            int rc;
            try
            {
                prev = new PathInfo(PathDict);
                rc = 0;
            }
            catch (Exception)
            {
                rc = -1;
            }

            if (rc == 0)
            {
                prev.Type = "fs";
                PathDict = null;
            }
            else
            {
                Console.WriteLine("could not merge stroke and fill path");
                Append();
            }
        }

        
    }

    public class Walker : FzPathWalker2
    {
        public LineartDevice Dev;

        public Walker(LineartDevice dev) : base()
        {
            use_virtual_moveto();
            use_virtual_lineto();
            use_virtual_curveto();
            use_virtual_closepath();
            this.Dev = dev;
        }

        public override void closepath(fz_context arg_0)
        {
            try
            {
                Console.WriteLine(Dev.LineCount);
                if (Dev.LineCount == 3)
                    if (Utils.CheckRect(Dev) != 0)
                        return;
                Dev.PathDict.ClosePath = true;
                Dev.LineCount = 0;
            }
            catch (Exception)
            {

            }
        }

        public override void curveto(fz_context arg_0, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            try
            {
                Dev.LineCount = 0;
                FzPoint p1 = mupdf.mupdf.fz_make_point(x1, y1);
                FzPoint p2 = mupdf.mupdf.fz_make_point(x2, y2);
                FzPoint p3 = mupdf.mupdf.fz_make_point(x3, y3);
                p1 = mupdf.mupdf.fz_transform_point(p1, Dev.Ctm);
                p2 = mupdf.mupdf.fz_transform_point(p2, Dev.Ctm);
                p3 = mupdf.mupdf.fz_transform_point(p3, Dev.Ctm);
                Dev.PathRect = mupdf.mupdf.fz_include_point_in_rect(Dev.PathRect, p1);
                Dev.PathRect = mupdf.mupdf.fz_include_point_in_rect(Dev.PathRect, p2);
                Dev.PathRect = mupdf.mupdf.fz_include_point_in_rect(Dev.PathRect, p3);

                Item curve = new Item() { Type = "c", LastPoint = new Point(Dev.LastPoint), P1 = new Point(p1), P2 = new Point(p2), P3 = new Point(p3)};

                Dev.LastPoint = p3;
                Dev.PathDict.Items.Add(curve);
            }
            catch (Exception ex)
            {
                throw new Exception("curveto exception");
            }
        }

        public override void lineto(fz_context arg_0, float x, float y)
        {
            try
            {
                FzPoint p1 = mupdf.mupdf.fz_transform_point(mupdf.mupdf.fz_make_point(x, y), Dev.Ctm);
                Dev.PathRect = mupdf.mupdf.fz_include_point_in_rect(Dev.PathRect, p1);
                Item line = new Item() { Type = "l", LastPoint = new Point(Dev.LastPoint), P1 = new Point(p1) };

                Dev.LastPoint = p1;

                List<Item> items = Dev.PathDict.Items;
                items.Add(line);
                Dev.LineCount += 1;
                if (Dev.LineCount == 4 && Dev.PathType != Utils.trace_device_FILL_PATH)
                    Utils.CheckQuad(Dev);
            }
            catch (Exception)
            {
                throw new Exception("lineto exception");
            }
        }

        public override void moveto(fz_context arg_0, float x, float y)
        {
            try
            {
                Dev.LastPoint = mupdf.mupdf.fz_transform_point(mupdf.mupdf.fz_make_point(x, y), Dev.Ctm);
                if (Dev.PathRect.fz_is_infinite_rect() != 0)
                    Dev.PathRect = mupdf.mupdf.fz_make_rect
                        (Dev.LastPoint.x, Dev.LastPoint.y,
                          Dev.LastPoint.x, Dev.LastPoint.y);
                Dev.LineCount = 0;
            }
            catch (Exception) { throw new Exception("moveto exception"); }
        }
    }
}
