using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    internal class GS1Utils
    {
        static private string m_numbers = "0123456789";
        static private string m_capitalLetter = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        static private string m_gtinAlphabet = m_numbers;

        /// <summary>
        /// Function returns the number of combinations r selected from n
        /// Combinations = n! / ((n - r)! * r!)
        /// (based on the C code from the ISO/IEC 24724-2011 standard)
        /// </summary>
        static internal int combins(int n, int r)
        {
            int maxDenom, minDenom;

            if (n - r > r)
            {
                minDenom = r;
                maxDenom = n - r;
            }
            else
            {
                minDenom = n - r;
                maxDenom = r;
            }
            int val = 1;
            int j = 1;
            for (int i = n; i > maxDenom; i--)
            {
                val *= i;
                if (j <= minDenom)
                {
                    val /= j;
                    j++;
                }
            }
            for (; j <= minDenom; j++)
            {
                val /= j;
            }
            return (val);
        }

        /// <summary>
        /// Function for generating sizes (widths) of GS1 DataBar elements
        /// (based on C code from the ISO/IEC 24724-2011 standard)
        /// </summary>
        /// <param name="val">The input value.</param>
        /// <param name="n">The number of modules.</param>
        /// <param name="elements">The number of pairs of elements in the set (for GS1DataBarOmnidirectional = 4).</param>
        /// <param name="maxWidth">The maximum width of an element in modules.</param>
        /// <param name="noNarrow">if set to <c>true</c>, skip templates that do not have elements with a width of one module.</param>
        /// <returns>The sizes (widths) of the elements.</returns>

        static internal int[] getRSSwidths(int val, int n, int elements, int maxWidth, bool noNarrow)
        {
            int elmWidth;
            int subVal;
            int narrowMask = 0;
            int[] widths = new int[elements];
            for (int bar = 0; bar < elements - 1; bar++)
            {
                narrowMask |= (1 << bar);
                for (elmWidth = 1; ; elmWidth++)
                {
                    // get all combinations
                    subVal = combins(n - elmWidth - 1, elements - bar - 2);
                    // excluding combinations where there are no elements of width one module
                    if ((!noNarrow) && (narrowMask == 0) &&
                           (n - elmWidth - (elements - bar - 1) >= elements - bar - 1))
                    {
                        subVal -= combins(n - elmWidth - (elements - bar), elements - bar - 2);
                    }
                    // excluding combinations where the element width is greater than maxVal
                    if (elements - bar - 1 > 1)
                    {
                        int lessVal = 0;
                        for (int mxwElement = n - elmWidth - (elements - bar - 2);
                                         mxwElement > maxWidth;
                                         mxwElement--)
                        {
                            lessVal += combins(n - elmWidth - mxwElement - 1, elements - bar - 3);
                        }
                        subVal -= lessVal * (elements - 1 - bar);
                    }
                    else if (n - elmWidth > maxWidth)
                    {
                        subVal--;
                    }
                    val -= subVal;
                    if (val < 0) break;
                    narrowMask &= ~(1 << bar);
                }
                val += subVal;
                n -= elmWidth;
                widths[bar] = elmWidth;
            }
            widths[elements - 1] = n;
            return widths;
        }

        static private int getCharPosition(char c)
        {
            int charPos = m_gtinAlphabet.IndexOf(c);
            if (charPos == -1)
                throw new BarcodeException("Incorrect char in GTIN");

            return charPos;
        }

        /// <summary>
        /// Gets the GTIN checksum.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The checksum char.</returns>
        static internal char getGTINChecksum(string value)
        {
            int sum = 0;
            int valueLength = value.Length - 1;
            for (int i = 0; i < value.Length; i++)
            {
                if (i % 2 == 0)
                    sum += getCharPosition(value[valueLength - i]) * 3;
                else
                    sum += getCharPosition(value[valueLength - i]);
            }

            int lastDigit = (int)(Math.Ceiling(sum / 10.0) * 10) - sum;

            return m_gtinAlphabet[lastDigit];
        }

        static internal bool IsGTIN(string value)
        {
            int index = value.IndexOf("(01)");
            string gtin;
            if (index == 0)
                gtin = value.Substring(4);
            else
                gtin = value;

            if (gtin.Length == 0 || gtin.Length > 14) // GTIN-14(GTIN-13,GTIN-12,GTIN-8)
                return false;

            foreach (char c in gtin)
            {
                if (m_gtinAlphabet.IndexOf(c) == -1)
                    return false;
            }

            // user wants to enter value with check digit
            // we need to verify that digit
            char checksum = getGTINChecksum(gtin.Substring(0, gtin.Length - 1));
            if (gtin[gtin.Length - 1] != checksum)
                return false;

            return true;
        }

        static internal string GetEncodedGTINValue(string value, bool forCaption)
        {
            StringBuilder sb = new StringBuilder(value);
            while (sb.ToString().Length < 14)
                sb.Insert(0, new char[] { '0' });
            string s = sb.ToString();
            if (forCaption)
            {
                string AI = "(01)"; // for DataBar Omnidirectional Application Identifier = 01
                return AI + s;
            }
            else
            {
                string lf = "0"; //linkage flag == 0 if GS1 DataBar symbol stands alone (=> for DataBar Omnidirectional linkage flag == 0)
                //             == 1 if 2D Composite Component and its separator pattern are printed above the GS1 DataBar symbol
                return lf + s.Substring(0, s.Length - 1); // remove the checksum
            }
        }

        static internal string Numbers
        {
            get { return m_numbers; }
        }

        static internal string CapitalLetter
        {
            get { return m_capitalLetter; }
        }

        static internal void addArray(intList list, int[] array, bool flip)
        {
            if (flip)
                for (int i = array.Length - 1; i >= 0; i--)
                {
                    list.Add(array[i]);
                }
            else
                for (int i = 0; i < array.Length; i++)
                {
                    list.Add(array[i]);
                }
        }

    }
}
