using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using mupdf;
using static mupdf.FzBandWriter;

namespace CSharpMuPDF
{
    public class MuPDFPage
    {
        private PdfPage _nativePage;

        private MuPDFDocument _parent;

        public int NUMBER { get; set; }

        public bool IsOwn { get; set; }

        public int ROTATION
        {
            get
            {
                if (_nativePage == null)
                    return 0;
                return Utils.PageRotation(_nativePage);
            }
        }

        public Dictionary<int, MuPDFAnnotation> ANNOT_REFS = new Dictionary<int, MuPDFAnnotation>();

        public Matrix transformationMatrix
        {
            get
            {
                FzMatrix ctm = new FzMatrix();
                PdfPage page = _nativePage;
                if (page == null)
                    return new Matrix(ctm);

                FzRect mediabax = new FzRect(FzRect.Fixed.Fixed_UNIT);
                page.pdf_page_transform(mediabax, ctm);

                if (ROTATION % 360 == 0)
                    return new Matrix(ctm);
                else
                    return new Matrix(1, 0, 0, -1, 0, CROPBOX.Height);
            }
        }

        public Rect ARTBOX
        {
            get
            {
                Rect rect = OtherBox("ArtBox");
                if (rect is null)
                    return CROPBOX;
                Rect mb = MEDIABOX;
                return new Rect(rect[0], mb.Y1 - rect[3], rect[2], mb.Y1 - rect[1]);
            }
        }

        public Rect MEDIABOX
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

        public Rect BLEEDBOX
        {
            get
            {
                Rect rect = OtherBox("BLEEDBOX");
                if (rect is null)
                    return CROPBOX;
                Rect mb = MEDIABOX;
                return new Rect(rect[0], mb.Y1 - rect[3], rect[2], mb.Y1 - rect[1]);
            }
        }

        public Rect CROPBOX
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

        public Matrix derotationMatrix
        {
            get
            {
                PdfPage page = _nativePage;
                if (page == null)
                    return new Matrix(new FzMatrix());//issues
                return new Matrix(Utils.DerotatePageMatrix(page));
            }
        }

        public MuPDFAnnotation FIRST_ANNOT
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

        public FzLink FIRST_LINK//issue
        {
            get
            {
                
                return _nativePage.pdf_load_links();
            }
        }

        public Widget FIRST_WIDGET
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

        public MuPDFDocument PARENT
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
                NUMBER = 0;
            else
                NUMBER = _nativePage.m_internal.super.number;
        }

        public MuPDFPage(FzPage fzPage, MuPDFDocument parent)
        {
            _nativePage = fzPage.pdf_page_from_fz_page();
            _parent = parent;

            if (_nativePage == null)
                NUMBER = 0;
            else
                NUMBER = _nativePage.m_internal.super.number;
        }

        private PdfAnnot AddCaretAnnot(Point point)
        {
            PdfPage page = _nativePage;
            PdfAnnot annot = page.pdf_create_annot(pdf_annot_type.PDF_ANNOT_CARET);
            if (point != null)
            {
                FzRect r = annot.pdf_annot_rect();
                r = new FzRect(point.X, point.Y, point.X + r.x1 -r.x0, point.Y + r.y1 -r.y0);
                annot.pdf_set_annot_rect(r);
            }
            annot.pdf_update_annot();
            Utils.AddAnnotId(annot, "A");
            
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
            catch (Exception e)
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

        private MuPDFAnnotation AddTextMarker(List<Quad> quads, PdfAnnotType annotType)
        {
            MuPDFAnnotation ret = null;
            PdfAnnot annot = null;
            PdfPage page = _nativePage;
            if (!_parent.IsPDF)
                throw new Exception("is not pdf");
            int rotation = Utils.PageRotation(page);
            try
            {
                if (rotation != 0)
                    page.obj().pdf_dict_put_int(new PdfObj("Rotate"), rotation);
                annot = page.pdf_create_annot((pdf_annot_type)annotType);
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
            catch (Exception ex)
            {
                if (rotation != 0)
                    page.obj().pdf_dict_put_int(new PdfObj("Rotate"), rotation);
                ret = null;
            }
            if (ret == null)
                return null;
            ANNOT_REFS.Add(ret.GetHashCode(), ret);
            
            return ret;
        }

        private List<Rect> GetHighlightSelection(List<Quad> quads, Point start, Point stop, Rect clip)
        {
            if (clip is null)
                clip = this.MEDIABOX;
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
                Rect bbox = new Rect(b.BBOX);
                if (bbox.IsInfinite || bbox.IsEmpty)
                    continue;

                foreach (LineStruct line in b.LINES)
                {
                    bbox = new Rect(line.BBOX);
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
                    q.Add(r.QUAD);
            }
            else
                q = quads;
            MuPDFAnnotation ret = AddTextMarker(q, PdfAnnotType.PDF_ANNOT_HIGHLIGHT);
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
            int oldRotation = ROTATION;
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

        public MuPDFAnnotation AddUnderlineAnnot(List<Quad> quads = null, Point start = null, Point stop = null, Rect clip = null)
        {
            List<Quad> q = new List<Quad>();
            if (quads == null)
            {
                List<Rect> rs = GetHighlightSelection(q, start, stop, clip);
                foreach (Rect r in rs)
                    q.Add(r.QUAD);
            }

            return AddTextMarker(q, PdfAnnotType.PDF_ANNOT_UNDERLINE);
        }

        public static PdfObj PdfObjFromStr(PdfDocument doc, string src)
       {
            byte[] bSrc = Encoding.UTF8.GetBytes(src);
            IntPtr scrPtr = new IntPtr(bSrc.Length);
            Marshal.Copy(bSrc, 0, scrPtr, bSrc.Length);
            SWIGTYPE_p_unsigned_char swigSrc = new SWIGTYPE_p_unsigned_char(scrPtr, true);

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
                catch (Exception e)
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
            ANNOT_REFS.Clear();
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
            NUMBER = 0;
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

        private string GetOptionalContent(int oc)
        {
            if (oc == 0) return null;

            MuPDFDocument doc = _parent;
            string check = "";//doc.XREF//issue
            if (!(check.Contains("/Type/OCG") || check.Contains("/Type/OCMD")))
                throw new Exception("bad optional content: 'oc'");
            Dictionary<int, string> props = new Dictionary<int, string>();

            foreach ((string p, int x) in GetResourceProperties())
            {
                props[x] = p;
            }
            if (props.Keys.Contains(oc))
                return props[oc];
            int i = 0;
            string mc = $"MC{i}";

            while (props.Values.Contains(mc))
            {
                i += 1;
                mc = $"MC{i}";
            }
            
            SetResourceProperty(mc, oc);
            return mc;
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
    }
}
