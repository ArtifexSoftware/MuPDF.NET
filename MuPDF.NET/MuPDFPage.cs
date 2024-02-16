using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Text;
using mupdf;
using static mupdf.FzBandWriter;

namespace MuPDF.NET
{
    public class MuPDFPage : IDisposable
    {
        private PdfPage _nativePage;

        private MuPDFDocument _parent;

        public PdfObj PageObj
        {
            get
            {
                return _nativePage.obj();
            }
        }

        public int Number { get; set; }

        public bool IsOwn { get; set; }

        public Point MediaBoxSize
        {
            get
            {
                return new Point(MediaBox.X1, MediaBox.Y1);
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
                if (_nativePage == null)
                    return 0;
                return Utils.PageRotation(_nativePage);
            }
        }

        public Dictionary<int, dynamic> AnnotRefs = new Dictionary<int, dynamic>();

        public Rect GetBound()
        {
            FzPage page = _nativePage.super();
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
                PdfPage page = _nativePage;
                if (page == null)
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
                PdfPage page = _nativePage;
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
                PdfPage page = _nativePage;
                Rect ret = null;
                if (page == null)
                    ret = new Rect(page.pdf_bound_page(fz_box_type.FZ_CROP_BOX));
                else
                    ret = Utils.GetCropBox(page.obj());

                return ret;
            }
        }

        public Matrix DerotationMatrix
        {
            get
            {
                PdfPage page = _nativePage;
                if (page == null)
                    return new Matrix(new FzMatrix());//issues
                return new Matrix(Utils.DerotatePageMatrix(page));
            }
        }

        public MuPDFAnnotation FirstAnnot
        {
            get
            {
                PdfPage page = _nativePage;
                if (page == null)
                    return null;
                PdfAnnot annot = page.pdf_first_annot();
                if (annot == null)
                    return null;

                MuPDFAnnotation ret = new MuPDFAnnotation(annot);
                return ret;
            }
        }

        public FzLink FirstLink//issue
        {
            get
            {
                
                return _nativePage.pdf_load_links();
            }
        }

        public Widget FirstWidget
        {
            get
            {
                /*int annot = 0;*/
                PdfPage page = _nativePage;
                if (page == null)
                    return null;
                PdfAnnot annot = page.pdf_first_widget();
                if (annot == null)
                    return null;

                MuPDFAnnotation val = new MuPDFAnnotation(annot);

                Widget widget = new Widget();
                return widget;//issue
            }
        }

        public List<Quad> SearchFor(
            string needle,
            Rect clip = null,
            bool quads = false,
            int flags = (int)(TextFlags.TEXT_DEHYPHENATE | TextFlags.TEXT_PRESERVE_WHITESPACE | TextFlags.TEXT_PRESERVE_LIGATURES | TextFlags.TEXT_MEDIABOX_CLIP),
            MuPDFSTextPage stPage = null
            )
        {
            MuPDFSTextPage tp = stPage;
            if (tp == null)
                tp = GetSTextPage(clip, flags);
            List<Quad> ret = MuPDFSTextPage.Search(tp, needle, quad: quads);
            if (stPage == null)
                tp.Dispose();

            return ret;
        }

