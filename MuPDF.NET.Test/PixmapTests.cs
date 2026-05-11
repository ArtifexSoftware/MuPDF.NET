using System;
using System.IO;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for the Pixmap class.
    /// Ported from tests/test_pixmap.py.
    /// </summary>
    public class PixmapTests
    {
        // ─── Construction ───────────────────────────────────────────────

        [Fact]
        public void Pixmap_FromIRect()
        {
            var cs = Colorspace.CsRGB;
            var irect = new IRect(0, 0, 100, 100);
            using var pix = new Pixmap(cs, irect);
            Assert.Equal(100, pix.Width);
            Assert.Equal(100, pix.Height);
            Assert.Equal(3, pix.N);
            Assert.Equal(0, pix.Alpha);
        }

        [Fact]
        public void Pixmap_WithAlpha()
        {
            var cs = Colorspace.CsRGB;
            var irect = new IRect(0, 0, 50, 50);
            using var pix = new Pixmap(cs, irect, alpha: true);
            Assert.Equal(1, pix.Alpha);
            Assert.Equal(4, pix.N);
        }

        [Fact]
        public void Pixmap_GrayColorspace()
        {
            var cs = Colorspace.CsGRAY;
            var irect = new IRect(0, 0, 10, 10);
            using var pix = new Pixmap(cs, irect);
            Assert.Equal(1, pix.N);
        }

        // ─── Properties ─────────────────────────────────────────────────

        [Fact]
        public void Pixmap_IRect()
        {
            var irect = new IRect(10, 20, 110, 120);
            using var pix = new Pixmap(Colorspace.CsRGB, irect);
            var ir = pix.IRect;
            Assert.Equal(10, ir.X0);
            Assert.Equal(20, ir.Y0);
            Assert.Equal(110, ir.X1);
            Assert.Equal(120, ir.Y1);
        }

        [Fact]
        public void Pixmap_Stride()
        {
            var irect = new IRect(0, 0, 10, 10);
            using var pix = new Pixmap(Colorspace.CsRGB, irect);
            Assert.Equal(30, pix.Stride); // 10 pixels * 3 components
        }

        [Fact]
        public void Pixmap_Colorspace()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            Assert.NotNull(pix.Colorspace);
        }

        [Fact]
        public void Pixmap_Samples()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            var samples = pix.Samples;
            Assert.Equal(5 * 5 * 3, samples.Length);
        }

        [Fact]
        public void Pixmap_Size()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 10, 10));
            Assert.True(pix.Size > 0);
        }

        [Fact]
        public void Pixmap_Digest()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            var digest = pix.Digest;
            Assert.NotNull(digest);
            Assert.True(digest.Length > 0);
        }

        // ─── Operations ─────────────────────────────────────────────────

        [Fact]
        public void Pixmap_ClearWith()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            pix.ClearWith(255);
            var samples = pix.Samples;
            Assert.Equal(255, samples[0]);
        }

        [Fact]
        public void Pixmap_ClearWithRect()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 10, 10));
            pix.ClearWith(128, new IRect(0, 0, 5, 5));
        }

        [Fact]
        public void Pixmap_GammaWith()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            pix.ClearWith(128);
            pix.GammaWith(1.5f);
        }

        [Fact]
        public void Pixmap_InvertIRect()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            pix.ClearWith(100);
            pix.InvertIRect();
            var samples = pix.Samples;
            Assert.Equal(155, samples[0]);
        }

        [Fact]
        public void Pixmap_SetOrigin()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            pix.SetOrigin(10, 20);
            Assert.Equal(10, pix.X);
            Assert.Equal(20, pix.Y);
        }

        [Fact]
        public void Pixmap_SetSamples()
        {
            using var pix = new Pixmap(Colorspace.CsGRAY, new IRect(0, 0, 2, 2));
            var data = new byte[4];
            data[0] = 255;
            pix.SetSamples(data);
            Assert.Equal(255, pix.Samples[0]);
        }

        [Fact]
        public void Pixmap_SetSamples_WrongLength_Throws()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            Assert.Throws<ArgumentException>(() => pix.SetSamples(new byte[10]));
        }

        // ─── Save / ToBytes ─────────────────────────────────────────────

        [Fact]
        public void Pixmap_ToPng()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 10, 10));
            pix.ClearWith(200);
            var png = pix.ToPng();
            Assert.NotNull(png);
            Assert.True(png.Length > 0);
        }

        [Fact]
        public void Pixmap_ToBytes()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 10, 10));
            pix.ClearWith(100);
            var bytes = pix.ToBytes("png");
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void Pixmap_Save()
        {
            var path = Path.Combine(Path.GetTempPath(), $"mupdf_test_{Guid.NewGuid()}.png");
            try
            {
                using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 10, 10));
                pix.ClearWith(150);
                pix.Save(path, "png");
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ─── Shrink ─────────────────────────────────────────────────────

        [Fact]
        public void Pixmap_Shrink()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 100, 100));
            // fz_subsample_pixmap: factor n divides dimensions by 2^n (see Pixmap.Shrink XML doc).
            pix.Shrink(2);
            Assert.Equal(25, pix.Width);
            Assert.Equal(25, pix.Height);
        }

        // ─── Dispose / ToString ─────────────────────────────────────────

        [Fact]
        public void Pixmap_Dispose()
        {
            var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            pix.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = pix.Width);
        }

        [Fact]
        public void Pixmap_ToString()
        {
            using var pix = new Pixmap(Colorspace.CsRGB, new IRect(0, 0, 5, 5));
            Assert.Contains("Pixmap", pix.ToString());
        }
    }
}
