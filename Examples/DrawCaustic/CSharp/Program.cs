// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

(float, float) pvon(double a)
{
    return ((float)Math.Cos(a), (float)Math.Sin(a));
}

(float, float) pbis(double a)
{
    return ((float)Math.Cos(3 * a - Math.PI), (float)(Math.Sin(3 * a - Math.PI)));
}

string prefix = "output";
float[] coffee = Utils.GetColor("coffee");
float[] yellow = Utils.GetColor("yellow");
float[] blue = Utils.GetColor("blue");

Document doc = new Document();
Page page = doc.NewPage(-1, 800, 800);
Point center = new Point(page.Rect.Width / 2, page.Rect.Height / 2);

float radius = page.Rect.Width / 2;

Shape img = page.NewShape();
img.DrawCircle(center, radius);
img.Finish(color: coffee, fill: coffee);

int count = 200;
double interval = Math.PI / count;
for (int i = 1; i < count; i++)
{
    double a = -Math.PI / 2 + i * interval;
    
    (float x, float y) = pvon(a);
    Point von = new Point(x, y) * radius + center;

    (x, y) = pbis(a);
    Point bis = new Point(x, y) * radius + center;
    img.DrawLine(von, bis);
}

img.Finish(width: 1, color: yellow, closePath: true);

img.DrawCircle(center, radius);
img.Finish(color: blue);
page.SetCropBox(img.Rect);
img.Commit();

doc.Save(prefix + ".pdf");

doc.GetPagePixmap(0, new IdentityMatrix()).Save(prefix + ".png");
string svg = page.GetSvgImage();
File.WriteAllText(prefix + ".svg", svg);