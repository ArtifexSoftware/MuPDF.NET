using System.Collections.Generic;

namespace MuPDF.NET
{
    /// <summary>
    /// Return value for <see cref="Page.InsertTextbox"/> /
    /// Supports legacy <c>float</c> assignment and tuple deconstruction <c>(rc, rest)</c>.
    /// </summary>
    public readonly struct InsertTextboxResult
    {
        public InsertTextboxResult(float rc, List<string> rest)
        {
            Rc = rc;
            Rest = rest ?? new List<string>();
        }

        /// <summary>Spare height, or a negative value if the text did not fit.</summary>
        public float Rc { get; }

        /// <summary>Lines that did not fit in the box.</summary>
        public List<string> Rest { get; }

        public static implicit operator float(InsertTextboxResult result) => result.Rc;

        public void Deconstruct(out int rc, out List<string> rest)
        {
            rc = Rc < 0 ? -1 : (int)Rc;
            rest = Rest;
        }
    }
}