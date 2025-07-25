﻿using mupdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MuPDF.NET
{
    public class TextWriter
    {
        static TextWriter()
        {
            Utils.InitApp();
        }

        private FzText _nativeText;

        /// <summary>
        /// Text opacity
        /// </summary>
        public float Opacity { get; set; }

        /// <summary>
        /// Page rectangle used by this TextWriter
        /// </summary>
        public Rect Rect { get; set; }

        public Matrix Ctm { get; set; }

        public Matrix ICtm { get; set; }

        /// <summary>
        /// Last written to a PDF page
        /// </summary>
        public Point LastPoint { get; set; }

        /// <summary>
        /// Area occupied so far
        /// </summary>
        public Rect TextRect { get; set; }

        public List<Font> UsedFonts { get; set; }

        public bool ThisOwn { get; set; }

        /// <summary>
        /// text color
        /// </summary>
        public float[] Color { get; set; } = { 0, 0, 0 };

        public TextWriter(Rect pageRect, float opacity = 1, float[] color = null)
        {
            _nativeText = mupdf.mupdf.fz_new_text();

            Opacity = opacity;
            Rect = pageRect;
            Ctm = new Matrix(1, 0, 0, -1, 0, Rect.Height);
            ICtm = ~Ctm;
            LastPoint = new Point();
            TextRect = new Rect();
            UsedFonts = new List<Font>();
            if (color != null)
                Color = color;
            ThisOwn = false;
        }

        public Rect Bbox
        {
            get
            {
                return new Rect(mupdf.mupdf.fz_bound_text(_nativeText, new FzStrokeState(0), new FzMatrix())) + new Rect(10, 10, -10, -10);
            }
        }

        /// <summary>
        /// Add some new text in horizontal writing.
        /// </summary>
        /// <param name="pos">start position of the text, the bottom left point of the first character.</param>
        /// <param name="text">a string of arbitrary length. It will be written starting at position “pos”.</param>
        /// <param name="font">a Font. If omitted, fitz.Font("helv") will be used.</param>
        /// <param name="fontSize">the fontsize, a positive number, default 11.</param>
        /// <param name="language">the language to use, e.g. “en” for English. Meaningful values should be compliant with the ISO 639 standards 1, 2, 3 or 5. Reserved for future use: currently has no effect as far as we know.</param>
        /// <param name="right2left">whether the text should be written from right to left. Applicable for languages like Arabian or Hebrew. Default is False. If True, any Latin parts within the text will automatically converted.</param>
        /// <param name="smallCaps"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public (Rect, Point) Append(Point pos, string text, Font font, float fontSize = 11.0f, string language = null, int right2left = 0, int smallCaps = 0)
        {
            pos = pos * ICtm;
            int markupDir = 0;
            int wmode = 0;

            bool newFontFlag = false;

            if (font == null || font.IsNull)
            {
                font = new Font("helv");
                newFontFlag = true;
            }

            if (!font.IsWriteable)
            {
                throw new Exception($"Unsupported font {font.Name}");
            }

            if (right2left != 0)
            {
                text = CleanRtl(text);
                text = String.Join("", text.Reverse().ToArray());
                right2left = 0;
            }

            fz_text_language lang = mupdf.mupdf.fz_text_language_from_string(language);
            FzMatrix trm = mupdf.mupdf.fz_make_matrix(fontSize, 0, 0, fontSize, pos.X, pos.Y);
            
            if (smallCaps == 0)
                trm = _nativeText.fz_show_string(font.ToFzFont(), trm, text, wmode, right2left, (fz_bidi_direction)markupDir, lang);
            else
                trm = Utils.ShowStringCS(_nativeText, font, trm, text, wmode, right2left, (fz_bidi_direction)markupDir, lang);

            LastPoint = new Point(trm.e, trm.f) * Ctm;
            TextRect = Bbox * Ctm;
            (Rect, Point) ret = (TextRect, LastPoint);
            if (font.Flags["mono"] == 1)
                UsedFonts.Add(font);

            if (newFontFlag)
            {
                font.Dispose();
            }
            
            return ret;
        }

        /// <summary>
        /// Revert the sequence of Latin text parts
        /// </summary>
        /// <param name="text">target string to be reverted</param>
        /// <returns>reverted string</returns>
        public string CleanRtl(string text)
        {
            List<int> idx = new List<int>();
            List<int> idx2 = new List<int>();

            if (string.IsNullOrEmpty(text))
                return null;

            string[] words = text.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];

                if (!(w.Length < 2 || w.ToCharArray().Any(c => (int)c > 255)))
                {
                    words[i] = new string(w.ToCharArray().Reverse().ToArray());
                    idx.Add(i);
                }
            }

            
            for (int j = 0; j < idx.Count ; j ++)
            {
                if (idx2.Count == 0)
                    idx2.Add(idx[j]);
                else if (idx[j] > idx2[idx2.Count - 1] + 1)
                {
                    if (idx2.Count > 1)
                    {
                        Array.Reverse(words, idx2[0], idx2[idx2.Count - 1] - idx2[0] + 1);
                    }
                    idx2 = new List<int>() { idx[j] };
                }
                else if (idx[j] == idx2[idx2.Count - 1] + 1)
                    idx2.Add(idx[j]);
            }

            text = string.Join(" ", words);
            return text;
        }

        /// <summary>
        /// Add some new text in vertical, top-to-bottom writing.
        /// </summary>
        /// <param name="pos">start position of the text, the bottom left point of the first character.</param>
        /// <param name="text">a string. It will be written starting at position “pos”.</param>
        /// <param name="font"> a Font. If omitted, fitz.Font("helv") will be used.</param>
        /// <param name="fontSiz">the fontsize, a positive float, default 11.</param>
        /// <param name="language">the language to use</param>
        /// <param name="smallCaps"></param>
        /// <returns></returns>
        public (Rect, Point) Appendv(Point pos, string text, Font font = null, float fontSiz = 11.0f,
            string language = null, bool smallCaps = false)
        {
            float lheight = fontSiz * 1.2f;
            foreach (char c in text)
            {
                Append(pos, c.ToString(), font: font, fontSize: fontSiz, language: language, smallCaps: smallCaps ? 1 : 0);
                pos.Y += lheight;
            }

            return (TextRect, LastPoint);
        }

        /// <summary>
        /// Write the TextWriter text to a page, which is the only mandatory parameter. The other parameters can be used to temporarily override the values used when the TextWriter was created.
        /// </summary>
        /// <param name="page">write to this Page.</param>
        /// <param name="color"> override the value of the TextWriter for this output.</param>
        /// <param name="opacity">override the value of the TextWriter for this output.</param>
        /// <param name="overlay">put in foreground (default) or background.</param>
        /// <param name="morph">modify the text appearance by applying a matrix to it.</param>
        /// <param name="matrix"></param>
        /// <param name="renderMode">The PDF Tr operator value. Values: 0 (default), 1, 2, 3 (invisible).</param>
        /// <param name="oc"> the xref of an OCG or OCMD.</param>
        /// <exception cref="Exception"></exception>
        public void WriteText(Page page, float[] color = null, float opacity = -1, int overlay = 1, Morph morph = null,
            Matrix matrix = null, int renderMode = 0, int oc = 0)
        {
            if ((Rect - page.Rect).Abs() > 1e-3)
                throw new Exception("incompatible page rect");
            if (matrix != null && morph != null)
                throw new Exception("only one of matrix, m");
            if (opacity == -1)
                opacity = Opacity;
            if (color == null)
                color = Color;
            
            PdfPage pdfPage = page.GetPdfPage();
            float alpha = 1;
            FzColorspace colorSpace;

            if (opacity >= 0 && opacity < 1)
                alpha = opacity;
            int nCol = 1;
            float[] devColor = { 0, 0 , 0, 0 };
            if (color != null)
            {
                devColor = Annot.ColorFromSequence(color);

                if (devColor == null)
                    nCol = -1;
                else
                    nCol = devColor.Length;
            }

            if (nCol == 3)
                colorSpace = mupdf.mupdf.fz_device_rgb();
            else if (nCol == 4)
                colorSpace = mupdf.mupdf.fz_device_cmyk();
            else
                colorSpace = mupdf.mupdf.fz_device_gray();

            PdfObj resources = pdfPage.doc().pdf_new_dict(5);
            FzBuffer contents = mupdf.mupdf.fz_new_buffer(1024);
            FzDevice dev = mupdf.mupdf.pdf_new_pdf_device(pdfPage.doc(), new FzMatrix(), resources, contents);

            IntPtr pDevColor = Marshal.AllocHGlobal(devColor.Length * sizeof(float));
            Marshal.Copy(devColor, 0, pDevColor, devColor.Length);
            SWIGTYPE_p_float swigDevColor = new SWIGTYPE_p_float(pDevColor, true);

            dev.fz_fill_text(_nativeText, new FzMatrix(), colorSpace, swigDevColor, alpha, new FzColorParams(mupdf.mupdf.fz_default_color_params));
            dev.fz_close_device();
            dev.Dispose();

            (int, int) maxNums = Utils.MergeResources(pdfPage, resources);
            string cont = Utils.EscapeStrFromBuffer(contents);
            (int maxAlp, int maxFont) = maxNums;
            string[] oldLines = cont.Split('\n');

            string optCont = page.GetOptionalContent(oc);
            string bdc = "";
            string emc = "";
            if (!string.IsNullOrEmpty(optCont))
            {
                bdc = $"/OC /{optCont} BDC";
                emc = "EMC";
            }

            List<string> newContLines = new List<string>() { "q" };
            if (!string.IsNullOrEmpty(bdc))
                newContLines.Add(bdc);

            Point cb = page.CropBoxPosition;
            float delta = 0;
            if (page.Rotation == 90 || page.Rotation == 270)
                delta = page.Rect.Height - page.Rect.Width;

            Rect mb = page.MediaBox;
            if (!cb.IsZero()  || mb.Y0 != 0 || delta != 0)
                newContLines.Add($"1 0 0 1 {cb.X} {cb.Y + mb.Y0 - delta} cm");

            Matrix matrix_ = new Matrix();
            if (morph != null)
            {
                Point p = morph.P * ICtm;
                Matrix matrixDelta = (new Matrix(1f, 1f)).Pretranslate(p.X, p.Y);
                matrix_ = ~matrixDelta * morph.M * matrixDelta;
            }

            if (morph != null || matrix != null)
                newContLines.Add($"{matrix_.A} {matrix_.B} {matrix_.C} {matrix_.D} {matrix_.E} {matrix_.F} cm");

            foreach (string line in oldLines)
            {
                string line_ = line;
                if (line_.EndsWith(" cm") || string.IsNullOrEmpty(line_))
                    continue;
                if (line_ == "BT")
                {
                    newContLines.Add(line_);
                    newContLines.Add($"{renderMode} Tr");
                    continue;
                }
                if (line_.EndsWith(" gs"))
                {
                    int alp = Convert.ToInt32(line_.Split(' ')[0].Substring(4)) + maxAlp;
                    line_ = $"/Alp{alp} gs";
                }
                else if (line_.EndsWith(" Tf"))
                {
                    string[] temp = line_.Split(' ');
                    float fSize = (float)Convert.ToDouble(temp[1]);
                    float w = 1f;
                    if (renderMode != 0)
                        w = fSize * 0.05f;
                    newContLines.Add($"{w} w");
                    int font = Convert.ToInt32(temp[0].Substring(2)) + maxFont;
                    line_ = string.Join(" ", (new List<string>() { $"/F{font}" }).Concat(temp.Skip(1)));
                }
                else if (line_.EndsWith(" rg"))
                    newContLines.Add(line_.Replace("rg", "RG"));
                else if (line_.EndsWith(" g"))
                    newContLines.Add(line_.Replace(" g", " G"));
                else if (line_.EndsWith(" k"))
                    newContLines.Add(line_.Replace(" k", " K"));
                newContLines.Add(line_);
            }
            if (!string.IsNullOrEmpty(emc))
                newContLines.Add(emc);
            newContLines.Add("Q\n");

            byte[] content = Encoding.UTF8.GetBytes(string.Join("\n", newContLines));
            Utils.InsertContents(page, content, overlay);
            foreach (Font font in UsedFonts)
                Utils.RepairMonoFont(page, font);
        }

        /// <summary>
        /// Fill a given rectangle with text in horizontal writing mode.
        /// </summary>
        /// <param name="rect">the area to fill. No part of the text will appear outside of this.</param>
        /// <param name="text">the text.</param>
        /// <param name="pos">start storing at this point. Default is a point near rectangle top-left.</param>
        /// <param name="font">the Font.</param>
        /// <param name="fontSize">the fontsize.</param>
        /// <param name="lineHeight"></param>
        /// <param name="align">text alignment. Use one of TEXT_ALIGN_LEFT, TEXT_ALIGN_CENTER, TEXT_ALIGN_RIGHT or TEXT_ALIGN_JUSTIFY.</param>
        /// <param name="warn">on text overflow do nothing, warn, or raise an exception. Overflow text will never be written.</param>
        /// <param name="rtl"></param>
        /// <param name="smallCaps"></param>
        /// <returns>List of lines that did not fit in the rectangle.</returns>
        /// <exception cref="Exception"></exception>
        public List<(string, float)> FillTextbox(
            Rect rect,
            string text,
            Font font,
            Point pos = null,
            float fontSize = 11,
            float lineHeight = 0,
            int align = 0,
            bool warn = true,
            bool rtl = false,
            bool smallCaps = false)
        {
            if (rect.IsEmpty)
                throw new Exception("fill rect must not empty");
            if (font.IsNull)
                font = new Font("helv");

            // Return length of a string.
            float TextLen(string x)
            {
                return font.TextLength(x, fontSize: fontSize, smallCaps: smallCaps ? 1 : 0);
            }

            // Return list of single character lengths for a string.
            List<float> CharLengths(string x)
            {
                return font.GetCharLengths(x, fontSize: fontSize, smallCaps: smallCaps ? 1 : 0);
            }

            (Rect, Point) AppendThis(Point _pos, string _text)
            {
                return Append(_pos, _text, font: font, fontSize: fontSize, smallCaps: smallCaps ? 1 : 0);
            }

            float tolerance = fontSize * 0.2f; // extra distance to left border
            float spaceLen = TextLen(" ");
            float stdWidth = rect.Width - tolerance;
            float stdStart = rect.X0 + tolerance;

            // Cut any word in pieces no longer than 'width'.
            (List<string>, List<float>) NormWords(float _width, List<string> _words)
            {
                List<string> nwords = new List<string>();
                List<float> wordLengths = new List<float>();

                foreach (string word in _words)
                {
                    List<float> charLengths = CharLengths(word);
                    float wl = charLengths.Sum(x => x);
                    if (wl <= _width) // nothing to do - copy over
                    {
                        nwords.Add(word);
                        wordLengths.Add(wl);
                        continue;
                    }

                    // word longer than rect width - split it in parts
                    int n = charLengths.Count;
                    string word_ = word;
                    while (n > 0)
                    {
                        wl = charLengths.Take(n).Sum(x => x);
                        if (wl <= _width)
                        {
                            nwords.Add(word_.Substring(0, n));
                            wordLengths.Add(wl);
                            word_ = word_.Substring(n);
                            charLengths = charLengths.Skip(n).ToList();
                            n = charLengths.Count;
                        }
                        else
                            n -= 1;
                    }
                }

                return (nwords, wordLengths);
            }

            // Justified output of a line.
            void OutputJustify(Point _start, string line)
            {
                // ignore leading / trailing / multiple spaces
                string[] words = line.Split(' ').Where(x => x != "").ToArray();
                int nwords = words.Length;
                if (nwords == 0)
                    return;
                if (nwords == 1) // single word cannot be justified
                {
                    AppendThis(_start, words[0]);
                    return;
                }
                float tl = words.Sum(x => TextLen(x)); // total word lengths
                int gaps = nwords - 1; // number of word gaps
                float gapl = (stdWidth - tl) / gaps; // width of each gap
                foreach (string w in words)
                {
                    (Rect _, Point lp) = AppendThis(_start, w); // output one word
                    _start.X = lp.X + gapl; // next start at word end plus gap
                }
            }
            
            float asc = font.Ascender;
            float dsc = font.Descender;
            float lheight = 0;
            if (lineHeight == 0)
            {
                if (asc - dsc <= 1)
                    lheight = 1.2f;
                else
                    lheight = asc - dsc;
            }
            else
                lheight = lineHeight;

            float LineHeight = fontSize * lheight; // effective line height
            float width = stdWidth; // available horizontal space

            // starting point of text
            if (pos == null)
                pos = rect.TopLeft + new Point(tolerance, fontSize * asc);
            if (!(rect.IncludePoint(pos).EqualTo(rect)))
            {
                List<(string, float)> retLines = new List<(string, float)>();
                retLines.Add((text, TextLen(text)));
                return retLines;
            }

            float factor = 0;
            if (align == (int)TextAlign.TEXT_ALIGN_CENTER)
                factor = 0.5f;
            else if (align == (int)TextAlign.TEXT_ALIGN_RIGHT)
                factor = 1.0f;

            // split in lines if just a string was given
            string[] textLines = text.Split('\n');
            int maxLines = (int)((rect.Y1 - pos.Y) / LineHeight) + 1;

            List<(string, float)> newLines = new List<(string, float)>();
            List<int> noJustify = new List<int>();

            int i = -1;
            foreach (string line in textLines)
            {
                i += 1;
                if (line == "" || line == " ")
                {
                    newLines.Add((line, spaceLen));
                    width = rect.Width - tolerance;
                    noJustify.Add(newLines.Count - 1);
                    continue;
                }
                if (i == 0)
                    width = rect.X1 - pos.X;
                else
                    width = rect.Width - tolerance;

                string line_ = line;
                if (rtl)
                    line_ = CleanRtl(line_);

                float tl = TextLen(line_);
                if (tl <= width)
                {
                    newLines.Add((line, tl));
                    noJustify.Add((newLines.Count - 1));
                    continue;
                }

                string[] words = line.Split(' ');
                (List<string> words_, List<float> wordLengths_) = NormWords(stdWidth, new List<string>(words));

                int n = words_.Count;
                while (n > 0)
                {
                    string line0 = string.Join(" ", words_.Take(n).ToArray());
                    float wl = wordLengths_.Take(n).Sum() + spaceLen * (n - 1);
                    if (wl <= width)
                    {
                        newLines.Add((line0, wl));
                        //words_ = words_.Take(n).ToList();
                        words_.RemoveRange(0, n);
                        //wordLengths_ = wordLengths_.Take(n).ToList();
                        wordLengths_.RemoveRange(0, n);
                        n = words_.Count;
                        line0 = null;
                    }
                    else
                    {
                        n -= 1;
                    }

                    if (words_.Count == 0)
                    {
                        break;
                    }
                }
            }

            // List of lines created. Each item is (text, tl), where 'tl' is the PDF
            // output length (float) and 'text' is the text. Except for justified text,
            // this is output-ready.
            int nLines = newLines.Count;
            if (nLines > maxLines)
            {
                if (warn)
                    Console.WriteLine($"Only fitting {maxLines} of {nLines} lines");
                else
                    throw new Exception($"Only fitting {maxLines} of {nLines} lines");
            }

            Point start = new Point();
            noJustify.Add(newLines.Count - 1); // no justifying of last line
            for (i = 0; i < maxLines; i++)
            {
                string line;
                float tl;
                try
                {
                    (line, tl) = newLines[0];
                    newLines.RemoveAt(0);
                }
                catch(Exception)
                {
                    break;
                }

                if (rtl) // Arabic, Hebrew
                    line = string.Join("", line.Reverse().ToArray());

                if (i == 0) // may have different start for first line
                    start = pos;

                if (align == (int)TextAlign.TEXT_ALIGN_JUSTIFY && !noJustify.Contains(i) && tl < stdWidth)
                {
                    OutputJustify(start, line);
                    start.X = stdStart;
                    start.Y += LineHeight;
                    continue;
                }

                if (i > 0 || pos.X == stdStart) // left, center, right alignments
                    start.X += (width - tl) * factor;

                AppendThis(start, line);
                start.X = stdStart;
                start.Y += LineHeight;
            }

            return newLines; // return non-written lines
        }
    }
}