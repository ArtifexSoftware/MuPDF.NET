using MuPDF.NET;

PDFRenderer renderer = new PDFRenderer();
renderer.GeneratePDF();

public class PDFRenderer
{
    private readonly MuPDF.NET.Font _font = new("helv");
    private readonly MuPDF.NET.Font _fontBold = new("hebo");
    private readonly int _defaultFontSize = 8;
    private readonly int _headerFontSize = 12;

    /* Margins */
    private readonly int _defaultMarginVertical = 12;
    private readonly int _defaultMarginHorizontal = 12;

    private readonly MuPDF.NET.Point _pageStart = new(50, 72);

    private readonly string _HTML = "<html><body style=\"text-align:Left;font-family:Segoe UI;font-style:normal;font-weight:normal;font-size:12;color:#000000;\"><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span /></p><p><span>Colten - line break</span></p><p><span /></p><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span> </span></p><p><span>Colten - line break</span></p><p><span /></p><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span /></p><p><span>Colten - line break</span></p><p><span /></p><p><span>生产准备：</span></p><p><span>1. 每日生产进行维护保养，请参照并填写Philips 自动螺丝起点检表《WI-Screw assembly-Makita DF010&amp;Kilews </span></p><p><span>SKD-B512L-F01》</span></p><p><span>2 .扭力计UNIT选择‘lbf.in’，‘P-P’模式，每四小时检查一次，每次检查5组数据，只有合格才可以生产；并填写</span></p><p><span>力扭矩记录表，表单号 ： F-EN-34 。</span></p><p><span>1.电动起子力矩： 5&#177;1 in-lbs，电动螺丝起编号：5.0。</span></p><p><span>2.电动起子力矩：10&#177;1 in-lbs，电动螺丝起编号：10.0。</span></p><p><span>3.电动起子力矩：12&#177;1 in-lbs，电动螺丝起编号：12.0。</span></p><p><span /></p></body></html>";


    struct TextBox
    {
        public required string Label;
        public required float LabelLength;
        public required string Content;
        public required float ContentLength;
    }

    /// <summary>
    /// Describes which direction a textbox group should be rendered.
    /// </summary>
    enum TextboxGroupDirection
    {
        Horizontal,
        Vertical
    }

    /* Define colors. MuPDF expects rbg values to be unit values (i.e., 0-1).*/
    private readonly float[] _gray = ConvertToURGB(211, 211, 211);
    private readonly float[] _pastelBlue = ConvertToURGB(167, 199, 231);

    /// <summary>
    /// This method simply converts RGB values to Unit RGB.
    /// </summary>
    /// <param name="red"></param>
    /// <param name="green"></param>
    /// <param name="blue"></param>
    /// <returns></returns>
    private static float[] ConvertToURGB(int red, int green, int blue)
        => [red / (float)255, green / (float)255, blue / (float)255];

    /// <summary>
    /// Calculates the center of <c>Rect</c> Y axis including
    /// an offset for the font height.
    /// </summary>
    /// <param name="y0"></param>
    /// <param name="y1"></param>
    /// <param name="fontHeight"></param>
    /// <returns></returns>
    private static float CenterFontY(float y0, float y1, float fontHeight)
        => ((y0 + y1) / 2) + (fontHeight / 2f) - 2;

    private string ResizeContent(string txt, float maxWidth)
    {
        var ellipsis = "...";
        var ellipsisLen = _font.TextLength(ellipsis);

        var newContent = "";
        foreach (var c in txt)
        {
            var len = _font.TextLength(newContent, fontSize: _defaultFontSize);
            if (len + ellipsisLen >= maxWidth)
            {
                newContent += ellipsis;
                break;
            }

            newContent += c;
        }

        return newContent;
    }


    /// <summary>
    /// Helper function to create a Textbox struct.
    /// It will calculate the label length and content length.
    /// </summary>
    /// <param name="lbl"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    private TextBox CreateTextbox(string lbl, string content)
    {
        var labelLen = _font.TextLength(lbl, fontSize: _defaultFontSize);
        var contentLen = _font.TextLength(content, fontSize: _defaultFontSize);
        return new TextBox
        {
            Label = lbl,
            LabelLength = labelLen,
            Content = content,
            ContentLength = contentLen,
        };
    }

