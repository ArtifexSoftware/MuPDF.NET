using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MuPDF.NET.Test
{
    public class TableTest
    {
        [Test]
        public void BorderedTable()
        {
            Document doc = new Document("../../../resources/bordered-table.pdf");
            Page page = doc[0];
            int cellCount = 0;

            List<Table> tables = page.GetTables();
            foreach (var table in tables)
            {
                List<List<string>> text = table.Extract();
                foreach (var row in text)
                {
                    foreach (var cell in row)
                    {
                        cellCount++;
                    }
                }
            }

            doc.Close();

            Assert.That(cellCount, Is.EqualTo(186));
        }

        [Test]
        public void NonBorderedTable()
        {
            Document doc = new Document("../../../resources/non-bordered-table.pdf");
            Page page = doc[0];
            int cellCount = 0;

            List<Table> tables = page.GetTables(vertical_strategy: "text", horizontal_strategy: "text");
            foreach (var table in tables)
            {
                List<List<string>> text = table.Extract();
                foreach (var row in text)
                {
                    foreach (var cell in row)
                    {
                        cellCount++;
                    }
                }
            }

            doc.Close();

            Assert.That(cellCount, Is.EqualTo(54));
        }
    }
}
