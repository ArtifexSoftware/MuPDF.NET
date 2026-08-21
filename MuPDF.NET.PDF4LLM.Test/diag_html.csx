using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MuPDF.NET;
using MuPDF.NET.PDF4LLM;
using MuPDF.NET.PDF4LLM.Helpers;
using MuPDF.NET.PDF4LLM.Layout;

var pdf = args[0];
Console.WriteLine($"IsAvailable={PyMuPdfLayout.IsAvailable}");
Console.WriteLine($"UseLayout before={MuPDF4LLM.UseLayout}");
MuPDF4LLM.SetUseLayout(true);
Console.WriteLine($"UseLayout after={MuPDF4LLM.UseLayout}");

var calls = new List<ParseDocumentCallInfo>();
DocumentLayout.ParseDocumentObserver = info => {
    calls.Add(info);
    Console.WriteLine($"ParseDocument renderHtmlTables={info.RenderHtmlTables}");
};

string md = MuPDF4LLM.ToMarkdown(pdf, pages: new List<int> { 0 }, tableOutput: "html", useOcr: false);
Console.WriteLine($"table tags={Regex.Matches(md, "<table").Count}");
Console.WriteLine($"has md table={md.Contains("| --- |")}");
Console.WriteLine($"md len={md.Length}");
Console.WriteLine("--- md excerpt ---");
Console.WriteLine(md.Length > 800 ? md.Substring(0, 800) : md);
Console.WriteLine($"calls={calls.Count}");
