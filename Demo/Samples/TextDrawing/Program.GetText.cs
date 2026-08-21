namespace Demo
{
    internal partial class Program
    {
        internal static void TestGetText()
        {
            Console.WriteLine("\n=== TestGetText =======================");

            string testFilePath = DemoPaths.Input("columns.pdf");
            Document doc = new Document(testFilePath);

            for (int i = 0; i < doc.PageCount; i++)
            {
                Page page = doc[i];
                var text = Utils.GetText(page, option: "dict");
                Console.WriteLine(text);
                page.Dispose();
            }

            doc.Close();
        }
    }
}
