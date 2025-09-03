using System;
using System.Diagnostics;
using System.Text;

namespace BarcodeReader.Core.Common
{
    /// <summary>
    /// Guess encoding by bytes
    /// </summary>
    internal static class EncodingUtils
    {
#if (WINDOWS_PHONE || SILVERLIGHT4 || SILVERLIGHT5 || NETFX_CORE || PORTABLE || NETSTANDARD)
      private const String PLATFORM_DEFAULT_ENCODING = "UTF-8";
#else
        private static readonly String PLATFORM_DEFAULT_ENCODING = Encoding.Default.WebName;
#endif
        /// <summary>
        /// SJIS
        /// </summary>
        public static String SHIFT_JIS = "SJIS";
        /// <summary>
        /// GB2312
        /// </summary>
        public static String GB2312 = "GB2312";
        private const String EUC_JP = "EUC-JP";
        private const String UTF8 = "UTF-8";
        private const String ISO88591 = "ISO-8859-1";
        private static readonly bool ASSUME_SHIFT_JIS =
           String.Compare(SHIFT_JIS, PLATFORM_DEFAULT_ENCODING, StringComparison.OrdinalIgnoreCase) == 0 ||
           String.Compare(EUC_JP, PLATFORM_DEFAULT_ENCODING, StringComparison.OrdinalIgnoreCase) == 0;

        /// <summary>
        /// Guesses the encoding.
        /// </summary>
        /// <param name="bytes">bytes encoding a string, whose encoding should be guessed</param>
        /// <param name="hints">decode hints if applicable</param>
        /// <returns>name of guessed encoding; at the moment will only guess one of:
        /// {@link #SHIFT_JIS}, {@link #UTF8}, {@link #ISO88591}, or the platform
        /// default encoding if none of these can possibly be correct</returns>
        public static String GuessEncoding(byte[] bytes)
        {
            // For now, merely tries to distinguish ISO-8859-1, UTF-8 and Shift_JIS,
            // which should be by far the most common encodings.
            int length = bytes.Length;
            bool canBeISO88591 = true;
            bool canBeShiftJIS = true;
            bool canBeUTF8 = true;
            int utf8BytesLeft = 0;
            int utf2BytesChars = 0;
            int utf3BytesChars = 0;
            int utf4BytesChars = 0;
            int sjisBytesLeft = 0;
            int sjisKatakanaChars = 0;
            int sjisCurKatakanaWordLength = 0;
            int sjisCurDoubleBytesWordLength = 0;
            int sjisMaxKatakanaWordLength = 0;
            int sjisMaxDoubleBytesWordLength = 0;
            int isoHighOther = 0;
            int isoHighOther2 = 0;

            bool utf8bom = bytes.Length > 3 &&
                bytes[0] == 0xEF &&
                bytes[1] == 0xBB &&
                bytes[2] == 0xBF;

            for (int i = 0;
                 i < length && (canBeISO88591 || canBeShiftJIS || canBeUTF8);
                 i++)
            {

                int value = bytes[i] & 0xFF;

                // UTF-8 stuff
                if (canBeUTF8)
                {
                    if (utf8BytesLeft > 0)
                    {
                        if ((value & 0x80) == 0)
                        {
                            canBeUTF8 = false;
                        }
                        else
                        {
                            utf8BytesLeft--;
                        }
                    }
                    else if ((value & 0x80) != 0)
                    {
                        if ((value & 0x40) == 0)
                        {
                            canBeUTF8 = false;
                        }
                        else
                        {
                            utf8BytesLeft++;
                            if ((value & 0x20) == 0)
                            {
                                utf2BytesChars++;
                            }
                            else
                            {
                                utf8BytesLeft++;
                                if ((value & 0x10) == 0)
                                {
                                    utf3BytesChars++;
                                }
                                else
                                {
                                    utf8BytesLeft++;
                                    if ((value & 0x08) == 0)
                                    {
                                        utf4BytesChars++;
                                    }
                                    else
                                    {
                                        canBeUTF8 = false;
                                    }
                                }
                            }
                        }
                    }
                }

                // ISO-8859-1 stuff
                if (canBeISO88591)
                {
                    if (value > 0x7F && value < 0xA0)
                    {
                        canBeISO88591 = false;
                    }
                    else if (value > 0x9F)
                    {
                        if (value < 0xC0 || value == 0xD7 || value == 0xF7)
                        {
                            isoHighOther++;
                        }
                        isoHighOther2++;
                    }
                }

                // Shift_JIS stuff
                if (canBeShiftJIS)
                {
                    if (sjisBytesLeft > 0)
                    {
                        if (value < 0x40 || value == 0x7F || value > 0xFC)
                        {
                            canBeShiftJIS = false;
                        }
                        else
                        {
                            sjisBytesLeft--;
                        }
                    }
                    else if (value == 0x80 || value == 0xA0 || value > 0xEF)
                    {
                        canBeShiftJIS = false;
                    }
                    else if (value > 0xA0 && value < 0xE0)
                    {
                        sjisKatakanaChars++;
                        sjisCurDoubleBytesWordLength = 0;
                        sjisCurKatakanaWordLength++;
                        if (sjisCurKatakanaWordLength > sjisMaxKatakanaWordLength)
                        {
                            sjisMaxKatakanaWordLength = sjisCurKatakanaWordLength;
                        }
                    }
                    else if (value > 0x7F)
                    {
                        sjisBytesLeft++;
                        //sjisDoubleBytesChars++;
                        sjisCurKatakanaWordLength = 0;
                        sjisCurDoubleBytesWordLength++;
                        if (sjisCurDoubleBytesWordLength > sjisMaxDoubleBytesWordLength)
                        {
                            sjisMaxDoubleBytesWordLength = sjisCurDoubleBytesWordLength;
                        }
                    }
                    else
                    {
                        //sjisLowChars++;
                        sjisCurKatakanaWordLength = 0;
                        sjisCurDoubleBytesWordLength = 0;
                    }
                }
            }

            if (canBeUTF8 && utf8BytesLeft > 0)
            {
                canBeUTF8 = false;
            }
            if (canBeShiftJIS && sjisBytesLeft > 0)
            {
                canBeShiftJIS = false;
            }

            // Easy -- if there is BOM or at least 1 valid not-single byte character (and no evidence it can't be UTF-8), done
            if (canBeUTF8 && (utf8bom || utf2BytesChars + utf3BytesChars + utf4BytesChars > 0))
            {
                return UTF8;
            }
            // Easy -- if assuming Shift_JIS or at least 3 valid consecutive not-ascii characters (and no evidence it can't be), done
            if (canBeShiftJIS && (ASSUME_SHIFT_JIS || sjisMaxKatakanaWordLength >= 3 || sjisMaxDoubleBytesWordLength >= 3))
            {
                return SHIFT_JIS;
            }
            // Distinguishing Shift_JIS and ISO-8859-1 can be a little tough for short words. The crude heuristic is:
            // - If we saw
            //   - only two consecutive katakana chars in the whole text, or
            //   - at least 10% of bytes that could be "upper" not-alphanumeric Latin1,
            // - then we conclude Shift_JIS, else ISO-8859-1
            if (canBeISO88591 && canBeShiftJIS)
            {
                return (sjisMaxKatakanaWordLength == 2 && sjisKatakanaChars == 2) || isoHighOther * 10 >= length
                    ? SHIFT_JIS : ISO88591;
            }

            // Otherwise, try in order ISO-8859-1, Shift JIS, UTF-8 and fall back to default platform encoding
            if (canBeISO88591)
            {
                if (isoHighOther2 * 2 >= length)
                    return PLATFORM_DEFAULT_ENCODING;//too many letters with grave/tilde/acute
                else
                    return ISO88591;
            }
            if (canBeShiftJIS)
            {
                return SHIFT_JIS;
            }
            if (canBeUTF8)
            {
                return UTF8;
            }
            // Otherwise, we take a wild guess with platform encoding
            return PLATFORM_DEFAULT_ENCODING;
        }

