using System.Text;

namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    enum BarCodeDataType
    {
		String, Numeric, Base256, Control, Base900
    }

    // Stores any type of data extracted from the barcode
#if CORE_DEV
    public
#else
    internal
#endif
    abstract class ABarCodeData
    {
        public abstract BarCodeDataType Type { get; }
        public new abstract string ToString();

        public virtual bool IsSimilar(ABarCodeData obj)
        {
            return obj.Type == this.Type;
        }

        protected bool RawDataEquals(int[] left, int[] right)
        {
            if (left == right)
                return true;

            if (left == null || right == null)
                return false;

            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        protected bool RawDataEquals(byte[] left, byte[] right)
        {
            if (left == right)
                return true;

            if (left == null || right == null)
                return false;

            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }
    }

	internal class StringBarCodeData : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.String;
            }
        }

        public string Value;

        public StringBarCodeData(string value)
        {
            Value = value;
        }

        public StringBarCodeData(byte[] value)
        {
            char[] userData = new char[value.Length];
            for (int i = 0; i < userData.Length; ++i)
            {
                userData[i] = (char) value[i];
            }
            Value = new string(userData);
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool IsSimilar(ABarCodeData obj)
        {             
            if (obj.GetType() != this.GetType() || !base.IsSimilar(obj))
                return false;

            return this.Value == (obj as StringBarCodeData).Value;
        }
    }

    internal class NumericBarCodeData : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.Numeric;
            }
        }

        public byte[] Value;

        public NumericBarCodeData(byte[] value)
        {
            Value = value;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte b in Value)
            {
                builder.Append(b.ToString());
            }

            return builder.ToString();
        }

        public override bool IsSimilar(ABarCodeData obj)
        {             
            if (obj.GetType() != this.GetType() || !base.IsSimilar(obj))
                return false;

            return RawDataEquals(this.Value, (obj as NumericBarCodeData).Value);
        }

    }

    internal class Base900BarCodeData : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.Base900;
            }
        }

        public int[] Value;

        public Base900BarCodeData(int[] value)
        {
            Value = value;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (int b in Value)
            {
                builder.Append("\\"+b.ToString().PadLeft(3,'0'));
            }

            return builder.ToString();
        }


        public override bool IsSimilar(ABarCodeData obj)
        {
            if (obj.GetType() != this.GetType() || !base.IsSimilar(obj))
                return false;

            return RawDataEquals(this.Value, (obj as Base900BarCodeData).Value);
        }
    }

	internal class Base256BarCodeData : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.Base256;
            }
        }

        public byte[] Value;
        public System.Text.Encoding encoding = null;

        public Base256BarCodeData(byte[] value)
        {
            Value = value;
        }

        public Base256BarCodeData(byte[] value, System.Text.Encoding encoding)
        {
            Value = value;
            this.encoding = encoding;
        }

        // This conversion routine cannot return the byte[] as string - string is UTF-8, and it would represent
        // non-ASCII chars in a weird way
        public override string ToString()
        {
            Encoding e = encoding ?? System.Text.Encoding.GetEncoding(28591); //default is iso-8859-1 (8-bit encoding)
            
            return encoding.GetString(Value);
        }

        public override bool IsSimilar(ABarCodeData obj)
        {
            if (obj.GetType() != this.GetType() || !base.IsSimilar(obj))
                return false;

            return RawDataEquals(this.Value, (obj as Base256BarCodeData).Value);
        }
    }

    #region Datamatrix control characters
    // These control chars represent various functions within the barcode itself. It is the user's task to 
    // interpret them, as their meaning is at higher level.

    class FNC1Symbol : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get 
            {
                return BarCodeDataType.Control;
            }
        }

        public override string ToString()
        {
            return "<FNC1>";
        }
    }

    class Macro05Symbol : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get 
            {
                return BarCodeDataType.Control;
            }
        }

        public override string ToString()
        {
            return "<Macro05>";
        }
    }

    class Macro06Symbol : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.Control;
            }
        }

        public override string ToString()
        {
            return "<Macro06>";
        }
    }

    // this tells that the barcode is used to program the reader
    class ReaderProgramSymbol : ABarCodeData
    {
        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.Control;
            }
        }

        public override string ToString()
        {
            return "<Reader program>";
        }
    }

    // describes the ECI in effect for the rest of the data
    class ECISwitchSymbol : ABarCodeData
    {
        public readonly int ECINumber;

        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.Control;
            }
        }

        public ECISwitchSymbol(int eci)
        {
            ECINumber = eci;
        }

        public override string ToString()
        {
            return "<ECI " + ECINumber + ">";
        }
    }

    // This tells that the current barcode is the Position-th of BarcodeCount Datamatrices.
    // The file Id is a user data, can be anything from 1-254
	internal class StructuredAppendSymbol : ABarCodeData
    {
        public readonly int Position;

        public readonly int BarcodeCount;

        public readonly int FileId1;

        public readonly int FileId2;

        public override BarCodeDataType Type
        {
            get
            {
                return BarCodeDataType.Control;
            }
        }

        public StructuredAppendSymbol(int position, int fileId1, int fileId2)
        {
            BarcodeCount = position%16;
            Position = position >> 4;

            FileId1 = fileId1;
            FileId2 = fileId2;
        }

        public override string ToString()
        {
            return "<" + Position + " of " + BarcodeCount + ", file1: " + FileId1 + ", file2: " + FileId2 + ">";
        }
    }
    #endregion
}
