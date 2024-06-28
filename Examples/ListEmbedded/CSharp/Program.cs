// See https://aka.ms/new-console-template for more information

using MuPDF.NET;

Document doc = new Document("input.pdf");

int nameLen = 0;
int filenameLen = 0;
int totalLen = 0;
int totalSize = 0;

List<(string, string, int, int)> efList = new List<(string, string, int, int)> (); ;

for (int i = 0; i < doc.GetEmbfileCount(); i++)
{
    EmbfileInfo info = doc.GetEmbfileInfo(i);
    efList.Add((info.Name, info.FileName, info.Length, info.Size));
    nameLen = Math.Max(info.Name.Length, nameLen);
    filenameLen = Math.Max(info.FileName.Length, filenameLen);
    totalLen += info.Length;
    totalSize += info.Size;
}

if (efList.Count < 1)
    Console.WriteLine("no embedded files in input.pdf");

float ratio = totalSize / (float)totalLen;
float saves = 1 - ratio;

string header = String.Format("{0,-" + (nameLen + 4) + "}{1,-" + (filenameLen + 4) + "}{2,10}{3,11}",
    "Name", "Filename", "Length", "Size");

string line = new string('-', header.Length);

foreach (var info in efList) // Replace ef_list with actual list
{
    Console.WriteLine(String.Format("{0,-" + (nameLen + 3) + "}{1,-" + (filenameLen + 3) + "}{2,10}{3,10}",
        info.Item1, info.Item2, info.Item2, info.Item4));
}

Console.WriteLine($"{efList.Count} embedded files in 'input.pdf'. Totals:");

Console.WriteLine($"File lengths: {totalLen}, compressed: {totalSize}, ratio: {Math.Round(ratio * 100, 2)}% (savings: {Math.Round(saves * 100, 2)}%).");
Console.WriteLine(line);
