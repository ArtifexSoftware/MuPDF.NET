Imports System
Imports mupdf.NET

Module Program
    Sub Main(args As String())
        Dim doc As New Document()
        Dim page As Page = doc.NewPage()

        Dim gold As Single() = {1.0F, 1.0F, 0}
        Dim blue As Single() = {0, 0, 1}
        Dim gray As Single() = {0.9F, 0.9F, 0.9F}
        Dim fontSize As Single = 11
        Dim lineHeight As Single = fontSize + 10

        Dim r11 As New Rect(50, 100, 200, 100 + lineHeight)
        Dim r12 As Rect = r11 + New Rect(r11.Width + 2, 0, 2 * r11.Width + 2, 0)
        page.InsertTextbox(r11, "simple Text field:", fontName:="Atop", fontFile:="e://res/apo.ttf", align:=2)

        Dim widget As New Widget(page)
        widget.BorderColor = blue
        widget.BorderWidth = 0.3F
        widget.BorderStyle = "d"
        widget.BorderDashes = New Integer() {2, 3}
        widget.FieldName = "textfield-1"
        widget.FieldType = 7
        widget.FillColor = gold
        widget.Rect = r12
        widget.TextColor = blue
        widget.TextFont = "tibo"
        widget.TextFontSize = fontSize
        widget.TextMaxLen = 40
        widget.FieldValue = "Times-Roman-Bold, max. 40 chars"
        Dim annot As Annot = page.AddWidget(widget)

        Dim r21 As Rect = r11 + New Rect(0, 2 * lineHeight, 0, 2 * lineHeight)
        Dim r22 As Rect = r21 + New Rect(r21.Width + 2, 0, lineHeight, 0)
        page.InsertTextbox(r21, "CheckBox:", fontName:="Atop", fontFile:="e://res/apo.ttf", align:=2)

        widget = New Widget(page)
        widget.BorderStyle = "s"
        widget.BorderColor = blue
        widget.BorderWidth = 3.0F
        widget.FieldName = "Button-1"
        widget.FieldType = DirectCast(PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX, Integer)
        widget.FillColor = gold
        widget.Rect = r22
        widget.TextColor = blue
        widget.TextFont = "ZaDb"
        widget.FieldValue = "Yes"
        annot = page.AddWidget(widget)

        Dim r31 As Rect = r21 + New Rect(0, 2 * lineHeight, 0, 2 * lineHeight)
        Dim r32 As Rect = r31 + New Rect(r31.Width + 2, 0, r31.Width + 2, 0)
        page.InsertTextbox(r31, "ListBox:", fontName:="Atop", fontFile:="e://res/apo.ttf", align:=2)

        widget = New Widget(page)
        widget.FieldName = "ListBox-1"
        widget.FieldType = DirectCast(PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX, Integer)
        widget.FillColor = gold
        widget.ChoiceValues = New List(Of Object) From {"Franfurt", "Hamhurg", "Stuttgart", "Hannover"}
        widget.Rect = r32
        widget.TextColor = blue
        widget.TextFontSize = fontSize
        widget.FieldFlags = 4
        widget.FieldValue = widget.ChoiceValues(0)
        annot = page.AddWidget(widget)

        Dim r41 As Rect = r31 + New Rect(0, 2 * lineHeight, 0, 2 * lineHeight)
        Dim r42 As Rect = r41 + New Rect(r41.Width + 2, 0, r41.Width + 2, 0)
        page.InsertTextbox(r41, "ComboBox, editable:", fontName:="Atop", fontFile:="e://res/apo.ttf", align:=2)

        widget = New Widget(page)
        widget.FieldFlags = 4
        widget.FieldName = "ComboBox-1"
        widget.FieldType = DirectCast(PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX, Integer)
        widget.FillColor = gold
        widget.ChoiceValues = New List(Of Object) From {"Spanien", "Frankreich", "Holland", "Dänemark", "Schweden", "Norwegen", "England", "Polen", "Russland", "Italien", "Portugal", "Griechenland"}
        widget.Rect = r42
        widget.TextColor = blue
        widget.TextFontSize = fontSize
        widget.FieldValue = widget.ChoiceValues(1)
        annot = page.AddWidget(widget)

        doc.Save("output.pdf", clean:=1, garbage:=4)
    End Sub
End Module
