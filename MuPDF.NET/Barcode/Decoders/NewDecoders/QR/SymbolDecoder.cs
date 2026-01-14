using System.Collections;
using System.Text;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.QR
{
    internal delegate ABarCodeData Decoder(int version, BitArray dataStream, Encoding textEncoding, ref int index);

    class SymbolDecoder
    {
        public static readonly char[] AlphaSet = new char[]
                                                     {
                                                         '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
                                                         'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                                                         'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', '$', '%',
                                                         '*', '+', '-', '.', '/', ':'
                                                     };

        public static readonly int[] QRCountBitsNumeric = new int[]
                                                              {
                                                                  10, 10, 10, 10, 10, 10, 10, 10, 10, 12, 12, 12, 12, 12,
                                                                  12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 14, 14,
                                                                  14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 3, 4, 5,
                                                                  6
                                                              };

        public static readonly int[] QRCountBitsAlpha = new int[]
                                                            {
                                                                9, 9, 9, 9, 9, 9, 9, 9, 9, 11, 11, 11, 11, 11, 11, 11, 11,
                                                                11, 11, 11, 11, 11, 11, 11, 11, 11, 13, 13, 13, 13, 13, 13,
                                                                13, 13, 13, 13, 13, 13, 13, 13, 0, 3, 4, 5
                                                            };

        public static readonly int[] QRCountBitsByte = new int[]
                                                           {
                                                               8, 8, 8, 8, 8, 8, 8, 8, 8, 16, 16, 16, 16, 16, 16, 16, 16,
                                                               16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
                                                               16, 16, 16, 16, 16, 16, 16, 16, 0, 0, 4, 5
                                                           };

        public static readonly int[] QRCountBitsKanji = new int[]
                                                            {
                                                                8, 8, 8, 8, 8, 8, 8, 8, 8, 10, 10, 10, 10, 10, 10, 10, 10,
                                                                10, 10, 10, 10, 10, 10, 10, 10, 10, 12, 12, 12, 12, 12, 12,
                                                                12, 12, 12, 12, 12, 12, 12, 12, 0, 0, 3, 4
                                                            };
        
        public static readonly Hashtable QRDecoderMap = new Hashtable();

        public static readonly Hashtable QRDecoderMapM2 = new Hashtable();

        public static readonly Hashtable QRDecoderMapM3 = new Hashtable();

        public static readonly Hashtable QRDecoderMapM4 = new Hashtable();

        static SymbolDecoder()
        {
            QRDecoderMap.Add(1, new Decoder(DecodeNumeric));
            QRDecoderMap.Add(2, new Decoder(DecodeAlpha));
            QRDecoderMap.Add(4, new Decoder(DecodeByte));
            QRDecoderMap.Add(8, new Decoder(DecodeKanji));
            QRDecoderMap.Add(7, new Decoder(DecodeECI));
            QRDecoderMap.Add(5, new Decoder(DecodeFNC1first));
            QRDecoderMap.Add(9, new Decoder(DecodeFNC1second));
            QRDecoderMap.Add(3, new Decoder(DecodeStructuredAppend));            

            QRDecoderMapM2.Add(0, new Decoder(DecodeNumeric));
            QRDecoderMapM2.Add(1, new Decoder(DecodeAlpha));

            QRDecoderMapM3.Add(0, new Decoder(DecodeNumeric));
            QRDecoderMapM3.Add(1, new Decoder(DecodeAlpha));
            QRDecoderMapM3.Add(2, new Decoder(DecodeByte));
            QRDecoderMapM3.Add(3, new Decoder(DecodeKanji));

            QRDecoderMapM4.Add(0, new Decoder(DecodeNumeric));
            QRDecoderMapM4.Add(1, new Decoder(DecodeAlpha));
            QRDecoderMapM4.Add(2, new Decoder(DecodeByte));
            QRDecoderMapM4.Add(3, new Decoder(DecodeKanji));
        }

        public static ABarCodeData DecodeECI(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            if (index >= dataStream.Count)
            {
                return null;
            }

            int countOnes = 0;
            while (index < dataStream.Count && dataStream[index] == true) { index++; countOnes++; }

            if (index >= dataStream.Count)
            {
                return null;
            }

            index++; //skip first 0
            if (countOnes > 2)
            {
                return null;
            }

            int[] lengths=new int[]{7,14,21};
            int ECIAssignment= QRUtils.BitSliceValue(dataStream, ref index, lengths[countOnes]);
            string assignment = "000000" + ECIAssignment;
            return new StringBarCodeData("]Q2\\"+assignment.Substring(assignment.Length-6));
        }

        public static ABarCodeData DecodeFNC1first(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            return new StringBarCodeData("]Q3\\");
        }

        public static ABarCodeData DecodeFNC1second(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            int application = QRUtils.BitSliceValue(dataStream, ref index, 8);
            return new StringBarCodeData("]Q5\\"+application);
        }

        public static ABarCodeData DecodeStructuredAppend(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            int N = QRUtils.BitSliceValue(dataStream, ref index, 4); //number of barcode
            int total = QRUtils.BitSliceValue(dataStream, ref index, 4); //total of barcodes
            int parity = QRUtils.BitSliceValue(dataStream, ref index, 8);
            return new StringBarCodeData("]Q2\\MI"+(N+1)+"\\MO1"+(total+1)+"\\MF001\\MY");
        }

        public static ABarCodeData DecodeNumeric(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            if (version < 1 || version - 1 >= QRCountBitsNumeric.Length)
            {
                return null;
            }

            int countBits = QRCountBitsNumeric[version - 1];
            int dataLength = QRUtils.BitSliceValue(dataStream, ref index, countBits);
            int remainderLength = dataLength%3;
            int mainPartBlockCount = dataLength/3;
            int dataIndex = 0;
            byte[] data = new byte[dataLength];
            for (int i = 0; i < mainPartBlockCount; ++i)
            {
                int tripletValue = QRUtils.BitSliceValue(dataStream, ref index, 10);
                data[dataIndex++] = (byte) (tripletValue/100);
                tripletValue %= 100;
                data[dataIndex++] = (byte)(tripletValue / 10);
                tripletValue %= 10;
                data[dataIndex++] = (byte)(tripletValue);
            }

            if (remainderLength == 2)
            {
                int pairValue = QRUtils.BitSliceValue(dataStream, ref index, 7);
                data[dataIndex++] = (byte)(pairValue / 10);
                pairValue %= 10;
                data[dataIndex] = (byte)(pairValue);
            }
            else if (remainderLength == 1)
            {
                int value = QRUtils.BitSliceValue(dataStream, ref index, 4);
                data[dataIndex] = (byte)(value);
            }

            return new NumericBarCodeData(data);
        }

        public static ABarCodeData DecodeAlpha(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            if (version < 1 || version - 1 >= QRCountBitsAlpha.Length)
            {
                return null;
            }

            int countBits = QRCountBitsAlpha[version - 1];
            int dataLength = QRUtils.BitSliceValue(dataStream, ref index, countBits);
            int remainderLength = dataLength%2;
            int mainPartBlockCount = dataLength/2;
            int dataIndex = 0;
            char[] data = new char[dataLength];
            for (int i = 0; i < mainPartBlockCount; ++i)
            {
                int pairValue = QRUtils.BitSliceValue(dataStream, ref index, 11);
                int first = (pairValue/45)%45; //TODO %45 added to avoid crash
                int second = pairValue%45;
                if (first >= AlphaSet.Length || second >= AlphaSet.Length)
                {
                    return null;
                }

                data[dataIndex++] = AlphaSet[first];
                data[dataIndex++] = AlphaSet[second];
            }

            if (remainderLength > 0)
            {
                int remainderValue = QRUtils.BitSliceValue(dataStream, ref index, 6);
                if (remainderValue >= AlphaSet.Length)
                {
                    return null;
                }
                data[dataIndex] = AlphaSet[remainderValue];
            }

            return new StringBarCodeData(new string(data));
        }

        public static ABarCodeData DecodeByte(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            if (version < 1 || version - 1 >= QRCountBitsByte.Length)
            {
                return null;
            }

            int countBits = QRCountBitsByte[version - 1];
            int dataLength = QRUtils.BitSliceValue(dataStream, ref index, countBits);
            byte[] data = new byte[dataLength];
            for (int i = 0; i < dataLength; ++i)
            {
                data[i] = (byte) QRUtils.BitSliceValue(dataStream, ref index, 8);
            }

            if (textEncoding != null)
                return new Base256BarCodeData(data, textEncoding);

            //try to guess encoding
            var encName = EncodingUtils.GuessEncoding(data);
            var enc = Encoding.GetEncoding(encName);

            return new Base256BarCodeData(data, enc);
        }

        public static ABarCodeData DecodeKanji(int version, BitArray dataStream, Encoding textEncoding, ref int index)
        {
            if (version < 1 || version - 1 >= QRCountBitsKanji.Length)
            {
                return null;
            }

            int countBits = QRCountBitsKanji[version - 1];
            int dataLength = QRUtils.BitSliceValue(dataStream, ref index, countBits);
            byte[] data = new byte[dataLength*2];
            for (int i = 0; i < dataLength; ++i)
            {
                int value = QRUtils.BitSliceValue(dataStream, ref index, 13);
                int originalValue = value%0xC0 + ((value/0xC0) << 8);
                int offset = originalValue < 0x1F00 ? 0x8140 : 0xC140;
                originalValue += offset;
                data[2 * i + 1] = (byte)(originalValue & 0xFF);
                data[2*i] = (byte) ((originalValue >> 8) & 0xFF);
            }

            Encoding shiftJIS = Encoding.GetEncoding(932);
            char[] shiftJISChars = shiftJIS.GetChars(data);
            return new StringBarCodeData(new string(shiftJISChars));
        }
    }
}
