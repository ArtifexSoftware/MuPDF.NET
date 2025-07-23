using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.CodaBlockF
{
    //Class to sample a region (4 corners) and decode a codablock-F barcode.
    //The region is sampled in row direction, at a very small step to ensure several scans for barcode row.
    //The number of rows is encoded in the first char of the first row. The row id is encoded in the first 
    //char of the next rows.
    //Uses a BarSymbolReader, a generic barcode reader to convert bars to codebytes. Then these codebytes 
    //are decoded using 128 encoding schema.
    class CodaBlockFDecoder
    {
        public BarCodeRegion Decode(ImageScaner scan, MyPointF up, MyPointF endUp, MyPointF endDown, MyPointF down, FoundPattern foundPattern)
        {
            MyVectorF vdY = (up - down);
            float startLength = vdY.Length;
            vdY = vdY.Normalized;
            MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

            MyVectorF endVdY = (endUp - endDown).Normalized;
            float d = foundPattern.moduleLength;
            int[][] symbols = null;

            //scan region rows, at small step of barcode module length. Result is stored in symbols array.
            //symbols array is initialized later, when the first row is read and the number of rows is known.
            //Seems that results are better using bar widths instead of E (sum of 2 consecutive bar widths).
            BarSymbolReader reader = new BarSymbolReader(scan, 6, 11, true, true, foundPattern.moduleLength, _patterns, ShortStop, false, null);
            while (d < startLength)
            {
                MyPointF a = up - vdY * d;
                MyPointF b = endUp - endVdY * d;
                float error, maxError,confidence;
                int[] row = reader.Read(a, b, out error, out maxError, out confidence); //convert scanline to bytecodes array.
                int nRow=-1, nRows=-1;
                if (verifyCheckSum(row)) //if checksum is right, use the data row. Otherwise skip to the next row.
                {
                    int s = row[1];
                    int l = row[2];
                    //extract encodation A, B or C, and number of rows or rowId.
                    switch (s)
                    {
                        case Shift:
                        case CodeB:
                            if (0 <= l && l <= 10) { l = l + 34; nRows = l; nRow = 0; }
                            else if (64 <= l && l <= 95) { l = l - 62; nRows = l; nRow = 0; }
                            else if (11 <= l && l <= 15) { l = l - 10; nRow = l; }
                            else if (26 <= l && l <= 63) { l = l - 20; nRow = l; }
                            break;
                        case CodeC:
                            if (0 <= l && l <= 42) { nRows = l + 2; nRow = 0; }
                            else if (43 <= l && l <= 85) { nRow = l - 42; }
                            break;
                    }
                }
                //store data in the symbols array
                if (nRow==0 && symbols==null && nRows>=2 && nRows<=44) symbols=new int[nRows][];
                if (symbols!=null && nRow >=0 && nRow<symbols.Length && symbols[nRow] == null) symbols[nRow] = row;
                d += foundPattern.moduleLength;
            }
            if (symbols == null) return null;

            //Decode all rows and check global CRC
            string message = "";
            bool CRC = false;
            int charPos=0, K1=0, K2=0;
            for (int i = 0; i < symbols.Length; i++)
                if (symbols[i] == null) return null;
                else message += decodeRow(symbols[i], i, symbols.Length, ref charPos, ref K1, ref K2, out CRC);

            //If the barcode is correctly decoded, return the decoded data. If row CRC's are right but
            //global CRC don't, set confidence to 0.
            BarCodeRegion reg=new BarCodeRegion(up, endUp, endDown, down);
            reg.Data = new ABarCodeData[] { new StringBarCodeData(message) };
            reg.Confidence = CRC?1f:0f;
            return reg;
        }

        //function to check code128 CRC
        private bool verifyCheckSum(int[] symbols)
        {
            if (symbols.Length < 3)
            {
                // at least StartX, Stop and checksum should be in every Code128 code
                return false;
            }

            int total = symbols[0];
            for (int i = 1; i < symbols.Length - 2; i++)
                total += symbols[i] * i;

            return (total % 103) == symbols[symbols.Length - 2];
        }

        //function to acumulate global CRC (2 bytecodes) values. It is called row by row.
        private void updateK1K2(int symbol, string s, int nRow, ref int charPos, ref int K1, ref int K2)
        {
            if (symbol == FNC1 && nRow==0 || symbol==FNC3 || symbol==FNC4A || symbol==FNC4B) {}
            else 
            {
                if (symbol==FNC1 || symbol == FNC2)
                    s = Convert.ToString((char)29);

                for (int i = 0; i < s.Length; i++)
                {
                    int ascii = (int)s[i];
                    K1 = (K1 + ascii * (charPos + 1)) % 86;
                    K2 = (K2 + ascii * charPos) % 86;
                    charPos++;
                }
            }
        }

        //Code128 decoding algorithm with codaBlock-F global CRC calculations.
        private string decodeRow(int[] rawData, int nRow, int nRows, ref int charPos, ref int K1, ref int K2, out bool CRC)
        {
            CRC = true;
            string msg = "";

            //Initial encoding schema A, B or C. rawData[0] is always the start-A pattern, not used.
            int currentCodeSet = -1;
            switch (rawData[1])
            {
                case Shift: currentCodeSet = CodeA; break;
                case CodeB: currentCodeSet = CodeB; break;
                case CodeC: currentCodeSet = CodeC; break;
                default: return null;
            }

            bool hasShift = false, fnc4 = false;
            int lastSymbol = rawData.Length- (nRow < nRows - 1 ? 2 : 4);
            for (int i = 3; i < lastSymbol; i++)
            {
                int symbol = rawData[i];

                if ((currentCodeSet == CodeA || currentCodeSet == CodeB) && symbol == Shift) hasShift = true;
                else if (symbol == CodeA || symbol == CodeB || symbol == CodeC) currentCodeSet = symbol;
                else if (currentCodeSet == CodeA && symbol == FNC4A || 
                    currentCodeSet == CodeB && symbol == FNC4B) fnc4 = true;
                else
                {
                    string c = ""; 
                    if (fnc4)
                    {
                        c = Convert.ToString((char)(symbol + 128));
                        fnc4 = false;
                    }
                    else
                    {
                        int codeSet = currentCodeSet;
                        if (hasShift)
                        {
                            codeSet = (codeSet == CodeA ? CodeB : CodeA);
                            hasShift = false;
                        }
                        switch (codeSet)
                        {
                            case CodeA:
                                if (symbol < 64) c = Convert.ToString((char)(symbol + ' '));
                                else if (symbol < 96) c = Convert.ToString((char)(symbol - 64));
                                else if (symbol == FNC1) c = "[FNC1]";
                                else if (symbol == FNC2) c = "[FNC2]";
                                else if (symbol == FNC3) c = "[FNC3]";
                                else if (symbol == FNC4A) c = "[FNC4]";
                                break;
                            case CodeB:
                                if (symbol < 96) c = Convert.ToString((char)(symbol + ' '));
                                else if (symbol == FNC1) c = "[FNC1]";
                                else if (symbol == FNC2) c = "[FNC2]";
                                else if (symbol == FNC3) c = "[FNC3]";
                                else if (symbol == FNC4B) c = "[FNC4]";
                                break;
                            case CodeC:
                                if (symbol < 100) c = Convert.ToString(symbol).PadLeft(2, '0');
                                else if (symbol == FNC1) c = "[FNC1]";
                                break;
                        }
                    }
                    updateK1K2(symbol, c, nRow, ref charPos, ref K1, ref K2);
                    if (c == "") c = "[?]";
                    msg += c;
                }
            }

            if (nRow == nRows - 1) //check CRC
            {
                CRC = checkCRC(rawData[lastSymbol], currentCodeSet, K1) && 
                    checkCRC(rawData[lastSymbol+1], currentCodeSet, K2);
            }

            return msg;
        }

        private bool checkCRC(int v, int codeSet, int K)
        {
            if (codeSet == CodeA || codeSet == CodeB)
            {
                if (v < 15) v += 32;
                else if (v < 64) v += 22;
                else v -= 64;
            }
            return v == K;
        }




        private const int Shift = 98;

        private const int CodeC = 99;
        private const int CodeB = 100;
        private const int CodeA = 101;

        private const int FNC1 = 102;
        private const int FNC2 = 97;
        private const int FNC3 = 96;
        private const int FNC4A = 101;
        private const int FNC4B = 100;

        private const int StartA = 103;
        private const int StartB = 104;
        private const int StartC = 105;
        private const int Stop = 106;
        private const int ShortStop = 107;

        private static readonly int[][] _patterns =
        {
            new int[] { 2, 1, 2, 2, 2, 2 }, //0
            new int[] { 2, 2, 2, 1, 2, 2 },
            new int[] { 2, 2, 2, 2, 2, 1 },
            new int[] { 1, 2, 1, 2, 2, 3 },
            new int[] { 1, 2, 1, 3, 2, 2 },
            new int[] { 1, 3, 1, 2, 2, 2 },
            new int[] { 1, 2, 2, 2, 1, 3 },
            new int[] { 1, 2, 2, 3, 1, 2 },
            new int[] { 1, 3, 2, 2, 1, 2 },
            new int[] { 2, 2, 1, 2, 1, 3 },
            new int[] { 2, 2, 1, 3, 1, 2 },//10
            new int[] { 2, 3, 1, 2, 1, 2 },
            new int[] { 1, 1, 2, 2, 3, 2 },
            new int[] { 1, 2, 2, 1, 3, 2 },
            new int[] { 1, 2, 2, 2, 3, 1 },
            new int[] { 1, 1, 3, 2, 2, 2 },
            new int[] { 1, 2, 3, 1, 2, 2 },
            new int[] { 1, 2, 3, 2, 2, 1 },
            new int[] { 2, 2, 3, 2, 1, 1 },
            new int[] { 2, 2, 1, 1, 3, 2 },
            new int[] { 2, 2, 1, 2, 3, 1 },//20
            new int[] { 2, 1, 3, 2, 1, 2 },
            new int[] { 2, 2, 3, 1, 1, 2 },
            new int[] { 3, 1, 2, 1, 3, 1 },
            new int[] { 3, 1, 1, 2, 2, 2 },
            new int[] { 3, 2, 1, 1, 2, 2 },
            new int[] { 3, 2, 1, 2, 2, 1 },
            new int[] { 3, 1, 2, 2, 1, 2 },
            new int[] { 3, 2, 2, 1, 1, 2 },
            new int[] { 3, 2, 2, 2, 1, 1 },
            new int[] { 2, 1, 2, 1, 2, 3 },//30
            new int[] { 2, 1, 2, 3, 2, 1 },
            new int[] { 2, 3, 2, 1, 2, 1 },
            new int[] { 1, 1, 1, 3, 2, 3 },
            new int[] { 1, 3, 1, 1, 2, 3 },
            new int[] { 1, 3, 1, 3, 2, 1 },
            new int[] { 1, 1, 2, 3, 1, 3 },
            new int[] { 1, 3, 2, 1, 1, 3 },
            new int[] { 1, 3, 2, 3, 1, 1 },
            new int[] { 2, 1, 1, 3, 1, 3 },
            new int[] { 2, 3, 1, 1, 1, 3 },//40
            new int[] { 2, 3, 1, 3, 1, 1 },
            new int[] { 1, 1, 2, 1, 3, 3 },
            new int[] { 1, 1, 2, 3, 3, 1 },
            new int[] { 1, 3, 2, 1, 3, 1 },
            new int[] { 1, 1, 3, 1, 2, 3 },
            new int[] { 1, 1, 3, 3, 2, 1 },
            new int[] { 1, 3, 3, 1, 2, 1 },
            new int[] { 3, 1, 3, 1, 2, 1 },
            new int[] { 2, 1, 1, 3, 3, 1 },
            new int[] { 2, 3, 1, 1, 3, 1 },//50
            new int[] { 2, 1, 3, 1, 1, 3 },
            new int[] { 2, 1, 3, 3, 1, 1 },
            new int[] { 2, 1, 3, 1, 3, 1 },
            new int[] { 3, 1, 1, 1, 2, 3 },
            new int[] { 3, 1, 1, 3, 2, 1 },
            new int[] { 3, 3, 1, 1, 2, 1 },
            new int[] { 3, 1, 2, 1, 1, 3 },
            new int[] { 3, 1, 2, 3, 1, 1 },
            new int[] { 3, 3, 2, 1, 1, 1 },
            new int[] { 3, 1, 4, 1, 1, 1 },//60
            new int[] { 2, 2, 1, 4, 1, 1 },
            new int[] { 4, 3, 1, 1, 1, 1 },
            new int[] { 1, 1, 1, 2, 2, 4 },
            new int[] { 1, 1, 1, 4, 2, 2 },
            new int[] { 1, 2, 1, 1, 2, 4 },
            new int[] { 1, 2, 1, 4, 2, 1 },
            new int[] { 1, 4, 1, 1, 2, 2 },
            new int[] { 1, 4, 1, 2, 2, 1 },
            new int[] { 1, 1, 2, 2, 1, 4 },
            new int[] { 1, 1, 2, 4, 1, 2 },//70
            new int[] { 1, 2, 2, 1, 1, 4 },
            new int[] { 1, 2, 2, 4, 1, 1 },
            new int[] { 1, 4, 2, 1, 1, 2 },
            new int[] { 1, 4, 2, 2, 1, 1 },
            new int[] { 2, 4, 1, 2, 1, 1 },
            new int[] { 2, 2, 1, 1, 1, 4 },
            new int[] { 4, 1, 3, 1, 1, 1 },
            new int[] { 2, 4, 1, 1, 1, 2 },
            new int[] { 1, 3, 4, 1, 1, 1 },
            new int[] { 1, 1, 1, 2, 4, 2 },//80
            new int[] { 1, 2, 1, 1, 4, 2 },
            new int[] { 1, 2, 1, 2, 4, 1 },
            new int[] { 1, 1, 4, 2, 1, 2 },
            new int[] { 1, 2, 4, 1, 1, 2 },
            new int[] { 1, 2, 4, 2, 1, 1 },
            new int[] { 4, 1, 1, 2, 1, 2 },
            new int[] { 4, 2, 1, 1, 1, 2 },
            new int[] { 4, 2, 1, 2, 1, 1 },
            new int[] { 2, 1, 2, 1, 4, 1 },
            new int[] { 2, 1, 4, 1, 2, 1 },//90
            new int[] { 4, 1, 2, 1, 2, 1 },
            new int[] { 1, 1, 1, 1, 4, 3 },
            new int[] { 1, 1, 1, 3, 4, 1 },
            new int[] { 1, 3, 1, 1, 4, 1 },
            new int[] { 1, 1, 4, 1, 1, 3 },
            new int[] { 1, 1, 4, 3, 1, 1 },
            new int[] { 4, 1, 1, 1, 1, 3 },
            new int[] { 4, 1, 1, 3, 1, 1 },
            new int[] { 1, 1, 3, 1, 4, 1 },
            new int[] { 1, 1, 4, 1, 3, 1 },//100
            new int[] { 3, 1, 1, 1, 4, 1 },
            new int[] { 4, 1, 1, 1, 3, 1 },
            new int[] { 2, 1, 1, 4, 1, 2 }, //startA     103
            new int[] { 2, 1, 1, 2, 1, 4 }, //startB     104
            new int[] { 2, 1, 1, 2, 3, 2 },  //startC    105
            new int[] { 2, 3, 3, 1, 1, 1, 2}, //stop     106
            new int[] { 2, 3, 3, 1, 1, 1} // short stop  107
        };

    }
}
