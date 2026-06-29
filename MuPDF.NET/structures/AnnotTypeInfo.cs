namespace MuPDF.NET
{
    /// <summary>
    /// MuPDF.NET <c>Annot.Type</c> as <c>(PdfAnnotType, string, string)</c> with legacy conversions.
    /// </summary>
    public readonly struct AnnotTypeInfo
    {
        public PdfAnnotType PdfType { get; }
        public string Name { get; }
        public string Intent { get; }

        /// <summary>Legacy tuple-style access (<c>Type.Item1</c>).</summary>
        public PdfAnnotType Item1 => PdfType;

        /// <summary>Legacy tuple-style access (<c>Type.Item2</c>).</summary>
        public string Item2 => Name;

        /// <summary>Legacy tuple-style access (<c>Type.Item3</c>).</summary>
        public string Item3 => Intent;

        public AnnotTypeInfo(PdfAnnotType pdfType, string name, string intent)
        {
            PdfType = pdfType;
            Name = name;
            Intent = intent;
        }

        public void Deconstruct(out PdfAnnotType pdfType, out string name, out string intent)
        {
            pdfType = PdfType;
            name = Name;
            intent = Intent;
        }

        public static implicit operator PdfAnnotType(AnnotTypeInfo t) => t.PdfType;

        public static implicit operator AnnotationType(AnnotTypeInfo t) => (AnnotationType)(int)t.PdfType;

        public static implicit operator int(AnnotTypeInfo t) => (int)t.PdfType;

        public static bool operator ==(AnnotTypeInfo left, AnnotationType right) =>
            (int)left.PdfType == (int)right;

        public static bool operator !=(AnnotTypeInfo left, AnnotationType right) =>
            !(left == right);

        public static bool operator ==(AnnotationType left, AnnotTypeInfo right) =>
            right == left;

        public static bool operator !=(AnnotationType left, AnnotTypeInfo right) =>
            right != left;

        public override string ToString() => Name ?? PdfType.ToString();

        public override bool Equals(object obj) =>
            obj is PdfAnnotType pt && PdfType == pt
            || obj is AnnotationType at && (int)PdfType == (int)at
            || obj is AnnotTypeInfo other && PdfType == other.PdfType && Name == other.Name && Intent == other.Intent;

        public override int GetHashCode() => ((int)PdfType, Name, Intent).GetHashCode();
    }
}