        public static Encoding GetEncodingFromECI(string eci)
        {
            if (!string.IsNullOrEmpty(eci) && eci.StartsWith("]Q2\\0000", StringComparison.Ordinal))
            {
                if (int.TryParse(eci.Substring(eci.Length - 6, 6), out int code))
                {
                    try
                    {
                        switch (code)
                        {
                            case 3: return Encoding.GetEncoding("iso-8859-1");
                            case 4: return Encoding.GetEncoding("iso-8859-2");
                            case 5: return Encoding.GetEncoding("iso-8859-3");
                            case 6: return Encoding.GetEncoding("iso-8859-4");
                            case 7: return Encoding.GetEncoding("iso-8859-5");
                            case 8: return Encoding.GetEncoding("iso-8859-6");
                            case 9: return Encoding.GetEncoding("iso-8859-7");
                            case 10: return Encoding.GetEncoding("iso-8859-8");
                            case 11: return Encoding.GetEncoding("iso-8859-9");
                            case 12: return Encoding.GetEncoding("iso-8859-10");
                            case 13: return Encoding.GetEncoding("iso-8859-11");
                            case 15: return Encoding.GetEncoding("iso-8859-13");
                            case 16: return Encoding.GetEncoding("iso-8859-14");
                            case 17: return Encoding.GetEncoding("iso-8859-15");
                            case 18: return Encoding.GetEncoding("iso-8859-16");
                            case 20: return Encoding.GetEncoding("shift_jis");
                            case 21: return Encoding.GetEncoding("windows-1250");
                            case 22: return Encoding.GetEncoding("windows-1251");
                            case 23: return Encoding.GetEncoding("windows-1252");
                            case 24: return Encoding.GetEncoding("windows-1256");
                            case 25: return Encoding.GetEncoding("utf-16BE");
                            case 26: return Encoding.UTF8;
                            case 27: return Encoding.ASCII;
                            case 28: return Encoding.GetEncoding("big5");
                            case 29: return Encoding.GetEncoding("GB18030");
                            case 30: return Encoding.GetEncoding("euc-kr");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("GetEncodingFromECI(): Unregistered encoding?");
                    }
                }
            }

            return null;
        }
    }
}
