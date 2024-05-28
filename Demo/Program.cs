using MuPDF.NET;
using System.Text;

namespace Demo
{
    class Program
    {

        static void Main(string[] args)
        {
            Document doc = new Document();
            Page page = doc.NewPage();
            Rect rect = new Rect(50, 50, 400, 500);

            string text = "Der Kleine Schwertwal (Pseudorca crassidens), auch bekannt als Unechter oder Schwarzer Schwertwal, ist eine Art der Delfine (Delphinidae) und der einzige rezente Vertreter der Gattung Pseudorca.\r\n\r\nEr ähnelt dem Orca in Form und Proportionen, ist aber einfarbig schwarz und mit einer Maximallänge von etwa sechs Metern deutlich kleiner.\r\n\r\nKleine Schwertwale bilden Schulen von durchschnittlich zehn bis fünfzig Tieren, wobei sie sich auch mit anderen Delfinen vergesellschaften und sich meistens abseits der Küsten aufhalten.\r\n\r\nSie sind in allen Ozeanen gemäßigter, subtropischer und tropischer Breiten beheimatet, sind jedoch vor allem in wärmeren Jahreszeiten auch bis in die gemäßigte bis subpolare Zone südlich der Südspitze Südamerikas, vor Nordeuropa und bis vor Kanada anzutreffen.";
            int ocg = doc.AddOcg("ocg1");
            float[] blue = Utils.GetColor("lightblue");
            float[] red = Utils.GetColorHSV("red");
            page.InsertTextbox(
                rect,
                text,
                align: (int)TextAlign.TEXT_ALIGN_LEFT,
                fontSize: 12,
                color: blue,
                oc: ocg,
                fontName: "kenpixel",
                fontFile: "kenpixel.ttf");


            doc.Save("output.pdf");
        }
    }
}
