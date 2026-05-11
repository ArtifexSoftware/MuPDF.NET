using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Python-name compatibility surface for <c>Page</c>.
    /// These wrappers preserve naming traceability to <c>src/__init__.py:class Page</c>.
    /// </summary>
    public partial class Page
    {
        // Properties mirrored from Python naming.
        public int number => Number;
        public Rect rect => Rect;
        public Rect bound() => Bound();
        public Rect mediabox() => MediaBox;
        public Point mediabox_size() => MediaBoxSize;
        public string language() => Language;
        public object get_layout() => GetLayout();
        public object layout_information
        {
            get => LayoutInformation;
            set => LayoutInformation = value;
        }
        public Rect cropbox() => CropBox;
        public Rect bleedbox() => BleedBox;
        public Rect trimbox() => TrimBox;
        public Rect artbox() => ArtBox;
        public int rotation() => Rotation;
        public Matrix transformation_matrix => TransformationMatrix;
        public Matrix derotation_matrix => DerotationMatrix;
        public int xref => Xref;
        public Annot first_annot => FirstAnnot;
        public Link first_link => FirstLink;
        public Widget first_widget => FirstWidget;
        public bool is_wrapped => IsWrapped;

        // Annotation and link generators.
        public IEnumerable<Annot> annots(params AnnotationType[] types) =>
            types == null || types.Length == 0 ? Annots() : Annots(types);
        public IEnumerable<Link> links(params LinkType[] kinds) =>
            kinds == null || kinds.Length == 0 ? Links() : Links(kinds);
        public IEnumerable<Widget> widgets(params WidgetType[] types) =>
            types == null || types.Length == 0 ? Widgets() : Widgets(types);

        // Annotation creation.
        public Annot _add_caret_annot(Point point) => AddCaretAnnot(point);
        public Annot _add_file_annot(Point point, byte[] buffer_, string filename, string ufilename = null, string desc = null, string icon = null)
            => AddFileAnnot(point, buffer_, filename, ufilename, desc, icon ?? "PushPin");
        public Annot _add_freetext_annot(
            Rect rect,
            string text,
            float fontsize = 11,
            string fontname = null,
            float[] text_color = null,
            float[] fill_color = null,
            float[] border_color = null,
            float border_width = 0,
            int[] dashes = null,
            Point[] callout = null,
            int line_end = (int)mupdf.pdf_line_ending.PDF_ANNOT_LE_OPEN_ARROW,
            float opacity = 1,
            int align = 0,
            int rotate = 0,
            bool richtext = false,
            string style = null)
            => AddFreeTextAnnot(rect, text, fontsize, fontname, text_color, fill_color, border_color, border_width, dashes, callout, (mupdf.pdf_line_ending)line_end, opacity, align, rotate, richtext, style);
        public Annot _add_ink_annot(Point[][] list) => AddInkAnnot(list);
        public Annot _add_line_annot(Point p1, Point p2) => AddLineAnnot(p1, p2);
        public Annot _add_multiline(Point[] points, object annot_type)
        {
            int type = annot_type is int i ? i : (int)(annot_type is AnnotationType at ? at : AnnotationType.PolyLine);
            if (type == (int)AnnotationType.Polygon) return AddPolygonAnnot(points);
            return AddPolylineAnnot(points);
        }
        public Annot _add_redact_annot(Quad quad, string? text = null, string? da_str = null, int align = 0, float[]? fill = null, float[]? text_color = null)
            => AddRedactAnnot(quad, text, null, 11, align, fill, text_color);
        public Annot _add_square_or_circle(Rect rect, object annot_type)
        {
            int type = annot_type is int i ? i : (int)(annot_type is AnnotationType at ? at : AnnotationType.Square);
            if (type == (int)AnnotationType.Circle) return AddCircleAnnot(rect);
            return AddRectAnnot(rect);
        }
        public Annot _add_stamp_annot(Rect rect, int stamp = 0) => AddStampAnnot(rect, stamp);
        public Annot _add_text_annot(Point point, string text, string icon = null) => AddTextAnnot(point, text, icon ?? "Note");
        public Annot _add_text_marker(Quad[] quads, object annot_type)
        {
            int type = annot_type is int i ? i : (int)(annot_type is AnnotationType at ? at : AnnotationType.Highlight);
            if (type == (int)AnnotationType.Underline) return AddUnderlineAnnot(quads);
            if (type == (int)AnnotationType.StrikeOut) return AddStrikeoutAnnot(quads);
            if (type == (int)AnnotationType.Squiggly) return AddSquigglyAnnot(quads);
            return AddHighlightAnnot(quads);
        }
        public void _addAnnot_FromString(object linklist)
        {
            int lcount = Helpers.PythonTupleLikeCount(linklist);
            if (lcount < 1)
                return;

            _ = _pdf_page();

            for (int i = 0; i < lcount; i++)
            {
                object txtpy = Helpers.PythonTupleLikeItem(linklist, i);
                string text = txtpy as string ?? txtpy?.ToString();
                if (string.IsNullOrEmpty(text))
                {
                    Console.WriteLine($"skipping bad link / annot item {i:d}.");
                    continue;
                }
                try
                {
                    Helpers.AppendPdfAnnotFromObjectString(this, text);
                }
                catch
                {
                    Console.WriteLine($"skipping bad link / annot item {i:d}.");
                }
            }

            var pdf = RequireParent().NativePdfDocument;
            Helpers.JM_refresh_links(pdf, NativePdfPage);
            SyncLinkWrapperCache();
        }
        public Annot add_text_annot(Point point, string text, string icon = "Note") => AddTextAnnot(point, text, icon);
        public Annot add_freetext_annot(Rect rect, string text, float fontsize = 12, string fontname = "helv",
            float[] text_color = null, float[] fill_color = null, float[] border_color = null, float border_width = 0,
            int[] dashes = null, Point[] callout = null, int line_end = (int)mupdf.pdf_line_ending.PDF_ANNOT_LE_OPEN_ARROW,
            float opacity = 1, int align = 0, int rotate = 0, bool richtext = false, string style = null)
            => AddFreeTextAnnot(rect, text, fontsize, fontname, text_color, fill_color, border_color, border_width, dashes, callout, (mupdf.pdf_line_ending)line_end, opacity, align, rotate, richtext, style);
        public Annot add_line_annot(Point p1, Point p2) => AddLineAnnot(p1, p2);
        public Annot add_rect_annot(Rect rect) => AddRectAnnot(rect);
        public Annot add_circle_annot(Rect rect) => AddCircleAnnot(rect);
        public Annot add_polyline_annot(Point[] points) => AddPolylineAnnot(points);
        public Annot add_polygon_annot(Point[] points) => AddPolygonAnnot(points);
        public Annot add_highlight_annot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null) => AddHighlightAnnot(quads, start, stop, clip);
        public Annot add_underline_annot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null) => AddUnderlineAnnot(quads, start, stop, clip);
        public Annot add_strikeout_annot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null) => AddStrikeoutAnnot(quads, start, stop, clip);
        public Annot add_squiggly_annot(Quad[] quads = null, Point start = null, Point stop = null, IRect clip = null) => AddSquigglyAnnot(quads, start, stop, clip);
        public Annot add_caret_annot(Point point) => AddCaretAnnot(point);
        public Annot add_stamp_annot(Rect rect, int stamp = 0) => AddStampAnnot(rect, stamp);
        public Annot add_file_annot(Point point, byte[] buffer, string filename, string ufilename = null, string desc = null, string icon = "PushPin")
            => AddFileAnnot(point, buffer, filename, ufilename, desc, icon);
        public Annot add_ink_annot(Point[][] handwriting) => AddInkAnnot(handwriting);
        public Annot add_redact_annot(Quad quad, string text = null, string fontname = null, float fontsize = 11,
            int align = 0, float[] fill = null, float[] text_color = null, bool cross_out = true)
            => AddRedactAnnot(quad, text, fontname, fontsize, align, fill, text_color);

        // Annotation/link operations.
        public bool apply_redactions(int images = 2, int graphics = 1, int text = 0) => ApplyRedactions(images, graphics, text);
        public Annot delete_annot(Annot annot) => DeleteAnnot(annot);
        public void delete_link(Link link) => DeleteLink(link);
        public void delete_link(Dictionary<string, object> linkdict) => DeleteLink(linkdict);
        public Annot add_widget(Widget widget) => AddWidget(widget);
        public Widget delete_widget(Widget widget) => DeleteWidget(widget);
        public Annot load_annot(string ident) => LoadAnnot(ident);
        public Annot load_annot(int ident) => LoadAnnot(ident);
        public Link load_links() => LoadLinks();
        public Widget load_widget(int xref) => LoadWidget(xref);
        /// <summary>Python <c>insert_link</c> (returns <c>None</c>); prefer this for strict parity.</summary>
        public void insert_link(Dictionary<string, object> lnk, bool mark = true) => InsertLinkVoid(lnk, mark);
        /// <summary>Convenience: same as <see cref="InsertLink"/> (returns first link after refresh).</summary>
        public Link insert_link_returning_link(Dictionary<string, object> lnk, bool mark = true) => InsertLink(lnk, mark);
        public void set_links(List<Dictionary<string, object>> links) => SetLinks(links);
        public void update_link(Dictionary<string, object> lnk) => UpdateLink(lnk);
        public List<Dictionary<string, object>> get_links() => GetLinks();

        // Rendering and text extraction.
        public Pixmap get_pixmap(Matrix matrix = null, Colorspace cs = null, IRect clip = null, bool alpha = false, bool annots = true)
            => GetPixmap(matrix, cs, clip, alpha, annots);
        public DisplayList get_displaylist(int annots = 1) => GetDisplayList(annots);
        public string get_svg_image(Matrix matrix = null, int text_as_path = 1) => GetSvgImage(matrix, text_as_path);
        public TextPage get_textpage(int flags = 0, IRect clip = null) => GetTextPage(flags, clip);
        public string get_text(string option = "text", IRect clip = null, int flags = 0, TextPage textpage = null, string sort = null)
            => GetText(option, clip, flags, textpage, sort);
        public string get_textbox(Rect rect, TextPage textpage = null) => GetTextbox(rect, textpage);
        public string get_text_selection(Point p1, Point p2, IRect clip = null, TextPage textpage = null)
            => GetTextSelection(p1, p2, clip, textpage);
        public TextPage get_textpage_ocr(int flags = 0, string language = "eng", int dpi = 72, bool full = false, string tessdata = null)
            => GetTextPageOcr(flags, language, dpi, full, tessdata);
        public List<(float x0, float y0, float x1, float y1, string text, int blockNo, int blockType)> get_text_blocks(int flags = 0) => GetTextBlocks(flags);
        public List<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> get_text_words(int flags = 0) => GetTextWords(flags);
        public List<Quad> search_for(string needle, Quad clip = null, int max_hits = 16, int flags = 0, TextPage textpage = null)
            => SearchFor(needle, clip, max_hits, flags, textpage);

        /// <summary>Python <c>Page.search_for(..., quads=False)</c> — merged <see cref="Rect"/> hits.</summary>
        public List<Rect> search_for_rects(string needle, Quad clip = null, int max_hits = 16, int flags = 0, TextPage textpage = null)
            => SearchForRects(needle, clip, max_hits, flags, textpage);

        // Insertion helpers.
        public int insert_text(Point point, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, float rotate = 0, int render_mode = 0, float border_width = 0.05f)
            => InsertText(point, text, fontsize, fontname, color, rotate, render_mode, border_width);
        public (int rc, List<string> rest) insert_textbox(Rect rect, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, int align = 0, float border_width = 0.05f, float expandtabs = 1, int render_mode = 0)
            => InsertTextbox(rect, text, fontsize, fontname, color, align, border_width, expandtabs, render_mode);
        public (double spare_height, double scale) insert_htmlbox(Rect rect, string text, string css = null, float scale_low = 0,
            Archive archive = null, int rotate = 0, int oc = 0, float opacity = 1, bool overlay = true,
            bool scale_word_width = true, bool verbose = false)
            => InsertHtmlbox(rect, text, css, scale_low, archive, rotate, oc, opacity, overlay, scale_word_width, verbose);
        public int show_pdf_page(Rect rect, Document docsrc, int pno = 0, bool keep_proportion = true, bool overlay = true, int oc = 0, int rotate = 0, Rect clip = null)
            => ShowPdfPage(rect, docsrc, pno, keep_proportion, overlay, oc, rotate, clip);
        public void _set_resource_property(string name, int xref)
            => Helpers.JM_set_resource_property(NativePdfPage.obj(), name, xref);
        public int _show_pdf_page(object fz_srcpage, int overlay = 1, Matrix matrix = null, int xref = 0, int oc = 0, Rect clip = null, Graftmap graftmap = null, string _imgname = null)
        {
            var srcPdfPage = Helpers.AsPdfPage(fz_srcpage);
            if (clip == null)
            {
                var srcBBox = new mupdf.FzRect();
                var srcCtm = new mupdf.FzMatrix();
                srcPdfPage.obj().pdf_page_obj_transform(srcBBox, srcCtm);
                clip = Helpers.RectFromFz(srcBBox);
            }
            return ShowPdfPageInternal(
                srcPdfPage,
                overlay: overlay != 0,
                matrix: matrix ?? Matrix.Identity,
                xref: xref,
                oc: oc,
                clip: clip,
                graftmap: graftmap?.NativeGraftMap,
                imgName: _imgname ?? MakeShowPdfResourceName(),
                pdfOut: Parent.NativePdfDocument,
                targetPageObj: NativePdfPage.obj());
        }
        public mupdf.PdfPage _pdf_page(bool required = true)
            => Helpers.AsPdfPage(this, required);
        public (int push, int pop) _count_q_balance()
            => CountQBalance();
        public List<(string name, int xref)> _get_resource_properties()
            => GetResourceProperties();
        public string _get_optional_content(int? oc)
        {
            if (!oc.HasValue || oc.Value == 0)
                return null;
            int xref = oc.Value;
            string check = Parent.XrefObject(xref, compressed: true);
            if (check.IndexOf("/Type/OCG", StringComparison.Ordinal) < 0
                && check.IndexOf("/Type/OCMD", StringComparison.Ordinal) < 0)
                throw new ValueErrorException("bad optional content: 'oc'");

            var propsByXref = new Dictionary<int, string>();
            foreach (var p in _get_resource_properties())
                propsByXref[p.xref] = p.name;
            if (propsByXref.TryGetValue(xref, out string existing))
                return existing;

            int i = 0;
            string mc = $"MC{i}";
            var usedNames = new HashSet<string>(propsByXref.Values);
            while (usedNames.Contains(mc))
            {
                i++;
                mc = $"MC{i}";
            }
            _set_resource_property(mc, xref);
            return mc;
        }
        public void _set_pagebox(string boxtype, Rect rect)
        {
            switch (boxtype)
            {
                case "CropBox":
                    SetCropBox(rect);
                    break;
                case "BleedBox":
                    SetBleedBox(rect);
                    break;
                case "TrimBox":
                    SetTrimBox(rect);
                    break;
                case "ArtBox":
                    SetArtBox(rect);
                    break;
                default:
                    throw new ValueErrorException("bad boxtype");
            }
        }
        public Rect _other_box(string boxtype)
        {
            switch (boxtype)
            {
                case "BleedBox":
                    return BleedBox;
                case "TrimBox":
                    return TrimBox;
                case "ArtBox":
                    return ArtBox;
                default:
                    return null;
            }
        }
        public void _reset_annot_refs() => ResetAnnotRefsInternal();
        public void _erase()
            => TearDownFromParent();
        public mupdf.FzStextPage _get_textpage(Rect clip = null, int flags = 0, Matrix matrix = null)
        {
            var options = new mupdf.fz_stext_options();
            options.flags = flags;
            var rect = clip == null ? mupdf.mupdf.fz_bound_page(NativePage) : clip.ToFzRect();
            var tpage = new mupdf.FzStextPage(rect);
            var dev = tpage.fz_new_stext_device(new mupdf.FzStextOptions(options));
            mupdf.mupdf.fz_run_page(NativePage, dev, (matrix ?? Matrix.Identity).ToFzMatrix(), new mupdf.FzCookie());
            mupdf.mupdf.fz_close_device(dev);
            return tpage;
        }
        public string _set_opacity(string gstate = null, double CA = 1, double ca = 1, string blendmode = null)
        {
            if (CA >= 1 && ca >= 1 && blendmode == null)
                return null;
            int tCA = (int)Math.Round(Math.Max(CA, 0) * 100);
            if (tCA >= 100) tCA = 99;
            int tca = (int)Math.Round(Math.Max(ca, 0) * 100);
            if (tca >= 100) tca = 99;
            gstate = $"fitzca{tCA:00}{tca:00}";

            var page = _pdf_page();
            var resources = mupdf.mupdf.pdf_dict_get(page.obj(), mupdf.mupdf.pdf_new_name("Resources"));
            if (resources.m_internal == null)
                resources = mupdf.mupdf.pdf_dict_put_dict(page.obj(), mupdf.mupdf.pdf_new_name("Resources"), 2);
            var extg = mupdf.mupdf.pdf_dict_get(resources, mupdf.mupdf.pdf_new_name("ExtGState"));
            if (extg.m_internal == null)
                extg = mupdf.mupdf.pdf_dict_put_dict(resources, mupdf.mupdf.pdf_new_name("ExtGState"), 2);
            int n = mupdf.mupdf.pdf_dict_len(extg);
            for (int i = 0; i < n; i++)
            {
                var key = mupdf.mupdf.pdf_dict_get_key(extg, i);
                string name = mupdf.mupdf.pdf_to_name(key);
                if (name == gstate)
                    return gstate;
            }
            var opa = mupdf.mupdf.pdf_new_dict(page.doc(), 3);
            mupdf.mupdf.pdf_dict_put_real(opa, mupdf.mupdf.pdf_new_name("CA"), (float)CA);
            mupdf.mupdf.pdf_dict_put_real(opa, mupdf.mupdf.pdf_new_name("ca"), (float)ca);
            if (!string.IsNullOrEmpty(blendmode))
                mupdf.mupdf.pdf_dict_put_name(opa, mupdf.mupdf.pdf_new_name("BM"), blendmode);
            mupdf.mupdf.pdf_dict_puts(extg, gstate, opa);
            return gstate;
        }
        public bool _apply_redactions(int text, int images, int graphics)
            => ApplyRedactions(images, graphics, text);
        public Annot _load_annot(string name, int xref = 0)
            => xref == 0 ? LoadAnnot(name) : LoadAnnot(xref);
        public int _insertFont(string fontname, string bfname, string fontfile, byte[] fontbuffer, bool set_simple, int idx, int wmode, int serif, int encoding, int ordering)
            => InsertFont(fontname, fontfile, fontbuffer, set_simple, encoding);
        public Pixmap _makePixmap(object doc, Matrix ctm, Colorspace cs, int alpha = 0, int annots = 1, IRect clip = null)
            => GetPixmap(ctm ?? Matrix.Identity, cs ?? Colorspace.CsRGB, clip, alpha != 0, annots != 0);
        public (int xref, Dictionary<string, int> digests) _insert_image(
            string filename = null,
            Pixmap pixmap = null,
            byte[] stream = null,
            string imask = null,
            Rect clip = null,
            int overlay = 1,
            int rotate = 0,
            int keep_proportion = 1,
            int oc = 0,
            int width = 0,
            int height = 0,
            int xref = 0,
            int alpha = -1,
            string _imgname = null,
            Dictionary<string, int> digests = null)
        {
            int imageXref = InsertImage(
                clip ?? Rect,
                filename: filename,
                stream: stream,
                pixmap: pixmap,
                mask: imask,
                rotate: rotate,
                xref: xref,
                oc: oc,
                keepProportion: keep_proportion != 0,
                alpha: alpha,
                overlay: overlay != 0 ? "true" : "false");
            return (imageXref, null);
        }
        public Annot _addWidget(object field_type, string field_name)
        {
            var pdfPage = _pdf_page();
            var annot = mupdf.mupdf.pdf_create_annot(pdfPage, mupdf.pdf_annot_type.PDF_ANNOT_WIDGET);
            if (annot == null || annot.m_internal == null)
                throw new InvalidOperationException("cannot create widget");
            var obj = mupdf.mupdf.pdf_annot_obj(annot);
            string ft = "Tx";
            if (field_type is WidgetType wt)
            {
                ft = wt switch
                {
                    WidgetType.Button => "Btn",
                    WidgetType.CheckBox => "Btn",
                    WidgetType.RadioButton => "Btn",
                    WidgetType.ComboBox => "Ch",
                    WidgetType.ListBox => "Ch",
                    WidgetType.Signature => "Sig",
                    _ => "Tx"
                };
            }
            else if (field_type is int wi && Enum.IsDefined(typeof(WidgetType), wi))
            {
                return _addWidget((WidgetType)wi, field_name);
            }
            mupdf.mupdf.pdf_dict_put(obj, mupdf.mupdf.pdf_new_name("FT"), mupdf.mupdf.pdf_new_name(ft));
            if (!string.IsNullOrEmpty(field_name))
                mupdf.mupdf.pdf_dict_put_text_string(obj, mupdf.mupdf.pdf_new_name("T"), field_name);
            mupdf.mupdf.pdf_update_annot(annot);
            Helpers.AddAnnotId(annot, "W");
            return new Annot(annot, this);
        }

        // Resources.
        public List<(int xref, string ext, string type, string baseName, string name, string encoding)> get_fonts(bool full = false) => GetFonts(full);
        public List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> get_images(bool full = false) => GetImages(full);
        public Rect get_image_bbox(object name) => GetImageBbox(name);
        public (Rect bbox, Matrix transform) get_image_bbox(object name, bool transform) => GetImageBbox(name, transform);
        public List<Dictionary<string, object>> get_image_info(bool hashes = false, bool xrefs = false) => GetImageInfo(hashes, xrefs);
        public List<Rect> get_image_rects(object name) => GetImageRects(name);
        public List<(Rect bbox, Matrix transform)> get_image_rects(object name, bool transform) => GetImageRects(name, transform);
        public List<Dictionary<string, object>> get_xobjects() => GetXobjects();
        public int insert_image(Rect rect, string filename = null, byte[] stream = null, Pixmap pixmap = null,
            string mask = null, int rotate = 0, int xref = 0, int oc = 0, bool keep_proportion = true,
            int alpha = -1, string overlay = "true")
            => InsertImage(rect, filename, stream, pixmap, mask, rotate, xref, oc, keep_proportion, alpha, overlay);
        public int insert_font(string fontname = "helv", string fontfile = null, byte[] fontbuffer = null, bool set_simple = false, int encoding = 0)
            => InsertFont(fontname, fontfile, fontbuffer, set_simple, encoding);
        public Matrix rotation_matrix() => RotationMatrix;
        public void refresh() => Refresh();
        public void replace_image(int xref, string filename = null, Pixmap pixmap = null, byte[] stream = null)
            => ReplaceImage(xref, filename, pixmap, stream);

        // Geometry / boxes.
        public void set_rotation(int rotation) => SetRotation(rotation);
        public void set_language(string language = null) => SetLanguage(language);
        public void set_mediabox(Rect rect) => SetMediaBox(rect);
        public void set_cropbox(Rect rect) => SetCropBox(rect);
        public void set_bleedbox(Rect rect) => SetBleedBox(rect);
        public void set_trimbox(Rect rect) => SetTrimBox(rect);
        public void set_artbox(Rect rect) => SetArtBox(rect);
        public Point cropbox_position() => CropBoxPosition();

        // Drawing and content streams.
        public Shape new_shape() => NewShape();
        public void clean_contents(int sanitize = 1) => CleanContents(sanitize);
        public byte[] read_contents() => ReadContents();
        public void set_contents(int xref) => SetContents(xref);
        public List<int> get_contents() => GetContents();
        public void wrap_contents() => WrapContents();
        public Matrix remove_rotation() => RemoveRotation();
        public List<Dictionary<string, object>> get_drawings(bool extended = false) => GetDrawings(extended);
        public List<Dictionary<string, object>> get_cdrawings() => GetCdp();
        public List<Dictionary<string, object>> get_texttrace() => GetTexttrace();
        public string get_label() => GetLabel();

        // Annotation metadata helpers.
        public List<string> annot_names() => AnnotNames();
        public List<(int xref, AnnotationType type, string id)> annot_xrefs() => AnnotXrefs();

        // Tables / execution.
        public TableFinder find_tables(TableSettings settings = null) => FindTables(settings);
        public void run(mupdf.FzDevice dev, Matrix transform) => Run(dev, transform);
        public void clip_to_rect(Rect rect) => ClipToRect(rect);
        public List<(string name, int xref, string type)> get_oc_items() => GetOcItems();
        public void write_text(Rect rect = null, IEnumerable<TextWriter> writers = null, bool overlay = true,
            float[] color = null, float? opacity = null, bool keep_proportion = true, int rotate = 0, int oc = 0)
            => WriteText(rect, writers, overlay, color, opacity, keep_proportion, rotate, oc);

        public Point draw_line(Point p1, Point p2, float[] color = null, float width = 1, string line_cap = null, string line_join = null, float[] dashes = null, float opacity = 1, string blend_mode = null, int overlay = 1, string morph = null, int oc = 0)
            => DrawLine(p1, p2, color, width, line_cap, line_join, dashes, opacity, blend_mode, overlay, morph, oc);
        public Point draw_rect(Rect rect, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawRect(rect, color, fill, width, opacity, overlay);
        public Point draw_circle(Point center, float radius, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawCircle(center, radius, color, fill, width, opacity, overlay);
        public Point draw_oval(Rect rect, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawOval(rect, color, fill, width, opacity, overlay);
        public Point draw_curve(Point p1, Point p2, Point p3, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawCurve(p1, p2, p3, color, fill, width, opacity, overlay);
        public Point draw_squiggle(Point p1, Point p2, float breadth = 2, float[] color = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawSquiggle(p1, p2, breadth, color, width, opacity, overlay);
        public Point draw_zigzag(Point p1, Point p2, float breadth = 2, float[] color = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawZigzag(p1, p2, breadth, color, width, opacity, overlay);
        public Point draw_sector(Point center, Point point, float angle, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, bool full_sector = true, int overlay = 1)
            => DrawSector(center, point, angle, color, fill, width, opacity, full_sector, overlay);
        public Point draw_polyline(Point[] points, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawPolyline(points, color, fill, width, opacity, overlay);
        public Point draw_quad(Quad quad, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawQuad(quad, color, fill, width, opacity, overlay);
        public Point draw_bezier(Point p1, Point p2, Point p3, Point p4, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => DrawBezier(p1, p2, p3, p4, color, fill, width, opacity, overlay);

        // Legacy alias names from Python _alias(Page, ...).
        public DisplayList getDisplayList(int annots = 1) => get_displaylist(annots);
        public List<(int xref, string ext, string type, string baseName, string name, string encoding)> getFontList(bool full = false) => get_fonts(full);
        public List<(int xref, string smask, int width, int height, int bpc, string colorspace, string altCs, string name, string filter)> getImageList(bool full = false) => get_images(full);
        public string getSVGimage(Matrix matrix = null, int text_as_path = 1) => get_svg_image(matrix, text_as_path);
        public TextPage getTextPage(int flags = 0, IRect clip = null) => get_textpage(flags, clip);
        public int showPDFpage(Rect rect, Document docsrc, int pno = 0, bool keep_proportion = true, bool overlay = true, int oc = 0, int rotate = 0, Rect clip = null)
            => show_pdf_page(rect, docsrc, pno, keep_proportion, overlay, oc, rotate, clip);
        public Point drawQuad(Quad quad, float[] color = null, float[] fill = null, float width = 1, float opacity = 1, int overlay = 1)
            => draw_quad(quad, color, fill, width, opacity, overlay);
        public void setCropBox(Rect rect) => set_cropbox(rect);
        public void setMediaBox(Rect rect) => set_mediabox(rect);
        public bool _isWrapped => is_wrapped;
    }
}
