// See https://aka.ms/new-console-template for more information

using MuPDF.NET;

Document doc = new Document();
Page page = doc.NewPage(width: 500, height: 500);
Point center = (page.Rect.TopLeft + page.Rect.BottomRight) / 2.0f;
float radius = 200.0f;
int n = 523;
int curve = 2;

Point p0 = center - new Point(radius, 0);
float theta = -360.0f / n;

float[] stroke = new float[3] { 1, 0, 0 };
float[] fill = new float[3] { 0, 1, 0 };
float[] border = new float[3] { 0, 0, 1 };

Shape shape = page.NewShape();
shape.DrawCircle(center, radius);
shape.Finish(color: border, fill: fill, width: 1);


List<Point> points = new List<Point>(new Point[] { p0, });
Point point = p0;
for (int i = 1; i < n; i++)
{
    point = shape.DrawSector(center, point, theta, true);
    points.Add(point);
}

shape.DrawCont = "";

for (int i = 0; i < n; i++)
{
    int tar = curve * i % n;
    shape.DrawLine(points[i], points[tar]);
}

shape.Finish(color: stroke, width: 0.2f);
shape.Commit();
doc.Save("e://res/output.pdf", deflate: 1);
