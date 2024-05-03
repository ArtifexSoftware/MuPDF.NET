using System;
using System.Text.RegularExpressions;

namespace MuPDF.NET
{
    public class LinkDest
    {
        public string Dest { get; set; }

        public string FileSpec { get; set; }

        public int Flags { get; set; }

        public bool IsMap { get; set; }

        public bool IsUri { get; set; }

        public Point TopLeft { get; set; } = new Point(0, 0);

        public string NewWindow { get; set; }

        public Point BottomRight { get; set; } = new Point(0, 0);

        public LinkType Kind { get; set; }

        public int Page { get; set; }

        public string Uri { get; set; }

        public Dictionary<string, dynamic> Named { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj">Link or Outline destination</param>
        /// <param name="rlink"></param>
        /// <param name="document"></param>
        public LinkDest(dynamic obj, (List<int>, float, float) rlink, MuPDFDocument document = null)
        {
            bool isExt = obj.IsExternal;
            bool isInt = !isExt;
            this.Dest = "";
            this.FileSpec = "";
            this.Flags = 0;
            this.IsMap = false;
            this.IsUri = false;
            this.Kind = LinkType.LINK_NONE;
            this.TopLeft = new Point(0, 0);
            this.NewWindow = "";
            this.Page = obj.Page;
            this.BottomRight = new Point(0, 0);
            this.Uri = obj.Uri;
            
            if (rlink.Item1 != null && !Uri.StartsWith("#"))
            {
                Uri = $"#page={rlink.Item1[0] + 1}&zoom=0,{rlink.Item2},{rlink.Item3}";
            }
            if (obj.IsExternal)
            {
                Page = -1;
                Kind = LinkType.LINK_URI;
            }
            if (Uri == null)
            {
                Page = -1;
                Kind = LinkType.LINK_NONE;
            }
            if (isInt && Uri != null)
            {
                Uri = Uri.Replace("&zoom=nan", "&zoom=0");
                if (Uri.StartsWith("#"))
                {
                    Kind = LinkType.LINK_GOTO;
                    Regex regex = new Regex("^#page=([0-9]+)&zoom=([0-9.]+),(-?[0-9.]+),(-?[0-9.]+)$");
                    Match m = regex.Match(Uri);
                    if (m.Success)
                    {
                        Page = int.Parse(m.Groups[1].Value) - 1;
                        TopLeft = new Point((float)Convert.ToDouble(m.Groups[3].Value), (float)Convert.ToDouble(m.Groups[4].Value));
                        Flags = Flags | (int)LinkFlags.LINK_FLAG_L_VALID | (int)LinkFlags.LINK_FLAG_T_VALID;
                    }
                    else
                    {
                        m = Regex.Match(Uri, "^#page=([0-9]+)$");
                        if (m.Success)
                        {
                            Kind = LinkType.LINK_NAMED;
                            m = Regex.Match(Uri, "^#nameddest=(.*)");
                            if (document != null && m.Success)
                            {
                                string named = m.Groups[1].Value;
                                Console.WriteLine(named);
                                this.Named = document.ResolveNames();
                                if (Named is null)
                                    Named = new Dictionary<string, dynamic>();
                                Named.Add("nameddest", named);
                            }
                            else Named = Uri2Dict(Uri.Substring(1));
                        }
                    }
                }
                else
                {
                    Kind = LinkType.LINK_NAMED;
                    Named = Uri2Dict(Uri);
                }
            }
            if (obj.IsExternal)
            {
                if (Uri is null) { }
                else if (Uri.StartsWith("http://") || Uri.StartsWith("https://") || Uri.StartsWith("mailto:") || Uri.StartsWith("ftp://"))
                {
                    IsUri = true;
                    Kind = LinkType.LINK_URI;
                }
                else if (Uri.StartsWith("file://"))
                {
                    FileSpec = Uri.Substring(7);
                    IsUri = false;
                    Uri = "";
                    Kind = LinkType.LINK_LAUNCH;
                    string[] ftab = FileSpec.Split("#");
                    if (ftab.Length == 2)
                    {
                        if (ftab[1].StartsWith("page="))
                        {
                            Kind = LinkType.LINK_GOTOR;
                            FileSpec = ftab[0];
                            Page = int.Parse(ftab[1].Substring(5)) - 1;
                        }
                    }
                }
                else
                {
                    IsUri = true;
                    Kind = LinkType.LINK_LAUNCH;
                }
            }
        }

        public static Dictionary<string, dynamic> Uri2Dict(string uri)
        {
            List<string> items = new List<string>(uri.Substring(1).Split('&'));
            Dictionary<string, dynamic> ret = new Dictionary<string, dynamic>();
            foreach (string item in items)
            {
                int eq = item.IndexOf('=');
                if (eq >= 0)
                    ret.Add(item.Substring(1), item.Substring(eq + 1));
                else
                    ret[item] = null;
            }
            return ret;
        }
    }
}
