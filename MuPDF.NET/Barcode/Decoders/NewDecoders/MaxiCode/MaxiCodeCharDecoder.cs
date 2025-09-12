using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.MaxiCode
{
    class MaxiCodeCharDecoder
    {
        const char SHIFTA = '\uFFF0';
        const char SHIFTB = '\uFFF1';
        const char SHIFTC = '\uFFF2';
        const char SHIFTD = '\uFFF3';
        const char SHIFTE = '\uFFF4';
        const char TWOSHIFTA = '\uFFF5';
        const char THREESHIFTA = '\uFFF6';
        const char LATCHA = '\uFFF7';
        const char LATCHB = '\uFFF8';
        const char LOCK = '\uFFF9';
        const char ECI = '\uFFFA';
        const char NS = '\uFFFB';
        const char PAD = '\uFFFC';
        const char FS = '\u001C';
        const char GS = '\u001D';
        const char RS = '\u001E';
        const string NINE_DIGITS = "000000000";
        const string THREE_DIGITS = "000";

        static readonly String[] charSets = {
                                 "\nABCDEFGHIJKLMNOPQRSTUVWXYZ"+ECI+FS+GS+RS+NS+' '+PAD+"\"#$%&'()*+,-./0123456789:"+SHIFTB+SHIFTC+SHIFTD+SHIFTE+LATCHB,
                                 "`abcdefghijklmnopqrstuvwxyz"+ECI+FS+GS+RS+NS+'{'+PAD+"}~\u007F;<=>?[\\]^_ ,./:@!|"+PAD+TWOSHIFTA+THREESHIFTA+PAD+SHIFTA+SHIFTC+SHIFTD+SHIFTE+LATCHA,
                                 "\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8\u00C9\u00CA\u00CB\u00CC\u00CD\u00CE\u00CF\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D7\u00D8\u00D9\u00DA"+ECI+FS+GS+RS+NS+"\u00DB\u00DC\u00DD\u00DE\u00DF\u00AA\u00AC\u00B1\u00B2\u00B3\u00B5\u00B9\u00BA\u00BC\u00BD\u00BE\u0080\u0081\u0082\u0083\u0084\u0085\u0086\u0087\u0088\u0089"+LATCHA+' '+LOCK+SHIFTD+SHIFTE+LATCHB,
                                 "\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E6\u00E7\u00E8\u00E9\u00EA\u00EB\u00EC\u00ED\u00EE\u00EF\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F7\u00F8\u00F9\u00FA"+ECI+FS+GS+RS+NS+"\u00FB\u00FC\u00FD\u00FE\u00FF\u00A1\u00A8\u00AB\u00AF\u00B0\u00B4\u00B7\u00B8\u00BB\u00BF\u008A\u008B\u008C\u008D\u008E\u008F\u0090\u0091\u0092\u0093\u0094"+LATCHA+' '+SHIFTC+LOCK+SHIFTE+LATCHB,
                                 "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\n\u000B\u000C\r\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A"+ECI+PAD+PAD+'\u001B'+NS+FS+GS+RS+"\u001F\u009F\u00A0\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A9\u00AD\u00AE\u00B6\u0095\u0096\u0097\u0098\u0099\u009A\u009B\u009C\u009D\u009E"+LATCHA+' '+SHIFTC+SHIFTD+LOCK+LATCHB,
//                                 "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\n\u000B\u000C\r\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F\u0020\u0021\"\u0023\u0024\u0025\u0026\u0027\u0028\u0029\u002A\u002B\u002C\u002D\u002E\u002F\u0030\u0031\u0032\u0033\u0034\u0035\u0036\u0037\u0038\u0039\u003A\u003B\u003C\u003D\u003E\u003F"
                              };

        static readonly int[] dataLengthForModes = new int[] { 0, 0, 84, 84, 77, 93, 0 };

        //This method receives a byte array from the sampling process and returns the decoded string
        //performing error correction.
        //byte array is divided into 2 parts:
        //- primary : 0..19   --> 0..9 data, 10..19 ecc
        //- secondary: 20..143 --> 
        //                      EEC: 68 + 56 ecc
        //                      SEC: 84 + 40 ecc
        public static string Decode(byte[] bytes, out float confidence)
        {
            byte[] primary = new byte[20];
            byte[] secondary = new byte[124];
            Array.Copy(bytes, primary, 20);
            Array.Copy(bytes, 20, secondary, 0, 124);

            //-------------- ERROR CORRECTION -------------------
            //correct primary always EEC
            confidence = 0F;
            float confPrimary, confOdd, confEven;
            if (!Correct(primary, 10, out confPrimary)) return null;


            int mode = primary[0] & 15; //001111  modules 3..6
            //EEC or SEC(mode=5) secondary
            byte[] secOdd=Extract(secondary, 1);
            byte[] secEven=Extract(secondary, 0);
            int errorCodeWords = (mode != 5 ? 20 : 28); //SEC for all except for mode 5 that uses EEC
            if (!Correct(secOdd, errorCodeWords, out confOdd))  return null;
            if (!Correct(secEven, errorCodeWords, out confEven)) return null;
            Join(secOdd, secEven, secondary);
            confidence = (confPrimary + confEven + confOdd) / 3F;

            //-------------- DECODE DATA BYTES -----------------
            String msg = "";
#if DEBUG
            //msg = "mode: " + mode + " -->";
#endif
            byte[] data = mode <= 6 ? new byte[dataLengthForModes[mode]] : null;
            switch (mode)
            {
                case 0:
                case 1:
                    msg += "obsolete mode";
                    break;
                case 2:
                case 3:
                    //primary(0..9 Structured Carrier Message) + secondary)
                    string structuredCarrier= DecodeStructuredCarrier(primary, mode);
                    Array.Copy(secondary, 0, data, 0, data.Length);
                    string message= DecodeBytes(data);

                    string comp = "[)>" + RS + "01" + GS;
                    if (message.StartsWith(comp))
                        msg += message.Substring(0, comp.Length + 2) + structuredCarrier + GS + message.Substring(comp.Length + 2);
                    else
                        msg += structuredCarrier + message;
                    break;
                case 4:
                case 5:
                    Array.Copy(primary, 1, data, 0, 9);
                    Array.Copy(secondary, 0, data, 9, data.Length - 9);
                    msg += DecodeBytes(data);
                    break;
                case 6:
                    msg += "silence mode";
                    break;
                default: msg += "bad mode";
                    break;
            }
            return msg;
        }

        static byte[] Extract(byte[] data, int offset)
        {
            byte[] r = new byte[data.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = data[i*2+offset];
            return r;
        }

        static void Join(byte[] odd, byte[] even, byte[] data)
        {
            for (int i = 0; i < odd.Length; i++)
            {
                data[i * 2] = even[i];
                data[i * 2 + 1] = odd[i];
            }
        }

        static bool Correct(byte[] data, int errorCodeWords, out float confidence)
        {
            int[] tmp = new int[data.Length];
            for (int i = 0; i < data.Length; i++) tmp[i] = (int)data[i];
            ReedSolomon rs = new ReedSolomon(tmp, errorCodeWords, 6, 67, 1);
            rs.Correct(out confidence);
            if (!rs.CorrectionSucceeded) return false;
            for (int i = 0; i < data.Length; i++) data[i] = (byte)rs.CorrectedData[i];
            return true;
        }

        //PRYMARY has 10 codewords of data --> 60 bits
        //mode 4 bits
        //postal code 36 bits
        //country code 10 bits
        //service class 10 bits
        public static string DecodeStructuredCarrier(byte[] data, int mode)
        {
            int[] modes = DecodeBits(data, 6, new int[] {2, 5 }, 4);
            int serviceClass = DecodeBits(data, 6, new int[] { 54, 59, 48, 51}, 10)[0]; //10 bits
            int countryCode = DecodeBits(data, 6, new int[] { 52, 53, 42, 47, 36, 37}, 10)[0]; //10 bits
            string sPostalCode = "---------";
            if (mode == 2)
            {
                int postalCodeLength = DecodeBits(data, 6, new int[] { 38, 41, 30, 31}, 6)[0]; //6 bits
                int postalCode = DecodeBits(data, 6, new int[] { 32, 35, 24, 29, 18, 23, 12, 17, 6, 11, 0, 1}, 30)[0]; //30 bits
                sPostalCode = ToZeroInt(postalCode, postalCodeLength);
            }
            else //mode==3
            {
                int[] postalCode = DecodeBits(data, 6, new int[] { 38, 41, 30, 35, 24, 29, 18, 23, 12, 17, 6, 11, 0, 1}, 6); //36 bits --> 6 codewords
                byte[] bPostalCode = new byte[postalCode.Length];
                for (int i = 0; i < postalCode.Length; i++) bPostalCode[i] = (byte)postalCode[i];
                sPostalCode = DecodeBytes(bPostalCode);
            }
//#if DEBUG
//            return "[" + sPostalCode + "-" + ToZeroInt(countryCode, 3) + "-" + ToZeroInt(serviceClass, 3) + "]";
//#else
            return sPostalCode + RS + ToZeroInt(countryCode, 3) + RS + ToZeroInt(serviceClass, 3);
//#endif
        }

        //indexes from MSB to LSB
        static int[] DecodeBits(byte[] data, int codewordLength, int[] indexes, int nBitsPerCodeword)
        {
            bool[] bits = ReadBits(data, codewordLength, indexes);
            if (bits.Length % nBitsPerCodeword != 0) throw new Exception("ERROR in readBits");

            int[] a = new int[bits.Length / nBitsPerCodeword];
            int codeword = 0, j = 0, i=0;
            while (i < bits.Length)
            {
                int k = (bits[i] ? (1 << (i % nBitsPerCodeword)) : 0);
                codeword += k;
                i++;
                if ((i) % nBitsPerCodeword == 0) { a[j] = codeword; j++; codeword = 0; }
            }
            return a;
        }

        //indexes from MSB to LSB
        static bool[] ReadBits(byte[] data, int codewordLength, int[] indexes)
        {
            int l = 0;
            for (int i=0;i<indexes.Length; i+=2) l+=(indexes[i+1]-indexes[i]+1);
            bool[] a=new bool[l];
            int pos = l - 1;
            for (int i = 0; i < indexes.Length; i += 2)
                for (int j = indexes[i]; j <= indexes[i+1]; j++) 
                    a[pos--] = ((data[j / codewordLength] >> (codewordLength -1 - j % codewordLength)) & 1 )== 1;
            return a;
        }

        static string ToZeroInt(int n, int length)
        {
            string s = Convert.ToString(n);
            return s.PadLeft(length, '0');            
        }

        public static string DecodeBytes(byte[] data)
        {
            int pos = 0, count=-1, charSet=0, prevCharSet=-1;
            string s = "";
            while (pos < data.Length)
            {
                char ch = charSets[charSet][data[pos++]];
                switch (ch)
                {
                    case LATCHA: charSet = 0; count = -1; break;
                    case LATCHB: charSet = 1; count = -1; break;
                    case SHIFTA: prevCharSet = charSet; charSet = 0; count = 1; break;
                    case TWOSHIFTA: prevCharSet = charSet; charSet = 0; count = 2; break;
                    case THREESHIFTA: prevCharSet = charSet; charSet = 0; count = 3; break;
                    case SHIFTB: prevCharSet = charSet; charSet = 1; count = 1; break;
                    case SHIFTC: prevCharSet = charSet; charSet = 2; count = 1; break;
                    case SHIFTD: prevCharSet = charSet; charSet = 3; count = 1; break;
                    case SHIFTE: prevCharSet = charSet; charSet = 4; count = 1; break;
                    case LOCK: count = -1; break;
                    case NS: 
                        //5 codewords --> numeric
                        long n = 0;
                        for (int i = 0; i < 5; i++) n = (n << 6) + (pos<data.Length?(int)data[pos++]:0);
                        string ns = "000000000" + Convert.ToString(n);
//#if DEBUG
//                        s += "NS("+ns.Substring(ns.Length-9)+")";
//#else
                        s += ns.Substring(ns.Length - 9);
//#endif
                        break;
                    case ECI:
                        int codeword = data[pos++];
                        int eciValue = 0;
                        if ((codeword & 32)==1)
                            if ((codeword & 16)==1)
                                if ((codeword & 8)==1)
                                    eciValue = (codeword & 3) << 18 + data[pos++] << 12 + data[pos++] << 6 + data[pos++]; // 1110xx
                                else eciValue = (codeword & 7) << 12 + data[pos++] << 6 + data[pos++]; // 110xxx
                            else eciValue = (codeword & 15) << 6 + data[pos++]; // 10xxxx
                        else eciValue = codeword & 31; // 011111
//#if DEBUG 
//                        s += "ECI{" + eciValue + "}";
//#else
                        s += "\\" + eciValue + "";
//#endif
                        break;
                    default: s += ch; count--; if (count == 0) { charSet = prevCharSet; count = -1; } break;
                }
            }
            //remove PAD chars at the end
            s = s.TrimEnd(new char[] { PAD });
            return s;
        }
    }
}
