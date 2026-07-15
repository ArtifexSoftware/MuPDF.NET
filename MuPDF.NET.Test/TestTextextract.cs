// Extract page text in various formats.
// No checks performed - just contribute to code coverage.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <remarks>
    /// Inputs: <c>TestDocuments/TestTextextract/</c>; outputs: <c>TestDocuments/_Output/TestTextextract/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestTextextract
    {
        private const string TestClassName = nameof(TestTextextract);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static string Indent(string text, string prefix)
        {
            return string.Join("\n", text.Replace("\n", "\n").Split('\n').Select(line => prefix + line));
        }

        /// <summary>Cast nested block/line/span lists from <c>get_text</c> dicts (typed lists, not <c>List&lt;object&gt;</c>).</summary>
        private static List<Dictionary<string, object>> AsDictList(object value) =>
            (List<Dictionary<string, object>>)value;

        /// <summary><c>get_text</c> dict bboxes are <c>float[]</c> (x0, y0, x1, y1), not <see cref="Rect"/>.</summary>
        private static Rect BboxFromDict(object bbox)
        {
            if (bbox is Rect r)
                return r;
            if (bbox is float[] f && f.Length >= 4)
                return new Rect(f[0], f[1], f[2], f[3]);
            if (bbox is float[] d && d.Length >= 4)
                return new Rect((float)d[0], (float)d[1], (float)d[2], (float)d[3]);
            throw new InvalidOperationException($"unexpected bbox type: {bbox?.GetType().FullName}");
        }

        private static float RectTupleRms(Rect a, (float x0, float y0, float x1, float y1) b)
        {
            float[] vals = { a.X0, a.Y0, a.X1, a.Y1 };
            float[] exp = { b.x0, b.y0, b.x1, b.y1 };
            float e = 0;
            for (int i = 0; i < 4; i++)
            {
                float d = vals[i] - exp[i];
                e += d * d;
            }
            return (float)Math.Sqrt(e / 4);
        }

        private static int Flags0() =>
            mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
            | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
            | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP;

        [Fact]
        public void test_extract1()
        {
            // filename = os.path.join(scriptdir, "resources", "symbol-list.pdf")
            using var doc = new Document(Doc("symbol-list.pdf"));
            // page = doc[0]
            var page = doc[0];
            // text = page.GetText("text")
            _ = page.GetText("text");
            // blocks = page.GetText("blocks")
            _ = page.GetText("blocks");
            // words = page.GetText("words")
            _ = page.GetText("words");
            // d1 = page.GetText("dict")
            _ = page.GetText("dict");
            // d2 = page.GetText("json")
            _ = page.GetText("json");
            // d3 = page.GetText("rawdict")
            _ = page.GetText("rawdict");
            // d3 = page.GetText("rawjson")
            _ = page.GetText("rawjson");
            // text = page.GetText("html")
            _ = page.GetText("html");
            // text = page.GetText("xhtml")
            _ = page.GetText("xhtml");
            // text = page.GetText("xml")
            _ = page.GetText("xml");
            _ = Helpers.GetHighlightSelection(page, page.Rect.TopLeft, page.Rect.BottomRight);
            // ConversionHeader/ConversionTrailer are MuPDF helpers not ported to MuPDF.NET yet.
        }

        [Fact]
        public void test_extract4()
        {
            // Rebased-specific.
            using var document = new Document(Doc("2.pdf"));
            // page = document[4]
            var page = document[4];

            // text = page.GetText('html')
            string text = (string)page.GetText("html");
            string outStext = Out("test_extract4.html");
            File.WriteAllText(outStext, text, Encoding.UTF8);
            Console.WriteLine($"Have written to: {outStext}");

            // Native FzDocumentWriter HTML export.
            string outExtract = Out("test_extract4_1.html");
            using (var writer = new mupdf.FzDocumentWriter(
                outExtract,
                "html",
                mupdf.FzDocumentWriter.PathType.PathType_DOCX))
            {
                using var device = mupdf.mupdf.fz_begin_page(writer, mupdf.mupdf.fz_bound_page(page.NativePage));
                page.NativePage.fz_run_page(device, new mupdf.FzMatrix(), new mupdf.FzCookie());
                mupdf.mupdf.fz_end_page(writer);
                mupdf.mupdf.fz_close_document_writer(writer);
            }
            Assert.True(File.Exists(outExtract) && new System.IO.FileInfo(outExtract).Length > 0);
            Console.WriteLine($"Have written to: {outExtract}");

            string GetTextWithSpaceGuess(float spaceGuess)
            {
                var buffer = new mupdf.FzBuffer(10);
                using var output = new mupdf.FzOutput(buffer);
                using var writer = new mupdf.FzDocumentWriter(
                    output,
                    $"text,space-guess={spaceGuess.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                    mupdf.FzDocumentWriter.OutputType.OutputType_DOCX);
                using var device = mupdf.mupdf.fz_begin_page(writer, mupdf.mupdf.fz_bound_page(page.NativePage));
                page.NativePage.fz_run_page(device, new mupdf.FzMatrix(), new mupdf.FzCookie());
                mupdf.mupdf.fz_end_page(writer);
                mupdf.mupdf.fz_close_document_writer(writer);
                string t = Encoding.UTF8.GetString(buffer.fz_buffer_extract());
                int n = t.Count(c => c == ' ');
                Console.WriteLine($"space_guess={spaceGuess}: n={n}");
                return t;
            }

            // page = document[4]
            page = document[4];
            string text0 = GetTextWithSpaceGuess(0);
            string text1 = GetTextWithSpaceGuess(0.5f);
            _ = GetTextWithSpaceGuess(0.001f);
            _ = GetTextWithSpaceGuess(0.1f);
            _ = GetTextWithSpaceGuess(0.3f);
            _ = GetTextWithSpaceGuess(0.9f);
            _ = GetTextWithSpaceGuess(5.9f);
            Assert.Equal(text0, text1);
        }

        [Fact]
        public void test_2954()
        {
            int flags0 = Flags0();
            using var document = new Document(Doc("test_2954.pdf"));

            const string expectedGood =
                "IT-204-IP (2021) Page 3 of 5\nNYPA2514    12/06/21\nPartner's share of \n" +
                " modifications (see instructions)\n20\n State additions\nNumber\n" +
                "A ' Total amount\nB '\n State allocated amount\n" +
                "EA '\n20a\nEA '\n20b\nEA '\n20c\nEA '\n20d\nEA '\n20e\nEA '\n20f\n" +
                "Total addition modifications (total of column A, lines 20a through 20f)\n" +
                ". . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . \n" +
                "21\n21\n22\n State subtractions\n" +
                "Number\nA ' Total amount\nB '\n State allocated amount\n" +
                "ES '\n22a\nES '\n22b\nES '\n22c\nES '\n22d\nES '\n22e\nES '\n22f\n23\n23\n" +
                "Total subtraction modifications (total of column A, lines 22a through 22f). . . . . . . . . . . . . . . . . . . . . . . . . . . . \n" +
                "Additions to itemized deductions\n24\nAmount\n" +
                "Letter\n" +
                "24a\n24b\n24c\n24d\n24e\n24f\n" +
                "Total additions to itemized deductions (add lines 24a through 24f)\n" +
                ". . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . \n" +
                "25\n25\n" +
                "Subtractions from itemized deductions\n" +
                "26\nLetter\nAmount\n26a\n26b\n26c\n26d\n26e\n26f\n" +
                "Total subtractions from itemized deductions (add lines 26a through 26f) . . . . . . . . . . . . . . . . . . . . . . . . . . . . \n" +
                "27\n27\n" +
                "This line intentionally left blank. . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . \n" +
                "28\n28\n118003213032\n";

            static bool CheckGood(string text) =>
                text.Replace("\n", "") == expectedGood.Replace("\n", "");

            const int nFffdGood = 0;
            const int nFffdBad = 749;

            (string text, int nFffd) Get(int? flags = null)
            {
                string t = (string)document[0].GetText(flags: flags);
                int n = t.Count(c => c == '\uFFFD');
                return (t, n);
            }

            var (textNone, nFffdNone) = Get();
            var (text0, nFffd0) = Get(flags0);
            var (text1, nFffd1) = Get(flags0 | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE);

            Assert.Equal(nFffdGood, nFffdNone);
            Assert.Equal(nFffdBad, nFffd0);
            Assert.Equal(nFffdGood, nFffd1);
            Assert.True(CheckGood(textNone));
            Assert.False(CheckGood(text0));
            Assert.True(CheckGood(text1));
        }

        [Fact]
        public void test_3027()
        {
            using var doc = new Document(Doc("2.pdf"));
            // page = doc[0]
            var page = doc[0];
            // textpage = page.GetTextPage()
            var textpage = page.GetTextPage();
            var dict = (Dictionary<string, object>)Utils.GetText(page, "dict", textpage: textpage);
            Assert.True(dict.ContainsKey("blocks"));
        }

        [Fact]
        public void test_3186()
        {
            string path = Doc("test_3186.pdf");

            // codespell:ignore-begin — expected strings from MuPDF test (NUL bytes as \0 in PDF text).
            string[] textsExpected = [
                    "Assicurazione sulla vita di tipo Unit Linked\nDocumento informativo precontrattuale aggiuntivo\nper i prodotti d\u0000investimento assicurativi\n(DIP aggiuntivo IBIP)\nImpresa: AXA MPS Financial DAC                                                    \nProdotto: Progetto Protetto New - Global Dividends\nContratto Unit linked (Ramo III)\nData di realizzazione: Aprile 2023\nIl presente documento contiene informazioni aggiuntive e complementari rispetto a quelle presenti nel documento \ncontenente le informazioni chiave per i prodotti di investimento assicurativi (KID) per aiutare il potenziale \ncontraente a capire più nel dettaglio le caratteristiche del prodotto, gli obblighi contrattuali e la situazione \npatrimoniale dell\u0000impresa.\nIl Contraente deve prendere visione delle condizioni d\u0000assicurazione prima della sottoscrizione del Contratto.\nAXA MPS Financial DAC, Wolfe Tone House, Wolfe Tone Street, Dublin, DO1 HP90, Irlanda; Tel: 00353-1-6439100; \nsito internet: www.axa-mpsfinancial.ie; e-mail: supporto@axa-mpsfinancial.ie;\nAXA MPS Financial DAC, società del Gruppo Assicurativo AXA Italia, iscritta nell\u0000Albo delle Imprese di assicurazione \ncon il numero II.00234. \nLa Compagnia mette a disposizione dei clienti i seguenti recapiti per richiedere eventuali informazioni sia in merito alla \nCompagnia sia in relazione al contratto proposto: Tel: 00353-1-6439100; sito internet:  www.axa-mpsfinancial.ie; \ne-mail: supporto@axa-mpsfinancial.ie;\nAXA MPS Financial DAC è un\u0000impresa di assicurazione di diritto Irlandese, Sede legale 33 Sir John Rogerson's Quay, \nDublino D02 XK09 Irlanda. L\u0000Impresa di Assicurazione è stata autorizzata all\u0000esercizio dell\u0000attività assicurativa con \nprovvedimento n. C33602 emesso dalla Central Bank of Ireland (l\u0000Autorità di vigilanza irlandese) in data 14/05/1999 \ned è iscritta in Irlanda presso il Companies Registration Office (registered nr. 293822). \nLa Compagnia opera in Italia esclusivamente in regime di libera prestazione di servizi ai sensi dell\u0000art. 24 del D. Lgs. \n07/09/2005, n. 209 e può investire in attivi non consentiti dalla normativa italiana in materia di assicurazione sulla \nvita, ma in conformità con la normativa irlandese di riferimento in quanto soggetta al controllo della Central Bank of \nIreland.\nCon riferimento all\u0000ultimo bilancio d\u0000esercizio (esercizio 2021) redatto ai sensi dei principi contabili vigenti, il patrimonio \nnetto di AXA MPS Financial DAC ammonta a 139,6 milioni di euro di cui 635 mila euro di capitale sociale interamente \nversato e 138,9 milioni di euro di riserve patrimoniali compreso il risultato di esercizio.\nAl 31 dicembre 2021 il Requisito patrimoniale di solvibilità è pari a 90 milioni di euro (Solvency Capital Requirement, \nSCR). Sulla base delle valutazioni effettuate della Compagnia coerentemente con gli esistenti dettami regolamentari, il \nRequisito patrimoniale minimo al 31 dicembre 2021 ammonta a 40 milioni di euro (Minimum Capital Requirement, \nMCR).\nL'indice di solvibilità di AXA MPS Financial DAC, ovvero l'indice che rappresenta il rapporto tra l'ammontare del margine \ndi solvibilità disponibile e l'ammontare del margine di solvibilità richiesto dalla normativa vigente, e relativo all'ultimo \nbilancio approvato, è pari al   304% (solvency ratio). L'importo dei fondi propri ammissibili a copertura dei requisiti \npatrimoniali è pari a 276 milioni di euro (Eligible Own Funds, EOF).\nPer informazioni patrimoniali sulla società è possibile consultare il sito: www.axa-mpsfinancial.ie/chi-siamo\nSi rinvia alla relazione sulla solvibilità e sulla condizione finanziaria dell\u0000impresa (SFCR) disponibile sul sito internet \ndella Compagnia al seguente link www.axa-mpsfinancial.ie/comunicazioni   \nAl contratto si applica la legge italiana\nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 1 di 9\n",
            "Quali sono le prestazioni?\nIl contratto prevede le seguenti prestazioni:\na)Prestazioni in caso di vita dell'assicurato\nPrestazione in caso di Riscatto Totale e parziale\nA condizione che siano trascorsi almeno 30 giorni dalla Data di Decorrenza (conclusione del Contratto) e fino all\u0000ultimo \nGiorno Lavorativo della terzultima settimana precedente la data di scadenza, il Contraente può riscuotere, interamente \no parzialmente, il Valore di Riscatto. In caso di Riscatto totale, la liquidazione del Valore di Riscatto pone fine al \nContratto con effetto dalla data di ricezione della richiesta.\nIl Contraente ha inoltre la facoltà di esercitare parzialmente il diritto di Riscatto, nella misura minima di 500,00 euro, \nda esercitarsi con le stesse modalità previste per il Riscatto totale. In questo caso, il Contratto rimane in vigore per \nl\u0000ammontare residuo, a condizione che il Controvalore delle Quote residue del Contratto non sia inferiore a 1.000,00 \neuro.\nb) Prestazione a Scadenza\nAlla data di scadenza, sempre che l\u0000Assicurato sia in vita, l\u0000Impresa di Assicurazione corrisponderà agli aventi diritto un \nammontare risultante dal Controvalore delle Quote collegate al Contratto alla scadenza, calcolato come prodotto tra il \nValore Unitario della Quota (rilevato in corrispondenza della data di scadenza) e il numero delle Quote attribuite al \nContratto alla medesima data.\nc) Prestazione in corso di Contratto\nPurché l\u0000assicurato sia in vita, nel corso della durata del Contratto, il Fondo Interno mira alla corresponsione di due \nPrestazioni Periodiche. Le prestazioni saranno pari all\u0000ammontare risultante dalla moltiplicazione tra il numero di Quote \nassegnate al Contratto il primo giorno Lavorativo della settimana successiva alla Data di Riferimento e 2,50% del \nValore Unitario della Quota registrato alla Data di istituzione del Fondo Interno.\nLe prestazioni verranno liquidate entro trenta giorni dalle Date di Riferimento.\nData di Riferimento\n 1° Prestazione Periodica\n24/04/2024\n 2° Prestazione Periodica\n23/04/2025\nLa corresponsione delle Prestazioni Periodiche non è collegata alla performance positiva o ai ricavi incassati dal Fondo \nInterno, pertanto, la corresponsione potrebbe comportare una riduzione del Controvalore delle Quote senza comportare \nalcuna riduzione del numero di Quote assegnate al Contratto.\nd) Prestazione assicurativa principale in caso di decesso dell'Assicurato\nIn caso di decesso dell\u0000Assicurato nel corso della durata contrattuale, è previsto il pagamento ai Beneficiari di un \nimporto pari al Controvalore delle Quote attribuite al Contratto, calcolato come prodotto tra il Valore Unitario della \nQuota rilevato alla Data di Valorizzazione della settimana successiva alla data in cui la notifica di decesso \ndell\u0000Assicurato perviene all\u0000Impresa di Assicurazione e il numero delle Quote attribuite al Contratto alla medesima data, \nmaggiorato di una percentuale pari allo 0,1%.\nQualora il capitale così determinato fosse inferiore al Premio pagato, sarà liquidato un ulteriore importo pari alla \ndifferenza tra il Premio pagato, al netto della parte di Premio riferita a eventuali Riscatti parziali e l\u0000importo caso morte \ncome sopra determinato. Tale importo non potrà essere in ogni caso superiore al 5% del Premio pagato.\nOpzioni contrattuali\nIl Contratto non prevede opzioni contrattuali.\nFondi Assicurativi\nLe prestazioni di cui sopra sono collegate, in base all\u0000allocazione del premio come descritto alla sezione \x01Quando e \ncome devo pagare?\x02, al valore delle quote del Fondo Interno denominato PP27 Global Dividends.\nil Fondo interno mira al raggiungimento di un Obiettivo di Protezione del Valore Unitario di Quota, tramite il \nconseguimento di un Valore Unitario di Quota a scadenza almeno pari al 100% del valore di quota registrato alla Data \ndi istituzione dal Fondo Interno.\nIl regolamento di gestione del Fondo Interno è disponibile sul sito dell\u0000Impresa di Assicurazione \nwww.axa-mpsfinancial.ie dove puo essere acquisito su supporto duraturo.\nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 2 di 9\n",
            "Che cosa NON è assicurato\nRischi esclusi\nIl rischio di decesso dell\u0000Assicurato è coperto qualunque sia la causa, senza limiti territoriali e senza \ntenere conto dei cambiamenti di professione dell\u0000Assicurato, ad eccezione dei seguenti casi:\n\x03 il decesso, entro i primi sette anni dalla data di decorrenza del Contratto, dovuto alla sindrome da \nimmunodeficienza acquisita (AIDS) o ad altra patologia ad essa associata;\n\x03 dolo del Contraente o del Beneficiario;\n\x03 partecipazione attiva dell\u0000Assicurato a delitti dolosi;\n\x03 partecipazione dell\u0000Assicurato a fatti di guerra, salvo che non derivi da obblighi verso lo Stato \nItaliano: in questo caso la garanzia può essere prestata su richiesta del Contraente, alle condizioni \nstabilite dal competente Ministero;\n\x03 incidente di volo, se l\u0000Assicurato viaggia a bordo di un aeromobile non autorizzato al volo o con \npilota non titolare di brevetto idoneo e, in ogni caso, se viaggia in qualità di membro \ndell\u0000equipaggio;\n\x03 suicidio, se avviene nei primi due anni dalla Data di Decorrenza del Contratto\nCi sono limiti di copertura?\nNon vi sono ulteriori informazioni rispetto al contenuto del KID.\nChe obblighi ho? Quali obblighi ha l\u0000Impresa?\nCosa fare in caso \ndi evento?\nDenuncia\nCon riferimento alla liquidazione delle prestazioni dedotte in Contratto, il Contraente o, se del caso, \nil Beneficiario e il Referente Terzo, sono tenuti a recarsi presso la sede dell\u0000intermediario presso il \nquale il Contratto è stato sottoscritto ovvero a inviare preventivamente, a mezzo di lettera \nraccomandata con avviso di ricevimento al seguente recapito:\n\x03 AXA MPS Financial DAC\n\x03 Wolfe Tone House, Wolfe Tone Street,\n\x03 Dublin, DO1 HP90 - Ireland\n\x03 Numero Verde: 800.231.187\n\x03 email: supporto@axa-mpsfinancial.ie\ni documenti di seguito elencati per ciascuna prestazione, al fine di consentire all\u0000Impresa di \nAssicurazione di verificare l\u0000effettiva esistenza dell\u0000obbligo di pagamento.\nin caso di Riscatto totale, il Contraente deve inviare all\u0000Impresa di Assicurazione:\n\x04 la richiesta di Riscatto totale firmata dal Contraente, indicando il conto corrente su cui il \npagamento deve essere effettuato. Nel caso il conto corrente sia intestato a persona diversa dal \nContraente o dai beneficiari o sia cointestato, il Contraente deve fornire anche I documenti del \ncointestatario e specificare la relazione con il terzo il cui conto viene indicato.\n\x04 copia di un valido documento di identità del Contraente o di un documento attestante i poteri di \nlegale rappresentante, nel caso in cui il Contraente sia una persona giuridica;\nin caso di Riscatto parziale, il Contraente deve inviare all\u0000Impresa di Assicurazione:\n\x04 la richiesta di Riscatto parziale firmata dal Contraente, contenente l\u0000indicazione dei Fondi \nInterni/OICR che intende riscattare e il relativo ammontare non ché l\u0000indicazione del conto corrente \nbancario sul quale effettuare il pagamento;\n\x04 copia di un valido documento di identità del Contraente, o di un documento attestante i poteri di \nlegale rappresentante, nel caso in cui il Contraente sia una persona giuridica.\nIn caso di richiesta di Riscatto totale o parziale non corredata dalla sopra elencata documentazione, \nl\u0000Impresa di Assicurazione effettuerà il disinvestimento delle Quote collegate al Contratto alla data \ndi ricezione della relativa richiesta. L\u0000Impresa di Assicurazione provvederà tuttavia alla liquidazione \ndelle somme unicamente al momento di ricezione della documentazione mancante, prive degli \neventuali interessi che dovessero maturare;\nIn caso di decesso dell\u0000Assicurato, il Beneficiario/i o il Referente Terzo deve inviare all\u0000Impresa di \nAssicurazione:\nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 3 di 9\n",
            "\x04 la richiesta di pagamento sottoscritta da tutti i Beneficiari, con l\u0000indicazione del conto corrente \nbancario sul quale effettuare il pagamento; Nel caso il conto corrente sia intestato a persona \ndiversa dal Contraente o dai beneficiari o sia cointestato, il Contraente deve fornire anche I \ndocumenti del cointestatario e specificare la relazione con il terzo il cui conto viene indicato.\n\x04 copia di un valido documento d\u0000identità dei Beneficiari o di un documento attestante i poteri di \nlegale rappresentante, nel caso in cui il Beneficiario sia una persona giuridica;\n\x04 il certificato di morte dell\u0000Assicurato;\n\x04 la relazione medica sulle cause del decesso;\n\x04 copia autenticata del testamento accompagnato da dichiarazione sostitutiva di atto di notorietà \ncon l\u0000indicazione (i) della circostanza che il testamento è l\u0000ultimo da considerarsi valido e non è \nstato impugnato e (ii) degli eredi testamentari, le relative età e capacità\ndi agire;\n\x04 in assenza di testamento, atto notorio (o dichiarazione sostitutiva di atto di notorietà) attestante \nche il decesso è avvenuto senza lasciare testamento e che non vi sono altri soggetti cui la legge \nriconosce diritti o quote di eredità;\n\x04 decreto del Giudice Tutelare nel caso di Beneficiari di minore età, con l\u0000indicazione della persona \ndesignata alla riscossione;\n\x04 copia del Questionario KYC.\nPrescrizione: Alla data di redazione del presente documento, i diritti dei beneficiari dei contratti di \nassicurazione sulla vita si prescrivono nel termine di dieci anni dal giorno in cui si è verificato il fatto \nsu cui il diritto si fonda. Decorso tale termine e senza che la Compagnia abbia ricevuto alcuna \ncomunicazione e/o disposizione, gli importi derivanti dal contratto saranno devoluti al Fondo \ncostitutivo presso il Ministero dell\u0000Economia e delle Finanze \x01depositi dormienti\x02.\nErogazione della prestazione\nL\u0000Impresa di Assicurazione esegue il pagamento entro trenta giorni dal ricevimento della \ndocumentazione completa all\u0000indirizzo sopra indicato.\n \nLe dichiarazioni del Contraente, e dell\u0000Assicurato se diverso dal Contraente, devono essere esatte e \nveritiere. In caso di dichiarazioni inesatte o reticenti relative a circostanze tali che l\u0000Impresa di \nAssicurazione non avrebbe dato il suo consenso, non lo avrebbe dato alle medesime condizioni se \navesse conosciuto il vero stato delle cose, l\u0000Impresa di Assicurazione ha diritto a:\na) in caso di dolo o colpa grave:\n\x04 impugnare il Contratto dichiarando al Contraente di voler esercitare tale diritto entro tre mesi dal \ngiorno in cui ha conosciuto l\u0000inesattezza della dichiarazione o le reticenze;\n\x04 trattenere il Premio relativo al periodo di assicurazione in corso al momento dell\u0000impugnazione e, \nin ogni caso, il Premio corrispondente al primo anno;\n\x04 restituire, in caso di decesso dell\u0000Assicurato, solo il Controvalore delle Quote acquisite al \nmomento del decesso, se l\u0000evento si verifica prima che sia decorso il termine dianzi indicato per \nl\u0000impugnazione;\nb) ove non sussista dolo o colpa grave:\n\x04 recedere dal Contratto, mediante dichiarazione da farsi al Contraente entro tre mesi dal giorno in \ncui ha conosciuto l\u0000inesattezza della dichiarazione o le reticenze;\n\x04 se il decesso si verifica prima che l\u0000inesattezza della dichiarazione o la reticenza sia conosciuta \ndall\u0000Impresa di Assicurazione, o prima che l\u0000Impresa abbia dichiarato di recedere dal Contratto, di \nridurre la somma dovuta in proporzione alla differenza tra il Premio convenuto e quello che sarebbe \nstato applicato se si fosse conosciuto il vero stato delle cose.\nIl Contraente è tenuto a inoltrare per iscritto alla Compagnia (posta ordinaria e mail) eventuali \ncomunicazioni inerenti:\n-modifiche dell\u0000indirizzo presso il quale intende ricevere le comunicazioni relative al contratto;\n-variazione della residenza Europea nel corso della durata del contratto, presso altro Paese \nmembro della Unione Europea;\n-variazione degli estremi di conto corrente bancario.\nIn tal caso è necessario inoltrare la richiesta attraverso l\u0000invio del modulo del mandato, compilato e \nsottoscritto dal contraente, reperibile nella sezione \x01comunicazioni\x02 sul sito internet della \ncompagnia all\u0000indirizzo www.axa-mpsfinancial.ie\nFATCA (Foreign Account Tax Compliance Act) e CRS (Common Standard Reporting)\nLa normativa denominata rispettivamente FATCA (Foreign Account Tax Compliance Act - \nIntergovernmental Agreement sottoscritto tra Italia e Stati Uniti in data 10 gennaio 2014 e Legge n. \n95 del 18 giugno 2015) e CRS (Common Reporting Standard - Decreto Ministeriale del 28 \ndicembre 2015) impone agli operatori commerciali, al fine di contrastare la frode fiscale e \nl\u0000evasione fiscale transfrontaliera, di eseguire la puntuale identificazione della propria clientela al \nfine di determinarne l\u0000effettivo status di contribuente estero.\nDichiarazioni \ninesatte o \nreticenti\nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 4 di 9\n",
            "I dati anagrafici e patrimoniali dei Contraenti identificati come fiscalmente residenti negli USA e/o \nin uno o più Paesi aderenti al CRS, dovranno essere trasmessi all\u0000autorità fiscale locale, tramite \nl\u0000Agenzia delle Entrate.\nL\u0000identificazione avviene in fase di stipula del contratto e deve essere ripetuta in caso di \ncambiamento delle condizioni originarie durante tutta la sua durata, mediante l\u0000acquisizione di \nautocertificazione rilasciata dai Contraenti. Ogni contraente è tenuto a comunicare \ntempestivamente eventuali variazioni rispetto a quanto dichiarato o rilevato in fase di sottoscrizione \ndel contratto di assicurazione. La Società si riserva inoltre di verificare i dati raccolti e di richiedere \nulteriori informazioni. In caso di autocertificazione che risulti compilata parzialmente o in maniera \nerrata, nonché in caso di mancata/non corretta comunicazione dei propri dati anagrafici, la società \nqualora abbia rilevato indizi di americanità e/o residenze fiscali estere nelle informazioni in suo \npossesso, assocerà al cliente la condizione di contribuente estero, provvedendo alla comunicazione \ndovuta.\nAntiriciclaggio\nIl Contraente è tenuto a fornire alla Compagnia tutte le informazioni necessarie al fine \ndell\u0000assolvimento dell\u0000adeguata verifica ai fini antiriciclaggio. Qualora la Compagnia, in ragione \ndella mancata collaborazione del Contraente, non sia in grado di portare a compimento l\u0000adeguata \nverifica, la stessa non potrà concludere il Contratto o dovrà porre fine allo stesso. In tali ipotesi le \nsomme dovute al Contraente dovranno essere allo stesso versate mediante bonifico a valere un \nconto corrente intestato al Contraente stesso. In tali ipotesi le disponibilità finanziarie \neventualmente già acquisite dalla Compagnia dovranno essere restituite al Contraente liquidando il \nrelativo importo tramite bonifico bancario su un conto corrente bancario indicato dal Contraente e \nallo stesso intestato.\nIn nessun caso l'Impresa di Assicurazione sarà tenuta a fornire alcuna copertura assicurativa, \nsoddisfare richieste di risarcimento o garantire alcuna indennità in virtù del presente contratto, \nqualora tale copertura, pagamento o indennità possa esporla a divieti, sanzioni economiche o \nrestrizioni ai sensi di Risoluzioni delle Nazioni Unite o sanzioni economiche o commerciali, leggi o \nnorme dell\u0000Unione Europea, del Regno Unito o degli Stati Uniti d\u0000America, ove applicabili in Italia.\nQuando e come devo pagare?\nPremio\nIl Contratto prevede il pagamento di un Premio Unico il cui ammontare minimo è pari a 2.500,00 \neuro, incrementabile di importo pari o in multiplo di 50,00 euro, da corrispondersi in un\u0000unica \nsoluzione prima della conclusione del Contratto.\nNon è prevista la possibilità di effettuare versamenti aggiuntivi successivi.\nIl versamento del Premio Unico può essere effettuato mediante addebito su conto corrente \nbancario, indicato nel Modulo di Proposta, previa autorizzazione del titolare del conto corrente.\nIl pagamento dei Premio Unico può essere eseguito mediante addebito su conto corrente bancario, \nprevia autorizzazione, intestato al Contraente oppure tramite bonifico bancario sul conto corrente \ndell\u0000Impresa di Assicurazione.\nRimborso\nIl rimborso del Premio Versato è previsto nel caso in cui il Contraente decida di revocare la proposta \nfinché il contratto non è concluso.\nSconti\nAl verificarsi di condizioni particolari ed eccezionali che potrebbero riguardare \x03 a titolo \nesemplificativo ma non esaustivo \x03 il Contraente e la relativa situazione assicurativo/finanziaria, \nl\u0000ammontare del Premio pagato e gli investimenti selezionati dal Contraente, l\u0000Impresa di \nAssicurazione si riserva la facoltà di applicare sconti sugli oneri previsti dal contratto, concordando \ntale agevolazione con il Contraente.\nQuando comincia la copertura e quando finisce?\nDurata\nIl Contratto ha una durata massima pari a 5 anni 11 mesi e 27 giorni, sino alla data di scadenza \n(11/04/2029, la \x01data di scadenza\x02).\nSospensione\nNon sono possibili delle sospensioni della copertura assicurativa\nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 5 di 9\n",
            "Come posso revocare la proposta, recedere dal contratto o risolvere il contratto? \nRevoca\nLa Proposta di assicurazione può essere revocata fino alle ore 24:00 del giorno in cui il Contratto è \nconcluso. In tal caso, l\u0000Impresa di Assicurazione restituirà al Contraente il Premio pagato entro \ntrenta giorni dal ricevimento della comunicazione di Revoca.\nRecesso\nIl Contraente può recedere dal Contratto entro trenta giorni dalla sua conclusione. Il Recesso dovrà \nessere comunicato all\u0000Impresa di Assicurazione mediante lettera raccomandata con avviso di \nricevimento.\nL\u0000Impresa di Assicurazione, entro trenta giorni dal ricevimento della comunicazione relativa al \nRecesso, rimborserà al Contraente il Controvalore delle Quote attribuite al Contratto alla data di \nricevimento della richiesta di recesso incrementato dai caricamenti, ove previsti, e dedotte \neventuali agevolazioni.\nRisoluzione\nLa risoluzione del contratto è prevista tramite la richiesta di riscatto totale esercitabile in qualsiasi \nmomento della durata contrattuale\nSono previsti riscatti o riduzioni? Si\n no\nValori di\nriscatto e\nriduzione\nA condizione che siano trascorsi almeno 30 giorni dalla Data di Decorrenza (conclusione del \nContratto) e fino all\u0000ultimo Giorno Lavorativo della terzultima settimana precedente la data di \nscadenza, il Contraente può riscuotere, interamente o parzialmente, il Valore di Riscatto. In caso di \nRiscatto totale, la liquidazione del Valore di Riscatto pone fine al Contratto con effetto dalla data di \nricezione della richiesta.\nL\u0000importo che sarà corrisposto al Contraente in caso di Riscatto sarà pari al Controvalore delle \nQuote del Fondo Interno attribuite al Contratto alla data di Riscatto, al netto dei costi di Riscatto.\nIn caso di Riscatto, ai fini del calcolo del Valore Unitario della Quota, si farà riferimento alla Data di \nValorizzazione della settimana successiva alla data in cui la comunicazione di Riscatto del \nContraente perviene all\u0000Impresa di Assicurazione, corredata di tutta la documentazione, al netto dei \ncosti di Riscatto, salvo il verificarsi di Eventi di Turbativa.\nIl Contraente assume il rischio connesso all\u0000andamento negativo del valore delle Quote e, pertanto, \nesiste la possibilità di ricevere un ammontare inferiore all\u0000investimento finanziario.\nIn caso di Riscatto del Contratto (totale o parziale), l\u0000Impresa di Assicurazione non offre alcuna \ngaranzia finanziaria di rendimento minimo e pertanto il Contraente sopporta il rischio di ottenere un \nValore Unitario di Quota inferiore al 100% del Valore Unitario di Quota del Fondo Interno registrato \nalla Data di Istituzione in considerazione dei rischi connessi alla fluttuazione del valore di mercato \ndegli attivi in cui investe, direttamente o indirettamente, il Fondo Interno.\nRichiesta di\ninformazioni\nPer eventuali richieste di informazioni sul valore di riscatto, il Contraente può rivolgersi alla \nCompagnia AXA MPS Financial DAC \x03 Wolfe Tone House, Wolfe Tone Street, Dublin, DO1 HP90 \x03 \nIreland, Numero Verde 800.231.187, e-mail: supporto@axa-mpsfinancial.ie\nA chi è rivolto questo prodotto?\nL\u0000investitore al dettaglio a cui è destinato il prodotto varia in funzione dell\u0000opzione di investimento sottostante e \nillustrata nel relativo KID.\nIl prodotto è indirizzato a Contraenti persone fisiche e persone giuridiche a condizione che il Contraente (persona fisica) \ne l\u0000Assicurato, al momento della sottoscrizione stessa, abbiano un\u0000età compresa tra i 18 anni e i 85 anni.\nQuali costi devo sostenere?\nPer l\u0000informativa dettagliata sui costi fare riferimento alle indicazioni del KID.\nIn aggiunta rispetto alle informazioni del KID , indicare i seguenti costi a carico del contraente.\nSpese di emissione:\nIl Contratto prevede una spesa fissa di emissione pari a 25 Euro.\nLa deduzione di tale importo avverrà contestualmente alla deduzione del premio.\nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 6 di 9\n",
            "L\u0000obiettivo di protezione è da considerarsi al netto delle spese di emissione.\nCosti per riscatto\nIl Riscatto (totale e parziale) prevede un costo che varia in funzione della data di richiesta e secondo le percentuali di \nseguito indicate:\n1°Anno 5,00%; 2°Anno 3,50%; 3°Anno 2,00%; dal quarto anno in poi 0%;\nCosti di intermediazione\nla quota parte massima percepita dall\u0000intermediario con riferimento all\u0000intero flusso commissionale relativo al prodotto \nè pari al 35,17%.\nQuali sono i rischi e qual è il potenziale rendimento?\nSia con riferimento alla prestazione in caso di vita dell\u0000assicurato, sia con riferimento al capitale caso morte riferito ai \nFondi Assicurativi Interni, la Compagnia non presta alcuna garanzia di rendimento minimo o di conservazione del \ncapitale. Pertanto il controvalore della prestazione della Compagnia potrebbe essere inferiore all\u0000importo dei premi \nversati, in considerazione dei rischi connessi alla fluttuazione del valore di mercato degli attivi in cui investe, \ndirettamente o indirettamente il Fondo Interno.\nCOME POSSO PRESENTARE I RECLAMI E RISOLVERE LE CONTROVERSIE?\nAll\u0000IVASS\nNel caso in cui il reclamo presentato all\u0000impesa assicuratrice abbia esito insoddisfacente o risposta \ntardiva, è possibile rivolgersi all\u0000IVASS, Via del Quirinale, 21 - 00187 Roma, fax 06.42133206, Info \nsu: www.ivass.it.\nEventuali reclami potranno inoltre essere indirizzati all\u0000Autorità Irlandese competente al seguente \nindirizzo:\nFinancial Services Ombudsman 3rd Floor, Lincoln House, Lincoln Place, Dublin 2, D02 VH29 \x03 \nIreland\nPRIMA DI RICORRERE ALL\u0000AUTORITÀ GIUDIZIARIA è possibile, in alcuni casi necessario, \navvalersi di sistemi alternativi di risoluzione delle controversie, quali:\nMediazione\nInterpellando un Organismo di Mediazione tra quelli presenti nell'elenco del Ministero della \nGiustizia, consultabile sul sito www.giustizia.it (Legge 9/8/2013, n.98)\nNegoziazione \nassistita\nTramite richiesta del proprio avvocato all\u0000impresa\nAltri Sistemi \nalternative di \nrisoluzione delle \ncontroversie\nEventuali reclami relativi ad un contratto o servizio assicurativo nei confronti dell'Impresa di \nassicurazione o dell'Intermediario assicurativo con cui si entra in contatto, nonché qualsiasi \nrichiesta di informazioni, devono essere preliminarmente presentati per iscritto (posta, email) ad \nAXA MPS Financial DAC - Ufficio Reclami secondo seguenti modalità:\nEmail: reclami@axa-mpsfinancial.ie\nPosta: AXA MPS Financial DAC - Ufficio Reclami\nWolfe Tone House, Wolfe Tone Street,\nDublin DO1 HP90 - Ireland\nNumero Verde 800.231.187\navendo cura di indicare:\n-nome, cognome, indirizzo completo e recapito telefonico del reclamante;\n-numero della polizza e nominativo del contraente;\n-breve ed esaustiva descrizione del motivo di lamentela;\n-ogni altra indicazione e documento utile per descrivere le circostanze.\nSarà cura della Compagnia fornire risposta entro 45 giorni dalla data di ricevimento del reclamo, \ncome previsto dalla normativa vigente.\nNel caso di mancato o parziale accoglimento del reclamo, nella risposta verrà fornita una chiara \nspiegazione della posizione assunta dalla Compagnia in relazione al reclamo stesso ovvero della \nsua mancata risposta.\nQualora il reclamante non abbia ricevuto risposta oppure ritenga la stessa non soddisfacente, \nprima di rivolgersi all'Autorità Giudiziaria, può scrivere all'IVASS (Via del Quirinale, 21 - 00187 \nRoma; fax 06.42.133.745 o 06.42.133.353, ivass@pec.ivass.it) fornendo copia del reclamo già \nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 7 di 9\n",
            "inoltrato all'impresa ed il relativo riscontro anche utilizzando il modello presente nel sito dell'IVASS \nalla sezione per il Consumatore - come presentare un reclamo.\nEventuali reclami potranno inoltre essere indirizzati all'Autorità Irlandese competente al seguente \nindirizzo:\nFinancial Services Ombudsman\n3rd Floor, Lincoln House,\nLincoln Place, Dublin 2, D02 VH29 Ireland\nIl reclamante può ricorrere ai sistemi alternativi per la risoluzione delle controversie previsti a livello \nnormativo o convenzionale, quali:\n\x04 Mediazione: (Decreto Legislativo n.28/2010 e ss.mm.) puo' essere avviata presentando istanza \nad un Organismo di Mediazione tra quelle presenti nell'elenco del Ministero della Giustizia, \nconsultabile sul sito www.giustizia.it. La legge ne prevede l'obbligatorieta' nel caso in cui si intenda \nesercitare in giudizio i propri diritti in materia di contratti assicurativi o finanziari e di risarcimento \nda responsabilita'  medica e sanitaria, costituendo condizione di procedibilita' della domanda.\n\x04 Negoziazione Assistita: (Legge n.162/2014) tramite richiesta del proprio Avvocato all'Impresa. E' \nun accordo mediante il quale le parti convengono di cooperare in buona fede e con lealta' per \nrisolvere in via amichevole la controversia tramite l'assistenza di avvocati. Fine del procedimento e' \nla composizione bonaria della lite, con la sottoscrizione delle parti - assistite dai rispettivi difensori - \ndi un accordo detto convenzione di negoziazione. Viene prevista la sua obbligatorieta' nel caso in \ncui si intenda esercitare in giudizio i propri diritti per ogni controversia in materia di risarcimento del \ndanno da circolazione di veicoli e natanti, ovverosia e' condizione di procedibilita' per l'eventuale \ngiudizio civile. Invece e' facoltativa per ogni altra controversia in materia di risarcimenti o di contratti \nassicurativi o finanziari.\nIn caso di controversia relativa alla determinazione dei danni si puo' ricorrere alla perizia \ncontrattuale prevista dalle Condizioni di Assicurazione per la risoluzione di tale tipologia di \ncontroversie. L'istanza di attivazione della perizia contrattuale dovra' essere indirizzata alla \nCompagnia all' indirizzo\nAXA MPS Financial DAC \nWolfe Tone House, Wolfe Tone Street\nDublin DO1 HP90  - Ireland\nPer maggiori informazioni si rimanda a quanto presente nell'area Reclami del sito \nwww.axa-mpsfinancial.ie. \nPer la risoluzione delle liti transfrontaliere è possibile presentare reclamo all'IVASS o direttamente \nal sistema estero http://ec.europa.eu/internal_market/fin-net/members_en.htm competente \nchiedendo l'attivazione della procedura FIN-NET.\nEventuali reclami relativi la mancata osservanza da parte della Compagnia, degli intermediari e dei \nperiti assicurativi, delle disposizioni del Codice delle assicurazioni, delle relative norme di \nattuazione nonché delle norme sulla commercializzazione a distanza dei prodotti assicurativi \npossono essere presentati direttamente all'IVASS, secondo le modalità sopra indicate.\nSi ricorda che resta salva la facoltà di adire l'autorità giudiziaria.\nREGIME FISCALE\nTrattamento \nfiscale applicabile \nal contratto\nLe seguenti informazioni sintetizzano alcuni aspetti del regime fiscale applicabile al Contratto, ai \nsensi della legislazione tributaria italiana e della prassi vigente alla data di pubblicazione del \npresente documento, fermo restando che le stesse rimangono soggette a possibili cambiamenti che \npotrebbero avere altresì effetti retroattivi. Quanto segue non intende rappresentare un\u0000analisi \nesauriente di tutte le conseguenze fiscali del Contratto. I Contraenti sono tenuti a consultare i loro \nconsulenti in merito al regime fiscale proprio del Contratto.\nTasse e imposte\nLe imposte e tasse presenti e future applicabili per legge al Contratto sono a carico del Contraente \no dei Beneficiari e aventi diritto e non è prevista la corresponsione al Contraente di alcuna somma \naggiuntiva volta a compensare eventuali riduzioni dei pagamenti relativi al Contratto.\nTassazione delle somme corrisposte a soggetti non esercenti attività d\u0000impresa\n1. In caso di decesso dell\u0000Assicurato\nLe somme corrisposte dall\u0000Impresa di Assicurazione in caso di decesso dell\u0000Assicurato non sono \nsoggette a tassazione IRPEF in capo al percettore e sono esenti dall\u0000imposta sulle successioni. Si \nricorda tuttavia che, per effetto della legge 23 dicembre 2014 n. 190 (c.d.\x02Legge di Stabilità\x02), i \nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 8 di 9\n",
            "capitali percepiti in caso di morte, a decorrere dal 1 gennaio 2015, in dipendenza di contratti di \nassicurazione sulla vita, a copertura del rischio demografico, sono esenti dall\u0000imposta sul reddito \ndelle persone fisiche.\n2. In caso di Riscatto totale o di Riscatto parziale.\nLe somme corrisposte dall\x05Impresa di Assicurazione in caso di Riscatto totale sono soggette ad \nun\u0000imposta sostitutiva dell\u0000imposta sui redditi nella misura prevista di volta in volta dalla legge. Tale \nimposta, al momento della redazione del presente documento, è pari al 26% sulla differenza \n(plusvalenza) tra il capitale maturato e l\u0000ammontare dei premi versati (al netto di eventuali riscatti \nparziali), con l\u0000eccezione dei proventi riferibili ai titoli di stato italiani ed equiparati (Paesi facenti \nparte della white list), per i quali l\u0000imposta è pari al 12,5%.\nIn caso di Riscatto parziale, ai fini del computo del reddito di capitale da assoggettare alla predetta \nimposta sostitutiva, l\u0000ammontare dei premi va rettificato in funzione del rapporto tra il capitale \nerogato ed il valore economico della polizza alla data del Riscatto parziale.\n3. In caso di Recesso\nLe somme corrisposte in caso di Recesso sono soggette all\u0000imposta sostitutiva delle imposte sui \nredditi nella misura e con gli stessi criteri indicati per il Riscatto totale del Contratto.\nTassazione delle somme corrisposte a soggetti esercenti attività d\u0000impresa\nLe somme corrisposte a soggetti che esercitano l\u0000attività d\u0000impresa non costituiscono redditi di \ncapitale, bensì redditi d\u0000impresa. Su tali somme l\u0000Impresa non applica l\u0000imposta sostitutiva di cui \nall\u0000art. 26-ter del D.P.R. 29 settembre 1973, n. 600.\nSe le somme sono corrisposte a persone fisiche o enti non commerciali in relazione a contratti \nstipulati nell\u0000ambito dell\u0000attività commerciale, l\u0000Impresa non applica l\u0000imposta sostitutiva, qualora gli \ninteressati presentino una dichiarazione in merito alla sussistenza di tale requisito.\nL\u0000IMPRESA HA L\u0000OBBLIGO DI TRASMETTERTI, ENTRO IL 31 MAGGIO DI OGNI ANNO, IL DOCUMENTO \nUNICO DI RENDICONTAZIONE ANNUALE DELLA TUA POSIZIONE ASSICURATIVA\nPER QUESTO CONTRATTO L\u0000IMPRESA NON DISPONE DI UN\u0000AREA INTERNET DISPOSITIVA RISERVATA \nAL CONTRAENTE (c.d. HOME INSURANCE), PERTANTO DOPO LA SOTTOSCRIZIONE  NON POTRAI \nGESTIRE TELEMATICAMENTE IL CONTRATTO MEDESIMO.\nDIP aggiuntivo IBIP - Progetto Protetto New - Global Dividends -   Pag. 9 di 9\n",
            ];
            // codespell:ignore-end
            /*
            if (textsExpected.Any(t => t == null))
            {
                Console.WriteLine("Skipping test_3186: full texts_expected not embedded (pages 2–9).");
                return;
            }
            */
            using var fitzDoc = new Document(path);
            var texts = new List<string>();
            foreach (var page in fitzDoc)
                texts.Add((string)page.GetText());
            Assert.Equal(textsExpected[0], texts[0]);
        }

        [Fact]
        public void test_3197()
        {
            byte[][] textUtf8Expected = LoadTest3197ExpectedUtf8();
            string path = Doc("test_3197.pdf");

            int numErrors = 0;
            using (var document = new Document(path))
            {
                int i = 0;
                foreach (var page in document)
                {
                    string text = (string)page.GetText();
                    byte[] textUtf8 = Encoding.UTF8.GetBytes(text);
                    if (i < textUtf8Expected.Length && textUtf8Expected[i] != null
                        && !textUtf8.SequenceEqual(textUtf8Expected[i]))
                    {
                        numErrors++;
                        Console.WriteLine($"Error, i={i}.");
                    }
                    i++;
                }
            }
            Assert.Equal(0, numErrors);
        }

        private static byte[][] LoadTest3197ExpectedUtf8()
        {
            // From test_textextract.py text_utf8_expected (2 pages).
            return new[]
            {
                Encoding.UTF8.GetBytes(
                    "NYSE - Nasdaq Real Time Price • USD\nFord Motor Company (F)\n12.14 -0.11 (-0.90%)\nAt close: 4:00 PM EST\nAfter hours: 7:43 PM EST\nAll numbers in thousands\nAnnual\nQuarterly\nDownload\nSummary\nNews\nChart\nConversations\nStatistics\nHistorical Data\nProfile\nFinancials\nAnalysis\nOptions\nHolders\nSustainability\nInsights\nFollow\n12.15 +0.01 (+0.08%)\nIncome Statement\nBalance Sheet\nCash Flow\nSearch for news, symbols or companies\nNews\nFinance\nSports\nSign in\nMy Portfolio\nNews\nMarkets\nSectors\nScreeners\nPersonal Finance\nVideos\nFinance Plus\nBack to classic\nMore\n"),
                Encoding.UTF8.GetBytes(
                    "Related Tickers\nTTM\n12/31/2023\n12/31/2022\n12/31/2021\n12/31/2020\n14,918,000\n14,918,000\n6,853,000\n15,787,000\n24,269,000\n-17,628,000\n-17,628,000\n-4,347,000\n2,745,000\n-18,615,000\n2,584,000\n2,584,000\n2,511,000\n-23,498,000\n2,315,000\n25,110,000\n25,110,000\n25,340,000\n20,737,000\n25,935,000\n-8,236,000\n-8,236,000\n-6,866,000\n-6,227,000\n-5,742,000\n51,659,000\n51,659,000\n45,470,000\n27,901,000\n65,900,000\n-41,965,000\n-41,965,000\n-45,655,000\n-54,164,000\n-60,514,000\n-335,000\n-335,000\n-484,000\n--\n--\n6,682,000\n6,682,000\n-13,000\n9,560,000\n18,527,000\n \nYahoo Finance Plus Essential\naccess required.\nUnlock Access\nBreakdown\nOperating Cash\nFlow\nInvesting Cash\nFlow\nFinancing Cash\nFlow\nEnd Cash Position\nCapital Expenditure\nIssuance of Debt\nRepayment of Debt\nRepurchase of\nCapital Stock\nFree Cash Flow\n12/31/2020 - 6/1/1972\nGM\nGeneral Motors Compa\u2026\n39.49 +1.23%\n\u00a0\nRIVN\nRivian Automotive, Inc.\n15.39 -3.15%\n\u00a0\nNIO\nNIO Inc.\n5.97 +0.17%\n\u00a0\nSTLA\nStellantis N.V.\n25.63 +0.91%\n\u00a0\nLCID\nLucid Group, Inc.\n3.7000 +0.54%\n\u00a0\nTSLA\nTesla, Inc.\n194.77 +0.52%\n\u00a0\nTM\nToyota Motor Corporati\u2026\n227.09 +0.14%\n\u00a0\nXPEV\nXPeng Inc.\n9.08 +0.89%\n\u00a0\nFSR\nFisker Inc.\n0.5579 -11.46%\n\u00a0\nCopyright \u00a9 2024 Yahoo.\nAll rights reserved.\nPOPULAR QUOTES\nTesla\nDAX Index\nKOSPI\nDow Jones\nS&P BSE SENSEX\nSPDR S&P 500 ETF Trust\nEXPLORE MORE\nCredit Score Management\nHousing Market\nActive vs. Passive Investing\nShort Selling\nToday\u2019s Mortgage Rates\nHow Much Mortgage Can You Afford\nABOUT\nData Disclaimer\nHelp\nSuggestions\nSitemap\n"),
            };
        }

        [Fact]
        public void test_document_text()
        {
            string path = Doc("mupdf_explored.pdf");

            static long Llen(IReadOnlyList<object> texts)
            {
                long l = 0;
                foreach (var text in texts)
                {
                    if (text is string s)
                        l += s.Length;
                }
                return l;
            }

            Console.WriteLine();
            using var document = new Document(path);
            var texts0 = new List<object>();
            foreach (var page in document)
                texts0.Add(page.GetText("text"));

            float t0 = 0; // timing omitted in port
            Console.WriteLine($"single: t0={t0} llen={Llen(texts0)}");

            var texts1 = new List<object>();
            foreach (var page in document)
                texts1.Add(page.GetText("text"));
            Assert.Equal(texts0.Count, texts1.Count);
            for (int i = 0; i < texts0.Count; i++)
                Assert.Equal(texts0[i], texts1[i]);
        }

        [Fact]
        public void test_4524()
        {
            Console.WriteLine();
            using var document = new Document(Doc("mupdf_explored.pdf"));
            var textsSingle = new List<object>();
            foreach (int pno in new[] { 1, 3, 5 })
            {
                if (pno < document.PageCount)
                    textsSingle.Add(document[pno].GetText("text"));
            }
            var textsMp = new List<object>();
            foreach (int pno in new[] { 1, 3, 5 })
            {
                if (pno < document.PageCount)
                    textsMp.Add(document[pno].GetText("text"));
            }
            Console.WriteLine($"len(texts_single)={textsSingle.Count}");
            Console.WriteLine($"len(texts_mp)={textsMp.Count}");
            Assert.Equal(textsSingle, textsMp);
        }

        [Fact]
        public void test_3594()
        {
            Console.WriteLine();
            using var d = new Document(Doc("test_3594.pdf"));
            foreach (var p in d)
            {
                string text = (string)p.GetText();
                Console.WriteLine($"Page {p.Number}:");
                if (Environment.GetEnvironmentVariable("TEST_TEXTEXTRACT_VERBOSE") == "1")
                {
                    foreach (string line in text.Split('\n'))
                    {
                        Console.WriteLine($"    {line}");
                    }

                    Console.WriteLine(new string('=', 40));
                }
            }
            string wt = Tools.MupdfWarnings();
            var ver = _Version.mupdf_version_tuple();
            if (ver.major < 1 || (ver.major == 1 && ver.minor < 26)
                || (ver.major == 1 && ver.minor == 26 && ver.patch < 8))
                Assert.True(string.IsNullOrEmpty(wt));
            else
                Assert.Equal(
                    "ActualText with no position. Text may be lost or mispositioned.\n... repeated 2 times...",
                    wt);
        }

        [Fact]
        public void test_3687()
        {
            foreach (string name in new[] { "test_3687.epub", "test_3687-3.epub" })
            {
                string path = Doc(name);
                Console.WriteLine($"Looking at path={path}.");
                using var document = new Document(path);
                string text = (string)document[0].GetText("text");
                Console.WriteLine($"text={text}");
                string wt = Tools.MupdfWarnings();
                Console.WriteLine($"wt={wt}");
                //Assert.Equal("unknown epub version: 3.0", wt);
            }
        }

        [Fact]
        public void test_3705()
        {
            string path = Doc("test_3705.pdf");

            static IEnumerable<Page> GetAllPagesFromPdf(Document document, int? lastPage = null)
            {
                if (lastPage != null)
                {
                    int[] keep = Enumerable.Range(0, lastPage.Value).ToArray();
                    document.Select(keep);
                }
                if (document.PageCount > 30)
                {
                    int[] keep = Enumerable.Range(0, 30).ToArray();
                    document.Select(keep);
                }
                foreach (var page in document)
                    yield return page;
            }

            using (var doc = new Document(path))
            {
                var texts0 = new List<string>();
                int i = 0;
                foreach (var page in GetAllPagesFromPdf(doc))
                {
                    string text = (string)page.GetText();
                    Console.WriteLine($"{i} {text.Length} chars");
                    texts0.Add(text);
                    i++;
                }

                var texts1 = new List<string>();
                using (var doc2 = new Document(path))
                {
                    foreach (var page in doc2)
                    {
                        if (page.Number >= 30)
                            break;
                        texts1.Add((string)page.GetText());
                    }
                }
                Assert.Equal(texts1, texts0);
            }

            string wt = Tools.MupdfWarnings();
            var ver = _Version.mupdf_version_tuple();
            if (ver.major > 1 || (ver.major == 1 && ver.minor >= 27 && ver.patch >= 1))
                Assert.Equal("", wt);
            else if (ver.major == 1 && ver.minor == 27)
            {
                string expected =
                    "format error: No common ancestor in structure tree\nstructure tree broken, assume tree is missing";
                expected = string.Join("\n", Enumerable.Repeat(expected, 56));
                Assert.Equal(expected, wt);
            }
            else if (ver.major == 1 && ver.minor == 26 && ver.patch >= 8)
                Assert.Contains("Actualtext with no position", wt);
        }

        [Fact]
        public void test_3650()
        {
            using var doc = new Document(Doc("test_3650.pdf"));
            var blocks = doc[0].get_text_blocks();
            var t = blocks.Select(block => block.text).ToList();
            Console.WriteLine($"t={string.Join(", ", t.Select(x => x.Length.ToString()))}");
            Assert.Equal(
                new List<string>
                {
                    "RECUEIL DES ACTES ADMINISTRATIFS\n",
                    "n° 78 du 28 avril 2023\n",
                },
                t);
        }

        [Fact]
        public void test_4026()
        {
            using var document = new Document(Doc("test_4026.pdf"));
            var page = document[4];
            var blocks = page.get_text_blocks();
            for (int i = 0; i < blocks.Count; i++)
                Console.WriteLine($"block {i}: {blocks[i]}");
            Assert.Equal(8, blocks.Count);
        }

        [Fact]
        public void test_3725()
        {
            using var document = new Document(Doc("test_3725.pdf"));
            string text = (string)document[0].GetText();
            if (Environment.GetEnvironmentVariable("TEST_TEXTEXTRACT_VERBOSE") == "1")
            {
                Console.WriteLine(Indent(text, "    "));
            }

        }

        [Fact]
        public void test_4147()
        {
            foreach ((bool expectVisible, string name) in new (bool, string)[]
            {
                (false, "test_4147.pdf"),
                (true, "symbol-list.pdf"),
            })
            {
                string path = Doc(name);
                Console.WriteLine($"expect_visible={expectVisible} path={path}");
                using var document = new Document(path);
                var page = document[0];
                var text = (Dictionary<string, object>)page.GetText("rawdict");
                foreach (var block in AsDictList(text["blocks"]))
                {
                    if ((int)block["type"] != 0)
                        continue;
                    foreach (var line in AsDictList(block["lines"]))
                    {
                        foreach (var span in AsDictList(line["spans"]))
                        {
                            int charFlags = Convert.ToInt32(span["char_flags"]);
                            if (expectVisible)
                                Assert.NotEqual(0, charFlags & mupdf.mupdf.FZ_STEXT_FILLED);
                            else
                            {
                                Assert.Equal(0, charFlags & mupdf.mupdf.FZ_STEXT_FILLED);
                                Assert.Equal(0, charFlags & mupdf.mupdf.FZ_STEXT_STROKED);
                            }
                            Assert.Equal(0, Convert.ToInt32(span["bidi"]));
                            foreach (var ch in AsDictList(span["chars"]))
                                Assert.IsType<bool>(ch["synthetic"]);
                        }
                    }
                }
            }
        }

        [Fact]
        public void test_4139()
        {
            int flags =
                mupdf.mupdf.FZ_STEXT_PRESERVE_IMAGES
                | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
                | mupdf.mupdf.FZ_STEXT_USE_CID_FOR_UNKNOWN_UNICODE;

            using var document = new Document(Doc("test_4139.pdf"));
            var page = document[0];
            var dicts = (Dictionary<string, object>)page.GetText("dict", flags: flags, sort: true);
            var seen = new HashSet<int>();
            int bCtr = 0;
            foreach (var b in AsDictList(dicts["blocks"]))
            {
                if (b.TryGetValue("lines", out var linesObj) && linesObj is List<Dictionary<string, object>> lines)
                {
                    int lCtr = 0;
                    foreach (var l in lines)
                    {
                        int sCtr = 0;
                        foreach (var s in AsDictList(l["spans"]))
                        {
                            int color = 0;
                            if (!seen.Contains(color))
                            {
                                seen.Add(color);
                                Console.WriteLine($"B{bCtr}.L{lCtr}.S{sCtr}: color={color} hex={color:X}");
                                Assert.Equal(0, color);
                                Assert.Equal(255, Convert.ToInt32(s["alpha"]));
                            }
                            sCtr++;
                        }
                        lCtr++;
                    }
                }
                bCtr++;
            }
        }

        [Fact]
        public void test_4245()
        {
            string path = Doc("test_4245.pdf");

            using (var document = new Document(path))
            {
                var page = document[0];
                var regions = page.SearchForRects("Bart Simpson");
                Console.WriteLine($"regions={regions}");
                page.AddHighlightAnnot(regions);
            }
            using (var document = new Document(path))
            {
                var page = document[0];
                var regions = page.SearchForRects("Bart Simpson");
                foreach (var region in regions)
                {
                    var highlight = page.AddHighlightAnnot(region);
                    highlight.Update();
                }
                var pixmap = page.GetPixmap();
                string pathOut = Out("test_4245.png");
                pixmap.Save(pathOut);
                string pathExpected = Doc("test_4245_expected.png");
                float rms = _Compare.PixmapsRms(pathExpected, pixmap);
                using var pixmapDiff = _Compare.PixmapsDiff(pathExpected, pixmap);
                pixmapDiff.Save(Out("test_4245_diff.png"));
                Console.WriteLine($"rms={rms}");
                Assert.True(rms < 0.01);
            }
        }

        [Fact]
        public void test_4180()
        {
            string path = Doc("test_4180.pdf");

            Pixmap pixmap;
            using (var document = new Document(path))
            {
                var page = document[0];
                var regions = page.SearchForRects("Reference is made");
                foreach (var region in regions)
                    page.AddRedactAnnot(region, fillColor: _Constants.black);
                page.ApplyRedactions();
                pixmap = page.GetPixmap();
            }
            string pathOut = Out("test_4180.png");
            pixmap.Save(pathOut);
            string pathExpected = Doc("test_4180_expected.png");
            float rms = _Compare.PixmapsRms(pathExpected, pixmap);
            using var pixmapDiff = _Compare.PixmapsDiff(pathExpected, pixmap);
            pixmapDiff.Save(Out("test_4180_diff.png"));
            Console.WriteLine($"rms={rms}");
            Assert.True(rms < 0.01);
        }

        [Fact]
        public void test_4182()
        {
            string path = Doc("test_4182.pdf");

            var linelist = new List<object[]>();
            using (var document = new Document(path))
            {
                var page = document[0];
                var dict = (Dictionary<string, object>)page.GetText("dict");
                foreach (var block in AsDictList(dict["blocks"]))
                {
                    if ((int)block["type"] != 0)
                        continue;
                    int paranum = Convert.ToInt32(block["number"]);
                    if (!block.TryGetValue("lines", out var linesObj) || linesObj is not List<Dictionary<string, object>> lines)
                        continue;
                    foreach (var line in lines)
                    {
                        foreach (var span in AsDictList(line["spans"]))
                        {
                            string st = (string)span["text"];
                            if (!string.IsNullOrWhiteSpace(st))
                            {
                                var bbox = BboxFromDict(span["bbox"]);
                                page.DrawRect(bbox, _Constants.red);
                                linelist.Add(new object[] { paranum, bbox, st });
                            }
                        }
                    }
                }
                var pixmap = page.GetPixmap();
                pixmap.Save(Out("test_4182.png"));
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    foreach (var l in linelist)
                    {
                        Console.WriteLine(l);
                    }

                }
                string pathExpected = Doc("test_4182_expected.png");
                using var pixmapDiff = _Compare.PixmapsDiff(pathExpected, pixmap);
                pixmapDiff.Save(Out("test_4182_diff.png"));
                float rms = _Compare.PixmapsRms(pathExpected, pixmap);
                Console.WriteLine($"rms={rms}");
                Assert.True(rms < 0.01);
            }
        }

        [Fact]
        public void test_4179()
        {
            string path = Doc("test_4179.pdf");

            int aa = mupdf.mupdf.fz_aa_level();
            bool oldSkipQuad = Helpers.SkipQuadCorrections;
            Tools.SetAaLevel(0);
            Helpers.SkipQuadCorrections = true;
            try
            {
                using var document = new Document(path);
                var page = document[0];
                const string charSqrt = "\u221a";

                var bboxesSearch = page.SearchForRects(charSqrt);
                Assert.Single(bboxesSearch);
                Console.WriteLine($"bboxes_search[0]:\n    {bboxesSearch[0]}");
                page.DrawRect(bboxesSearch[0], _Constants.red);
                Assert.True(RectTupleRms(bboxesSearch[0],
                    (250.0489959716797f, 91.93604278564453f, 258.34783935546875f, 101.34073638916016f)) < 0.01);

                int searchFlags =
                    mupdf.mupdf.FZ_STEXT_DEHYPHENATE
                    | mupdf.mupdf.FZ_STEXT_PRESERVE_WHITESPACE
                    | mupdf.mupdf.FZ_STEXT_PRESERVE_LIGATURES
                    | mupdf.mupdf.FZ_STEXT_MEDIABOX_CLIP
                    | mupdf.mupdf.FZ_STEXT_ACCURATE_BBOXES;

                var bboxesSearchAccurate = page.SearchForRects(charSqrt, flags: searchFlags);
                Assert.Single(bboxesSearchAccurate);
                Console.WriteLine($"bboxes_search_accurate[0]\n    {bboxesSearchAccurate[0]}");
                page.DrawRect(bboxesSearchAccurate[0], _Constants.green);
                Assert.True(RectTupleRms(bboxesSearchAccurate[0],
                    (250.0489959716797f, 99.00948333740234f, 258.34783935546875f, 108.97208404541016f)) < 0.01);

                var bboxesIterateAccurate = new List<Rect>();
                var dict = (Dictionary<string, object>)page.GetText(
                    "rawdict", flags: mupdf.mupdf.FZ_STEXT_ACCURATE_BBOXES);
                foreach (var block in AsDictList(dict["blocks"]))
                {
                    if ((int)block["type"] != 0 || !block.TryGetValue("lines", out var linesObj))
                        continue;
                    foreach (var line in AsDictList(linesObj))
                    {
                        foreach (var span in AsDictList(line["spans"]))
                        {
                            foreach (var ch in AsDictList(span["chars"]))
                            {
                                if ((string)ch["c"] == charSqrt)
                                {
                                    var bboxIterate = BboxFromDict(ch["bbox"]);
                                    bboxesIterateAccurate.Add(bboxIterate);
                                    Console.WriteLine($"bbox_iterate_accurate:\n    {bboxIterate}");
                                    page.DrawRect(bboxIterate, _Constants.blue);
                                }
                            }
                        }
                    }
                }

                Assert.NotEqual(bboxesSearchAccurate[0], bboxesSearch[0]);
                Assert.Equal(bboxesSearchAccurate, bboxesIterateAccurate);

                var pixmap = page.GetPixmap();
                pixmap.Save(Out("test_4179.png"));
                string pathExpected = Doc("test_4179_expected.png");
                float rms = _Compare.PixmapsRms(pathExpected, pixmap);
                using var pixmapDiff = _Compare.PixmapsDiff(pathExpected, pixmap);
                pixmapDiff.Save(Out("test_4179_diff.png"));
                Console.WriteLine($"rms={rms}");
                Assert.True(rms < 0.01);
            }
            finally
            {
                Tools.SetAaLevel(aa);
                Helpers.SkipQuadCorrections = oldSkipQuad;
            }
        }

        [Fact]
        public void test_extendable_textpage()
        {
            Console.WriteLine();

            string path = Out("test_extendable_textpage.pdf");
            string path3 = Out("test_extendable_textpage3.pdf");

            Rect rect;
            using (var document = new Document())
            {
                document.NewPage();
                document.NewPage();
                var page0 = document[0];
                var page1 = document[1];
                float y = 100;
                const float lineHeight = 9.6f;
                const string abcd = "abcd";
                const string efgh = "efgh";
                for (int i = 0; i < 4; i++)
                {
                    page0.InsertText(new Point(100, y + lineHeight), new string(abcd[i], 16));
                    page1.InsertText(new Point(100, y + lineHeight), new string(efgh[i], 16));
                    y += lineHeight;
                    if (i % 2 == 0)
                        y += lineHeight;
                }
                rect = new Rect(100, 100, 200, y);
                page0.DrawRect(rect, _Constants.red);
                page1.DrawRect(rect, _Constants.red);
                document.Save(path);
            }

            using (var document = new Document(path))
            {
                float yPos = 0;
                var cookie = new mupdf.FzCookie();
                var stextPage = new mupdf.FzStextPage(
                    new mupdf.FzRect(mupdf.FzRect.Fixed.Fixed_INFINITE));
                var stextOptions = new mupdf.FzStextOptions();
                var device = stextPage.fz_new_stext_device(stextOptions);

                var ctm0 = new mupdf.FzMatrix(1, 0, 0, 1, -(float)rect.X0, -(float)rect.Y0 + yPos);
                document[0].NativePage.fz_run_page(device, ctm0, cookie);
                yPos += (float)(rect.Y1 - rect.Y0);

                var ctm1 = new mupdf.FzMatrix(1, 0, 0, 1, -(float)rect.X0, -(float)rect.Y0 + yPos);
                document[1].NativePage.fz_run_page(device, ctm1, cookie);
                yPos += (float)(rect.Y1 - rect.Y0);

                mupdf.mupdf.fz_close_device(device);

                using var textPage = new TextPage(stextPage);
                Console.WriteLine("Using text_page.extractDICT().");
                Console.WriteLine($"text_page.rect={textPage.Rect}");
                var d = textPage.ExtractDict(sort: true);
                float? y0Prev = null;
                int pno = 0;
                float ydelta = 0;
                foreach (var block in AsDictList(d["blocks"]))
                {
                    Console.WriteLine($"block bbox={string.Join(",", (float[])block["bbox"])}");
                    foreach (var line in AsDictList(block["lines"]))
                    {
                        Console.WriteLine($"    line bbox={string.Join(",", (float[])line["bbox"])}");
                        foreach (var span in AsDictList(line["spans"]))
                        {
                            var bbox = (float[])span["bbox"];
                            float x0 = bbox[0], y0 = bbox[1], x1 = bbox[2], y1 = bbox[3];
                            float dy = y0Prev == null ? 0 : y0 - y0Prev.Value;
                            y0Prev = y0;
                            string spanText = (string)span["text"];
                            Console.WriteLine($"        dy={dy,5:F2} height={y1 - y0:F2} {x0:F2} {y0:F2} {x1:F2} {y1:F2} text={spanText}");
                            if (spanText.Contains("eee"))
                            {
                                pno = 1;
                                ydelta = (float)(rect.Y1 - rect.Y0);
                            }
                            y0 -= ydelta;
                            y1 -= ydelta;
                            document[pno].DrawRect(new Rect(x0, y0, x1, y1), _Constants.green);
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Using text_page.extractText()");
                string extractText = textPage.ExtractText(sort: true);
                Console.WriteLine(extractText);

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Using extractBLOCKS");
                var blockLines = new List<string>();
                foreach (var (x0, y0, x1, y1, line, no, type_) in textPage.ExtractBlocks())
                {
                    Console.WriteLine("block:");
                    Console.WriteLine($"    bbox=({x0}, {y0}, {x1}, {y1}) no={no}");
                    Console.WriteLine($"    line={line}");
                    blockLines.Add(line);
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("extractBLOCKS joined by newlines:");
                Console.WriteLine(string.Join("\n", blockLines));

                string[] expected =
                {
                    "aaaaaaaaaaaaaaaa\n",
                    "bbbbbbbbbbbbbbbb\ncccccccccccccccc\n",
                    "dddddddddddddddd\neeeeeeeeeeeeeeee\n",
                    "ffffffffffffffff\ngggggggggggggggg\n",
                    "hhhhhhhhhhhhhhhh\n",
                };
                Assert.Equal(expected, blockLines);
                document.Save(path3);
            }
        }

        [Fact]
        public void test_4363()
        {
            Console.WriteLine();
            string path = Doc("test_4363.pdf");
            Console.WriteLine($"version={Artifex.Versions.MuPDF}");
            int n = 0;
            var texts = new List<string>();
            using (var document = new Document(path))
            {
                Assert.Equal(1, document.PageCount);
                var page = document[0];
                var t = page.SearchForRects("tour");
                Console.WriteLine($"t={t}");
                n += t.Count;
                texts.Add((string)page.GetText());
            }
            Console.WriteLine($"n={n}");
            Console.WriteLine($"len(texts)={texts.Count}");
            string text = texts[0];
            Console.WriteLine("text:");
            Console.WriteLine($"text={text}");
            const string textExpected =
                "Deal Roadshow SiteTour\n" +
                "We know your process. We know your standard.\n" +
                "Professional Site Tour Video Productions for the Capital Markets.\n" +
                "1\n";
            if (text != textExpected)
            {
                Console.WriteLine($"Expected:\n    {textExpected}");
                Console.WriteLine($"Found:\n    {text}");
                Assert.Fail("Unexpected text in test_4363");
            }
        }

        [Fact]
        public void test_4546()
        {
            string text;
            string path = Doc("test_4546.pdf");
            using (var document = new Document(path))
                text = (string)document[0].GetText();

            const string expectedMupdf1270 =
                "JOB No.: \n \nS/O No. 托运单号码   \nSINORICH TRANSPORT LIMITED \nSHIPPING ORDER \n托运单 \n 市场部: \n88570009 \n88577019 \n88572702 \n 操作部: \n88570008 \n88570004 \n 文件部: \n88570003\n \nNotify Party(complete name and address, ";

            var ver = _Version.mupdf_version_tuple();
            string wt = Tools.MupdfWarnings();
            Console.WriteLine($"version={Tools.MupdfVersion()}");
            Console.WriteLine($"text is:\n{Indent(text, "    ")}");

            if (ver.major > 1 || (ver.major == 1 && ver.minor >= 26 && ver.patch >= 8))
            {
                Assert.Equal(expectedMupdf1270, text.Substring(0, Math.Min(expectedMupdf1270.Length, text.Length)));
                Assert.Contains("Actualtext with no position", wt);
            }
            else if (ver.major == 1 && ver.minor >= 26)
                Assert.False(string.IsNullOrEmpty(wt));
        }

        [Fact]
        public void test_4503()
        {
            Dictionary<string, object> span0 = null;
            string path = Doc("test_4503.pdf");
            string text0 = null;
            Console.WriteLine();
            var ver = _Version.mupdf_version_tuple();
            Console.WriteLine($"mupdf_version_tuple=({ver.major}, {ver.minor}, {ver.patch})");

            int flags = Constants.TextFlagsDict | mupdf.mupdf.FZ_STEXT_COLLECT_STYLES;
            using (var document = new Document(path))
            {
                var page = document[0];
                var text = (Dictionary<string, object>)page.GetText("rawdict", flags: flags);
                int i = 0;
                foreach (var block in AsDictList(text["blocks"]))
                {
                    Console.WriteLine($"block {i}:");
                    int j = 0;
                    foreach (var line in AsDictList(block["lines"]))
                    {
                        Console.WriteLine($"    line {j}:");
                        int k = 0;
                        foreach (var span in AsDictList(line["spans"]))
                        {
                            var sb = new StringBuilder();
                            foreach (var ch in AsDictList(span["chars"]))
                                sb.Append((string)ch["c"]);
                            string st = sb.ToString();
                            int spanFlags = Convert.ToInt32(span["flags"]);
                            int charFlags = Convert.ToInt32(span["char_flags"]);
                            Console.WriteLine($"        span {k}: flags=0x{spanFlags:X} char_flags=0x{charFlags:X}: {st}");
                            if (st.Contains("the right to request the state to review"))
                            {
                                span0 = span;
                                text0 = st;
                            }
                            k++;
                        }
                        j++;
                    }
                    i++;
                }
            }
            Assert.NotNull(span0);
            int charFlags0 = Convert.ToInt32(span0["char_flags"]);
            int strikeout = charFlags0 & mupdf.mupdf.FZ_STEXT_STRIKEOUT;
            Console.WriteLine($"strikeout={strikeout}");

            if (ver.major > 1 || (ver.major == 1 && ver.minor >= 26 && ver.patch >= 3))
            {
                Assert.NotEqual(0, strikeout);
                Assert.Equal("the right to request the state to review and, if appropriate,", text0);
            }
            else if (ver.major == 1 && ver.minor == 26 && ver.patch >= 2)
            {
                Assert.NotEqual(0, strikeout);
                Assert.Equal("the right to request the state to review ", text0);
            }
            else if (ver.major == 1 && ver.minor == 27 && ver.patch >= 2)
            {
                Assert.NotEqual(0, strikeout);
                Assert.Equal("the right to request the state to review and, if appropriate,", text0);
            }
            else if (ver.major == 1 && ver.minor == 28 && ver.patch >= 0)
            {
                Assert.NotEqual(0, strikeout);
                Assert.Equal("the right to request the state to review and, if appropriate,", text0);
            }
            else
            {
                Assert.Equal(0, strikeout);
                Assert.Equal(
                    "notice the right to request the state to review and, if appropriate,",
                    text0);
            }
        }
    }
}