// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document doc = new Document();
Page page = doc.NewPage();
Shape img = page.NewShape();

int nedge = 5;
int breadth = 2;
float beta = -1.0f * 360 / nedge;
Point center = new Point(300, 300);
Point p0 = new Point(300, 200);
Point p1 = p0;
List<Point> points = new List<Point>() { p0, };

for (int i = 0; i < nedge - 1; i ++)
{
    p0 = img.DrawSector(center, p0, beta);
    points.Add(p0);
}

img.DrawCont = "";

points.Add(p1);
for (int i = 0; i < nedge; i++)
    img.DrawSquiggle(points[i], points[i + 1], breadth: breadth);

img.Finish(color: new float[3] { 0f, 0f, 1f}, fill: new float[3] {1, 1, 0}, closePath: false);
page.SetCropBox(img.Rect);
img.Commit();

doc.Save("output.pdf");

File.WriteAllText("output.svg", page.GetSvgImage());