        public Rect OtherBox(string boxtype)
        {
            FzRect rect = new FzRect(FzRect.Fixed.Fixed_INFINITE);
            PdfPage page = _nativePage;
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

        public MuPDFDocument Parent
        {
            get
            {
                return _parent;
            }
        }

        public MuPDFPage(PdfPage nativePage, MuPDFDocument parent)
        {
            _nativePage = nativePage;
            _parent = parent;

            if (_nativePage == null)
                Number = 0;
            else
                Number = _nativePage.m_internal.super.number;
        }

        public MuPDFPage(FzPage fzPage, MuPDFDocument parent)
        {
            _nativePage = fzPage.pdf_page_from_fz_page();
            _parent = parent;

            if (_nativePage == null)
                Number = 0;
            else
                Number = _nativePage.m_internal.super.number;
        }

        private PdfAnnot AddCaretAnnot(Point point)
        {
            PdfPage page = _nativePage;
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

        private MuPDFAnnotation AddFileAnnot(
            Point point,
            byte[] buffer_,
            string filename,
            dynamic ufilename,
            string desc,
            string icon)
        {
            PdfPage page = _nativePage;
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

            return new MuPDFAnnotation(annot);
        }

        public MuPDFAnnotation AddFreeTextAnnot(
            FzRect rect,
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
            PdfPage page = _nativePage;
            float[] fColor = MuPDFAnnotation.ColorFromSequence(fillColor);
            float[] tColor = MuPDFAnnotation.ColorFromSequence(textColor);
            if (rect.fz_is_infinite_rect() != 0 && rect.fz_is_empty_rect() != 0)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_RECT"]);
            }

            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_FREE_TEXT);
            PdfObj annotObj = annot.pdf_annot_obj();
            annot.pdf_set_annot_contents(text);
            annot.pdf_set_annot_rect(rect);
            annotObj.pdf_dict_put_int(new PdfObj("Rotate"), rotate);
            annotObj.pdf_dict_put_int(new PdfObj("Q"), align);

            if (fColor.Length > 0)
            {
                IntPtr fColorPtr = new IntPtr(fColor.Length);
                Marshal.Copy(fColor, 0, fColorPtr, fColor.Length);
                SWIGTYPE_p_float swigFColor = new SWIGTYPE_p_float(fColorPtr, false);
                annot.pdf_set_annot_color(fColor.Length, swigFColor);
            }

            Utils.MakeAnnotDA(annot, tColor.Length, tColor, fontName, fontSize);
            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            MuPDFAnnotation val = new MuPDFAnnotation(annot);

            byte[] ap = val.GetAP();
            int BT = Convert.ToString(ap).IndexOf("BT");
            int ET = Convert.ToString(ap).IndexOf("ET");
            ap = Utils.ToByte(Convert.ToString(ap).Substring(BT, ET));

            Rect r = new Rect(rect);
            float w = r[2] - r[0];
            float h = r[3] - r[1];
            if ((new List<float>() { 90, -90, 270}).Contains(rotate))
            {
                float t = w;
                w = h;
                h = t;
            }

            byte[] re = Utils.ToByte($"0 0 {w} {h} re");
            ap = MuPDFAnnotation.MergeByte(MuPDFAnnotation.MergeByte(re, Utils.ToByte($"\nW\nn\n")), ap);
            byte[] ope = null;
            byte[] bWidth = null;
            byte[] fillBytes = Utils.ToByte((MuPDFAnnotation.ColorCode(fColor, "f")));
            if (fillBytes != null || fillBytes.Length != 0)
            {
                fillBytes = MuPDFAnnotation.MergeByte(fillBytes, Utils.ToByte("\n"));
                ope = Utils.ToByte("f");
            }
            byte[] strokeBytes = Utils.ToByte(MuPDFAnnotation.ColorCode(borderColor, "c"));
            if (strokeBytes != null || strokeBytes.Length != 0)
            {
                strokeBytes = MuPDFAnnotation.MergeByte(strokeBytes, Utils.ToByte("\n"));
                bWidth = Utils.ToByte("1 w\n");
                ope = Utils.ToByte("S");
            }

            if (fillBytes != null && strokeBytes != null)
                ope = Utils.ToByte("B");
            if (ope != null)
            {
                ap = MuPDFAnnotation.MergeByte(
                    MuPDFAnnotation.MergeByte(
                        MuPDFAnnotation.MergeByte(
                            MuPDFAnnotation.MergeByte(
                                MuPDFAnnotation.MergeByte(bWidth, fillBytes), strokeBytes), re), Utils.ToByte("\n")),
                    MuPDFAnnotation.MergeByte(Utils.ToByte("\n"), ap));
            }

            val.SetAP(ap);
            return val;
        }

        private MuPDFAnnotation AddInkAnnot(dynamic list)
        {
            PdfPage page = _nativePage;
            if (list is Tuple ||  list is List<List<Point>>)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_ARG_INK_ANNOT"]);
            }

