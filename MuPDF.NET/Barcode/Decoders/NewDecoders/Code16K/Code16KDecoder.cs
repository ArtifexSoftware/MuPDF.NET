using System;
using System.Collections.Generic;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Code16K
{
    //Class to decode a Code16K barcode from the top left and bottom left corners. 
    //Traces N perpendicular lines (perpendicular to top-bottom corners in a moduleLength step) to scan
    //all barcode rows. This scan is redundant, and all samples are stored in a data structure. At the end,
    //the most common values are used to decode the barcode. 
    class Code16KDecoder
    {
        public BarCodeRegion Decode(ImageScaner scan, MyPointF up, MyPointF down, FoundPattern foundPattern)
        {
            //main directions
            MyVectorF vdY = (up - down);
            float startLength = vdY.Length;
            vdY = vdY.Normalized;
            MyVectorF vdX = new MyVectorF(-vdY.Y, vdY.X);

            float d = foundPattern.moduleLength;
            //data structure to store all redundant samples of each row.
            Dictionary<Int32, SymbolSamples[]> symbols = new Dictionary<Int32,SymbolSamples[]>();

            //scan region rows, at small step of barcode module length. Result is stored in symbols array.
            //symbols array is initialized later, when the first row is read and the number of rows is known.
            //Seems that results are better using bar widths instead of E (sum of 2 consecutive bar widths).
            int[] nBars=new int[]{4,1,6,6,6,6,6,4};
            int[] nModules=new int[]{7,1,11,11,11,11,11,7};
            int[] tableIndexs=new int[]{0,-1,1,1,1,1,1,0};
            int[][][] ttSymbols = new int[][][] { _startStop, _patterns };


            BarSymbolReader reader = new BarSymbolReader(scan, nBars, nModules, tableIndexs, true, true, foundPattern.moduleLength, ttSymbols, false, null);
            while (d < startLength)
            {
                MyPointF a = up - vdY * d; MyPoint last;
                float error, maxError, confidence;
                int[] row = reader.Read(a, vdX, out error, out maxError, out confidence, out last); //convert scanline to bytecodes array.

                if (row != null && row.Length == 8)
                {
                    //find nRow (0..15)
                    int nRow = -1;
                    if (row[0] == row[7]) nRow = row[0];
                    else if (row[0] == (row[7] + 4) % 8) nRow = row[0] + 8;

                    if (nRow >= 0 && nRow <= 15)
                    {
                        if (!symbols.ContainsKey(nRow))
                        {
                            SymbolSamples[] s = symbols[nRow] = new SymbolSamples[5];
                            for (int i = 0; i < 5; i++) s[i] = new SymbolSamples();
                        }
                        SymbolSamples[] samples = symbols[nRow];
                        for (int i = 2; i < 7; i++) samples[i - 2].add(row[i]);
                    }
                }

                d += foundPattern.moduleLength;
            }

            //extract nRows and starting mode
            if (!symbols.ContainsKey(0)) return null;
            symbols[0][0].sort();
            int S = ((SymbolSample)symbols[0][0].samples[0]).symbol;
            int startMode = S % 7;
            int nRows = S / 7 + 2;

            //extract codewords
            int[] codewords = new int[nRows*5];
            for (int r = 0; r < nRows; r++)
            {
                if (!symbols.ContainsKey(r)) return null;
                for (int i = 0; i < 5; i++)
                {
                    symbols[r][i].sort();
                    codewords[r*5+i] = ((SymbolSample)symbols[r][i].samples[0]).symbol;
                }
            }

            //check CRC
            int chk1 = 0, chk2=0;
            for (int i = 0; i < codewords.Length - 2; i++) chk1 = (chk1 + codewords[i] * (i + 2)) % 107;
            for (int i = 0; i < codewords.Length - 1; i++) chk2 = (chk2 + codewords[i] * (i + 1)) % 107;

            string msg = decodeRow(codewords);

            float l = foundPattern.moduleLength * (7 + 11 * 5 + 7);
            BarCodeRegion reg = new BarCodeRegion(up, up + vdX * l, down + vdX * l, down);
            reg.Data = new ABarCodeData[] { new StringBarCodeData(msg) };
            reg.Confidence = (chk1==codewords[codewords.Length-2] && chk2==codewords[codewords.Length-1]?1f:0f);
            return reg;
        }

        //method to decode all codewords from a code16K barcode. Starts with the starting mode (rawData[0]%7) used to set
        //the initial decoding table.
        private string decodeRow(int[] rawData)
        {
            //Initial encoding schema A, B or C.
            string[] currentCodeSet = null;
            string char0 = null;
            switch (rawData[0]%7)
            {
                case 0: currentCodeSet = tableA; break;
                case 1: currentCodeSet = tableB; break;
                case 2: currentCodeSet = tableC; break;
                case 3: currentCodeSet = tableB; char0 = "FNC1"; break;
                case 4: currentCodeSet = tableC; char0 = "FNC1"; break;
                case 5: currentCodeSet = tableC; char0 = "1SA"; break;
                case 6: currentCodeSet = tableC; char0 = "2SA"; break;
                default: return null;
            }

            string msg = "";
            int nShift = 0;
            string[] currentShift = null;
            for (int i = 0; i < rawData.Length - 2; i++) if (i > 0 || char0 != null)
            {
                string[] t = (nShift > 0 ? currentShift : currentCodeSet);
                string ch = (i > 0 ? (rawData[i]!=-1?t[rawData[i]]:"?") : char0); 
                if (ch == "pad") ch = "";
                if (ch!=null) switch (ch) {
                    case "1SA": currentShift = tableA; nShift = 1; break;
                    case "2SA": currentShift = tableA; nShift = 2; break;
                    case "3SA": currentShift = tableA; nShift = 3; break;
                    case "1SB": currentShift = tableB; nShift = 1; break;
                    case "2SB": currentShift = tableB; nShift = 2; break;
                    case "3SB": currentShift = tableB; nShift = 3; break;
                    case "1SC": currentShift = tableC; nShift = 1; break;
                    case "2SC": currentShift = tableC; nShift = 2; break;
                    case "3SC": currentShift = tableC; nShift = 3; break;
                    case "CodeA": currentCodeSet = tableA; break;
                    case "CodeB": currentCodeSet = tableB; break;
                    case "CodeC": currentCodeSet = tableC; break;
                    default:
                        if (t == tableC && rawData[i] < 100) msg += Convert.ToString(rawData[i]).PadLeft(2, '0');
                        else if (ch.Length > 1) msg += "[" + ch + "]"; else msg += ch;
                        if (nShift>0) nShift--;
                        break;
                }
            }
            return msg;
        }

           
        private static readonly int[][] _startStop =
        { 
            new int[]{3,2,1,1}, new int[]{2,2,2,1}, new int[]{2,1,2,2}, new int[]{1,4,1,1}, 
            new int[]{1,1,3,2}, new int[]{1,2,3,1}, new int[]{1,1,1,4}, new int[]{3,1,1,2}
        };

        private static readonly int[][] _patterns =
        {
            new int[] { 2, 1, 2, 2, 2, 2 },
            new int[] { 2, 2, 2, 1, 2, 2 },
            new int[] { 2, 2, 2, 2, 2, 1 },
            new int[] { 1, 2, 1, 2, 2, 3 },
            new int[] { 1, 2, 1, 3, 2, 2 },
            new int[] { 1, 3, 1, 2, 2, 2 },
            new int[] { 1, 2, 2, 2, 1, 3 },
            new int[] { 1, 2, 2, 3, 1, 2 },
            new int[] { 1, 3, 2, 2, 1, 2 },
            new int[] { 2, 2, 1, 2, 1, 3 },
            new int[] { 2, 2, 1, 3, 1, 2 }, //10
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
            new int[] { 1, 1, 4, 3, 1, 1 },//96   FNC3  FNC3   96
            new int[] { 4, 1, 1, 1, 1, 3 },//97   FNC2  FNC2   97
            new int[] { 4, 1, 1, 3, 1, 1 },//98   1SB   1SA    98
            new int[] { 1, 1, 3, 1, 4, 1 },//99   CodeC CodeC  99
            new int[] { 1, 1, 4, 1, 3, 1 },//100  CodeB FNC4  CodeB
            new int[] { 3, 1, 1, 1, 4, 1 },//102  FNC4  CodeA CodeA
            new int[] { 4, 1, 1, 1, 3, 1 },//102  FNC1  FNC1  FNC1
            new int[] { 2, 1, 1, 4, 1, 2 },//103  PAD
            new int[] { 2, 1, 1, 2, 1, 4 },//104  2SB   2SA   2SB
            new int[] { 2, 1, 1, 2, 3, 2 },//105  2SC   2SC   2SB
            new int[] { 2, 1, 1, 1, 3, 3 } //106  3SC   3SC   3SB
        };

        string[] tableA = {
             " ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", 
             "*", "+", ",", "-", ".", "/", "0", "1", "2", "3", 
             "4", "5", "6", "7", "8", "9", ":", ";", "<", "=", 
             ">", "?", "@", "A", "B", "C", "D", "E", "F", "G", 
             "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", 
             "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "[", 
             "\\", "]", "^", "_", "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", 
             "ACK", "BEL", "BS", "HT", "LF", "VT", "FF", "CR", "SO", "SI", 
             "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", 
             "SUB", "ESC", "FS", "GS", "RS", "US", "FNC3", "FNC2", "1SB", "CodeC", 
             "CodeB", "FNC4", "FNC1", "pad", "2SB", "2SC","3SC"
        };

        string[] tableB = {
             " ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", 
             "*", "+", ",", "-", ".", "/", "0", "1", "2", "3", 
             "4", "5", "6", "7", "8", "9", ":", ";", "<", "=", 
             ">", "?", "@", "A", "B", "C", "D", "E", "F", "G", 
             "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", 
             "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "[", 
             "\\", "]", "^", "_", "`", "a", "b", "c", "d", "e", 
            "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", 
            "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", 
            "z", "{", "|", "}", "tilde", "DEL", "FNC3", "FNC2", "1SA", "CodeC", 
            "FNC4", "CodeA", "FNC1", "pad", "2SA", "2SC", "3SC" 
        };

        string[] tableC = {
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "", "", "", "", "", "", "", "", "", "", 
            "CodeB", "CodeA", "FNC1", "pad", "1SB", "2SB", "3SB"
        };
    }
}
