// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document doc = new Document();
float[] red = new float[3] { 1, 0, 0 };
float[] blue = new float[3] { 0, 0, 1 };
Page page = doc.NewPage(width: 400, height: 300);
Rect r = page.Rect + new Rect(4, 4, -4, -4);
Quad q = r.Quad;
float f = 0.0f / 100.0f;

float u, o;
if (f >= 0)
{
    u = f;
    o = 0;
}
else
{
    u = 0;
    o = -f;
}
Quad q1 = new Quad(
    q.UpperLeft + (q.UpperRight - q.UpperLeft) * o,
    q.UpperLeft + (q.UpperRight - q.UpperLeft) * (1 - o),
    q.LowerLeft + (q.LowerRight - q.LowerLeft) * u,
    q.LowerLeft + (q.LowerRight - q.LowerLeft) * (1 - u)
    );

float c1 = Math.Min(1, Math.Max(o, u));
float c3 = Math.Min(1, Math.Max(1 - u, 1 - o));
float[] fill = new float[3] { c1, 0, c3 };
Shape img = page.NewShape();
img.DrawOval(q1);
img.Finish(color: blue, fill: fill, width: 0.3f);

img.DrawCircle(q1.LowerLeft, 4);
img.DrawCircle(q1.UpperLeft, 4);
img.Finish(fill: red);

img.DrawCircle(q1.UpperRight, 4);
img.DrawCircle(q1.LowerRight, 4);
img.Finish(fill: blue);
img.Commit();

doc.Save("output.pdf");