    public void GeneratePDF()
    {
        Console.WriteLine("GeneratePDF");

        Document doc = new Document(); // empty output PDF
        Page page = doc.NewPage(-1);
        MuPDF.NET.TextWriter writer = new MuPDF.NET.TextWriter(page.Rect);

        var point = DrawProgramInfoRect(page, writer);

        /* Now onto the steps */
        point.Y += (_headerFontSize * 1.2f);

        writer.Append(point, "Steps", _fontBold, fontSize: _headerFontSize);

        var html = _HTML;
        Story descStory = new(html);

        var storyWidth = page.Rect.Width - point.X;
        var fit = descStory.FitHeight(storyWidth, origin: new Point(0, 0));

        bool overflow;
        float filledHeight;
        if (fit.Filled is Rect filled)
        {
            while (true)
            {
                (point, overflow, filledHeight) = DrawStepDescription(page, point, filled, descStory);

                if (!overflow) break;

                filled.Y1 -= filledHeight;

                /* Need a new page since we have overflow. Before 
                 * making the new page, need to finish writing everything 
                 * on the previous page. */
                writer.WriteText(page);
                page = doc.NewPage(-1);
                writer = new(page.Rect);
                point.X = _pageStart.X;
                point.Y = _pageStart.Y;
            }
        }

        point.X = _pageStart.X;
        point.Y += _defaultMarginVertical;

        point.Y += _headerFontSize * 1.2f;

        writer.Append(point, "Components", _fontBold, _headerFontSize);

        point.Y += _defaultMarginVertical;


        writer.WriteText(page);

        page = doc.NewPage(-1);
        writer = new(page.Rect);

        point.X = _pageStart.X;
        point.Y = _pageStart.Y;


        doc.Save("from_html.pdf", garbage: 4, deflate: 1, useObjstms: 1, deflateImages: 1);

        

    }

    /// <summary>
    /// Adds a list of textboxes to the page. They will be aligned either
    /// vertically or horizontally depening on the direction.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="textboxes"></param>
    /// <param name="start"></param>
    /// <param name="dir"></param>
    /// <param name="textboxWidth"></param>
    /// <returns></returns>IEnumerable<KeyValuePair<string, TextBox>>
    private Point DrawTextboxGroup(Page page, MuPDF.NET.TextWriter writer, IEnumerable<KeyValuePair<string, TextBox>> textboxes,
        Point start, TextboxGroupDirection dir, float textboxWidth, float maxLabelLen = -1)
    {
        var x0 = start.X;
        var y0 = start.Y;
        var fontHeight = _defaultFontSize * 1.2f;
        if (maxLabelLen < 0)
        {
            maxLabelLen = textboxes.Max(l => l.Value.LabelLength);
        }

        //MuPDF.NET.TextWriter writer = new(page.Rect);

        foreach (var tb in textboxes)
        {
            var centerP = new Point(x0, CenterFontY(y0, y0 + fontHeight, fontHeight));

            writer.Append(centerP, tb.Value.Label, _font, _defaultFontSize);

            /* Give the textbox some breathing room. */
            var textboxMargin = 5;
            var tbWidth = textboxWidth - textboxMargin;
            x0 = x0 + maxLabelLen + textboxMargin;

            var textboxRect = new Rect(x0, y0, x0 + tbWidth, y0 + fontHeight);
            page.DrawRect(textboxRect, color: _gray, fill: _gray, fillOpacity: .25f, dashes: "[5 .2] 0");

            centerP.X = x0;
            centerP.Y = CenterFontY(textboxRect.TopLeft.Y, textboxRect.BottomLeft.Y, fontHeight);

            /* Resize the content if it doesn't fit in the textbox */
            var content = tb.Value.Content;
            if (tb.Value.ContentLength > tbWidth)
            {
                content = ResizeContent(tb.Value.Content, tbWidth);
            }

            writer.Append(centerP, content, _font, _defaultFontSize);

            if (dir == TextboxGroupDirection.Horizontal)
            {
                x0 = textboxRect.TopRight.X + 5;
                y0 = start.Y;
            }
            else
            {
                x0 = start.X;
                y0 = textboxRect.BottomLeft.Y;
            }
        }

        return new Point(x0, y0);
    }

