namespace MuPDF.NET
{
    public enum WidgetType
    {
        Unknown = 0,
        Button = 1,
        CheckBox = 2,
        ComboBox = 3,
        ListBox = 4,
        RadioButton = 5,
        Signature = 6,
        Text = 7
    }

    /// <summary>
    /// Field type value accepting legacy <see cref="int"/> and PyMuPDF <see cref="WidgetType"/> assignments.
    /// </summary>
    public readonly struct WidgetFieldType : System.IEquatable<WidgetFieldType>
    {
        private readonly int _value;

        public WidgetFieldType(int value) => _value = value;

        public static implicit operator WidgetFieldType(int value) => new WidgetFieldType(value);

        public static implicit operator WidgetFieldType(WidgetType value) => new WidgetFieldType((int)value);

        public static implicit operator int(WidgetFieldType value) => value._value;

        public static implicit operator WidgetType(WidgetFieldType value) => (WidgetType)value._value;

        public static bool operator ==(WidgetFieldType left, int right) => left._value == right;

        public static bool operator ==(int left, WidgetFieldType right) => left == right._value;

        public static bool operator !=(WidgetFieldType left, int right) => left._value != right;

        public static bool operator !=(int left, WidgetFieldType right) => left != right._value;

        public bool Equals(WidgetFieldType other) => _value == other._value;

        public override bool Equals(object obj) => obj is WidgetFieldType other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();
    }
}
