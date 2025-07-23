using BarcodeReader.Core.RoyalMail;

namespace BarcodeReader.Core.RoyalMailKIX
{
	//Same decoder as RM but of variable length
    internal class KIXDecoder: RMDecoder
    {
        public KIXDecoder()
        {
        }

        const int MAX_ERRORS = 2;
        public override string Decode(bool[][] samples, out float confidence)
        {
            //Bars to chars
            confidence = 1.0f;
            int errors = 0;
            string code = "";
            for (int i = 0; i < samples.Length; i += 4)
            {
                string ch = DecodeChar(samples, i);
                if (ch == null) { code += "?"; confidence = 0f; if (errors++ > MAX_ERRORS) return null; }
                else code += ch;
            }
            return code;
        }
    }
}
