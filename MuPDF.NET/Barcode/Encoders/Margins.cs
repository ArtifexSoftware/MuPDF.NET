/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2010 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;
using System.Globalization;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Class that describes barcode margins.
    /// </summary>
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[TypeConverter(typeof(ExpandableObjectConverter))]
    public class Margins : ICloneable, IMargins
    {
        private int _left;
        private int _top;
        private int _right;
        private int _bottom;

        /// <summary>
        /// Occurs when margins get changed.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Raises the <see cref="E:Changed"/> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        protected virtual void FireChanged(object sender)
        {
            Changed?.Invoke(sender, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Margins"/> class.
        /// </summary>
        public Margins()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Margins"/> struct.
        /// </summary>
        /// <param name="left">The left margin.</param>
        /// <param name="top">The top margin.</param>
        /// <param name="right">The right margin.</param>
        /// <param name="bottom">The bottom margin.</param>
        public Margins(int left, int top, int right, int bottom)
        {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Margins"/> class.
        /// </summary>
        /// <param name="value">The string representing margins in the form "[0;0;0;0]".</param>
        public Margins(string value)
        {
            string[] tokens = value.Split(new char[] { '[', ']', ';' });
            if (tokens.Length != 6 || tokens[0].Length != 0 || tokens[5].Length != 0)
                throw new BarcodeException("Invalid string representation of Margins object.");

            Left = toInt(tokens[1]);
            Top = toInt(tokens[2]);
            Right = toInt(tokens[3]);
            Bottom = toInt(tokens[4]);
        }

        /// <summary>
        /// Converts string to an int value.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns>Conversion result.</returns>
        private static int toInt(string value)
        {
            return int.Parse(value, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:BarcodeWriter.Core.Margins"/>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:BarcodeWriter.Core.Margins"/>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:BarcodeWriter.Core.Margins"/>; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
        public override bool Equals(object obj)
        {
            Margins m = (Margins)obj;

            if (m.Bottom != Bottom || m.Left != Left || m.Right != Right || m.Top != Top)
                return false;

            return true;
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:BarcodeWriter.Core.Margins"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:BarcodeWriter.Core.Margins"/>.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(Left.ToString());
            sb.Append(";");
            sb.Append(Top.ToString());
            sb.Append(";");
            sb.Append(Right.ToString());
            sb.Append(";");
            sb.Append(Bottom.ToString());
            sb.Append("]");

            return sb.ToString();
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <inheritdoc />
        [Description("The left margin in pixels.")]
        public int Left
        {
            get => _left;
            set
            {
                _left = value;
                FireChanged(this);
            }
        }

        /// <inheritdoc />
        [Description("The top margin in pixels.")]
        public int Top
        {
            get => _top;
            set
            {
                _top = value;
                FireChanged(this);
            }
        }

        /// <inheritdoc />
        [Description("The right margin in pixels.")]
        public int Right
        {
            get => _right;
            set
            {
                _right = value;
                FireChanged(this);
            }
        }

        /// <inheritdoc />
        [Description("The bottom margin in pixels.")]
        public int Bottom
        {
            get => _bottom;
            set
            {
                _bottom = value;
                FireChanged(this);
            }
        }
    }
}