    /// <summary>
    /// Draw the Program Information box along with all related
    /// program metadata.
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    private Point DrawProgramInfoRect(Page page, MuPDF.NET.TextWriter writer)
    {
        var pageRect = page.Rect;
        var fontHeight = _defaultFontSize * 1.2f;

        /* Define all labels before hand to determine 
         * start point of the textboxes. */
        Dictionary<string, TextBox> textboxes = new()
        {
            /* LHS */
            {
                "Customer",
                CreateTextbox("Customer", "program.Customer!.Name")
            },
            {
                "DC",
                CreateTextbox("DC", "program.ProgramDict.DC")
            },
            {
                "OMD",
                CreateTextbox("OMD", "program.ProgramDict.OMD")
            },
            {
                "Version",
                CreateTextbox("Version", "V{program.ProgramDict.Version}")
            },
            {
                "LastChangedBy",
                CreateTextbox("LastChangedBy", "program.ProgramDict.LastModifiedBy?.Username")
            },
            {
                "Submitted",
                CreateTextbox("Submitted", "program.ProgramDict.Submitted?.ToString() ??")
            },

            /* RHS */
            {
                "Assembly",
                CreateTextbox("Assembly", "program.Assembly!.Name")
            },
            {
                "Previous DC",
                CreateTextbox("PreviousDC", "program.ProgramDict.PreviousDC")
            },
            {
                "ReleaseStatus",
                CreateTextbox("ReleaseStatus", "program.ProgramDict.ReleaseStatus.ToString()")
            },
            {
                "PreviousVersion",
                CreateTextbox("PreviousVersion", "V{program.ProgramDict.PreviousVersion}")
            },
            {
                "LastChanged",
                CreateTextbox("LastChanged", "program.ProgramDict.LastModified.ToString() ?? ")
            },
            {
                "Released",
                CreateTextbox("Released", "program.ProgramDict.Released?.ToString() ?? ")
            },
        };

        writer.Append(new Point(50, 72), "l[ProgramInformation]", _fontBold, _headerFontSize);

        var infoY = 72 + fontHeight;
        var programInfoRect = new Rect(50, infoY, pageRect.Width - 50, infoY + 80);
        page.DrawRect(programInfoRect, color: _pastelBlue, dashes: "[5 .2] 0");

        float textboxWidth = 125;

        float startX, x0, startY, y0;
        x0 = startX = programInfoRect.X0 + 5;
        y0 = startY = programInfoRect.Y0 + 5;

        var programNameDict = new Dictionary<string, TextBox>()
        {
            {
                "ProgramName",
                CreateTextbox("ProgramName", "program.ProgramDict.Name")
            }
        };

        var point = new Point(x0, y0);

        var lhs = textboxes.Take(6);
        var maxLabelLen = float.Max(lhs.Max(l => l.Value.LabelLength), programNameDict["ProgramName"].LabelLength);

        /* SPECIAL CASE: The ProgramName field needs to span the entire width
         * of the programInfoRect. All the other fields will split into two columns.*/
        var programNameTextboxWidth = programInfoRect.X1 - 60 - maxLabelLen;
        DrawTextboxGroup(page, writer, programNameDict, point,
            TextboxGroupDirection.Vertical, programNameTextboxWidth, maxLabelLen);
        point.Y = startY + fontHeight;

        DrawTextboxGroup(page, writer, lhs, point,
            TextboxGroupDirection.Vertical, textboxWidth, maxLabelLen);

        var rhs = textboxes.Skip(6);
        maxLabelLen = rhs.Max(l => l.Value.LabelLength);

        point.X = programInfoRect.X1 - maxLabelLen - textboxWidth - 5;
        point.Y = startY + fontHeight;

        DrawTextboxGroup(page, writer, rhs, point, TextboxGroupDirection.Vertical, textboxWidth);
        return programInfoRect.BottomLeft;
    }

    private (Point point, bool overflow, float filledHeight) DrawStepDescription(Page page, Point start, Rect rect, Story story)
    {
        var pageRect = page.Rect;

        float x0 = start.X;
        float y0 = start.Y;

        bool overflow = false;

        using var ms = new MemoryStream();
        var docWriter = new DocumentWriter(ms);

        Rect descRect = new(x0, y0, rect.X1, y0);

        // Check if rectangle is going to overflow page
        var remHeight = pageRect.Height - (y0 + rect.Height) - 72;

        if (remHeight >= 0) // We have enough space on this page.
        {
            story.Place(rect);
            var device = docWriter.BeginPage(rect);
            story.Draw(device);

            descRect.Y1 += rect.Y1;
        }
        else // Not enough space on page, draw what we can
        {
            var fillHeight = pageRect.Height - y0 - 72;
            Rect? filled = new(0, 0, rect.Width, fillHeight);

            story.Place(filled);
            var device = docWriter.BeginPage(filled);
            story.Draw(device);

            descRect.Y1 += fillHeight;
            overflow = true;
        }

        docWriter.EndPage();
        docWriter.Close();

        var buf = ms.GetBuffer();
        var doc = new Document(stream: buf);

        page.ShowPdfPage(descRect, doc);

        page.DrawRect(descRect, color: _pastelBlue, dashes: "[5 .2] 0");

        return (new Point(x0, descRect.Y1), overflow, descRect.Height);
    }

}
