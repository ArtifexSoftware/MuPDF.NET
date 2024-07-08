// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Pixmap pix0 = new Pixmap("input.jpg");
ColorSpace tarCS = pix0.ColorSpace;
float tarWidth = pix0.W * 3;
float tarHeight = pix0.H * 4;
IRect tarIrect = new IRect(0, 0, tarWidth, tarHeight);
Pixmap tarPix = new Pixmap(tarCS, tarIrect, pix0.Alpha);
tarPix.ClearWith(90);

for (int i = 0; i < 4; i++)
{
    float y = i * pix0.H;
    for (int j = 0; j < 3; j++)
    {
        float x = j * pix0.W;
        pix0.SetOrigin((int)x, (int)y);
        tarPix.Copy(pix0, pix0.IRect);
        tarPix.Save($"./output/target-{i}{j}.png");
    }
}
