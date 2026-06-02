using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// Legacy MuPDF.NET wrapper so <c>Document(stream: bytes, fileType: "pdf")</c> resolves unambiguously.
    /// </summary>
    public readonly struct DocumentStream
    {
        internal readonly byte[] Bytes;

        public DocumentStream(byte[] bytes) => Bytes = bytes ?? Array.Empty<byte>();

        public static implicit operator DocumentStream(byte[] bytes) => new(bytes);
    }


    public partial class Page
    {
        public static (Rect, Rect, Matrix) RectFunction(int rectN, Rect filled)
        {
            _ = rectN;
            var rect = filled ?? new Rect();
            return (new Rect(rect), new Rect(rect), new IdentityMatrix());
        }

        public bool WasWrapped
        {
            get => IsWrapped;
            set { _ = value; }
        }

        public Link FristLink => FirstLink;

        public mupdf.PdfObj PageObj
        {
            get
            {
                var page = _pdf_page(required: false);
                return page?.m_internal == null ? null : page.obj();
            }
        }

        public Rect GetBound() => Bound();

        public Rect OtherBox(string boxtype)
        {
            if (string.IsNullOrEmpty(boxtype))
                return null;
            switch (boxtype)
            {
                case "CropBox": return CropBox;
                case "MediaBox": return MediaBox;
                case "BleedBox": return BleedBox;
                case "TrimBox": return TrimBox;
                case "ArtBox": return ArtBox;
            }

            var page = _pdf_page(required: false);
            if (page?.m_internal == null)
                return null;
            var obj = mupdf.mupdf.pdf_dict_gets(page.obj(), boxtype);
            if (obj?.m_internal == null || mupdf.mupdf.pdf_is_array(obj) == 0)
                return null;
            var r = mupdf.mupdf.pdf_to_rect(obj);
            return new Rect(r.x0, r.y0, r.x1, r.y1);
        }

        public void SetClipPage(Rect rect) => ClipToRect(rect);

        public IEnumerable<Widget> GetWidgets(int[]? types = null) =>
            types == null ? Widgets() : Widgets(types);

        public IEnumerable<Annot> GetAnnots(List<PdfAnnotType>? types = null)
        {
            bool SkipDefault(AnnotationType t) =>
                t == AnnotationType.Link || t == AnnotationType.Popup || t == AnnotationType.Widget;

            if (types == null)
            {
                return Annots().Where(a => !SkipDefault(a.AnnotationType));
            }

            var requested = new HashSet<AnnotationType>(types.Select(t => (AnnotationType)(int)t));
            return Annots().Where(a => !SkipDefault(a.AnnotationType) && requested.Contains(a.AnnotationType));
        }

        public mupdf.PdfPage GetPdfPage() => _pdf_page(required: true);

        public Annot LoadAnnot(string name) => LoadAnnot((object)name);

        public Annot LoadAnnot(int xref) => LoadAnnot((object)xref);

        public string GetOptionalContent(int oc) => _get_optional_content(oc);

        public List<AnnotXref> GetUnusedAnnotXrefs() => GetAnnotXrefs();

        public void InsertLink(LinkInfo link, bool mark = true)
        {
            if (link == null)
                throw new ArgumentException("bad type: link");
            InsertLink(ToLinkDict(link), mark);
        }

        public void DeleteLink(LinkInfo link)
        {
            if (link == null)
                throw new ArgumentException("bad type: link");
            DeleteLink(ToLinkDict(link));
        }

        public void UpdateLink(LinkInfo link)
        {
            if (link == null)
                throw new ArgumentException("bad type: link");
            UpdateLink(ToLinkDict(link));
        }

        public void GetImageBbox(string name, bool transform = false)
        {
            if (transform)
                _ = GetImageBbox((object)name, true);
            else
                _ = GetImageBbox((object)name);
        }

        public void GetImageRects(string name, bool transform = false)
        {
            if (transform)
                _ = GetImageRects((object)name, true);
            else
                _ = GetImageRects((object)name);
        }

        public List<Box> GetImageRects(int name, bool transform = false)
        {
            if (!transform)
                return GetImageRects((object)name).Select(r => new Box { Rect = r, Matrix = Matrix.Identity }).ToList();

            return GetImageRects((object)name, true)
                .Select(t => new Box { Rect = t.bbox, Matrix = t.transform })
                .ToList();
        }

        public Annot AddMultiLine(List<Point> points, PdfAnnotType annotType)
        {
            if (points == null)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            var arr = points.ToArray();
            return annotType == PdfAnnotType.PDF_ANNOT_POLYGON
                ? AddPolygonAnnot(arr)
                : AddPolylineAnnot(arr);
        }

        public Annot AddInkAnnot(List<List<Point>> list)
        {
            if (list == null)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            return AddInkAnnot(list.Select(s => s?.ToArray() ?? Array.Empty<Point>()).ToArray());
        }

        public Annot AddPolygonAnnot(List<Point> points)
        {
            if (points == null)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            return AddPolygonAnnot(points.ToArray());
        }

        public Annot AddPolylineAnnot(List<Point> points)
        {
            if (points == null)
                throw new ArgumentException(Constants.MSG_BAD_ARG_POINTS);
            return AddPolylineAnnot(points.ToArray());
        }

        public Matrix CalcMatrix(Rect sr, Rect tr, bool keep = true, int rotate = 0)
        {
            Point smp = (sr.TopLeft + sr.BottomRight) / 2.0f;
            Point tmp = (tr.TopLeft + tr.BottomRight) / 2.0f;
            Matrix m = new Matrix(1, 0, 0, 1, -smp.X, -smp.Y) * new Matrix(rotate);
            Rect sr1 = sr * m;
            float fw = (float)(tr.Width / sr1.Width);
            float fh = (float)(tr.Height / sr1.Height);
            if (keep)
                fw = fh = Math.Min(fw, fh);
            m *= new Matrix(fw, fh);
            m *= new Matrix(1, 0, 0, 1, tmp.X, tmp.Y);
            return m;
        }

        public string GetTextWithLayout(Rect clip = null, int flags = 0, int tolerance = 5)
        {
            return Utils.GetTextWithLayout(this, clip, flags, tolerance);
        }            

        public List<Entry> GetXObjects() => Parent.GetPageXObjects(Number);

        public Annot AddWidget(PdfWidgetType fieldType, string fieldName)
        {
            var widget = new Widget
            {
                FieldType = (int)fieldType,
                FieldName = fieldName ?? string.Empty,
            };
            return AddWidget(widget);
        }

        /// <summary>MuPDF.NET overload: <see cref="Point"/> instead of <c>object</c>.</summary>
        public Annot AddFileAnnot(
            Point point,
            byte[] buffer_,
            string filename,
            string uFileName = null,
            string desc = null,
            string icon = null) =>
            AddFileAnnot((object)point, buffer_, filename, uFileName, desc, icon);

        public Annot AddStampAnnot(Rect rect, object stamp = null)
        {
            if (stamp == null)
                return AddStampAnnot(rect, 0);
            if (stamp is int i)
                return AddStampAnnot(rect, i);
            if (stamp is byte[] bytes)
                return AddStampAnnot(rect, bytes);
            if (stamp is string path)
                return AddStampAnnot(rect, path);
            if (stamp is Pixmap pixmap)
                return AddStampAnnot(rect, pixmap);
            throw new ArgumentException("bad stamp type");
        }

        public (float, float) InsertHtmlBox(
            Rect rect,
            object text,
            string? css = null,
            float opacity = 1,
            int rotate = 0,
            float scaleLow = 0,
            Archive? archive = null,
            int oc = 0,
            bool overlay = true,
            bool scaleWordWidth = true,
            bool verbose = false)
        {
            var (spareHeight, scale) = InsertHtmlbox(
                rect,
                text?.ToString() ?? string.Empty,
                css,
                scaleLow,
                archive,
                rotate,
                oc,
                opacity,
                overlay,
                scaleWordWidth,
                verbose);
            return ((float)spareHeight, (float)scale);
        }

        public List<Table> GetTables(
            Rect clip = null,
            string vertical_strategy = "lines",
            string horizontal_strategy = "lines",
            List<Edge> vertical_lines = null,
            List<Edge> horizontal_lines = null,
            float snap_tolerance = TableConstants.DefaultSnapTolerance,
            float snap_x_tolerance = 0.0f,
            float snap_y_tolerance = 0.0f,
            float join_tolerance = TableConstants.DefaultJoinTolerance,
            float join_x_tolerance = 0.0f,
            float join_y_tolerance = 0.0f,
            float edge_min_length = 3.0f,
            float min_words_vertical = TableConstants.DefaultMinWordsVertical,
            float min_words_horizontal = TableConstants.DefaultMinWordsHorizontal,
            float intersection_tolerance = 3.0f,
            float intersection_x_tolerance = 0.0f,
            float intersection_y_tolerance = 0.0f,
            float text_tolerance = 3.0f,
            float text_x_tolerance = 3.0f,
            float text_y_tolerance = 3.0f,
            string strategy = null,
            List<Line> add_lines = null)
        {
            return TableHelpers.FindTables(
                this,
                clip: clip,
                verticalStrategy: vertical_strategy,
                horizontalStrategy: horizontal_strategy,
                verticalLines: vertical_lines?.Select(v => v.x0).ToList(),
                horizontalLines: horizontal_lines?.Select(h => h.y0).ToList(),
                snapTolerance: snap_tolerance,
                snapXTolerance: snap_x_tolerance == 0.0f ? (float?)null : snap_x_tolerance,
                snapYTolerance: snap_y_tolerance == 0.0f ? (float?)null : snap_y_tolerance,
                joinTolerance: join_tolerance,
                joinXTolerance: join_x_tolerance == 0.0f ? (float?)null : join_x_tolerance,
                joinYTolerance: join_y_tolerance == 0.0f ? (float?)null : join_y_tolerance,
                edgeMinLength: edge_min_length,
                minWordsVertical: min_words_vertical,
                minWordsHorizontal: min_words_horizontal,
                intersectionTolerance: intersection_tolerance,
                intersectionXTolerance: intersection_x_tolerance == 0.0f ? (float?)null : intersection_x_tolerance,
                intersectionYTolerance: intersection_y_tolerance == 0.0f ? (float?)null : intersection_y_tolerance,
                textTolerance: text_tolerance,
                textXTolerance: text_x_tolerance,
                textYTolerance: text_y_tolerance,
                strategy: strategy)?.Tables ?? new List<Table>();
        }

        public List<Entry> GetFonts(bool full, int legacyMarker) =>
            GetFonts(full).Select(f => new Entry
            {
                Xref = f.xref,
                Ext = f.ext,
                Type = f.type,
                Name = f.baseName,
                RefName = f.name,
                Encoding = f.encoding,
                StreamXref = f.referencer ?? 0,
            }).ToList();

        public List<Entry> GetImages(bool full, int legacyMarker) => GetImages(full);

        public List<TextBlock> GetTextBlocks(IRect clip, int? flags, TextPage textPage, bool sort, int legacyMarker) =>
            GetTextBlocks(clip, flags, textPage, sort)
                .Select(t => new TextBlock
                {
                    X0 = t.x0,
                    Y0 = t.y0,
                    X1 = t.x1,
                    Y1 = t.y1,
                    Text = t.text,
                    BlockNum = t.blockNo,
                    Type = t.blockType,
                })
                .ToList();

        public List<WordBlock> GetTextWords(IRect clip, int? flags, TextPage textPage, bool sort, string delimiters, int legacyMarker) =>
            GetTextWords(clip, flags, textPage, sort, delimiters)
                .Select(t => new WordBlock
                {
                    X0 = t.x0,
                    Y0 = t.y0,
                    X1 = t.x1,
                    Y1 = t.y1,
                    Text = t.word,
                    BlockNum = t.blockNo,
                    LineNum = t.lineNo,
                    WordNum = t.wordNo,
                })
                .ToList();

        /// <summary>MuPDF.NET API: vector paths as <see cref="PathInfo"/>.</summary>
        public List<PathInfo> GetDrawings(bool extended = false) =>
            GetDrawingsDict(extended).Select(d => (PathInfo)d).ToList();

        public List<PathInfo> GetDrawings(bool extended, int legacyMarker) =>
            GetDrawingsDict(extended).Select(d => (PathInfo)d).ToList();

        /// <summary>MuPDF.NET API: cluster drawings supplied as <see cref="PathInfo"/>.</summary>
        public List<Rect> ClusterDrawings(
            Rect clip = null,
            List<PathInfo> drawings = null,
            float xTolerance = 3f,
            float yTolerance = 3f,
            bool finalFilter = true)
        {
            List<Dictionary<string, object>> dictDrawings = null;
            if (drawings != null)
            {
                dictDrawings = new List<Dictionary<string, object>>(drawings.Count);
                foreach (var path in drawings)
                {
                    if (path?.Rect == null)
                        continue;
                    dictDrawings.Add(new Dictionary<string, object> { ["rect"] = path.Rect });
                }
            }

            return ClusterDrawings(clip, dictDrawings, xTolerance, yTolerance, finalFilter);
        }

        public void AddAnnotFromString(List<string> links)
        {
            if (links == null || links.Count == 0)
                return;
            foreach (var text in links)
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                Helpers.AppendPdfAnnotFromObjectString(this, text);
            }
        }

        /// <summary>
        /// Read barcodes from page.
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="decodeEmbeddedOnly">Decode barcodes only from embedded images in the PDF resources.</param>
        /// <param name="barcodeFormat">Barcode format to decode.</param>
        /// <param name="tryHarder">Spend more time to try to find a barcode; optimize for accuracy, not speed.</param>
        /// <param name="tryInverted">Try to decode as inverted image.</param>
        /// <param name="pureBarcode">Image is a pure monochrome image of a barcode.</param>
        /// <param name="multi">Try to read multi barcodes on page.</param>
        /// <param name="autoRotate">Indicate whether the image should be automatically rotated.
        ///                          Rotation is supported for 90, 180 and 270 degrees.</param>
        public List<Barcode> ReadBarcodes(
            Rect clip = null,
            bool decodeEmbeddedOnly = false,
            BarcodeFormat barcodeFormat = BarcodeFormat.ALL,
            bool tryHarder = true,
            bool tryInverted = false,
            bool pureBarcode = false,
            bool multi = true,
            bool autoRotate = true)
        {
            return Utils.ReadBarcodes(this, clip, decodeEmbeddedOnly, barcodeFormat, tryHarder, tryInverted, pureBarcode, multi, autoRotate);
        }

        /// <summary>
        /// Write barcode to page.
        /// </summary>
        /// <param name="clip">Rect area on page to write</param>
        /// <param name="text">Contents to write</param>
        /// <param name="barcodeFormat">Format to encode; Supported formats: QR_CODE, EAN_8, EAN_13, UPC_A, CODE_39, CODE_128, ITF, PDF_417, CODABAR</param>
        /// <param name="characterSet">Use a specific character set for binary encoding (if supported by the selected barcode format)</param>
        /// <param name="disableEci">don't generate ECI segment if non-default character set is used</param>
        /// <param name="forceFitToRect">Resize output barcode image width/height with params, Avoid enabling this parameter, as it can reduce barcode recognition accuracy.</param>
        /// <param name="pureBarcode">Don't put the content string into the output image</param>
        /// <param name="marginLeft">Specifies margin left, in pixels, to use when generating the barcode</param>
        /// <param name="marginTop">Specifies margin top, in pixels, to use when generating the barcode</param>
        /// <param name="marginRight">Specifies margin right, in pixels, to use when generating the barcode</param>
        /// <param name="marginBottom">Specifies margin bottom, in pixels, to use when generating the barcode</param>
        /// <param name="narrowBarWidth">The width of the narrow bar in pixels</param>
        public void WriteBarcode(
            Rect clip,
            string text,
            BarcodeFormat barcodeFormat,
            string characterSet = null,
            bool disableEci = false,
            bool forceFitToRect = false,
            bool pureBarcode = false,
            int marginLeft = 0,
            int marginTop = 0,
            int marginRight = 0,
            int marginBottom = 0,
            int narrowBarWidth = 0
            )
        {
            Utils.WriteBarcode(this, clip,
                text, barcodeFormat, characterSet, disableEci, forceFitToRect, pureBarcode, marginLeft, marginTop, marginRight, marginBottom, narrowBarWidth);
        }

        public void Run(DeviceWrapper dw, Matrix m) => Run(dw?.ToFzDevice(), m);

        public mupdf.FzPage AsFzPage(object page)
        {
            if (page is Page p)
                return p.NativePage;
            if (page is mupdf.PdfPage pp)
                return pp.super();
            return page as mupdf.FzPage;
        }

        public string Format(float[] value)
        {
            if (value == null || value.Length == 0)
                return string.Empty;
            var sb = new StringBuilder();
            foreach (float v in value)
                sb.Append(v.ToString("0.######", CultureInfo.InvariantCulture)).Append(' ');
            return sb.ToString();
        }

        public int AnnotPreProcess(Page page)
        {
            if (page?.Parent == null || !page.Parent.IsPdf)
                throw new Exception("is not PDF");
            int oldRotation = page.Rotation;
            if (oldRotation != 0)
                page.SetRotation(0);
            return oldRotation;
        }

        public void AnnotPostProcess(Page page, Annot annot)
        {
            _ = page;
            _ = annot;
            // Current implementation manages annotation ownership internally.
        }

        public void Erase()
        {
            ResetAnnotRefsInternal();
            try { Parent?.ForgetPageRef(this); } catch { }
            Parent = null;
        }

        public float InsertTextbox(
            Rect rect,
            object text,
            string fontName = "helv",
            string? fontFile = null,
            float fontSize = 11,
            float lineHeight = 0,
            int setSimple = 0,
            int encoding = 0,
            float[]? color = null,
            float[]? fill = null,
            int expandTabs = 1,
            int align = 0,
            float borderWidth = 0.05f,
            int renderMode = 0,
            int rotate = 0,
            object? morph = null,
            bool overlay = true,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            Point? morphFix = null;
            Matrix? morphMat = null;
            if (morph is Morph morphObj)
            {
                morphFix = morphObj.P;
                morphMat = morphObj.M;
            }
            else if (morph is ValueTuple<Point, Matrix> tupleMorph)
            {
                morphFix = tupleMorph.Item1;
                morphMat = tupleMorph.Item2;
            }

            var modernResult = insert_textbox(
                rect,
                text?.ToString() ?? string.Empty,
                align: align,
                border_width: borderWidth,
                color: color,
                encoding: encoding,
                expandtabs: expandTabs,
                fill_opacity: fillOpacity,
                fill: fill,
                fontfile: fontFile,
                fontname: fontName,
                fontsize: fontSize,
                lineheight: lineHeight,
                morph_fix: morphFix,
                morph_mat: morphMat,
                oc: oc,
                overlay: overlay,
                render_mode: renderMode,
                rotate: rotate,
                set_simple: setSimple,
                stroke_opacity: strokeOpacity);
            return modernResult.Rc;
        }

        public void CleanContetns(int sanitize = 1) => CleanContents(sanitize);

        public string SetOpacity(string? gstate = null, float CA = 1, float ca = 1, string? blendmode = null) =>
            _set_opacity(gstate, CA, ca, blendmode);

        public void Recolor(string colorSpaceName)
        {
            int components = colorSpaceName?.ToLowerInvariant() switch
            {
                "gray" or "grey" or "g" => 1,
                "rgb" => 3,
                "cmyk" => 4,
                _ => 3,
            };
            Recolor(components);
        }

        private static Dictionary<string, object> ToLinkDict(LinkInfo link)
        {
            var d = new Dictionary<string, object>
            {
                ["kind"] = (int)link.Kind,
                ["from"] = link.From,
                ["page"] = link.Page,
                ["zoom"] = link.Zoom,
                ["xref"] = link.Xref,
            };
            if (link.To != null)
                d["to"] = link.To;
            else if (!string.IsNullOrEmpty(link.ToStr))
                d["to"] = link.ToStr;
            if (!string.IsNullOrEmpty(link.Name))
                d["name"] = link.Name;
            if (!string.IsNullOrEmpty(link.NamedDest))
                d["nameddest"] = link.NamedDest;
            if (!string.IsNullOrEmpty(link.Uri))
                d["uri"] = link.Uri;
            if (!string.IsNullOrEmpty(link.File))
                d["file"] = link.File;
            if (!string.IsNullOrEmpty(link.Id))
                d["id"] = link.Id;
            return d;
        }
    }

    public static class DocumentLegacyExtensions
    {
        /// <summary>MuPDF.NET <c>Document.set_toc</c> with <see cref="Toc"/> rows.</summary>
        public static int SetToc(this Document doc, List<Toc> tocs, int collapse = 1)
        {
            if (tocs == null || tocs.Count == 0)
                return doc.SetToc((IList<object>)null, collapse);
            var rows = new List<object>(tocs.Count);
            foreach (var t in tocs)
                rows.Add(new object[] { t.Level, t.Title, t.Page });
            return doc.SetToc(rows, collapse);
        }

        // Keep legacy call shape doc.NeedAppearances(int) without conflicting with Document.NeedAppearances property.
        public static int NeedAppearances(this Document doc, int value = 0)
        {
            if (!doc.IsFormPdf)
                return 0;
            bool hadNeedAppearances = doc.need_appearances() ?? false;
            doc.set_need_appearances(value != 0);
            return value == 0 ? (hadNeedAppearances ? 1 : 0) : value;
        }

        // Legacy shape: page.GetTextPage(Rect clip, int flags, Matrix matrix)
        public static TextPage GetTextPage(this Page page, Rect clip = null, int flags = 0, Matrix matrix = null)
        {
            _ = matrix;
            return page.GetTextPage(flags, clip == null ? null : new IRect(clip));
        }

        // Legacy Rect-based text helpers.
        public static dynamic GetText(
            this Page page,
            string option = "text",
            Rect clip = null,
            int? flags = null,
            TextPage textpage = null,
            bool sort = false) =>
            page.GetText(option, clip == null ? null : new IRect(clip), flags, textpage, sort);

        public static List<TextBlock> GetTextBlocks(
            this Page page,
            Rect clip = null,
            int? flags = null,
            TextPage textPage = null,
            bool sort = false)
        {
            var rows = page.GetTextBlocks(clip == null ? null : new IRect(clip), flags, textPage, sort);
            return rows.Select(t => new TextBlock
            {
                X0 = t.x0,
                Y0 = t.y0,
                X1 = t.x1,
                Y1 = t.y1,
                Text = t.text,
                BlockNum = t.blockNo,
                Type = t.blockType,
            }).ToList();
        }

        public static List<WordBlock> GetTextWords(
            this Page page,
            Rect clip = null,
            int? flags = null,
            TextPage textPage = null,
            bool sort = false,
            string delimiters = null)
        {
            var rows = page.GetTextWords(clip == null ? null : new IRect(clip), flags, textPage, sort, delimiters);
            return rows.Select(t => new WordBlock
            {
                X0 = t.x0,
                Y0 = t.y0,
                X1 = t.x1,
                Y1 = t.y1,
                Text = t.word,
                BlockNum = t.blockNo,
                LineNum = t.lineNo,
                WordNum = t.wordNo,
            }).ToList();
        }

        public static string GetTextSelection(
            this Page page,
            Point p1,
            Point p2,
            Rect clip = null,
            TextPage textpage = null) =>
            page.GetTextSelection(p1, p2, clip == null ? null : new IRect(clip), textpage);

        // Legacy search signature: Rect clip + quads flag.
        public static List<Quad> SearchFor(
            this Page page,
            string needle,
            Rect clip = null,
            bool quads = false,
            int flags = (int)(TextFlags.TEXT_DEHYPHENATE
                | TextFlags.TEXT_PRESERVE_WHITESPACE
                | TextFlags.TEXT_PRESERVE_LIGATURES
                | TextFlags.TEXT_MEDIABOX_CLIP),
            TextPage stPage = null)
        {
            var qclip = clip?.Quad;
            if (quads)
                return page.SearchFor(needle, qclip, maxHits: 0, flags: flags, textpage: stPage);

            return page.SearchForRects(needle, qclip, maxHits: 0, flags: flags, textpage: stPage)
                .Select(r => r.Quad)
                .ToList();
        }
    }

}
