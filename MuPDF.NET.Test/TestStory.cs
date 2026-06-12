using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>Port of <c>PyMuPDF-1.27.2.2/tests/test_story.py</c>.</summary>
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestStory/</c>; outputs: <c>TestDocuments/_Output/TestStory/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestStory
    {
        private const string TestClassName = nameof(TestStory);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static string Dedent(string text)
        {
            var lines = text.Replace("\r\n", "\n").Trim('\n').Split('\n');
            if (lines.Length == 0)
                return "";
            int minIndent = int.MaxValue;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                int indent = line.Length - line.TrimStart(' ').Length;
                if (indent < minIndent)
                    minIndent = indent;
            }
            if (minIndent == int.MaxValue)
                minIndent = 0;
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                var line = lines[i];
                if (line.Length >= minIndent)
                    sb.Append(line.Substring(minIndent));
                else
                    sb.Append(line);
            }
            return sb.ToString();
        }

        private static string SanitizeFileLeaf(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        // codespell:ignore-begin
        // springer_html = '''
        private const string springer_html = @"
<article>
<aside>
<img src=""springer.jpg"">
<br><i>Michael Springer ist Schriftsteller und Wis&#173;sen&#173;schafts&#173;publizist. Eine Sammlung seiner Einwürfe ist 2019 als Buch unter dem Titel <b>»Lauter Überraschungen. Was die Wis&#173;senschaft weitertreibt«</b> erschienen.<br><a>www.spektrum.de/artikel/2040277</a></i>
</aside>
<h1>SPRINGERS EINWÜRFE: INTIME VERBINDUNGEN</h1>

<h2>Wieso kann unsereins so vieles, was eine Maus nicht kann? Unser Gehirn ist nicht bloß größer, sondern vor allem überraschend vertrackt verdrahtet.</h2>

<p>Der Heilige Gral der Neu&#173;ro&#173;wis&#173;sen&#173;schaft ist die komplette Kartierung des menschlichen Gehirns – die ge&#173;treue Ab&#173;bildung des Ge&#173;strüpps der Nervenzellen mit den baum&#173;för&#173;mi&#173;gen Ver&#173;ästel&#173;ungen der aus ihnen sprie&#173;ßen&#173;den Den&#173;dri&#173;ten und den viel län&#173;ge&#173;ren Axo&#173;nen, wel&#173;che oft der Sig&#173;nal&#173;über&#173;tragung von einem Sin&#173;nes&#173;or&#173;gan oder zu einer Mus&#173;kel&#173;fa&#173;ser die&#173;nen. Zum Gesamtbild gehören die winzigen Knötchen auf den Dendriten; dort sitzen die Synapsen. Das sind Kontakt- und Schalt&#173;stel&#173;len, leb&#173;haf&#173;te Ver&#173;bin&#173;dungen zu anderen Neuronen.</p>

<p>Dieses Dickicht bis zur Ebene einzelner Zel&#173;len zu durchforsten und es räumlich dar&#173;zu&#173;stel&#173;len, ist eine gigantische Aufgabe, die bis vor Kurzem utopisch anmuten musste. Neu&#173;er&#173;dings vermag der junge For&#173;schungs&#173;zweig der Konnektomik (von Englisch: con&#173;nect für ver&#173;bin&#173;den) das Zusammenspiel der Neurone immer besser zu verstehen. Das gelingt mit dem Einsatz dreidimensionaler Elek&#173;tro&#173;nen&#173;mik&#173;ros&#173;ko&#173;pie. Aus Dünn&#173;schicht&#173;auf&#173;nah&#173;men von zerebralen Ge&#173;we&#173;be&#173;pro&#173;ben lassen sich plastische Bil&#173;der ganzer Zellverbände zu&#173;sam&#173;men&#173;setzen.</p>

<p>Da frisches menschliches Hirn&#173;ge&#173;we&#173;be nicht ohne Wei&#173;te&#173;res zu&#173;gäng&#173;lich ist – in der Regel nur nach chirurgischen Eingriffen an Epi&#173;lep&#173;sie&#173;pa&#173;tien&#173;ten –, hält die Maus als Mo&#173;dell&#173;or&#173;ga&#173;nis&#173;mus her. Die evolutionäre Ver&#173;wandt&#173;schaft von Mensch und Nager macht die Wahl plau&#173;sibel. Vor allem das Team um Moritz Helmstaedter am Max-Planck-Institut (MPI) für Hirnforschung in Frankfurt hat in den ver&#173;gangenen Jahren Expertise bei der kon&#173;nek&#173;tomischen Analyse entwickelt.</p>

<p>Aber steckt in unserem Kopf bloß ein auf die tausendfache Neu&#173;ro&#173;nen&#173;an&#173;zahl auf&#173;ge&#173;bläh&#173;tes Mäu&#173;se&#173;hirn? Oder ist menschliches Ner&#173;ven&#173;ge&#173;we&#173;be viel&#173;leicht doch anders gestrickt? Zur Beantwortung dieser Frage unternahm die MPI-Gruppe einen detaillierten Vergleich von Maus, Makake und Mensch (Science 377, abo0924, 2022).</p>

<p>Menschliches Gewebe stammte diesmal nicht von Epileptikern, son&#173;dern von zwei wegen Hirntumoren operierten Patienten. Die For&#173;scher wollten damit vermeiden, dass die oft jahrelange Behandlung mit An&#173;ti&#173;epi&#173;lep&#173;ti&#173;ka das Bild der synaptischen Verknüpfungen trübte. Sie verglichen die Proben mit denen eines Makaken und von fünf Mäusen.</p>

<p>Einerseits ergaben sich – einmal ab&#173;ge&#173;se&#173;hen von den ganz of&#173;fen&#173;sicht&#173;li&#173;chen quan&#173;titativen Unterschieden wie Hirngröße und Neu&#173;ro&#173;nen&#173;anzahl – recht gute Über&#173;ein&#173;stim&#173;mun&#173;gen, die somit den Gebrauch von Tier&#173;modellen recht&#173;fer&#173;ti&#173;gen. Doch in einem Punkt erlebte das MPI-Team eine echte Über&#173;raschung.</p>

<p>Gewisse Nervenzellen, die so genannten In&#173;ter&#173;neurone, zeichnen sich dadurch aus, dass sie aus&#173;schließ&#173;lich mit anderen Ner&#173;ven&#173;zel&#173;len in&#173;ter&#173;agieren. Solche »Zwi&#173;schen&#173;neu&#173;rone« mit meist kurzen Axonen sind nicht primär für das Verarbeiten externer Reize oder das Aus&#173;lösen körperlicher Reaktionen zuständig; sie be&#173;schäf&#173;ti&#173;gen sich bloß mit der Ver&#173;stär&#173;kung oder Dämpfung interner Signale.</p>

<p>Just dieser Neuronentyp ist nun bei Makaken und Menschen nicht nur mehr als doppelt so häufig wie bei Mäusen, sondern obendrein be&#173;son&#173;ders intensiv untereinander ver&#173;flochten. Die meisten Interneurone kop&#173;peln sich fast ausschließlich an ihresgleichen. Dadurch wirkt sich ihr konnektomisches Ge&#173;wicht ver&#173;gleichs&#173;weise zehnmal so stark aus.</p>

<p>Vermutlich ist eine derart mit sich selbst be&#173;schäf&#173;tigte Sig&#173;nal&#173;ver&#173;ar&#173;beitung die Vor&#173;be&#173;ding&#173;ung für ge&#173;stei&#173;gerte Hirn&#173;leis&#173;tungen. Um einen Ver&#173;gleich mit verhältnismäßig pri&#173;mi&#173;ti&#173;ver Tech&#173;nik zu wagen: Bei küns&#173;tli&#173;chen neu&#173;ro&#173;na&#173;len Netzen – Algorithmen nach dem Vor&#173;bild verknüpfter Nervenzellen – ge&#173;nü&#173;gen schon ein, zwei so genannte ver&#173;bor&#173;ge&#173;ne Schich&#173;ten von selbst&#173;be&#173;züg&#173;li&#173;chen Schaltstellen zwischen Input und Output-Ebene, um die ver&#173;blüf&#173;fen&#173;den Erfolge der künstlichen Intel&#173;ligenz her&#173;vor&#173;zu&#173;bringen.</p>
</article>
";
        //codespell:ignore-end

        [Fact]
        public void test_story()
        {
            // otf = os.path.abspath(f'{__file__}/../resources/PragmaticaC.otf')
            string otf = Doc("PragmaticaC.otf");
            // 2023-12-06: latest mupdf throws exception if path uses back-slashes.
            // otf = otf.replace('\\', '/')
            otf = otf.Replace('\\', '/');
            // CSS = f"""
            //     @font-face {{font-family: test; src: url({otf});}}
            string CSS = $@"
        @font-face {{font-family: test; src: url({otf});}}
    ";

            // HTML = """
            // <p style="font-family: test;color: blue">We shall meet again at a place where there is no darkness.</p>
            string HTML = """
    <p style="font-family: test;color: blue">We shall meet again at a place where there is no darkness.</p>
    """;

            Rect MEDIABOX = Utils.PaperRect("letter");
            // WHERE = MEDIABOX + (36, 36, -36, -36)
            Rect WHERE = MEDIABOX + new Rect(36, 36, -36, -36);
            // the font files are located in /home/chinese
            var arch = new Archive(".");
            // if not specified user_css, the output pdf has content
            using var story = new Story(HTML, CSS, archive: arch);

            using var writer = new DocumentWriter(Out("test_story.pdf"));

            bool more = true;

            // while more:
            while (more)
            {
                // device = writer.BeginPage(MEDIABOX)
                var device = writer.BeginPage(MEDIABOX);
                // more, _ = story.place(WHERE)
                (more, _) = story.place(WHERE);
                // story.draw(device)
                story.draw(device);
                // writer.EndPage()
                writer.EndPage();
            }

            // writer.close()
            writer.Close();
        }

        [Fact]
        public void test_2753()
        {
            Story.StoryRectFn rectfn = (rect_num, filled) =>
                (new Rect(0, 0, 200, 200), new Rect(50, 50, 100, 150), null);

            Document make_pdf(string html, string path_out)
            {
                using var story = new Story(html: html);
                // document = story.write_with_links(rectfn)
                var document = story.write_with_links(rectfn);
                Console.WriteLine($"test_2753(): Writing to: path_out={path_out}.");
                // document.Save(path_out)
                document.Save(path_out);
                // return document
                return document;
            }

            // doc_before = make_pdf(
            //         textwrap.dedent('''
            //             <p>Before</p>
            //             <p style="page-break-before: always;"></p>
            //             <p>After</p>
            //         )
            var doc_before = make_pdf(
                Dedent("""
                    <p>Before</p>
                    <p style="page-break-before: always;"></p>
                    <p>After</p>
                    """),
                Out("test_2753-before.pdf"));

            // doc_after = make_pdf(
            //         textwrap.dedent('''
            //             <p>Before</p>
            //             <p style="page-break-after: always;"></p>
            //             <p>After</p>
            //         )
            var doc_after = make_pdf(
                Dedent("""
                    <p>Before</p>
                    <p style="page-break-after: always;"></p>
                    <p>After</p>
                    """),
                Out("test_2753-after.pdf"));

            string path = Path.Combine(Path.GetDirectoryName(Out("test_2753.pdf"))!, "test_2753");
            // doc_before.Save(f'{path}_before.pdf')
            doc_before.Save($"{path}_before.pdf");
            // doc_after.Save(f'{path}_after.pdf')
            doc_after.Save($"{path}_after.pdf");
            Assert.Equal(2, doc_before.PageCount);
            Assert.Equal(2, doc_after.PageCount);
        }

        [Fact]
        public void test_fit_springer()
        {
            //     return

            int verbose = 0;
            using var story = new Story(springer_html);

            //     Checks that eval(call) returned parameter=expected. Also creates PDF
            //     using path that contains `call` in its leafname,
            void check(string call, Story.FitResult fit_result, float? expected)
            {
                // fit_result = eval(call)
                Console.WriteLine($"test_fit_springer(): call={call} => fit_result={fit_result}.");
                // if expected is None:
                if (expected is null)
                {
                    Assert.False(fit_result.big_enough ?? true);
                }
                else
                {
                    // document = story.write_with_links(lambda rectnum, filled: (fit_result.rect, fit_result.rect, None))
                    var document = story.write_with_links((rectnum, filled) =>
                        (fit_result.rect, fit_result.rect, null));
                    string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Out("test_fit_springer.pdf"))!,
                        SanitizeFileLeaf($"test_fit_springer_{call}_fit_result.parameter={fit_result.parameter}_fit_result.rect={fit_result.rect}.pdf")));
                    // document.Save(path)
                    document.Save(path);
                    Console.WriteLine($"Have saved document to {path}.");
                    Assert.True(Math.Abs((fit_result.parameter ?? 0) - expected.Value) < 0.001,
                        $"expected={expected} fit_result.parameter={fit_result.parameter}");
                }
            }

            check($"story.fit_scale(Rect(0, 0, 200, 200), scale_min=1, verbose={verbose})",
                story.fit_scale(new Rect(0, 0, 200, 200), scale_min: 1, verbose: verbose != 0), 3.685728073120117f);
            check($"story.fit_scale(Rect(0, 0, 595, 842), scale_min=1, verbose={verbose})",
                story.fit_scale(new Rect(0, 0, 595, 842), scale_min: 1, verbose: verbose != 0), 1.0174560546875f);
            check($"story.fit_scale(Rect(0, 0, 300, 421), scale_min=1, verbose={verbose})",
                story.fit_scale(new Rect(0, 0, 300, 421), scale_min: 1, verbose: verbose != 0), 2.02752685546875f);
            check($"story.fit_scale(Rect(0, 0, 600, 900), scale_min=1, scale_max=1, verbose={verbose})",
                story.fit_scale(new Rect(0, 0, 600, 900), scale_min: 1, scale_max: 1, verbose: verbose != 0), 1);

            // check(f'story.fit_height(20, verbose={verbose})', 10782.3291015625)
            check($"story.fit_height(20, verbose={verbose})",
                story.fit_height(20, verbose: verbose != 0), 10782.3291015625f);
            // check(f'story.fit_height(200, verbose={verbose})', 2437.4990234375)
            check($"story.fit_height(200, verbose={verbose})",
                story.fit_height(200, verbose: verbose != 0), 2437.4990234375f);
            // check(f'story.fit_height(2000, verbose={verbose})', 450.2998046875)
            check($"story.fit_height(2000, verbose={verbose})",
                story.fit_height(2000, verbose: verbose != 0), 450.2998046875f);
            // check(f'story.fit_height(5000, verbose={verbose})', 378.2998046875)
            check($"story.fit_height(5000, verbose={verbose})",
                story.fit_height(5000, verbose: verbose != 0), 378.2998046875f);
            // check(f'story.fit_height(5500, verbose={verbose})', 378.2998046875)
            check($"story.fit_height(5500, verbose={verbose})",
                story.fit_height(5500, verbose: verbose != 0), 378.2998046875f);

            // check(f'story.fit_width(3000, verbose={verbose})', 167.30859375)
            check($"story.fit_width(3000, verbose={verbose})",
                story.fit_width(3000, verbose: verbose != 0), 167.30859375f);
            // check(f'story.fit_width(2000, verbose={verbose})', 239.595703125)
            check($"story.fit_width(2000, verbose={verbose})",
                story.fit_width(2000, verbose: verbose != 0), 239.595703125f);
            // check(f'story.fit_width(1000, verbose={verbose})', 510.85546875)
            check($"story.fit_width(1000, verbose={verbose})",
                story.fit_width(1000, verbose: verbose != 0), 510.85546875f);
            // check(f'story.fit_width(500, verbose={verbose})', 1622.1272945404053)
            check($"story.fit_width(500, verbose={verbose})",
                story.fit_width(500, verbose: verbose != 0), 1622.1272945404053f);
            // check(f'story.fit_width(400, verbose={verbose})', 2837.507724761963)
            check($"story.fit_width(400, verbose={verbose})",
                story.fit_width(400, verbose: verbose != 0), 2837.507724761963f);
            // check(f'story.fit_width(300, width_max=200000, verbose={verbose})', None)
            check($"story.fit_width(300, width_max=200000, verbose={verbose})",
                story.fit_width(300, width_max: 200000, verbose: verbose != 0), null);
            // check(f'story.fit_width(200, width_max=200000, verbose={verbose})', None)
            check($"story.fit_width(200, width_max=200000, verbose={verbose})",
                story.fit_width(200, width_max: 200000, verbose: verbose != 0), null);

            // Run without verbose to check no calls to log() - checked by assert.
            check("story.fit_scale(Rect(0, 0, 600, 900), scale_min=1, scale_max=1, verbose=0)",
                story.fit_scale(new Rect(0, 0, 600, 900), scale_min: 1, scale_max: 1, verbose: false), 1);
            check("story.fit_scale(Rect(0, 0, 300, 421), scale_min=1, verbose=0)",
                story.fit_scale(new Rect(0, 0, 300, 421), scale_min: 1, verbose: false), 2.02752685546875f);
        }

        [Fact]
        public void test_write_stabilized_with_links()
        {
            //     We return one rect per page.
            //     #print(f'rectfn(): rect_num={rect_num} filled={filled}')
            //     return mediabox, rect, None
            Story.StoryRectFn rectfn = (rect_num, filled) =>
            {
                Rect rect = new Rect(10, 20, 290, 380);
                Rect mediabox = new Rect(0, 0, 300, 400);
                return (mediabox, rect, null);
            };

            string contentfn(List<StoryElementPositionInfo> positions)
            {
                // ret = ''
                string ret = "";
                // ret += textwrap.dedent('''
                //         <!DOCTYPE html>
                //         <body>
                //         <h2>Contents</h2>
                //         <ul>
                ret += Dedent("""
                        <!DOCTYPE html>
                        <body>
                        <h2>Contents</h2>
                        <ul>
                        """);
                // for position in positions:
                foreach (var position in positions)
                {
                    // if position.heading and (position.open_close & 1):
                    if (position.heading != 0 && (position.open_close & 1) != 0)
                    {
                        // text = position.text if position.text else ''
                        string text = position.text ?? "";
                        // if position.id:
                        if (!string.IsNullOrEmpty(position.id))
                        {
                            // ret += f'    <li><a href="#{position.id}">{text}</a>'
                            ret += $"    <li><a href=\"#{position.id}\">{text}</a>";
                        }
                        else
                        {
                            // ret += f'    <li>{text}'
                            ret += $"    <li>{text}";
                        }
                        // ret += f' page={position.page_num}\n'
                        ret += $" page={position.page_num}\n";
                    }
                }
                // ret += '</ul>\n'
                ret += "</ul>\n";
                // ret += textwrap.dedent(f'''
                //         <h1>First section</h1>
                //         ...
                ret += Dedent("""
                        <h1>First section</h1>
                        <p>Contents of first section.
                        <ul>
                        <li>External <a href="https://artifex.com/">link to https://artifex.com/</a>.
                        <li><a href="#idtest">Link to IDTEST</a>.
                        <li><a href="#nametest">Link to NAMETEST</a>.
                        </ul>
                    
                        <h1>Second section</h1>
                        <p>Contents of second section.
                        <h2>Second section first subsection</h2>
                    
                        <p>Contents of second section first subsection.
                        <p id="idtest">IDTEST
                    
                        <h1>Third section</h1>
                        <p>Contents of third section.
                        <p><a name="nametest">NAMETEST</a>.
                    
                        </body>
                        """);
                // return ret.strip()
                return ret.Trim();
            }

            var document = Story.write_stabilized_with_links(contentfn, rectfn);

            // Check links.
            // links = list()
            var links = new List<Dictionary<string, object>>();
            foreach (var page in document)
            {
                // links += page.GetLinks()
                links.AddRange(page.GetLinks().Select(l => (Dictionary<string, object>)l));
            }
            Console.WriteLine($"len(links)={links.Count}.");
            // external_links = dict()
            var external_links = new Dictionary<string, int>();
            // for i, link in enumerate(links):
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                Console.WriteLine($"    {i}: link={link}");
                if (link.TryGetValue("kind", out var kindObj) &&
                    Convert.ToInt32(kindObj) == Constants.LinkUri)
                {
                    // uri = link['uri']
                    string uri = link["uri"]?.ToString() ?? "";
                    // external_links.setdefault(uri, 0)
                    if (!external_links.ContainsKey(uri))
                        external_links[uri] = 0;
                    // external_links[uri] += 1
                    external_links[uri] += 1;
                }
            }

            // Check there is one external link.
            Console.WriteLine($"external_links={external_links}");
            Assert.Single(external_links);
            Assert.Contains("https://artifex.com/", external_links.Keys);

            // out_path = __file__.replace('.py', '.pdf')
            string out_path = Out("test_write_stabilized_with_links.pdf");
            // document.Save(out_path)
            document.Save(out_path);
        }

        [Fact]
        public void test_archive_creation()
        {
            using var s = new Story(archive: new Archive("."));
            using var s2 = new Story(archive: ".");
        }

        [Fact]
        public void test_3813()
        {

            // HTML = """
            // ...
            string HTML = """
    <p>Count is fine:</p>
    <ol>
        <li>Lorem
            <ol>
                <li>Sub Lorem</li>
                <li>Sub Lorem</li>
            </ol>
        </li>
        <li>Lorem</li>
        <li>Lorem</li>
    </ol>

    <p>Broken count:</p>
    <ol>
        <li>Lorem
            <ul>
                <li>Sub Lorem</li>
                <li>Sub Lorem</li>
            </ul>
        </li>
        <li>Lorem</li>
        <li>Lorem</li>
    </ol>
    """;
            Rect MEDIABOX = Utils.PaperRect("A4");
            // WHERE = MEDIABOX + (36, 36, -36, -36)
            Rect WHERE = MEDIABOX + new Rect(36, 36, -36, -36);

            using var story = new Story(html: HTML);
            string path = Out("test_3813.pdf");
            File.Delete(path);
            using var writer = new DocumentWriter(path);

            bool more = true;

            // while more:
            while (more)
            {
                // device = writer.BeginPage(MEDIABOX)
                var device = writer.BeginPage(MEDIABOX);
                // more, _ = story.place(WHERE)
                (more, _) = story.place(WHERE);
                // story.draw(device)
                story.draw(device);
                // writer.EndPage()
                writer.EndPage();
            }

            // writer.close()
            writer.Close();

            using (var document = new Document(path))
            {
                // page = document[0]
                var page = document[0];
                // text = page.GetText()
                string text = page.GetText()?.ToString() ?? "";
                // text_utf8 = text.encode()
                byte[] text_utf8 = Encoding.UTF8.GetBytes(text);

                // text_expected_utf8 = b'Count is \xef\xac\x81ne:\n1. Lorem\n1. Sub Lorem\n2. Sub Lorem\n2. Lorem\n3. Lorem\nBroken count:\n1. Lorem\n\xe2\x80\xa2  Sub Lorem\n\xe2\x80\xa2  Sub Lorem\n2. Lorem\n3. Lorem\n'
                byte[] text_expected_utf8 = new byte[]
                {
                    0x43, 0x6f, 0x75, 0x6e, 0x74, 0x20, 0x69, 0x73, 0x20, 0xef, 0xac, 0x81, 0x6e, 0x65, 0x3a, 0x0a,
                    0x31, 0x2e, 0x20, 0x4c, 0x6f, 0x72, 0x65, 0x6d, 0x0a, 0x31, 0x2e, 0x20, 0x53, 0x75, 0x62, 0x20,
                    0x4c, 0x6f, 0x72, 0x65, 0x6d, 0x0a, 0x32, 0x2e, 0x20, 0x53, 0x75, 0x62, 0x20, 0x4c, 0x6f, 0x72,
                    0x65, 0x6d, 0x0a, 0x32, 0x2e, 0x20, 0x4c, 0x6f, 0x72, 0x65, 0x6d, 0x0a, 0x33, 0x2e, 0x20, 0x4c,
                    0x6f, 0x72, 0x65, 0x6d, 0x0a, 0x42, 0x72, 0x6f, 0x6b, 0x65, 0x6e, 0x20, 0x63, 0x6f, 0x75, 0x6e,
                    0x74, 0x3a, 0x0a, 0x31, 0x2e, 0x20, 0x4c, 0x6f, 0x72, 0x65, 0x6d, 0x0a, 0xe2, 0x80, 0xa2, 0x20,
                    0x20, 0x53, 0x75, 0x62, 0x20, 0x4c, 0x6f, 0x72, 0x65, 0x6d, 0x0a, 0xe2, 0x80, 0xa2, 0x20, 0x20,
                    0x53, 0x75, 0x62, 0x20, 0x4c, 0x6f, 0x72, 0x65, 0x6d, 0x0a, 0x32, 0x2e, 0x20, 0x4c, 0x6f, 0x72,
                    0x65, 0x6d, 0x0a, 0x33, 0x2e, 0x20, 0x4c, 0x6f, 0x72, 0x65, 0x6d, 0x0a,
                };
                // text_expected = text_expected_utf8.decode()
                string text_expected = Encoding.UTF8.GetString(text_expected_utf8);

                Console.WriteLine($"text_utf8:\n    {text_utf8}");
                Console.WriteLine($"text_expected_utf8:\n    {text_expected_utf8}");
                foreach (var line in text.Split('\n'))
                {
                    Console.WriteLine($"    {line}");
                }

                foreach (var line in text_expected.Split('\n'))
                {
                    Console.WriteLine($"   {line}");
                }


                Assert.Equal(text_expected, text);
            }
        }
    }
}
