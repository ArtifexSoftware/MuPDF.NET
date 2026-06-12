using System;
using System.Collections.Generic;
using System.Linq;

namespace MuPDF.NET
{
    /// <summary>
    /// Legacy MuPDF.NET API for <see cref="Document"/> (readthedocs compatibility).
    /// </summary>
    /// <remarks><see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/>.</remarks>
    public partial class Document
    {
        /// <summary>
        /// Legacy readthedocs <c>ToFzDocument</c> — forwards to &lt;see cref="NativeDocument"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="NativeDocument"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public mupdf.FzDocument ToFzDocument() => NativeDocument;
        /// <summary>
        /// Legacy readthedocs <c>InitDocument</c> — Initializes outline and internal state after open.
        /// </summary>
        /// <remarks>See <see cref="InitDoc"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void InitDocument() => InitDoc();
        /// <summary>
        /// Legacy readthedocs <c>SetObjectValue</c> — forwards to &lt;see cref="SetObjectValue"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="SetObjectValue"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="key">PDF dictionary key name.</param>
        public static mupdf.PdfObj SetObjectValue(mupdf.PdfObj obj, string key, string value)
        {
            var pdf = obj.pdf_get_bound_document();
            return Helpers.JmSetObjectValue(pdf, obj, key, value);
        }
        /// <summary>
        /// Legacy readthedocs <c>GetChapterPageCount</c> — forwards to &lt;see cref="ChapterPageCount"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="ChapterPageCount"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetChapterPageCount(int chapter) => ChapterPageCount(chapter);
        /// <summary>
        /// Legacy readthedocs <c>Contains</c> — forwards to &lt;see cref="Contains"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="Contains"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool Contains((int chapter, int page) loc) => ContainsLocation(loc);
        /// <summary>
        /// Legacy readthedocs <c>GetPageNumberFromLocation</c> — forwards to &lt;see cref="PageNumberFromLocation"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="PageNumberFromLocation"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetPageNumberFromLocation(int chapter, int pno) =>
            PageNumberFromLocation((chapter, pno));
        /// <summary>
        /// Legacy readthedocs <c>GetPageXref</c> — forwards to &lt;see cref="PageXref"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="PageXref"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetPageXref(int pno) => PageXref(pno);
        /// <summary>
        /// Legacy readthedocs <c>GetPdfCatalog</c> — Gets the xref of the PDF catalog (root) dictionary.
        /// </summary>
        /// <remarks>See <see cref="PdfCatalog"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetPdfCatalog() => PdfCatalog;
        /// <summary>
        /// Legacy readthedocs <c>GetPdfTrailer</c> — Gets the trailer dictionary as a formatted string.
        /// </summary>
        /// <remarks>See <see cref="PdfTrailer"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public string GetPdfTrailer(int compressed = 0, int ascii = 0) =>
            PdfTrailer(compressed != 0, ascii != 0);
        /// <summary>
        /// Legacy readthedocs <c>GetPdfFileID</c> — forwards to &lt;see cref="PdfFileID"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="PdfFileID"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public List<string> GetPdfFileID() => _getPDFfileid();
        /// <summary>
        /// Legacy readthedocs <c>GetXrefLength</c> — forwards to &lt;see cref="XrefLength"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="XrefLength"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetXrefLength() => XrefLength;
        /// <summary>
        /// Legacy readthedocs <c>GetXrefObject</c> — forwards to &lt;see cref="XrefObject"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="XrefObject"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public string GetXrefObject(int xref, int compressed = 0, int ascii = 0) =>
            XrefObject(xref, compressed != 0, ascii != 0);
        /// <summary>
        /// Legacy readthedocs <c>GetXrefStream</c> — Gets the decompressed stream bytes at xref.
        /// </summary>
        /// <remarks>See <see cref="XrefStream"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public byte[] GetXrefStream(int xref) => XrefStream(xref);
        /// <summary>
        /// Legacy readthedocs <c>GetXrefStreamRaw</c> — forwards to &lt;see cref="XrefStreamRaw"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="XrefStreamRaw"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public byte[] GetXrefStreamRaw(int xref) => XrefStreamRaw(xref);
        /// <summary>
        /// Legacy readthedocs <c>SaveIncremental</c> — forwards to &lt;see cref="SaveIncremental"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="SaveIncremental"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void SaveIncremental() => SaveIncr();
        /// <summary>
        /// Legacy readthedocs <c>SetLayout</c> — Re-layouts a reflowable document to new dimensions.
        /// </summary>
        /// <remarks>See <see cref="Layout"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void SetLayout(string layout) => SetPageLayout(layout);
        /// <summary>
        /// Legacy readthedocs <c>SetLayout</c> — Re-layouts a reflowable document to new dimensions.
        /// </summary>
        /// <remarks>See <see cref="Layout"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="rect">Page layout rectangle for reflowable documents (top-left at origin).</param>
        /// <param name="width">Page width for reflowable layout (used with height if rect is omitted).</param>
        /// <param name="height">Page height for reflowable layout (used with width if rect is omitted).</param>
        /// <param name="fontSize">Default font size for reflowable layout.</param>
        public void SetLayout(Rect rect = null, float width = 0, float height = 0, int fontSize = 11)
        {
            if (rect != null && !rect.IsEmpty && !rect.IsInfinite)
            {
                Layout(rect, fontSize);
                return;
            }
            Layout(width, height, fontSize);
        }
        /// <summary>
        /// Legacy readthedocs <c>GetKeysXref</c> — forwards to &lt;see cref="XrefGetKeys"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="XrefGetKeys"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public List<string> GetKeysXref(int xref) => XrefGetKeys(xref);
        /// <summary>
        /// Legacy readthedocs <c>XrefIsXObject</c> — forwards to &lt;see cref="XrefIsXObject"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="XrefIsXObject"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool XrefIsXObject(int xref) => XrefIsXobject(xref);
        /// <summary>
        /// Legacy readthedocs <c>SetXmlMetaData</c> — forwards to &lt;see cref="SetXmlMetadata"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="SetXmlMetadata"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void SetXmlMetaData(string metadata) => SetXmlMetadata(metadata);
        /// <summary>
        /// Legacy readthedocs <c>MetaData</c> — Gets or sets the document metadata dictionary (PDF Info keys).
        /// </summary>
        /// <remarks>See <see cref="Metadata"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public Dictionary<string, string> MetaData
        {
            get => Metadata;
            set => Metadata = value;
        }
        /// <summary>
        /// Legacy readthedocs <c>GetOC</c> — forwards to &lt;see cref="OC"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="OC"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetOC(int xref) => GetOc(xref);
        /// <summary>
        /// Legacy readthedocs <c>SetOC</c> — forwards to &lt;see cref="OC"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="OC"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void SetOC(int xref, int oc) => SetOc(xref, oc);
        /// <summary>
        /// Legacy readthedocs <c>GetOCMD</c> — forwards to &lt;see cref="OCMD"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="OCMD"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">PDF cross-reference number of the object.</param>
        public OCMD GetOCMD(int xref)
        {
            var d = GetOcmd(xref);
            return new OCMD
            {
                Xref = d.TryGetValue("xref", out var xr) ? Convert.ToInt32(xr) : xref,
                Ocgs = d.TryGetValue("ocgs", out var ocgs) && ocgs is IEnumerable<object> eo ? eo.Select(Convert.ToInt32).ToArray() : Array.Empty<int>(),
                Policy = d.TryGetValue("policy", out var pol) ? pol?.ToString() : null,
                Ve = d.TryGetValue("ve", out var ve) && ve is IEnumerable<object> veo ? veo.ToArray() : Array.Empty<object>(),
            };
        }
        /// <summary>
        /// Legacy readthedocs <c>SetOCMD</c> — forwards to &lt;see cref="OCMD"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="OCMD"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="ocmd">Optional content membership dictionary.</param>
        /// <param name="xref">PDF cross-reference number of the object.</param>
        /// <param name="ocgs">List of OCG xref numbers.</param>
        /// <param name="policy">OCMD policy: AnyOn, AnyOff, AllOn, or AllOff.</param>
        /// <param name="ve">Visibility expression (nested lists) for OCMD.</param>
        public int SetOCMD(
            OCMD ocmd = null,
            int xref = 0,
            int[] ocgs = null,
            string policy = null,
            object[] ve = null)
        {
            if (ocmd != null)
            {
                if (xref == 0)
                    xref = ocmd.Xref;
                if (ocgs == null)
                    ocgs = ocmd.Ocgs;
                if (string.IsNullOrEmpty(policy))
                    policy = ocmd.Policy;
                if (ve == null)
                    ve = ocmd.Ve;
            }
            return SetOcmd(xref, ocgs?.ToList(), policy, ve);
        }
        /// <summary>
        /// Legacy readthedocs <c>IsPDF</c> — forwards to &lt;see cref="IsPdf"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="IsPdf"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsPDF => IsPdf;
        /// <summary>
        /// Legacy readthedocs <c>IsFormPDF</c> — forwards to &lt;see cref="IsFormPDF"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="IsFormPDF"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int IsFormPDF
        {
            get
            {
                mupdf.PdfDocument pdf = Helpers.AsPdfDocument(this, required: false);
                if (pdf.m_internal == null)
                    return -1;

                int count = -1;
                try
                {
                    mupdf.PdfObj fields = PdfDictGetl(
                        mupdf.mupdf.pdf_trailer(pdf),
                        mupdf.mupdf.PDF_ENUM_NAME_Root,
                        mupdf.mupdf.PDF_ENUM_NAME_AcroForm,
                        mupdf.mupdf.PDF_ENUM_NAME_Fields);
                    if (mupdf.mupdf.pdf_is_array(fields) != 0)
                        count = mupdf.mupdf.pdf_array_len(fields);
                }
                catch
                {
                    return -1;
                }

                return count >= 0 ? count : -1;
            }
        }
        /// <summary>
        /// Legacy readthedocs <c>Recolor</c> — PDF only: execute <see cref="Page.Recolor"/> for all pages
        /// </summary>
        /// <remarks>See <see cref="Recolor"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void Recolor(int pageNum, int colorNum) => RecolorPage(pageNum, colorNum);
        /// <summary>
        /// Legacy readthedocs <c>Recolor</c> — PDF only: execute <see cref="Page.Recolor"/> for all pages
        /// </summary>
        /// <remarks>See <see cref="Recolor"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void Recolor(int pageNum, string colorSpaceName)
        {
            int components = colorSpaceName?.ToLowerInvariant() switch
            {
                "gray" or "grey" or "g" => 1,
                "rgb" => 3,
                "cmyk" => 4,
                _ => 3,
            };
            RecolorPage(pageNum, components);
        }
        /// <summary>
        /// Legacy readthedocs <c>LayerUIConfigs</c> — forwards to &lt;see cref="LayerUIConfigs"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="LayerUIConfigs"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public List<LayerConfigUI> LayerUIConfigs()
        {
            return LayerUiConfigs().Select(ui => new LayerConfigUI
            {
                Number = ui.TryGetValue("number", out var n) ? Convert.ToInt32(n) : 0,
                Text = ui.TryGetValue("text", out var t) ? t?.ToString() : null,
                Type = ui.TryGetValue("type", out var ty) ? ty?.ToString() : null,
                Depth = ui.TryGetValue("depth", out var d) ? Convert.ToInt32(d) : 0,
                On = ui.TryGetValue("on", out var on) && Convert.ToBoolean(on),
                IsLocked = ui.TryGetValue("locked", out var lk) && Convert.ToBoolean(lk),
            }).ToList();
        }
        /// <summary>
        /// Legacy readthedocs <c>RewriteImage</c> — forwards to &lt;see cref="RewriteImage"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="RewriteImage"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="dpiThreshold">Legacy parameter forwarded to &lt;see cref="RewriteImage"/&gt;.</param>
        /// <param name="dpiTarget">Legacy parameter forwarded to &lt;see cref="RewriteImage"/&gt;.</param>
        /// <param name="quality">JPEG quality for lossy recompression.</param>
        /// <param name="lossy">Whether lossy images may be recompressed.</param>
        /// <param name="lossless">Whether lossless images may be recompressed.</param>
        /// <param name="bitonal">Whether bitonal images may be processed.</param>
        /// <param name="color">Whether color images may be processed.</param>
        /// <param name="gray">Whether grayscale images may be processed.</param>
        /// <param name="setToGray">Legacy parameter forwarded to &lt;see cref="RewriteImage"/&gt;.</param>
        /// <param name="options">Low-level MuPDF PdfImageRewriterOptions (advanced).</param>
        public void RewriteImage(
            int dpiThreshold = -1,
            int dpiTarget = 0,
            int quality = 0,
            bool lossy = true,
            bool lossless = true,
            bool bitonal = true,
            bool color = true,
            bool gray = true,
            bool setToGray = false,
            mupdf.PdfImageRewriterOptions options = null)
        {
            if (setToGray)
                Recolor(1);

            if (options != null)
            {
                EnsurePdf();
                mupdf.mupdf.pdf_rewrite_images(NativePdfDocument, options);
                return;
            }

            if (dpiTarget < 0)
            {
                dpiThreshold = 0;
                dpiTarget = 0;
            }
            if (dpiTarget > 0 && dpiTarget >= dpiThreshold)
                throw new Exception($"dpi_target={dpiTarget} must be less than dpi_threshold={dpiThreshold}");

            RewriteImages(quality, dpiThreshold, dpiTarget, lossy, lossless, bitonal, color, gray);
        }
        /// <summary>
        /// Legacy readthedocs <c>AddEmbfile</c> — forwards to &lt;see cref="AddEmbfile"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="AddEmbfile"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int AddEmbfile(
            string name,
            byte[] buffer,
            string filename = null,
            string uFileName = null,
            string desc = null) =>
            AddEmbeddedFile(name, buffer, filename, uFileName, desc);
        /// <summary>
        /// Legacy readthedocs <c>GetEmbfileNames</c> — forwards to &lt;see cref="GetEmbeddedFileNames"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="GetEmbeddedFileNames"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public List<string> GetEmbfileNames() => GetEmbeddedFileNames();
        /// <summary>
        /// Legacy readthedocs <c>GetEmbfileCount</c> — Gets the number of embedded files.
        /// </summary>
        /// <remarks>See <see cref="EmbeddedFileCount"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetEmbfileCount() => EmbeddedFileCount;
        /// <summary>
        /// Legacy readthedocs <c>DeleteEmbfile</c> — forwards to &lt;see cref="DeleteEmbfile"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="DeleteEmbfile"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void DeleteEmbfile(int item) => DeleteEmbeddedFile(item);
        /// <summary>
        /// Legacy readthedocs <c>DeleteEmbfile</c> — forwards to &lt;see cref="DeleteEmbfile"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="DeleteEmbfile"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void DeleteEmbfile(string item) => DeleteEmbeddedFile(item);
        /// <summary>
        /// Legacy readthedocs <c>GetEmbfile</c> — forwards to &lt;see cref="GetEmbeddedFile"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="GetEmbeddedFile"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public byte[] GetEmbfile(int item) => GetEmbeddedFile(item);
        /// <summary>
        /// Legacy readthedocs <c>GetEmbfileInfo</c> — forwards to &lt;see cref="EmbfileInfo"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="EmbfileInfo"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="item">Embedded file index or name.</param>
        public EmbfileInfo GetEmbfileInfo(object item)
        {
            Dictionary<string, object> info = item is int i ? GetEmbeddedFileInfo(i) : GetEmbeddedFileInfo(item?.ToString() ?? string.Empty);
            static string S(Dictionary<string, object> d, string key) => d.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            static int I(Dictionary<string, object> d, string key) => d.TryGetValue(key, out var v) ? Convert.ToInt32(v) : 0;
            return new EmbfileInfo
            {
                Name = S(info, "name"),
                CreationDate = S(info, "creationDate"),
                ModDate = S(info, "modDate"),
                CheckSum = S(info, "checksum"),
                Collection = I(info, "collection"),
                FileName = S(info, "filename"),
                UFileName = S(info, "ufilename"),
                Desc = S(info, "desc"),
                Size = I(info, "size"),
                Length = I(info, "length"),
            };
        }
        /// <summary>
        /// Legacy readthedocs <c>GetEmbfileUpd</c> — forwards to &lt;see cref="EmbfileUpd"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="EmbfileUpd"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetEmbfileUpd(
            object item,
            byte[] buffer = null,
            string filename = null,
            string uFileName = null,
            string desc = null) =>
            item is int i
                ? UpdateEmbeddedFile(i, buffer, filename, uFileName, desc)
                : UpdateEmbeddedFile(item?.ToString() ?? string.Empty, buffer, filename, uFileName, desc);
        /// <summary>
        /// Legacy readthedocs <c>IsEnabledJournal</c> — forwards to &lt;see cref="IsEnabledJournal"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="IsEnabledJournal"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsEnabledJournal() => JournalIsEnabled;
        /// <summary>
        /// Legacy readthedocs <c>IsStream</c> — forwards to &lt;see cref="IsStream"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="IsStream"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public bool IsStream(int xref = 0) => XrefIsStream(xref);
        /// <summary>
        /// Legacy readthedocs <c>SetKeyXRef</c> — forwards to &lt;see cref="KeyXRef"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="KeyXRef"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void SetKeyXRef(int xref, string key, string value) => XrefSetKey(xref, key, value);
        /// <summary>
        /// Legacy readthedocs <c>RemoveTocItem</c> — forwards to &lt;see cref="RemoveTocItem"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="RemoveTocItem"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void RemoveTocItem(int xref) => _remove_toc_item(xref);
        /// <summary>
        /// Legacy readthedocs <c>UpdateTocItem</c> — forwards to &lt;see cref="UpdateTocItem"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="UpdateTocItem"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void UpdateTocItem(
            int xref,
            string action = null,
            string title = null,
            int flags = 0,
            bool collapse = false,
            float[] color = null) =>
            _update_toc_item(xref, action, title, flags, collapse, color);
        /// <summary>
        /// Legacy readthedocs <c>GetPageNumberFromLocation</c> — forwards to &lt;see cref="PageNumberFromLocation"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="PageNumberFromLocation"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pageId">Legacy parameter forwarded to &lt;see cref="PageNumberFromLocation"/&gt;.</param>
        public int GetPageNumberFromLocation(int pageId)
        {
            int pageN = PageCount;
            while (pageId < 0)
                pageId += pageN;
            if (pageId < 0 || pageId >= pageN)
                throw new Exception("page id not in document");
            return PageNumberFromLocation((0, pageId));
        }
        /// <summary>
        /// Legacy readthedocs <c>Convert2Pdf</c> — forwards to &lt;see cref="Convert2Pdf"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="Convert2Pdf"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public byte[] Convert2Pdf(int from = 0, int to = -1, int rotate = 0) => ConvertToPdf(from, to, rotate);
        /// <summary>
        /// Legacy readthedocs <c>AsPdfDocument</c> — forwards to &lt;see cref="AsPdfDocument"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="AsPdfDocument"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public static mupdf.PdfDocument AsPdfDocument(mupdf.FzDocument document, bool required = true) =>
            Helpers.AsPdfDocument(document, required);
        /// <summary>
        /// Legacy readthedocs <c>AsPdfDocument</c> — forwards to &lt;see cref="AsPdfDocument"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="AsPdfDocument"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public static mupdf.PdfDocument AsPdfDocument(Document document) =>
            Helpers.AsPdfDocument(document, required: false);
        /// <summary>
        /// Legacy readthedocs <c>ExtendTocItems</c> — forwards to &lt;see cref="ExtendTocItems"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="ExtendTocItems"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="items">Legacy parameter forwarded to &lt;see cref="ExtendTocItems"/&gt;.</param>
        public void ExtendTocItems(List<Toc> items)
        {
            var src = items?
                .Select(t => (t.Level, t.Title, t.Page, t.Link as Dictionary<string, object> ?? new Dictionary<string, object>()))
                .ToList() ?? new List<(int level, string title, int page, Dictionary<string, object> link)>();
            _extend_toc_items(src);
        }
        /// <summary>
        /// Legacy readthedocs <c>ForgetPage</c> — forwards to &lt;see cref="ForgetPage"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="ForgetPage"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void ForgetPage(Page page) => ForgetPageRef(page);
        /// <summary>
        /// Legacy readthedocs <c>EmbeddedfileIndex</c> — forwards to &lt;see cref="EmbeddedfileIndex"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="EmbeddedfileIndex"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int EmbeddedfileIndex(object item) => _embeddedFileIndex(item);
        /// <summary>
        /// Legacy readthedocs <c>FindBookmark</c> — retrieve page location after laid out document
        /// </summary>
        /// <remarks>See <see cref="FindBookmark"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="bm">Bookmark pointer from MakeBookmark.</param>
        public Location FindBookmark(int bm)
        {
            var (chapter, page) = FindBookmark((ulong)Math.Max(0, bm));
            return new Location { Chapter = chapter, Page = page };
        }
        /// <summary>
        /// Legacy readthedocs <c>CopyFullPage</c> — forwards to &lt;see cref="CopyFullPage"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="CopyFullPage"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void CopyFullPage(int pno, int to = -1) => FullCopyPage(pno, to);
        /// <summary>
        /// Legacy readthedocs <c>CopyXref</c> — forwards to &lt;see cref="CopyXref"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="CopyXref"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void CopyXref(int source, int target, List<string> keep = null) =>
            XrefCopy(source, target, keep);
        /// <summary>
        /// Legacy readthedocs <c>GetPageXObjects</c> — forwards to &lt;see cref="PageXObjects"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="PageXObjects"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">0-based page number. Negative values wrap from the end of the document.</param>
        public List<Entry> GetPageXObjects(int pno)
        {
            var rows = GetPageXobjects(pno) ?? new List<Dictionary<string, object>>();
            return rows.Select(r => new Entry
            {
                Xref = r.TryGetValue("xref", out var x) ? Convert.ToInt32(x) : 0,
                Name = r.TryGetValue("name", out var n) ? n?.ToString() : null,
                StreamXref = r.TryGetValue("stream_xref", out var sx) ? Convert.ToInt32(sx) : 0,
                Bbox = r.TryGetValue("bbox", out var b) && b is Rect rb ? rb : null,
            }).ToList();
        }
        /// <summary>
        /// Legacy readthedocs <c>GetPages</c> — Iterates pages with optional start, stop, and step (like Python slice).
        /// </summary>
        /// <remarks>See <see cref="Pages"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public List<Page> GetPages(int start, int stop, int step) => Pages(start, stop, step).ToList();

        // Legacy overload: mirrors MuPDF.NET ExtractFont(xref, infoOnly, named).
        /// <summary>
        /// Legacy readthedocs <c>ExtractFont</c> — PDF only: extract a font by xref
        /// </summary>
        /// <remarks>See <see cref="ExtractFont"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="xref">PDF cross-reference number of the object.</param>
        /// <param name="infoOnly">If non-zero, return font info without font file bytes.</param>
        /// <param name="named">Named font filter (legacy ExtractFont).</param>
        public FontInfo ExtractFont(int xref = 0, int infoOnly = 0, string named = null)
        {
            var (name, ext, type, content) = ExtractFont(xref);
            return new FontInfo
            {
                Name = name ?? string.Empty,
                Ext = ext ?? string.Empty,
                Type = type ?? string.Empty,
                Content = infoOnly != 0 ? Array.Empty<byte>() : (content ?? Array.Empty<byte>()),
            };
        }

        /// <summary>
        /// Legacy MuPDF.NET / PyMuPDF <c>Document(filename, stream, filetype, rect, width, height, fontsize)</c>.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Document(string, string, Rect, float, float, float)"/> or
        /// <see cref="Document(byte[], string, Rect, float, float, float)"/> for
        /// <c>fileName:</c> / <c>stream:</c> named arguments. This overload covers
        /// <c>new Document("pdf", bytes)</c> and <c>new Document(path, (byte[])null)</c>.
        /// </remarks>
        public Document(
            string fileName,
            byte[] stream,
            string fileType = null,
            Rect rect = null,
            float width = 0,
            float height = 0,
            int fontSize = 11)
        {
            InitFromLegacyOpen(fileName, stream, fileType, rect, width, height, fontSize);
        }

        /// <summary>MuPDF.NET named-args: <c>Document(stream: bytes, fileType: "pdf")</c>.</summary>
        public Document(
            DocumentStream stream,
            string fileType = null,
            Rect rect = null,
            float width = 0,
            float height = 0,
            int fontSize = 11)
        {
            InitFromByteArray(stream.Bytes, fileType, rect, width, height, fontSize);
        }

        public (string type, string value) GetKeyXref(int xref, string key) => XrefGetKey(xref, key);
        /// <summary>
        /// Legacy readthedocs <c>Select</c> — PDF only: select a subset of pages
        /// </summary>
        /// <remarks>See <see cref="Select"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public void Select(List<int> pages) => Select(pages?.ToArray() ?? Array.Empty<int>());
        /// <summary>
        /// Legacy readthedocs <c>SetPageLabels</c> — PDF only: add/update page label definitions
        /// </summary>
        /// <remarks>See <see cref="PageLabels"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="labels">Page label definition dictionaries.</param>
        public void SetPageLabels(List<Label> labels)
        {
            if (labels == null)
                return;
            var dicts = labels.Select(l => new Dictionary<string, object>
            {
                ["startpage"] = l.StartPage,
                ["prefix"] = l.Prefix ?? string.Empty,
                ["style"] = l.Style ?? string.Empty,
                ["firstpagenum"] = l.FirstPageNum,
            }).ToList();
            SetPageLabels(dicts);
        }
        /// <summary>
        /// Legacy readthedocs <c>Save</c> — PDF only: save the document
        /// </summary>
        /// <remarks>See <see cref="Save"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="filename">File path to open or save.</param>
        /// <param name="garbage">Garbage-collection level (0–4). Positive values exclude incremental save.</param>
        /// <param name="clean">If true, clean and sanitize content streams.</param>
        /// <param name="deflate">If true, deflate (compress) uncompressed streams.</param>
        /// <param name="deflateImages">If true, deflate uncompressed image streams.</param>
        /// <param name="deflateFonts">If true, deflate uncompressed font streams.</param>
        /// <param name="incremental">If true, save only changes (requires saving to the original path).</param>
        /// <param name="ascii">If true, restrict xref_object output to ASCII.</param>
        /// <param name="expand">Decompression level for objects (0, 1, 2, or 255 for all).</param>
        /// <param name="linear">If true, write a linearized PDF for fast web access.</param>
        /// <param name="noNewId">If true, do not regenerate the document /ID entry.</param>
        /// <param name="appearance">If true, regenerate widget appearance streams when saving.</param>
        /// <param name="pretty">If true, prettify PDF object syntax for readability.</param>
        /// <param name="encryption">Encryption method (see <see cref="Constants"/> PDF encryption members such as <see cref="Constants.PDF_ENCRYPT_AES_256"/>).</param>
        /// <param name="permissions">Permission flags bitmask (see <see cref="Constants.PDF_PERM_PRINT"/> and related <see cref="Constants"/> permission flags).</param>
        /// <param name="ownerPW">Legacy parameter forwarded to &lt;see cref="Save"/&gt;.</param>
        /// <param name="userPW">Legacy parameter forwarded to &lt;see cref="Save"/&gt;.</param>
        /// <param name="preserveMetadata">Legacy parameter forwarded to &lt;see cref="Save"/&gt;.</param>
        /// <param name="useObjstms">Legacy parameter forwarded to &lt;see cref="Save"/&gt;.</param>
        /// <param name="compressionEffort">Legacy parameter forwarded to &lt;see cref="Save"/&gt;.</param>
        /// <param name="raiseOnRepair">Legacy parameter forwarded to &lt;see cref="Save"/&gt;.</param>
        public void Save(
            string filename,
            int garbage = 0,
            int clean = 0,
            int deflate = 0,
            int deflateImages = 0,
            int deflateFonts = 0,
            int incremental = 0,
            int ascii = 0,
            int expand = 0,
            int linear = 0,
            int? noNewId = null,
            int appearance = 0,
            int pretty = 0,
            int encryption = 1,
            int permissions = 4095,
            string ownerPW = null,
            string userPW = null,
            int preserveMetadata = 1,
            int useObjstms = 0,
            int compressionEffort = 0,
            bool raiseOnRepair = false)
        {
            Save(
                (object)filename,
                garbage,
                clean,
                deflate,
                deflateImages,
                deflateFonts,
                incremental,
                ascii,
                expand,
                linear,
                noNewId,
                appearance,
                pretty,
                encryption,
                permissions,
                ownerPW,
                userPW,
                preserveMetadata,
                useObjstms,
                compressionEffort,
                raiseOnRepair);
        }
        /// <summary>
        /// Legacy readthedocs <c>Outline</c> — first `Outline` item
        /// </summary>
        /// <remarks>See <see cref="Outline"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public Outline Outline => GetOutline();
        /// <summary>
        /// Legacy readthedocs <c>LoadOutline</c> — forwards to &lt;see cref="LoadOutline"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="LoadOutline"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public Outline LoadOutline() => GetOutline();
        /// <summary>
        /// Legacy readthedocs <c>GetPdfCatelog</c> — forwards to &lt;see cref="GetPdfCatalog"/&gt;.
        /// </summary>
        /// <remarks>See <see cref="GetPdfCatalog"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        public int GetPdfCatelog() => GetPdfCatalog();

        /// <summary>Convert global page number to (chapter, page).</summary>
        public (int chapter, int page) GetLocationFromPageNumber(int pno) => LocationFromPageNumber(pno);
        /// <summary>
        /// Legacy readthedocs <c>GetPagePixmap</c> — create a pixmap of a page by page number
        /// </summary>
        /// <remarks>See <see cref="PagePixmap"/>. <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Document.html"/></remarks>
        /// <param name="pno">0-based page number. Negative values wrap from the end of the document.</param>
        /// <param name="matrix">Transformation matrix applied when rendering.</param>
        /// <param name="colorSpace">Legacy parameter forwarded to &lt;see cref="PagePixmap"/&gt;.</param>
        /// <param name="alpha">Whether to include an alpha channel in the pixmap.</param>
        /// <param name="clip">Clip rectangle in page coordinates.</param>
        public Pixmap GetPagePixmap(int pno, Matrix matrix, ColorSpace colorSpace, int alpha, Rect clip)
        {
            using var page = LoadPage(pno);
            Colorspace cs = colorSpace;
            return page.GetPixmap(matrix, cs, clip?.IRect, alpha != 0);
        }
    }
}