            FzMatrix ctm = new FzMatrix();
            page.pdf_page_transform(new FzRect(0), ctm);
            FzMatrix invCtm = ctm.fz_invert_matrix();
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_INK);
            PdfObj annotObj = annot.pdf_annot_obj();
            int n0 = list.Count;
            PdfObj inkList = page.doc().pdf_new_array(n0);

            for (int j = 0; j < n0; j ++)
            {
                dynamic subList = list[j];
                int n1 = subList.Count;
                PdfObj stroke = page.doc().pdf_new_array(n1 * 2);

                for (int i = 0; i < n1; i ++)
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
            return new MuPDFAnnotation(annot);
        }

        private MuPDFAnnotation AddLineAnnot(Point p1, Point p2)
        {
            PdfPage page = _nativePage;
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_LINE);
            annot.pdf_set_annot_line(p1.ToFzPoint(), p2.ToFzPoint());
            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            return new MuPDFAnnotation(annot);
        }

        private MuPDFAnnotation AddMultiLine(List<Point> points, PdfAnnotType annotType)
        {
            PdfPage page = _nativePage;
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
            return new MuPDFAnnotation(annot);
        }

        private MuPDFAnnotation AddRedactAnnot(Quad quad, string text = null, string dataStr = null, int align = 0, float[] fill = null, float[] textColr = null)
        {
            PdfPage page = _nativePage;
            float[] fCol = new float[4] { 1, 1, 1, 0 };
            int nFCol = 0;
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_REDACT);
            Rect r = quad.Rect;
            annot.pdf_set_annot_rect(r.ToFzRect());

            if (fill != null)
            {
                fCol = MuPDFAnnotation.ColorFromSequence(fill);
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
            return new MuPDFAnnotation(annot);
        }

        private MuPDFAnnotation AddSequareOrCircle(Rect rect, PdfAnnotType annotType)
        {
            PdfPage page = _nativePage;
            FzRect r = rect.ToFzRect();
            if (r.fz_is_infinite_rect() != 0 || r.fz_is_empty_rect() != 0)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_RECT"]);
            }

            PdfAnnot annot = page.pdf_create_annot((pdf_annot_type)annotType);
            annot.pdf_set_annot_rect(r);
            annot.pdf_update_annot();
            return new MuPDFAnnotation(annot);
        }

        private MuPDFAnnotation AddStampAnnot(Rect rect, int stamp = 0)
        {
            PdfPage page = _nativePage;
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
            if (r.fz_is_infinite_rect() != 0|| r.fz_is_empty_rect() != 0)
            {
                throw new Exception(Utils.ErrorMessages["MSG_BAD_RECT"]);
            }
            if (Utils.INRANGE(stamp, 0 , n - 1))
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
            return new MuPDFAnnotation(annot);
        }

        private MuPDFAnnotation AddTextAnnot(Point point, string text, string icon = null)
        {
            PdfPage page = _nativePage;
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_TEXT);
            FzRect r = annot.pdf_annot_rect();
            r = mupdf.mupdf.fz_make_rect(point.X, point.Y, point.X + r.x1 - r.x0, point.Y + r.y1 - r.y0);
            annot.pdf_set_annot_rect(r);
            annot.pdf_set_annot_contents(text);
            if (icon != null || icon != "")
                annot.pdf_set_annot_icon_name(icon);

            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            return new MuPDFAnnotation(annot);
        }

        private MuPDFAnnotation AddTextMarker(List<Quad> quads, pdf_annot_type annotType)
        {
            MuPDFAnnotation ret = null;
            PdfAnnot annot = null;
            PdfPage page = new PdfPage(_nativePage.m_internal);
            if (!_parent.IsPDF)
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
                catch(Exception)
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
                ret = new MuPDFAnnotation(annot);
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

            List<BlockStruct> blocks = Utils.GetText(this, "dict", clip, 0).BLOCKS;

            List<Rect> lines = new List<Rect>();
            foreach (BlockStruct b in  blocks)
            {
                Rect bbox = new Rect(b.Bbox);
                if (bbox.IsInfinite || bbox.IsEmpty)
                    continue;

                foreach (LineStruct line in b.Lines)
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

        public MuPDFAnnotation AddHighlightAnnot(dynamic quads, Point start = null, Point stop = null, Rect clip = null)
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
            MuPDFAnnotation ret = AddTextMarker(q, pdf_annot_type.PDF_ANNOT_HIGHLIGHT);
            return ret;
        }

        private MuPDFSTextPage _GetSTextPage(Rect clip = null, int flags = 0, Matrix matrix = null)
        {
            PdfPage page = _nativePage;
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
            //Console.WriteLine(new MuPDFSTextPage(stPage).ExtractText());
            return new MuPDFSTextPage(stPage);
        }

        public MuPDFSTextPage GetSTextPage(Rect clip, int flags = 0, Matrix matrix = null)
        {
            if (matrix == null)
                matrix = new Matrix(1, 1);
            int oldRotation = Rotation;
            if (oldRotation != 0) SetRotation(0);
            MuPDFSTextPage stPage = null;
            try
            {
                stPage = _GetSTextPage(clip, flags, matrix);
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
            PdfPage page = _nativePage;
            page.obj().pdf_dict_put_int(new PdfObj("Rotate"), rotation);

        }

        public MuPDFAnnotation AddUnderlineAnnot(dynamic quads = null, Point start = null, Point stop = null, Rect clip = null)
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

        public MuPDFAnnotation AddSquigglyAnnot(dynamic quads = null, Point start = null, Point stop = null, Rect clip = null)
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

        public MuPDFAnnotation AddStrikeoutAnnot(dynamic quads = null, Point start = null, Point stop = null, Rect clip = null)
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

        public static PdfObj PdfObjFromStr(PdfDocument doc, string src)
        {
            byte[] bSrc = Encoding.UTF8.GetBytes(src);
            IntPtr scrPtr = Marshal.AllocHGlobal(bSrc.Length);
            Marshal.Copy(bSrc, 0, scrPtr, bSrc.Length);
            SWIGTYPE_p_unsigned_char swigSrc = new SWIGTYPE_p_unsigned_char(scrPtr, true);
            Marshal.FreeHGlobal(scrPtr);

            FzBuffer buffer_ = mupdf.mupdf.fz_new_buffer_from_copied_data(swigSrc, (uint)bSrc.Length);
            FzStream stream = buffer_.fz_open_buffer();
            PdfLexbuf lexBuf = new PdfLexbuf(256);
            PdfObj ret = doc.pdf_parse_stm_obj(stream, lexBuf);

            return ret;
        }

        public void AddAnnotFromString(List<string> links)
        {
            PdfPage page = _nativePage;
            int lCount = links.Count;
            if (lCount < 1)
                return;
            int i = -1;

            if (page.obj().pdf_dict_get(new PdfObj("Annots")) == null)
                page.obj().pdf_dict_put_array(new PdfObj("Annots"), lCount);
            PdfObj annots = page.obj().pdf_dict_get(new PdfObj("Annots"));
            Debug.Assert(annots == null, $"{lCount} is {annots}");

            for (i = 0; i < lCount; i ++)
            {
                string text = links[i];
                if (text == null)
                {
                    Console.WriteLine($"skipping bad link / annot item {i}.\\n");
                    continue;
                }

                try
                {
                    PdfObj annot = page.doc().pdf_add_object(MuPDFPage.PdfObjFromStr(page.doc(), text));
                    PdfObj indObj = page.doc().pdf_new_indirect(annot.pdf_to_num(), 0);
                    annots.pdf_array_push(indObj);
                }
                catch (Exception)
                {
                    Console.WriteLine($"skipping bad link / annot item {i}.");
                }
            }
        }

        public MuPDFAnnotation AddWidget(PdfWidgetType fieldType, string fieldName)
        {
            PdfPage page = _nativePage;
            PdfDocument pdf = page.doc();
            PdfAnnot annot = Utils.CreateWidget(pdf, page, fieldType, fieldName);

            if (annot == null)
                throw new Exception("cannot create widget");
            Utils.AddAnnotId(annot, "W");

            return new MuPDFAnnotation(annot);
        }

        private int ApplyRedactions(int images)
        {
            PdfPage page = _nativePage;
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
        private void Erase()
        {
            this.ResetAnnotRefs();
            try
            {
                /*_parent.ForgetPage(this);*/
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            _parent = null;
            IsOwn = false;
            Number = 0;
        }

        private List<(string, int)> GetResourceProperties()
        {
            PdfPage page = _nativePage;
            List<(string, int)> rc = Utils.GetResourceProperties(page.obj());

            return rc;
        }

        private void SetResourceProperty(string mc, int xref)
        {
            PdfPage page = _nativePage;
            Utils.SetResourceProperty(page.obj(), mc, xref);
        }

        public void InsertLink(LinkStruct link, bool mark = true)
        {
            string annot = Utils.GetLinkText(this, link);
            if (annot == "" || annot == null)
                throw new Exception("link kind not supported");
            AddAnnotFromString(new List<string>() { annot });
        }

        private void InsertImage(
            string filename = null, Pixmap pixmap = null, byte[] stream = null, byte[] imask = null, Rect clip = null,
            int overlay = 1, int rotate = 0, int keepProportion = 1, int oc = 0, int width = 0, int height = 0,
            int xref = 0, int alpha = -1, string imageName = null, string digests = null)
        {
            FzBuffer maskBuf = new FzBuffer();
            PdfPage page = _nativePage;
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
                    if (filename != null)
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
                dynamic temp = null;//digest.//issue

                if (temp != null)
                {
                    imgXRef = temp;
                    PdfObj refer = page.doc().pdf_new_indirect(imgXRef, 0);
                    do_process_stream = 0;
                    do_have_imask = 0;
                    do_have_image = 0;
                }
                else
                {
                    FzImage image = null;
                    if (argPix.alpha() == 0)
                        image = argPix.fz_new_image_from_pixmap(new FzImage());
                    else
                    {
                        FzPixmap pm = argPix.fz_convert_pixmap(
                            new FzColorspace(0),
                            new FzColorspace(0),
                            new FzDefaultColorspaces(),
                            new FzColorParams(),
                            1
                            );
                        pm.m_internal.alpha = 0;
                        pm.m_internal.colorspace = null;
                        FzImage mask = pm.fz_new_image_from_pixmap(new FzImage());
                        image = argPix.fz_new_image_from_pixmap(mask);
                    }
                    do_process_stream = 0;
                    do_have_imask = 0;
                }
            }

            /*if (do_process_stream != 0)
            {
                FzMd5 state = new FzMd5();
                state.fz_md5_update(imgBuf.m_internal.data, imgBuf.m_internal.len);

                FzBuffer maskBuf = null;
                if (imask != null)
                {
                    maskBuf = Utils.BufferFromBytes(imask);
                    state.fz_md5_update(maskBuf.m_internal.data, maskBuf.m_internal.len);
                    vectoruc digests = state.fz_md5_final2();
                    byte[] md5
                }
            }*/
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
            MorphStruct morph = new MorphStruct(),
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
                img.Commit(overlay ? 1 : 0);
            return rc;
        }

        public (int, float) InsertHtmlBox(
            Rect rect,
            dynamic text,
            string css = null,
            float opacity = 0,
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
                throw new Exception("'text' must be a string or a Story");

            float scaleMax = scaleLow == 0 ? 0.0f : 1 / scaleLow;
            FitResult fit = story.FitScale(tempRect, scaleMin: 1, scaleMax: scaleMax);

            if (fit.BigEnough == false)
                return (-1, scaleLow);

            var filled = fit.Filled;
            float scale = 1 / fit.Parameter;

            float spareHeight = fit.Rect.Y1 - filled[3];
            if (scale != 1 || spareHeight < 0)
                spareHeight = 0;

            //story // issue

            // ShowP
            return (0, 0.0f);
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

            MuPDFPage srcPage = new MuPDFPage(src[pno], src);
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
            List<List<dynamic>> res = doc.GetPageXObjects(Number);
            for (int i = 0; i < res.Count; i++)
                iList.Add(iList[i][1]);

            res = doc.GetPageImages(Number);
            for (int i = 0; i < res.Count; i++)
                iList.Add(iList[i][7]);

            res = doc.GetPageFonts(Number);
            for (int i = 0; i < res.Count; i++)
                iList.Add(iList[i][4]);

            return 0;
        }

        public List<int> GetContents()
        {
            List<int> ret = new List<int> ();
            PdfObj obj = _nativePage.obj();
            PdfObj contents = obj.pdf_dict_get(new PdfObj("Contents"));
            if (contents.pdf_is_array() != 0)
            {
                int n = contents.pdf_array_len();
                for (int i = 0; i < n; i ++)
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
            MuPDFDocument doc = _parent;
            int xref = 0;

            if (doc is null)
                throw new Exception("orphaned object: parent is None");

            int idx = 0;
            if (fontName.StartsWith("/"))
                fontName = fontName.Substring(1);

            HashSet<char> INVALID_NAME_CHARS = new HashSet<char>(" \t\n\r\v\f()<>[]{}/%" + '\0');
            INVALID_NAME_CHARS.IntersectWith(fontName);

            if (INVALID_NAME_CHARS.Count > 0)
                throw new Exception($"bad fontname chars {INVALID_NAME_CHARS.ToString()}");

            FontStruct font = Utils.CheckFont(this, fontName);
            if (font != null)
            {
                xref = font.Xref;
                if (Utils.CheckFontInfo(doc, xref))
                    return xref;

                Utils.GetCharWidths(doc, xref);
                return xref;
            }

            string bfName = null;
            try
            {
                bfName = Utils.Base14_fontdict[fontName.ToLower()];
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

            /*if (fontName.ToLower())//issue
            {

            }*/

            if (fontFile == null)
            {
                throw new Exception("bad fontfile");
            }
            FontStruct val = _InsertFont(fontName, bfName, fontFile, fontBuffer, setSimple, idx,
                wmode, serif, encoding, CJK_number);

            if (val == null)
                return -1;

            FontStruct fontDict = val;
            var _ = Utils.GetCharWidths(doc, xref: fontDict.Xref, fontDict: fontDict);
            return fontDict.Xref;
        }

        public List<List<dynamic>> GetFonts(bool full = false)
        {
            return _parent.GetPageFonts(Number, full);
        }

        public PdfPage GetPdfPage()
        {
            return _nativePage;
        }

        private FontStruct _InsertFont(
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

            FontStruct value = Utils.InsertFont(pdf, bfName, fontFile, fontBuffer, setSimple, idx, wmode, serif, encoding, ordering);
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
            if (CA > 1 && ca >= 1 && blendMode == null)
                return null;
            int tCA = Convert.ToInt32(Math.Round(Math.Max(CA, 0) * 100));
            if (tCA >= 100)
                tCA = 99;
            int tca = Convert.ToInt32(Math.Round(Math.Max(ca, 0) * 100));
            if (tca >= 100)
                tca = 99;
            gstate = $"fitzca{tCA.ToString("2i")}{tca.ToString("2i")}";

            if (gstate == null) return null;

            PdfObj resources = PageObj.pdf_dict_get(new PdfObj("Resources"));
            if (resources == null)
                resources = mupdf.mupdf.pdf_dict_put_dict(PageObj, new PdfObj("Resources"), 2);
            PdfObj extg = resources.pdf_dict_get(new PdfObj("ExtGState"));
            int n = extg.pdf_dict_len();

            for (int i = 0; i< n; i++)
            {
                PdfObj o1 = extg.pdf_dict_get_key(i);
                string name = o1.pdf_to_name();
                if (name == gstate)
                    return gstate;
            }

            PdfObj opa = _nativePage.doc().pdf_new_dict(3);
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

        public List<(int, pdf_annot_type, string)> GetAnnotXrefs()
        {
            PdfPage page = GetPdfPage();
            if (page == null)
                return null;
            return Utils.GetAnnotXrefList(page.obj());
        }

        public void Dispose()
        {
            _nativePage.Dispose();
        }
    }
}
