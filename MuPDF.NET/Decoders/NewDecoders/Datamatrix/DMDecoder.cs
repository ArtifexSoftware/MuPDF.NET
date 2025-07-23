using System;
using System.Collections;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
	internal class DMDecoder
    {
        // decode method for ecc200 barcodes
        public static ABarCodeData[] DecodeECC200Data(int[] endcodedData, System.Text.Encoding encoding)
        {
            int index = 0;
            ArrayList decodedData = new ArrayList();
            //this is for indicating that we detected a pad char in ASCII mode -> end of data.
            bool finished = false;
            while (index < endcodedData.Length && !finished)
            {
                int symbol = endcodedData[index];
                if (symbol < 230 && symbol != 129 || symbol == SymbolDecoder.ASCIIUpperShift)
                {
                    // ASCII encodation mode
                    ABarCodeData[] plusData = SymbolDecoder.DecodeASCII(endcodedData, ref index);
                    if (plusData.Length > 0)
                    {
                        decodedData.AddRange(plusData);
                    }
                    continue;
                }
                switch (symbol)
                {
                    case 129: // pad char found, end of data
                        finished = true;
                        break;
                    case 230: // C40 mode
                        ++index;
                        ABarCodeData[] c40Data = SymbolDecoder.DecodeC40Text(endcodedData, ref index, SymbolDecoder.C40Set);
                        if (c40Data.Length > 0)
                        {
                            decodedData.AddRange(c40Data);
                        }
                        break;
                    case 231: // Base256 mode
                        ++index;
                        ABarCodeData[] base256Data = SymbolDecoder.DecodeBase256(endcodedData, ref index, encoding);
                        if (base256Data.Length > 0)
                        {
                            decodedData.AddRange(base256Data);
                        }
                        break;
                    case 238: // X12 mode
                        ++index;
                        ABarCodeData[] x12Data = SymbolDecoder.DecodeX12(endcodedData, ref index);
                        if (x12Data.Length > 0)
                        {
                            decodedData.AddRange(x12Data);
                        }
                        break;
                    case 239: // Text mode
                        ++index;
                        ABarCodeData[] textData = SymbolDecoder.DecodeC40Text(endcodedData, ref index,
                                                                             SymbolDecoder.TextSet);
                        if (textData.Length > 0)
                        {
                            decodedData.AddRange(textData);
                        }
                        break;
                    case 240: // Edifact mode
                        ++index;
                        ABarCodeData[] edifactData = SymbolDecoder.DecodeEdifact(endcodedData, ref index);
                        if (edifactData.Length > 0)
                        {
                            decodedData.AddRange(edifactData);
                        }
                        break;
                    case 242:
                        ++index; //ECI
                        decodedData.Add(SymbolDecoder.DecodeECI(endcodedData, ref index));
                        break;
                    case 232:
                        ++index; //FNC
                        decodedData.Add(new FNC1Symbol());
                        break;
                    case 233:
                        ++index; //Structured append
                        decodedData.Add(SymbolDecoder.DecodeStructuredAppend(endcodedData, ref index));
                        break;
                    case 234:
                        ++index; //Reader prog
                        decodedData.Add(new ReaderProgramSymbol());
                        break;
                    case 236:
                        ++index; //Macro 05
                        decodedData.Add(new Macro05Symbol());
                        break;
                    case 237:
                        ++index; //Macro 06
                        decodedData.Add(new Macro06Symbol());
                        break;
                    default:
                        ++index; //Error
                        break;
                }
            }

            ABarCodeData[] result = new ABarCodeData[decodedData.Count];
            decodedData.CopyTo(result);
            return result;
        }

        // decode method for non-ecc200 barcodes
        public static ABarCodeData[] DecodeNonECC200Data(BitArray bitArray, int dataLength, NonECC200DataFormat dataFormat, ushort crc, System.Text.Encoding encoding)
        {
            byte[] userData;
            switch (dataFormat) // switch on dataformat - Base256 and ASCII are easy, the rest of them are similar
            {
                case NonECC200DataFormat.Base256:
                    userData = SliceBitStream(bitArray, dataLength, 8);
                    break;
                case NonECC200DataFormat.ASCII:
                    userData = SliceBitStream(bitArray, dataLength, 7);
                    break;
                case NonECC200DataFormat.Base11:
                case NonECC200DataFormat.Base27:
                case NonECC200DataFormat.Base37:
                case NonECC200DataFormat.Base41:
                    userData = SymbolDecoder.DecodeBaseX(bitArray, dataLength, dataFormat);
                    break;
                default:
                    userData = new byte[0];
                    break;
            }

            // if the userData is null, we probably have invalid data
            if (userData == null)
            {
                return null;
            }

            // get the CRC to validate the source data
            ushort userCrc = NonECC200CRC.CalculateCRC(dataFormat, userData);
            if (userCrc != crc)
            {
                return null;
            }

            // Base256 data is returned as byte, the rest of them are strings
            return dataFormat == NonECC200DataFormat.Base256
                       ? new ABarCodeData[] {new Base256BarCodeData(userData, encoding)}
                       : new ABarCodeData[] {new StringBarCodeData(userData)};
        }

        // slices a bitstream width the given size, and returns the chunks as bytes
        static byte[] SliceBitStream(BitArray bitArray, int dataLength, int chunkSize)
        {
            if (bitArray.Length < dataLength*chunkSize)
            {
                return null;
            }

            byte[] userData = new byte[dataLength];
            int bitIndex = 0;
            for (int i = 0; i < dataLength; ++i)
            {
                for (int b = 0; b < chunkSize; ++b)
                {
                    if (bitArray[bitIndex++])
                    {
                        userData[i] = (byte)(userData[i] | (1 << b));
                    }
                }
            }

            return userData;
        }

        // calculates CRC for non-ecc200 bitstreams
        class NonECC200CRC
        {
            private static readonly Hashtable CRCPrexifes = new Hashtable();

            static NonECC200CRC()
            {
                CRCPrexifes.Add(NonECC200DataFormat.Base11, 0x0100);
                CRCPrexifes.Add(NonECC200DataFormat.Base27, 0x0200);
                CRCPrexifes.Add(NonECC200DataFormat.Base41, 0x0300);
                CRCPrexifes.Add(NonECC200DataFormat.Base37, 0x0400);
                CRCPrexifes.Add(NonECC200DataFormat.ASCII, 0x0500);
                CRCPrexifes.Add(NonECC200DataFormat.Base256, 0x0600);
            }

            // Input data in normal byte order, this method will reverse it for internal use, as per DM spec.
            public static ushort CalculateCRC(NonECC200DataFormat format, byte[] rawData)
            {
                byte[] data = new byte[rawData.Length + 2];
                // add the required data prefix (CRC register initial value)
                int prefix = (int)CRCPrexifes[format];
                data[0] = (byte)(prefix / 0x100);
                data[1] = (byte)(prefix % 0x100);
                Array.Copy(rawData, 0, data, 2, rawData.Length);
                data = Common.Utils.ReverseBitsPerByte(data);

                uint crcSum = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    uint byteValue = ((uint)data[i] << 8);
                    for (int bi = 0; bi < 8; bi++)
                    {
                        if (((crcSum ^ byteValue) & 0x8000) != 0)
                        {
                            crcSum = (crcSum << 1) ^ 0x1021;
                        }
                        else
                        {
                            crcSum <<= 1;
                        }
                        byteValue <<= 1;
                    }
                }

                // reverse the bitorder (again, and again...)
                ushort crc = 0;
                for (int i = 0; i < 16; ++i)
                {
                    if ((crcSum & (1 << i)) != 0)
                    {
                        crc = (ushort)(crc | (1 << (15 - i)));
                    }
                }

                return crc;
            }
        }

    }    
}
