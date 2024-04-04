using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class MuPDFDocumentTest : PdfTestBase
    {
        [SetUp]
        public void Setup()
        {
            doc = new MuPDFDocument("input.pdf");
            page = new MuPDFPage(doc.GetPage(0), doc);
        }
    }
}
