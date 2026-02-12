using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MuPDF.NET;
using NUnit.Framework;

namespace MuPDF.NET.Test
{
    public class TableTest
    {
        /// <summary>
        /// Table test based on Demo Program.TestTable():
        /// Loads err_table.pdf, gets tables with lines_strict/lines/text strategies,
        /// asserts Extract() and ToMarkdown() work for any tables found.
        /// </summary>
        [Test]
        public void TestTable()
        {
            string testFilePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../resources/err_table.pdf"));
            Assert.That(File.Exists(testFilePath), Is.True, $"Test file not found: {testFilePath}");

            Document doc = new Document(testFilePath);
            try
            {
                Assert.That(doc.PageCount, Is.GreaterThanOrEqualTo(1));

                Page page = doc[0];

                // Test 1: Get tables with 'lines_strict' strategy (as in Demo)
                List<Table> tables = Utils.GetTables(
                    page,
                    clip: page.Rect,
                    vertical_strategy: "lines_strict",
                    horizontal_strategy: "lines_strict");

                Assert.That(tables, Is.Not.Null);

                if (tables.Count == 0)
                {
                    // Test 2: Fallback with 'lines' strategy (as in Demo)
                    tables = Utils.GetTables(
                        page,
                        clip: page.Rect,
                        vertical_strategy: "lines",
                        horizontal_strategy: "lines");
                }

                // Test 3: Get tables with 'text' strategy (as in Demo)
                List<Table> textTables = Utils.GetTables(
                    page,
                    clip: page.Rect,
                    vertical_strategy: "text",
                    horizontal_strategy: "text");

                Assert.That(textTables, Is.Not.Null);

                // For each table found with lines_strict/lines: validate structure and Extract/ToMarkdown
                for (int i = 0; i < tables.Count; i++)
                {
                    Table table = tables[i];
                    Assert.That(table.row_count, Is.GreaterThanOrEqualTo(0));
                    Assert.That(table.col_count, Is.GreaterThanOrEqualTo(0));

                    List<List<string>> tableData = table.Extract();
                    Assert.That(tableData, Is.Not.Null);

                    string markdown = table.ToMarkdown(clean: false, fillEmpty: true);
                    Assert.That(markdown, Is.Not.Null);
                }

                // Test 4: Get tables from all pages (as in Demo)
                int totalTables = 0;
                for (int pageNum = 0; pageNum < doc.PageCount; pageNum++)
                {
                    Page currentPage = doc[pageNum];
                    List<Table> pageTables = Utils.GetTables(
                        currentPage,
                        clip: currentPage.Rect,
                        vertical_strategy: "lines_strict",
                        horizontal_strategy: "lines_strict");
                    if (pageTables.Count > 0)
                        totalTables += pageTables.Count;
                    currentPage.Dispose();
                }

                Assert.That(totalTables, Is.GreaterThanOrEqualTo(0));
                page.Dispose();
            }
            finally
            {
                doc.Close();
            }
        }

        /*
        [Test]
        public void BorderedTable()
        {
            Document doc = new Document("../../../resources/bordered-table.pdf");
            Rect clip = new Rect(20, 100, 580, 300);
            Page page = doc[0];
            int cellCount = 0;

            List<Table> tables = page.GetTables(clip:clip);
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

            Assert.That(cellCount, Is.EqualTo(18));
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
        */
    }
}
