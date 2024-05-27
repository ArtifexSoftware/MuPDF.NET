using mupdf;

namespace MuPDF.NET
{
    public class MuPDFWidget
    {
        public float[] BorderColor { get; set; }

        public string BorderStyle { get; set; }

        public float BorderWidth { get; set; }

        public int[] BorderDashes { get; set; }

        public List<List<string>> ChoiceValues { get; set; }

        public int RbParent { get; set; }

        public string FieldName { get; set; }

        public string FieldLabel { get; set; }

        public string FieldValue { get; set; }

        public int FieldFlags { get; set; }

        public int FieldType { get; set; }

        public int FieldDisplay { get; set; }

        public string FieldTypeString { get; set; }

        public float[] FillColor { get; set; }

        public string ButtonCaption { get; set; }

        public bool IsSigned { get; set; }

        public float[] TextColor { get; set; }

        public string TextFont { get; set; }

        public float TextFontSize { get; set; }

        public int TextMaxLen { get; set; }

        public int TextFormat { get; set; }

        public string TextDa { get; set; }

        public string Script { get; set; }

        public string ScriptStroke { get; set; }

        public string ScriptFormat { get; set; }

        public string ScriptChange { get; set; }

        public string ScriptCalc { get; set; }

        public string ScriptBlur { get; set; }

        public string ScriptFocus { get; set; }

        public int Xref { get; set; }

        public Rect Rect { get; set; }

        public MuPDFPage Parent { get; set; }

        public PdfAnnot _annot { get; set; }

        public MuPDFWidget(MuPDFPage page)
        {
            Parent = page;
            BorderColor = null;
            BorderStyle = "S";
            BorderWidth = 0;
            BorderDashes = null;
            ChoiceValues = null;
            RbParent = 0;

            FieldName = null;
            FieldLabel = null;
            FieldValue = null;
            FieldFlags = 0;
            FieldType = 0;
            FieldDisplay = 0;
            FieldTypeString = null;

            FillColor = null;
            ButtonCaption = null;
            IsSigned = false;
            TextColor = new float[] { 0, 0, 0 };
            TextFont = "Helv";
            TextFontSize = 0;
            TextMaxLen = 0;
            TextFormat = 0;
            TextDa = "";

            Script = null;
            ScriptStroke = null;
            ScriptFormat = null;
            ScriptCalc = null;
            ScriptChange = null;
            ScriptBlur = null;
            ScriptFocus = null;

            Rect = null;
            Xref = 0;
        }

        public override string ToString()
        {
            return $"Widget:(field_type={FieldTypeString}) script={Script}";
        }

        public void AdjustFont()
        {
            if (string.IsNullOrEmpty(TextFont))
            {
                TextFont = "Helv";
                return;
            }
            List<string> validFonts = new List<string>() { "Cour", "TiRo", "Helv", "ZaDb" };
            foreach (string f in validFonts)
                if (TextFont.ToLower() == f.ToLower())
                {
                    TextFont = f;
                    return;
                }
            TextFont = "Helv";
            return;
        }

        public void Checker()
        {
            if (!(FieldType >= 1 && FieldType < 8))
                throw new Exception("bad field type");

            if (FieldType == (int)PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON)
            {
                Document doc = Parent.Parent;
                (string kidsType, string kidsValue) = doc.GetKeyXref(Xref, "Parent/Kids");
                if (kidsType == "array")
                {
                    List<int> xrefs = kidsValue.Substring(1, kidsValue.Length - 2).Replace("0 R", "").Split("").Select(x => int.Parse(x)).ToList();
                    foreach (int xref in xrefs)
                    {
                        if (xref != Xref)
                            doc.SetKeyXRef(xref, "AS", "/Off");
                    }
                }
            }
        }

        public void ParseDa()
        {
            if (string.IsNullOrEmpty(TextDa))
                return;
            string font = "Helv";
            float fontSize = 0;
            float[] col = { 0, 0, 0 };
            string[] dat = TextDa.Split("");
            int i = 0;
            foreach (string item in dat)
            {
                if (item == "Tf")
                {
                    font = dat[i - 2].Substring(1);
                    fontSize = float.Parse(dat[i - 1]);
                    dat[i] = dat[i - 1] = dat[i - 2] = "";
                    continue;
                }
                if (item == "g")
                {
                    col = new float[] { float.Parse(dat[i - 1]) };
                    dat[i] = dat[i - 1] = "";
                    continue;
                }
                if (item == "rg")
                {
                    col = new float[3];
                    for (int j = i - 3; j < i; j++)
                        col[j - i + 3] = float.Parse(dat[j]);
                    dat[i] = dat[i - 1] = dat[i - 2] = dat[i - 3] = "";
                    continue;
                }
            }
            TextFont = font;
            TextFontSize = fontSize;
            TextColor = col;
            TextDa = "";
        }

