// See https://aka.ms/new-console-template for more information


using mupdf;
using MuPDF.NET;

internal class Program
{
    static void Main(string[] args)
    {
        float dimlimit = 0;
        float relsize = 0;
        float abssize = 0;

        ImageInfo RecoverPix(Document doc, Entry item)
        {
            int xref = item.Xref;
            int smask = item.Smask;
            
            if (smask > 0)
            {
                Pixmap pix0 = new Pixmap(doc.ExtractImage(xref).Image);
                if (pix0.Alpha != 0)
                {
                    pix0 = new Pixmap(pix0, 0);
                }
                Pixmap mask = new Pixmap(doc.ExtractImage(smask).Image);
                Pixmap pix = new Pixmap(doc.ExtractImage(xref).Image);

                string ext = "";
                if (pix0.N > 3)
                    ext = "pam";
                else
                    ext = "png";

                return new ImageInfo()
                {
                    Ext = ext,
                    ColorSpace = pix.ColorSpace.N,
                    Image = pix.ToBytes(ext)
                };
            }

            if (doc.GetXrefObject(xref, compressed: 1).Contains("/ColorSpace"))
            {
                Pixmap pix = new Pixmap(doc, xref);
                pix = new Pixmap(Utils.csRGB, pix);
                return new ImageInfo()
                {
                    Ext = "png",
                    ColorSpace = 3,
                    Image = pix.ToBytes("png")
                };
            }
            return doc.ExtractImage(xref);
        }

        Document doc = new Document(args[0]);
        int pageCount = doc.PageCount;

        List<int> xrefList = new List<int>();
        List<int> imgList= new List<int>();
        for (int i = 0; i < pageCount; i ++)
        {
            List<Entry> il = doc.GetPageImages(i);
            imgList = il.Select(i => i.Xref).ToList();
            foreach (Entry img in il)
            {
                int xref = img.Xref;
                if (xrefList.Contains(xref))
                    continue;
                float width = img.Width;
                float height = img.Height;
                if (Math.Min(width, height) <= dimlimit)
                    continue;

                ImageInfo image = RecoverPix(doc, img);
                int n = image.ColorSpace;
                byte[] imgData = image.Image;

                if (imgData.Length <= abssize)
                    continue;
                if (imgData.Length / (width * height * n) <= relsize)
                    continue;

                File.WriteAllBytes($"img{xref}.png", imgData);
            }
        }
    }
}
