using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET
{
    public class AnnotXref
    {
        public string Id { get; set; }

        public int Xref { get; set; }

        public PdfAnnotType AnnotType { get; set; }
    }
}
