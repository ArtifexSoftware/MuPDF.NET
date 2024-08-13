// For more information see https://aka.ms/fsharp-console-apps
open MuPDF.NET

let doc = new Document()
let page = doc.NewPage()

let gold = [| 1f; 1f; 0f |]
let blue = [| 0f; 0f; 1f |]
let gray = [| 0.9f; 0.9f; 0.9f |]
let fontSize = 11f
let lineHeight = fontSize + 10f

let r11 = new Rect(50f, 100f, 200f, 100f + lineHeight)
let r12 = r11 + new Rect(r11.Width + 2f, 0f, 2f * r11.Width + 2f, 0f)
let _ = page.InsertTextbox(r11, "simple Text field:", "Atop", "e://res/apo.ttf", 2f)

let mutable widget = new Widget(page)
widget.BorderColor <- blue
widget.BorderWidth <- 0.3f
widget.BorderStyle <- "d"
widget.BorderDashes <- [|2; 3|]
widget.FieldName <- "textfield-1"
widget.FieldType <- 7
widget.FillColor <- gold
widget.Rect <- r12
widget.TextColor <- blue
widget.TextFont <- "tibo"
widget.TextFontSize <- fontSize
widget.TextMaxLen <- 40
widget.FieldValue <- "Times-Roman-Bold, max. 40 chars"
let mutable annot = page.AddWidget(widget)

let r21 = r11 + new Rect(0f, 2f * lineHeight, 0f, 2f * lineHeight)
let r22 = r21 + new Rect(r21.Width + 2f, 0f, lineHeight, 0f)
let _ = page.InsertTextbox(r21, "CheckBox:", "Atop", "e://res/apo.ttf", 2f)

widget <- new Widget(page)
widget.BorderStyle <- "s"
widget.BorderColor <- blue
widget.BorderWidth <- 0.3f
widget.FieldName <- "Button-1"
widget.FieldType <- int PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX
widget.FillColor <- gold
widget.Rect <- r22
widget.TextColor <- blue
widget.TextFont <- "ZaDb"
widget.FieldValue <- "Yes"
annot <- page.AddWidget(widget)

let r31 = r21 + new Rect(0f, 2f * lineHeight, 0f, 2f * lineHeight)
let r32 = r31 + new Rect(r31.Width + 2f, 0f, r31.Width + 2f, 0f)
let _ = page.InsertTextbox(r31, "ListBox:", "Atop", "e://res/apo.ttf", 2f)

widget <- new Widget(page)
widget.FieldName <- "ListBox-1"
widget.FieldType <- int PdfWidgetType.PDF_WIDGET_TYPE_LISTBOX
widget.FillColor <- gold
widget.ChoiceValues <- ["Franfurt"; "Hamhurg"; "Stuttgart"; "Hannover"] |> List.ofSeq |> List.map box |> System.Collections.Generic.List<_>
widget.Rect <- r32
widget.TextColor <- blue
widget.TextFontSize <- fontSize
widget.FieldFlags <- 4
widget.FieldValue <- string widget.ChoiceValues.[0]
annot <- page.AddWidget(widget)

let r41 = r31 + new Rect(0f, 2f * lineHeight, 0f, 2f * lineHeight)
let r42 = r41 + new Rect(r41.Width + 2f, 0f, r41.Width + 2f, 0f)
let _ = page.InsertTextbox(r41, "ComboBox, editable:", "Atop", "e://res/apo.ttf", 2f)

widget <- new Widget(page)
widget.FieldFlags <- 4
widget.FieldName <- "ComboBox-1"
widget.FieldType <- int PdfWidgetType.PDF_WIDGET_TYPE_COMBOBOX
widget.FillColor <- gold
widget.ChoiceValues <- ["Spanien"; "Frankreich"; "Holland"; "Dänemark"; "Schweden"; "Norwegen"; "England"; "Polen"; "Russland"; "Italien"; "Portugal"; "Griechenland"]  |> List.ofSeq |> List.map box |> System.Collections.Generic.List<_>
widget.Rect <- r42
widget.TextColor <- blue
widget.TextFontSize <- fontSize
widget.FieldValue <- string widget.ChoiceValues.[1]
annot <- page.AddWidget(widget)

doc.Save("ouptut.pdf", clean=1, garbage=4)


