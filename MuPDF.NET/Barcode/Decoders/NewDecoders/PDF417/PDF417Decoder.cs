using System.Collections;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.PDF417
{
    // decode the data, according to the indicated encodation type
    internal class PDF417Decoder
    {
        public static ABarCodeData[] Decode(int[] encodedData, System.Text.Encoding encoding, int initialEncoding)
        {
            int index = 0;
            bool rememberTextCompactionSubMode = false;
            ArrayList decodedData = new ArrayList();
            SymbolDecoder symbolDecoder = new SymbolDecoder();
            while (index < encodedData.Length)
            {
                int symbol = encodedData[index];
                if (symbol < 900)
                {
                    switch (initialEncoding)
                    {
                        case 900:
                            // ASCII encodation mode
                            ABarCodeData[] plusData = symbolDecoder.DecodeText(encodedData, ref index, rememberTextCompactionSubMode);
                            if (plusData != null && plusData.Length > 0) decodedData.AddRange(plusData);
                            break;
                        case 901:
                            ABarCodeData[] chunkBase256Data = symbolDecoder.DecodeBase256(encodedData, ref index, false, encoding);
                            if (chunkBase256Data != null && chunkBase256Data.Length > 0) decodedData.AddRange(chunkBase256Data);
                            break;
                    }
                }
                else
                {
                    rememberTextCompactionSubMode = false;
                    switch (symbol)
                    {
                        case 900: // ASCII encodation or pad
                            ++index;
                            ABarCodeData[] plusData = symbolDecoder.DecodeText(encodedData, ref index, rememberTextCompactionSubMode);
                            if (plusData != null && plusData.Length > 0) decodedData.AddRange(plusData);
                            break;
                        case 913: // single byte mode
                            ++index;
                            rememberTextCompactionSubMode = true;
                            decodedData.Add(new Base256BarCodeData(new byte[] { (byte)encodedData[index++] }, encoding));
                            break;
                        case 924: // full length multibyte mode
                            ++index;
                            ABarCodeData[] fullBase256Data = symbolDecoder.DecodeBase256(encodedData, ref index, true, encoding);
                            if (fullBase256Data != null && fullBase256Data.Length > 0) decodedData.AddRange(fullBase256Data);
                            break;
                        case 901: // incomplete multibyte mode
                            ++index;
                            ABarCodeData[] chunkBase256Data = symbolDecoder.DecodeBase256(encodedData, ref index, false, encoding);
                            if (chunkBase256Data != null && chunkBase256Data.Length > 0) decodedData.AddRange(chunkBase256Data);
                            break;
                        case 902: // numeric mode
                            ++index;
                            ABarCodeData[] numericData = symbolDecoder.DecodeNumeric(encodedData, ref index, -1);
                            if (numericData != null && numericData.Length > 0) decodedData.AddRange(numericData);
                            break;
                        case 926: //ECI
                            index += 3;
                            break;
                        case 928: //Macro PDF control block
                            index++;
                            decodedData.Add(new StringBarCodeData("\\928"));
                            decodedData.AddRange(symbolDecoder.DecodeNumeric(encodedData, ref index, 2));
                            //decodedData.Add(new StringBarCodeData(" id:"));
                            decodedData.AddRange(symbolDecoder.DecodeBase900(encodedData, ref index, -1));
                            while (index < encodedData.Length && encodedData[index] == 923)
                            {
                                index++;
                                decodedData.Add(new StringBarCodeData("\\923"));
                                int optionType = encodedData[index++];
                                decodedData.Add(new StringBarCodeData("\\" + optionType.ToString().PadLeft(3, '0')));
                                switch (optionType)
                                {
                                    case 0: //file name
                                        //decodedData.Add(new StringBarCodeData(" fileName:"));
                                        decodedData.AddRange(symbolDecoder.DecodeText(encodedData, ref index, false));
                                        break;
                                    case 1: //segment count
                                        //decodedData.Add(new StringBarCodeData(" segmentCount:"));
                                        decodedData.AddRange(symbolDecoder.DecodeNumeric(encodedData, ref index, 4));
                                        break;
                                    case 2: //time stamp
                                        //decodedData.Add(new StringBarCodeData(" timeStamp:"));
                                        decodedData.AddRange(symbolDecoder.DecodeNumeric(encodedData, ref index, 6));
                                        break;
                                    case 3: //sender
                                        //decodedData.Add(new StringBarCodeData(" sender:"));
                                        decodedData.AddRange(symbolDecoder.DecodeText(encodedData, ref index, false));
                                        break;
                                    case 4: //address
                                        //decodedData.Add(new StringBarCodeData(" address:"));
                                        decodedData.AddRange(symbolDecoder.DecodeText(encodedData, ref index, false));
                                        break;
                                    case 5: //file size
                                        //decodedData.Add(new StringBarCodeData(" fileSize:"));
                                        decodedData.AddRange(symbolDecoder.DecodeNumeric(encodedData, ref index, -1));
                                        break;
                                    case 6: //checksum
                                        //decodedData.Add(new StringBarCodeData(" checksum:"));
                                        decodedData.AddRange(symbolDecoder.DecodeNumeric(encodedData, ref index, 4));
                                        break;
                                    default: //decodedData.Add(new StringBarCodeData(" optional:"));
                                        break;
                                }
                                decodedData.AddRange(symbolDecoder.DecodeBase900(encodedData, ref index, 1));
                            }
                            break;
                        case 922: //Macro PDF terminator
                            index++;
                            decodedData.Add(new StringBarCodeData("\\922"));
                            break;
                        default:
                            ++index;
                            //Error
                            break;
                    }
                }
            }
            ABarCodeData[] result = new ABarCodeData[decodedData.Count];
            decodedData.CopyTo(result);
            return result;
        }
    }
}
