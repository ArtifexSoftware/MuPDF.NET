using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 DataMatrix symbology. GS1 DataMatrix is a 2D
    /// (two-dimensional) barcode that holds
    /// large amounts of data in a relatively small space. These barcodes
    /// are used primarily in aerospace, pharmaceuticals, medical device
    /// manufacturing, and by the U.S. Department of Defense to add
    /// visibility to the value chain. GS1 DataMatrix can be used for parts
    /// that need to be tracked in the manufacturing process because the
    /// barcode allows users to encode a variety of information related
    /// to the product, such as date or lot number. They are not intended
    /// to be used on items that pass through retail point-of-sale (POS).
    /// </summary>
    /// <remarks>See also:
    /// http://www.gs1us.org/standards/barcodes/gs1_datamatrix
    /// http://www.idautomation.com/datamatrixfaq.html#GS1_DataMatrix
    /// http://www.pmpnews.com/article/gs1-datamatrix-fnc1-versus-gs-variable-length-field-separator-character
    /// </remarks>
    class GS1DataMatrixSymbology : DataMatrixSymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataMatrixSymbology"/> class.
        /// </summary>
        public GS1DataMatrixSymbology()
            : base()
        {
            m_type = TrueSymbologyType.GS1_DataMatrix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1DataMatrixSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1DataMatrixSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.GS1_DataMatrix;
        }

        /// <summary>
        /// Validates the value using GS1 Data Matrix symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
//            if (!GS1ValueChecker.Check(value))
//                return false;

            if (value.Length == 0)
                return false;

            return true;

        }

        /// <summary>
        /// Gets or sets the barcode value to encode.
        /// The value can be set in 2 forms: with parenthesis for AI and without.
        /// If a value comes in form like 'xxxxxxxxxxx' or like 'xxxxxx|xxxxx' then the SDK automatically sets brackets according to GS1 AI (Application Identifiers).
        /// If a value comes in form like (xx)yyyyyy(zz)nnn then the SDK do NOT verifies the value against AI.
        /// However you may verify if the value is valid according to GS1 rules or not by using Barcode.ValueIsValidGS1() bool function
        /// </summary>
        /// <value>The barcode value to encode.</value>
        public override string Value
        {
            get
            {
                return base.Value;
            }
            set
            {                
                bool isBracket = false;
                for (int i = 0; i < value.Length; i++)
                    if (value[i] == '(')
                    {
                        isBracket = true;
                        break;
                    }

                base.Value = value;

                if (isBracket)
                    base.Value = value; // just set the value
                else
                    // else find AI identifiers and convert value from the form like "xxxxxx" into "(yy)xxxx(zz)nn"
                    base.Value = ApplicationIdentifiers.SelectAIs(value);
         
            }
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            string restriction = "GS1 Data Matrix symbology allows to encode a maximum data size of 2,335 alphanumeric characters.\n";
            restriction += "\r\nThe value can be set in 2 forms: with parenthesis for AI and without.";
            restriction += "\r\nIf a value comes in form like 'xxxxxxxxxxx' or like 'xxxxxx|xxxxx' then the SDK automatically sets brackets according to GS1 AI (Application Identifiers).";
            restriction += "\r\nIf a value comes in form like (xx)yyyyyy(zz)nnn then the SDK do NOT verifies the value against AI.";
            restriction += "\r\n\r\nHowever you may verify if the value is valid according to GS1 rules or not by using Barcode.ValueIsValidGS1() bool function";
            return restriction;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "1112345617ABCDEF";
        }

        protected override DataMatrixCompactionMode CompactionMode
        {
            get { return DataMatrixCompactionMode.Ascii; }
        }

        protected override byte[] getDataForEncoding()
        {
            //return Encoding.ASCII.GetBytes(GS1ValueChecker.GetStripped(Value));

            string ss = GS1ValueChecker.GetStripped(Value);

            byte[] bbytes = new byte[ss.Length];

            int index = 0;
            foreach (char c in ss.ToCharArray())
            {
                bbytes[index++] = (byte)c;
            }
            return bbytes;
        }

        protected override DataMatrixCompactionMode[] getDefaultCompactionModes()
        {
            DataMatrixCompactionMode[] compactionModes = null;
            if (CompactionMode != DataMatrixCompactionMode.Auto)
            {
                compactionModes = new DataMatrixCompactionMode[m_data.Length + 1];
                for (int i = 0; i < compactionModes.Length; i++)
                    compactionModes[i] = CompactionMode;
            }

            return compactionModes;
        }

        protected override void initCodeWords(byte[] codewords, ref int codewordIndex)
        {
            codewords[codewordIndex++] = 232; // FNC1
        }

        protected override void produceAsciiCodewords(ref DataMatrixCompactionMode mode, DataMatrixCompactionMode newMode, ref int dataIndex, ref int codewordIndex, int codewordCountLimit, byte[] codewords)
        {
            try
            {

                if (mode != newMode)
                {
                    if (mode == DataMatrixCompactionMode.C40 || mode == DataMatrixCompactionMode.Text || mode == DataMatrixCompactionMode.X12)
                        codewords[codewordIndex++] = 254;	// escape C40/text/X12
                    else
                        codewords[codewordIndex++] = 0x7C;	// escape EDIFACT
                }

                mode = DataMatrixCompactionMode.Ascii;
                if (m_data.Length - dataIndex >= 2 && isDigit(m_data[dataIndex]) && isDigit(m_data[dataIndex + 1]))
                {
                    codewords[codewordIndex++] = (byte)((m_data[dataIndex] - '0') * 10 + m_data[dataIndex + 1] - '0' + 130);
                    dataIndex += 2;
                }
                else if (m_data[dataIndex] > 127)
                {
                    codewords[codewordIndex++] = 235;
                    codewords[codewordIndex++] = (byte)(m_data[dataIndex++] - 127);
                }
                else
                {
                    if ((char)m_data[dataIndex] == '(')
                    {
                        codewords[codewordIndex++] = 232; // FNC1
                        dataIndex++;
                    }
                    else
                    {
                        codewords[codewordIndex++] = (byte)(m_data[dataIndex++] + 1);
                    }
                }
            }
            catch { 
                // suppress exception
            }
        }
    }
}
