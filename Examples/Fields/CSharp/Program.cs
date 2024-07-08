// See https://aka.ms/new-console-template for more information
using MuPDF.NET;

Document doc = new Document();
Page page = doc.NewPage();

float[] gold = { 1f, 1f, 0 };
float[] blue = { 0, 0, 1 };
float[] gray = { 0.9f, 0.9f, 0.9f };
float fontSize = 11;
float lineHeight = fontSize + 10;

Rect r11 = new Rect(50, 100, 200, 100 + lineHeight);
Rect r12 = r11 + new Rect(r11.Width + 2, 0, 2 * r11.Width + 2, 0);
page.InsertTextbox(r11, "simple Text field:", fontName: "Atop", fontFile: "e://res/apo.ttf", align: 2);

Widget widget = new Widget(page);
widget.BorderColor = blue;
widget.BorderWidth = 0.3f;
widget.BorderStyle = "d";
widget.BorderDashes = [2, 3];
widget.FieldName = "textfield-1";
widget.FieldType = 7;
widget.FillColor = gold;
widget.Rect = r12;
widget.TextColor = blue;
widget.TextFont = "tibo";
widget.TextFontSize = fontSize;
widget.TextMaxLen = 40;
widget.FieldValue = "Times-Roman-Bold, max. 40 chars";
Annot annot = page.AddWidget(widget);

Rect r21 = r11 + new Rect(0, 2 * lineHeight, 0, 2 * lineHeight);
Rect r22 = r21 + new Rect(r21.Width + 2, 0, lineHeight, 0);
page.InsertTextbox(r21, "CheckBox:", fontName: "Atop", fontFile: "e://res/apo.ttf", align: 2);

widget = new Widget(page);
widget.BorderStyle = "s";
widget.BorderColor = blue;
widget.BorderWidth = 0.3f;
widget.FieldName = "Button-1";
widget.FieldType = (int)PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX;
widget.FillColor = gold;
widget.Rect = r22;
widget.TextColor = blue;
widget.TextFont = "ZaDb";
widget.FieldValue = "Yes";
annot = page.AddWidget(widget);

Rect r31 = r21 + new Rect(0, 2 * lineHeight, 0, 2 * lineHeight);
Rect r32 = r31 + new Rect(r31.Width + 2, 0, r31.Width + 2, 0);
page.InsertTextbox(r31, "ListBox:", fontName: "Atop", fontFile: "e://res/apo.ttf", align: 2);

widget = new Widget(page);
widget.FieldName = "ListBox-1";
widget.FieldType = (int)PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX;
widget.FillColor = gold;
widget.ChoiceValues = new List<dynamic>() { "Franfurt", "Hamhurg", "Stuttgart", "Hannover" };
widget.Rect = r32;
widget.TextColor = blue;
widget.TextFontSize = fontSize;
widget.FieldFlags = 4;
widget.FieldValue = widget.ChoiceValues[0];
annot = page.AddWidget(widget);

Rect r41 = r31 + new Rect(0, 2 * lineHeight, 0, 2 * lineHeight);
Rect r42 = r41 + new Rect(r41.Width + 2, 0, r41.Width + 2, 0);
page.InsertTextbox(r41, "ComboBox, editable:", fontName: "Atop", fontFile: "e://res/apo.ttf", align: 2);

widget = new Widget(page);
widget.FieldFlags = 4;
widget.FieldName = "ComboBox-1";
widget.FieldType = (int)PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX;
widget.FillColor = gold;
widget.ChoiceValues = new List<dynamic>() { "Spanien", "Frankreich", "Holland", "Dänemark", "Schweden", "Norwegen", "England", "Polen", "Russland", "Italien", "Portugal", "Griechenland" };
widget.Rect = r42;
widget.TextColor = blue;
widget.TextFontSize = fontSize;
widget.FieldValue = widget.ChoiceValues[1];
annot = page.AddWidget(widget);

Rect r51 = r41 + new Rect(0, 2 * lineHeight, 0, 2 * lineHeight);
Rect r52 = new Rect(r51.BottomLeft.X, r51.BottomLeft.Y, page.Rect.Width - 50, page.Rect.Height - 50);
page.InsertTextbox(r51, "multiline text field:", fontName: "Atop", fontFile: "e://res/apo.ttf", align: 2);
widget = new Widget(page);
widget.FieldName = "textfield-2";
widget.FieldType = (int)PdfWidgetType.PDF_WIDGET_TYPE_TEXT;
widget.FillColor = gray;
widget.Rect = r52;
widget.TextColor = blue;
widget.TextFont = "TiRo";
widget.TextFontSize = fontSize;
widget.FieldValue = "this\nis\na\nmulti-\nline\ntext.";
annot = page.AddWidget(widget);

doc.Save("widget.pdf", clean: 1, garbage: 4);