        public void Validate()
        {
            if (Rect.IsInfinite || Rect.IsEmpty)
                throw new Exception("bad rect");
            if (string.IsNullOrEmpty(FieldName))
                throw new Exception("field name missing");
            if (FieldLabel == "Unnamed")
                FieldLabel = null;
            Utils.CheckColor(BorderColor);
            Utils.CheckColor(FillColor);
            if (TextColor == null)
                TextColor = new float[] { 0, 0, 0 };
            Utils.CheckColor(TextColor);

            if (BorderWidth == 0)
                BorderWidth = 0;
            if (TextFontSize == 0)
                TextFontSize = 0;

            BorderStyle = BorderStyle.ToUpper().Substring(0, 1);

            bool btnType = (new List<PdfWidgetType> {
                PdfWidgetType.PDF_WIDGET_TYPE_BUTTON,
                PdfWidgetType.PDF_WIDGET_TYPE_CHECKBOX,
                PdfWidgetType.PDF_WIDGET_TYPE_RADIOBUTTON}).Contains((PdfWidgetType)FieldType);
            if (string.IsNullOrEmpty(Script))
                Script = null;

            if (btnType || string.IsNullOrEmpty(ScriptCalc))
                ScriptCalc = null;

            if (btnType || string.IsNullOrEmpty(ScriptFormat))
                ScriptFormat = null;

            if (btnType || string.IsNullOrEmpty(ScriptStroke))
                ScriptStroke = null;

            if (btnType || string.IsNullOrEmpty(ScriptBlur))
                ScriptBlur = null;

            if (btnType || string.IsNullOrEmpty(ScriptFocus))
                ScriptFocus = null;

            Checker();
        }

        /// <summary>
        /// Return the names of On / Off (i.e. selected / clicked or not) states a button field may have. While the ‘Off’ state usually is also named like so, the ‘On’ state is often given a name relating to the functional context, for example ‘Yes’, ‘Female’, etc.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<string>> ButtonStates()
        {
            if (!(FieldType == 2 || FieldType == 5))
                return null;
            Document doc = Parent.Parent;
            if (doc == null)
                return null;

            int xref = Xref;
            Dictionary<string, List<string>> states = new Dictionary<string, List<string>>();
            states.Add("normal", null);
            states.Add("down", null);
            (string, string) apn = doc.GetKeyXref(xref, "AP/N");
            if (apn.Item1 == "dict")
            {
                List<string> nstates = new List<string>();
                string t = apn.Item2.Substring(2, apn.Item2.Length - 2 - 2);
                string[] apnt = t.Split("/").Skip(1).ToArray();
                foreach (string x in apnt)
                    nstates.Add(x.Split()[0]);
                states["normal"] = nstates;
            }
            if (apn.Item1 == "xref")
            {
                List<string> nstates = new List<string>();
                int nxref = int.Parse(apn.Item2.Split(" ")[0]);
                string t = doc.GetXrefObject(nxref);
                string[] apnt = t.Split("/").Skip(1).ToArray();
                foreach (string x in apnt)
                    nstates.Add(x.Split()[0]);
                states["normal"] = nstates;
            }
            (string, string) apd = doc.GetKeyXref(xref, "AP/D");
            if (apd.Item1 == "dict")
            {
                List<string> dstates = new List<string>();
                string t = apd.Item2.Substring(2, apd.Item2.Length - 2 - 2);
                string[] apdt = t.Split("/").Skip(1).ToArray();
                foreach (string x in apdt)
                    dstates.Add(x.Split()[0]);
                states["down"] = dstates;
            }
            if (apd.Item1 == "xref")
            {
                List<string> dstates = new List<string>();
                int dxref = int.Parse(apd.Item2.Split(" ")[0]);
                string t = doc.GetXrefObject(dxref);
                string[] apdt = t.Split("/").Skip(1).ToArray();
                foreach (string x in apdt)
                    dstates.Add(x.Split()[0]);
                states["down"] = dstates;
            }
            return states;
        }

        public MuPDFAnnot Next
        {
            get
            {
                return (new MuPDFAnnot(_annot)).Next;
            }
        }

        /// <summary>
        /// Return the value of the “ON” state of check boxes and radio buttons. For check boxes this is always the value “Yes”. For radio buttons, this is the value to select / activate the button.
        /// </summary>
        /// <returns></returns>
        public string OnState()
        {
            if (!(FieldType == 2 || FieldType == 5))
                return null;
            if (FieldType == 2)
                return "Yes";
            Dictionary<string, List<string>> bstate = ButtonStates();
            if (bstate == null)
                bstate = new Dictionary<string, List<string>>();
            foreach (string k in bstate.Keys)
            {
                foreach (string v in bstate[k])
                {
                    if (v != "Off")
                        return v;
                }
            }
            Console.WriteLine("warning: radio button has no 'On' value");
            return "";
        }

        /// <summary>
        /// Reset the field’s value to its default – if defined – or remove it. Do not forget to issue update() afterwards.
        /// </summary>
        public void Reset()
        {
            Utils.ResetWidget(_annot);
        }

        /// <summary>
        /// After any changes to a widget, this method must be used to store them in the PDF 
        /// </summary>
        public void Update()
        {
            Validate();
            AdjustFont();

            TextDa = "";
            string fmt = "";

            if (TextColor != null && TextColor.Length == 3)
                fmt = $"{TextColor[0]} {TextColor[1]} {TextColor[2]} rg /" + "{0} {1} Tf" + TextDa;
            else if (TextColor.Length == 1)
                fmt = $"{TextColor[0]} g /" + "{0} {1} Tf" + TextDa;
            else if (TextColor.Length == 4)
                fmt = $"{TextColor[0]} {TextColor[1]} {TextColor[2]} {TextColor[3]} k /" + "{0} {1} Tf" + TextDa;
            
            TextDa = string.Format(fmt, TextFont, TextFontSize);
            if (!string.IsNullOrEmpty(ScriptCalc))
                Utils.EnsureWidgetCalc(_annot);
            
            Utils.SaveWidget(_annot, this);
            TextDa = "";
        }
    }
}
