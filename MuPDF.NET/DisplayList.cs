using System;

namespace MuPDF.NET
{
    /// <summary>
    /// MuPDF device wrapper for drawing, list recording, or text extraction.
    /// </summary>
    /// <remarks>
    /// PyMuPDF <c>DeviceWrapper</c> (<c>src/__init__.py</c>). Used with
    /// <see cref="DisplayList.Run"/> and <see cref="DocumentWriter.BeginPage"/>.
    /// </remarks>
    public class DeviceWrapper : IDisposable
    {
        internal mupdf.FzDevice device;
        private bool _disposed;

        /// <summary>
        /// Creates a device for pixmap rendering, display-list recording, or text extraction.
        /// </summary>
        /// <param name="args">
        /// <see cref="mupdf.FzDevice"/>, <see cref="Pixmap"/> (+ optional <see cref="IRect"/> clip),
        /// <see cref="mupdf.FzDisplayList"/>, or <see cref="mupdf.FzStextPage"/> (+ optional flags).
        /// </param>
        public DeviceWrapper(params object[] args)
        {
            if (args.Length == 1 && args[0] is mupdf.FzDevice dev)
            {
                device = dev;
            }
            else if (args.Length >= 1 && args[0] is Pixmap pm)
            {
                IRect? clip = args.Length > 1 ? args[1] as IRect : null;
                mupdf.FzIrect bbox = clip != null
                    ? clip.ToFzIRect()
                    : new mupdf.FzIrect(mupdf.mupdf.fz_infinite_irect);
                device = bbox.fz_is_infinite_irect() != 0
                    ? mupdf.mupdf.fz_new_draw_device(new mupdf.FzMatrix(), pm.NativePixmap)
                    : mupdf.mupdf.fz_new_draw_device_with_bbox(new mupdf.FzMatrix(), pm.NativePixmap, bbox);
            }
            else if (args.Length == 1 && args[0] is mupdf.FzDisplayList dl)
            {
                device = mupdf.mupdf.fz_new_list_device(dl);
            }
            else if (args.Length >= 1 && args[0] is mupdf.FzStextPage tp)
            {
                int flags = args.Length > 1 ? Convert.ToInt32(args[1]) : 0;
                using var opts = new mupdf.FzStextOptions(flags);
                device = mupdf.mupdf.fz_new_stext_device(tp, opts);
            }
            else
            {
                throw new Exception($"Unrecognised args for DeviceWrapper: [{string.Join(", ", args)}]");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                device?.Dispose();
                device = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>Returns the native <c>fz_device</c> handle.</summary>
        public mupdf.FzDevice ToFzDevice() => device;
    }

    /// <summary>
    /// Recorded page drawing commands for reuse (rendering and text extraction).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A display list caches parsed page content so it can be replayed without re-reading the PDF.
    /// Populate it with <see cref="Page.GetDisplayList"/> or create an empty list with
    /// <see cref="DisplayList(Rect)"/> and record via <see cref="Run"/>.
    /// </para>
    /// <para>
    /// Replay with <see cref="Run"/>, <see cref="GetPixmap"/>, or <see cref="GetTextPage"/>.
    /// PyMuPDF-aligned API; legacy readthedocs names are on <c>DisplayList.Legacy.cs</c>.
    /// </para>
    /// <para>
    /// See <see href="https://mupdfnet.readthedocs.io/en/latest/classes/DisplayList.html"/>.
    /// </para>
    /// </remarks>
    public partial class DisplayList : IDisposable
    {
        private mupdf.FzDisplayList _nativeDl;
        private bool _disposed;

        /// <summary>Whether this wrapper owns the native display list handle.</summary>
        public bool ThisOwn { get; set; } = true;

        internal mupdf.FzDisplayList NativeDisplayList
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(DisplayList));
                return _nativeDl;
            }
        }

