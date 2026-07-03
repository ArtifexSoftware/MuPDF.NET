using System;
using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Legacy MuPDF.NET API surface for <see cref="Annot"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This partial class restores names and signatures from the original MuPDF.NET wrapper
    /// documented at
    /// <see href="https://mupdfnet.readthedocs.io/en/latest/classes/Annot.html"/>.
    /// Implementations forward to the modern MuPDF-aligned API in <c>Annot.cs</c>.
    /// </para>
    /// <para>
    /// <b>PDF only.</b> An annotation is bound to its <see cref="Parent"/> page. If the page
    /// or document becomes invalid (close, structural change, reload), annotation wrappers may
    /// be orphaned and throw when accessed.
    /// </para>
    /// </remarks>
    public partial class Annot
    {
        /// <summary>
        /// Border, background, and fill colors of the annotation.
        /// </summary>
        /// <remarks>
        /// Legacy MuPDF.NET returned a <see cref="Color"/> object with <see cref="Color.Stroke"/>
        /// and <see cref="Color.Fill"/> arrays (float components 0–1).
        /// For MuPDF-style stroke-only access use <see cref="StrokeColor"/> on the main partial.
        /// </remarks>
        /// <value>A <see cref="Color"/> built from the PDF annotation dictionary.</value>
        public Color Colors => GetColorFromAnnot(AnnotObj, 0);

        /// <summary>
        /// Annotation appearance bounding box in page coordinates.
        /// </summary>
        /// <remarks>
        /// Legacy spelling <c>ApnBbox</c> (modern API: <see cref="ApnBBox"/>).
        /// Corresponds to the <c>/BBox</c> entry of the normal appearance stream (<c>/AP /N</c>),
        /// transformed into page space.
        /// </remarks>
        public Rect ApnBbox
        {
            get => ApnBBox;
            set => SetApnBBox(value);
        }

        /// <summary>
        /// Various annotation metadata (content, author, dates, subject, name, id).
        /// </summary>
        /// <remarks>
        /// Populated from the same fields as <see cref="GetInfo"/>, packaged as an
        /// <see cref="AnnotInfo"/> DTO for legacy code.
        /// Keys <c>content</c>, <c>title</c> (author), <c>creationDate</c>, <c>modDate</c>,
        /// <c>subject</c>, <c>name</c>, and <c>id</c> match the readthedocs description.
        /// </remarks>
        public AnnotInfo Info
        {
            get
            {
                var d = GetInfo() ?? new Dictionary<string, string>();
                string S(string k) => d.TryGetValue(k, out var v) ? v : null;
                return new AnnotInfo
                {
                    Content = S("content"),
                    Name = S("name"),
                    Title = S("title"),
                    CreationDate = S("creationDate"),
                    ModDate = S("modDate"),
                    Subject = S("subject"),
                    Id = S("id"),
                };
            }
        }

        /// <summary>
        /// Basic information about a file attached to this annotation.
        /// </summary>
        /// <remarks>
        /// Applies to file-attachment annotations. Returns filename, description, and size fields
        /// from <see cref="GetFileInfo"/>. Legacy property name <c>FileInfo</c>.
        /// </remarks>
        public FileInfo FileInfo
        {
            get
            {
                var d = GetFileInfo() ?? new Dictionary<string, object>();
                int I(string k) => d.TryGetValue(k, out var v) ? Convert.ToInt32(v) : 0;
                string S(string k) => d.TryGetValue(k, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
                return new FileInfo
                {
                    FileName = S("filename"),
                    Desc = S("desc"),
                    Length = I("length"),
                    Size = I("size"),
                };
            }
        }

        /// <summary>
        /// Annotation delta values relative to <see cref="Rect"/> (PDF <c>/RD</c> entry).
        /// </summary>
        /// <remarks>
        /// Four floats <c>(left, top, -right, -bottom)</c> describing the offset between
        /// <see cref="Rect"/> and an inner rectangle. If <c>/RD</c> is missing, returns
        /// <c>(0, 0, 0, 0)</c>.
        /// Inner rectangle: <c>rect + rect_delta</c> in MuPDF terms (component-wise on
        /// <see cref="Rect"/> corners).
        /// </remarks>
        public (float, float, float, float) RectDelta
        {
            get
            {
                var arr = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("RD"));
                if (mupdf.mupdf.pdf_array_len(arr) != 4)
                    return (0, 0, 0, 0);
                return (
                    mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(arr, 0)),
                    mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(arr, 1)),
                    -mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(arr, 2)),
                    -mupdf.mupdf.pdf_to_real(mupdf.mupdf.pdf_array_get(arr, 3)));
            }
        }

        /// <summary>
        /// Whether this wrapper is responsible for disposing the native annotation handle.
        /// </summary>
        /// <remarks>Legacy MuPDF.NET ownership flag used with SWIG-style lifetime rules.</remarks>
        public bool IsOwner { get; private set; } = true;

        /// <summary>
        /// Whether the native object is owned by this wrapper (SWIG <c>thisown</c>).
        /// </summary>
        public bool ThisOwn { get; set; } = true;

        /// <summary>
        /// Direct access to the underlying MuPDF <see cref="mupdf.PdfAnnot"/> handle.
        /// </summary>
        /// <remarks>Legacy field name <c>_annot</c>. Prefer <see cref="NativeAnnot"/> in new code.</remarks>
        public mupdf.PdfAnnot _annot => NativeAnnot;

        /// <summary>
        /// Release this annotation wrapper and detach it from the parent page.
        /// </summary>
        /// <remarks>
        /// Legacy equivalent of disposing the wrapper: clears <see cref="IsOwner"/> and
        /// <see cref="ThisOwn"/>, then calls <see cref="Dispose"/>.
        /// </remarks>
        public void Erase()
        {
            IsOwner = false;
            ThisOwn = false;
            Dispose();
        }

        /// <summary>
        /// The page that contains this annotation.
        /// </summary>
        /// <returns>The owning <see cref="Page"/> (same as <see cref="Parent"/>).</returns>
        public Page GetParent() => Parent;

        /// <summary>
        /// Return the native MuPDF annotation handle.
        /// </summary>
        /// <returns>The <see cref="mupdf.PdfAnnot"/> instance for this wrapper.</returns>
        public mupdf.PdfAnnot ToPdfAnnot() => NativeAnnot;

        /// <summary>
        /// Set the annotation flags field.
        /// </summary>
        /// <param name="flags">
        /// Integer flag bitmask. Combine values with bitwise OR (see PDF annotation flags
        /// in the Adobe PDF reference).
        /// </param>
        /// <remarks>Alias for assigning <see cref="Flags"/>.</remarks>
        public void SetFlags(int flags) => Flags = flags;

        /// <summary>
        /// Xref of the annotation to which this one responds (“In Response To”).
        /// </summary>
        /// <remarks>
        /// Reads PDF <c>/IRT</c>. Returns <c>0</c> if not set. Legacy property name
        /// <c>IrtXref</c>.
        /// </remarks>
        public int IrtXref
        {
            get
            {
                var irt = mupdf.mupdf.pdf_dict_get(AnnotObj, mupdf.mupdf.pdf_new_name("IRT"));
                return irt.m_internal != null ? mupdf.mupdf.pdf_to_num(irt) : 0;
            }
        }

        /// <summary>
        /// Define this annotation as “In Response To” another annotation.
        /// </summary>
        /// <param name="xref">PDF xref of the target annotation on this page.</param>
        /// <remarks>
        /// Legacy documentation name <c>SetIrtXRef</c>. Must refer to an existing annotation.
        /// Does not require <see cref="Update"/>.
        /// </remarks>
        public void SetIrtXRef(int xref) => SetIrtXref(xref);

        /// <summary>
        /// Define this annotation as “In Response To” another annotation.
        /// </summary>
        /// <param name="xref">PDF xref of the target annotation on this page.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="xref"/> is out of range for the document.
        /// </exception>
        /// <remarks>Writes PDF <c>/IRT</c>. Does not require <see cref="Update"/>.</remarks>
        public void SetIrtXref(int xref)
        {
            var pdf = ParentPdfDocument;
            if (xref < 1 || xref >= mupdf.mupdf.pdf_xref_len(pdf))
                throw new ArgumentException(Constants.MSG_BAD_XREF);
            var irt = mupdf.mupdf.pdf_new_indirect(pdf, xref, 0);
            mupdf.mupdf.pdf_dict_put(AnnotObj, mupdf.mupdf.pdf_new_name("IRT"), irt);
        }

        /// <summary>
        /// Open or close the annotation’s Popup, or the Text (“sticky note”) annotation itself.
        /// </summary>
        /// <param name="isOpen"><c>1</c> for open, <c>0</c> for closed (legacy int API).</param>
        /// <remarks>Forwards to <see cref="SetOpen(bool)"/>.</remarks>
        public void SetOpen(int isOpen) => SetOpen(isOpen != 0);

        /// <summary>
        /// Set the annotation appearance bounding box.
        /// </summary>
        /// <param name="bbox">New appearance bbox in page coordinates.</param>
        /// <remarks>Legacy spelling <c>SetApnBbox</c>; modern name <see cref="SetApnBBox"/>.</remarks>
        public void SetApnBbox(Rect bbox) => SetApnBBox(bbox);

        /// <summary>
        /// Legacy positional overload: <c>GetPixmap(matrix, dpi, colorSpace, alpha)</c>.
        /// </summary>
        /// <param name="matrix">Transformation for image creation (may be <c>null</c> for identity).</param>
        /// <param name="dpi">Resolution in dots per inch; <c>0</c> to use <paramref name="matrix"/> only.</param>
        /// <param name="colorSpace">Target colorspace; <c>null</c> for RGB.</param>
        /// <param name="alpha"><c>1</c> for alpha channel, <c>0</c> for opaque.</param>
        /// <remarks>
        /// Parameterless and named-argument calls use
        /// <see cref="GetPixmap(Matrix, Colorspace, bool, int)"/>.
        /// </remarks>
        public Pixmap GetPixmap(Matrix matrix, int dpi, Colorspace colorSpace, int alpha)
            => GetPixmap(matrix, colorSpace, alpha != 0, dpi);

        /// <summary>
        /// Retrieve sound data for a Sound annotation.
        /// </summary>
        /// <returns>
        /// A <see cref="Sound"/> object with rate, channels, bits-per-sample, encoding,
        /// compression, and raw stream bytes.
        /// </returns>
        /// <remarks>
        /// Legacy MuPDF.NET returned a <see cref="Sound"/> DTO. MuPDF-style dictionary access
        /// is available via <see cref="GetSoundDictionary"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">Not a sound annotation or missing sound stream.</exception>
        public Sound GetSound()
        {
            var d = GetSoundDictionary() ?? new Dictionary<string, object>();
            float F(string k) => d.TryGetValue(k, out var v) ? Convert.ToSingle(v) : 0f;
            int I(string k) => d.TryGetValue(k, out var v) ? Convert.ToInt32(v) : 0;
            string S(string k) => d.TryGetValue(k, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
            return new Sound
            {
                Rate = F("rate"),
                Channels = I("channels"),
                Bps = I("bps"),
                Encoding = S("encoding"),
                Compression = S("compression"),
                Stream = d.TryGetValue("stream", out var st) ? st as byte[] : null,
            };
        }

        /// <summary>
        /// Read border properties from a PDF annotation object as a <see cref="Border"/> DTO.
        /// </summary>
        /// <param name="annotObj">PDF annotation dictionary object.</param>
        /// <param name="legacyMarker">Unused; retained for binary compatibility with MuPDF.NET.</param>
        /// <returns>Width, style, dashes, and cloud intensity.</returns>
        public static Border GetBorderFromAnnot(mupdf.PdfObj annotObj, int legacyMarker)
        {
            _ = legacyMarker;
            var d = GetBorderFromAnnot(annotObj) ?? new Dictionary<string, object>();
            return new Border
            {
                Width = d.TryGetValue("width", out var w) ? Convert.ToSingle(w) : 0f,
                Style = d.TryGetValue("style", out var s) ? s?.ToString() ?? "S" : "S",
                Dashes = d.TryGetValue("dashes", out var da) ? da as int[] : null,
                Clouds = d.TryGetValue("clouds", out var c) ? Convert.ToSingle(c) : -1f,
            };
        }

        /// <summary>
        /// Read stroke and fill colors from a PDF annotation object as a <see cref="Color"/> DTO.
        /// </summary>
        /// <param name="annotObj">PDF annotation dictionary object.</param>
        /// <param name="legacyMarker">Unused; retained for binary compatibility with MuPDF.NET.</param>
        /// <returns>Stroke and fill color component arrays (0–1 per channel).</returns>
        public static Color GetColorFromAnnot(mupdf.PdfObj annotObj, int legacyMarker)
        {
            _ = legacyMarker;
            var d = GetColorFromAnnot(annotObj) ?? new Dictionary<string, object>();
            return new Color
            {
                Fill = d.TryGetValue("fill", out var f) ? f as float[] : null,
                Stroke = d.TryGetValue("stroke", out var s) ? s as float[] : null,
            };
        }

        /// <summary>
        /// Change stroke and fill colors from a <see cref="Color"/> DTO (legacy MuPDF.NET).
        /// </summary>
        /// <param name="colors">
        /// Stroke and/or fill components. Must not be <c>null</c>.
        /// </param>
        /// <remarks>
        /// For named <c>stroke</c> / <c>fill</c> arguments use
        /// <see cref="SetColors(float[], float[])"/> on the main partial (avoids overload ambiguity).
        /// </remarks>
        public void SetColors(Color colors)
        {
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));
            SetColors(colors.Stroke, colors.Fill);
        }

        /// <summary>
        /// Change stroke and fill colors with an optional <see cref="Color"/> DTO plus overrides.
        /// </summary>
        /// <param name="colors">When non-null, supplies defaults before <paramref name="stroke"/> / <paramref name="fill"/>.</param>
        /// <param name="stroke">Stroke color (1, 3, or 4 floats, 0–1).</param>
        /// <param name="fill">Fill color (same format).</param>
        /// <remarks>Three-argument legacy form; use <see cref="SetColors(float[], float[])"/> when <paramref name="colors"/> is not needed.</remarks>
        public void SetColors(Color colors, float[] stroke, float[] fill)
        {
            if (colors != null)
            {
                stroke = colors.Stroke;
                fill = colors.Fill;
            }
            SetColors(stroke, fill);
        }

        /// <summary>
        /// Update the default appearance (<c>/DA</c>) string on an annotation object.
        /// </summary>
        /// <param name="annot_">Native annotation handle.</param>
        /// <param name="dataStr">New default appearance string.</param>
        /// <remarks>
        /// Legacy helper used by widget/field code. Removes <c>/DS</c> and <c>/RC</c> when
        /// setting <c>/DA</c>. Failures are ignored (legacy behavior).
        /// </remarks>
        public static void UpdateData(mupdf.PdfAnnot annot_, string dataStr)
        {
            try
            {
                var annotObj = annot_?.pdf_annot_obj();
                if (annotObj?.m_internal == null)
                    return;
                annotObj.pdf_dict_put_text_string(new mupdf.PdfObj("DA"), dataStr);
                annotObj.pdf_dict_del(new mupdf.PdfObj("DS"));
                annotObj.pdf_dict_del(new mupdf.PdfObj("RC"));
            }
            catch
            {
                // Legacy: ignore failures.
            }
        }
    }
}