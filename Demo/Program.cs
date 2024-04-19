using MuPDF.NET;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            MuPDFDocument doc = new MuPDFDocument("input.pdf");

            for (int i = 0; i < doc.Len; i++)
            {
                if (doc[i].ApplyRedactions())
                    Console.WriteLine(i);
            }

            doc.Save("output.pdf");

            /*Stopwatch sw = Stopwatch.StartNew();

            sw.Start();

            byte[] data = doc.Write();

            sw.Stop();

            Console.WriteLine(sw.ElapsedMilliseconds);*/

        }
    }
}