        /// <summary>
        /// Creates a new, empty display list for the given page mediabox.
        /// </summary>
        /// <param name="mediabox">Page rectangle (typically <see cref="Page.Rect"/>).</param>
        public DisplayList(Rect mediabox)
        {
            if (mediabox == null)
                throw new ArgumentNullException(nameof(mediabox));
            _nativeDl = new mupdf.FzDisplayList(mediabox.ToFzRect());
        }

        internal DisplayList(mupdf.FzDisplayList dl)
        {
            _nativeDl = dl ?? throw new ArgumentNullException(nameof(dl));
            ThisOwn = false;
        }

        /// <summary>Returns the native <c>fz_display_list</c> handle.</summary>
        public mupdf.FzDisplayList ToFzDisplayList() => NativeDisplayList;

        /// <summary>
        /// Mediabox of this display list (equals the page rectangle when built via
        /// <see cref="Page.GetDisplayList"/>).
        /// </summary>
        public Rect Rect => Helpers.RectFromFz(mupdf.mupdf.fz_bound_display_list(NativeDisplayList));

        /// <summary>
        /// Renders the display list through a draw device and returns a pixmap.
        /// </summary>
        /// <param name="matrix">Transform matrix; default identity.</param>
        /// <param name="colorspace">Target colorspace; default RGB.</param>
        /// <param name="alpha">Include an alpha channel when <see langword="true"/>.</param>
        /// <param name="clip">
        /// Restrict rendering to the intersection of this area with <see cref="Rect"/>.
        /// </param>
        /// <returns>A new <see cref="Pixmap"/> of the rendered list.</returns>
        public Pixmap GetPixmap(Matrix matrix = null, Colorspace colorspace = null, bool alpha = false, IRect clip = null)
        {
            mupdf.FzColorspace fzColorspace = colorspace != null
                ? colorspace.ToFzColorspace()
                : new mupdf.FzColorspace(mupdf.FzColorspace.Fixed.Fixed_RGB);
            return Helpers.JmPixmapFromDisplayList(
                NativeDisplayList, matrix, fzColorspace, alpha ? 1 : 0, clip, null);
        }

        /// <summary>
        /// Runs the display list through a text device and returns a <see cref="TextPage"/>.
        /// </summary>
        /// <param name="flags">
        /// Stext option bits; default <c>3</c> =
        /// <see cref="Constants.TEXT_PRESERVE_LIGATURES"/> |
        /// <see cref="Constants.TEXT_PRESERVE_WHITESPACE"/>.
        /// See <see href="https://mupdfnet.readthedocs.io/en/latest/glossary/vars.html"/>.
        /// </param>
        public TextPage GetTextPage(int flags = 3)
        {
            using var stextOptions = new mupdf.FzStextOptions();
            stextOptions.flags = flags;
            return new TextPage(new mupdf.FzStextPage(NativeDisplayList, stextOptions));
        }

        /// <summary>
        /// Replays the display list through a custom device.
        /// </summary>
        /// <param name="device">
        /// Target device (legacy docs: <c>Device</c>; implemented as <see cref="DeviceWrapper"/>).
        /// </param>
        /// <param name="matrix">Transform applied to list contents.</param>
        /// <param name="area">
        /// Only content visible in this rectangle is processed (<see cref="Rect"/> or <see cref="IRect"/>).
        /// </param>
        public void Run(DeviceWrapper device, Matrix matrix, object area)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            mupdf.mupdf.fz_run_display_list(
                NativeDisplayList,
                device.device,
                Helpers.MatrixToFz(matrix),
                Helpers.JM_rect_from_py(area),
                new mupdf.FzCookie());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                if (ThisOwn)
                    _nativeDl?.Dispose();
                _nativeDl = null;
                ThisOwn = false;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        internal Pixmap get_pixmap(Matrix matrix = null, Colorspace colorspace = null, int alpha = 0, IRect clip = null)
            => GetPixmap(matrix, colorspace, alpha != 0, clip);

        internal TextPage get_textpage(int flags = 3) => GetTextPage(flags);

        internal void run(DeviceWrapper dw, Matrix m, object area) => Run(dw, m, area);
    }
}
