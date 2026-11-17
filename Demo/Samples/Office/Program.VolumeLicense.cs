namespace Demo
{
    internal partial class Program
    {
        /// <summary>
        /// Volume-license monitor. Opt-in because it posts a document count.
        /// A product 2 or 7 key with the volume-license flag is required.
        /// </summary>
        internal static void TestVolumeLicense(string[] args)
        {
            Environment.SetEnvironmentVariable("MUPDF_NET_OFFICE_ACTIVITY_VERBOSE", "1");

            string[] rest = args ?? Array.Empty<string>();
            if (rest.Length > 0 && string.Equals(rest[0], "volume-license", StringComparison.OrdinalIgnoreCase))
                rest = rest.Skip(1).ToArray();

            string key = ResolveVolumeKey(rest);
            if (string.IsNullOrWhiteSpace(key))
            {
                Console.WriteLine("No volume-license key.");
                Console.WriteLine("Pass --key <key>, set MUPDF_OFFICE_KEY, or add Activity=<key> to TestDocuments/office.keys.");
                return;
            }

            int flags = MuPDFOffice.Unlock(key, fontPathAuto: true);
            bool volume = (flags & MuPDFOffice.KeyFlagVolumeLicense) != 0;
            Console.WriteLine("Flags: 0x" + flags.ToString("X"));
            Console.WriteLine("Volume license: " + (volume ? "yes" : "no"));
            if (!volume)
            {
                Console.WriteLine("This key does not report document opens.");
                return;
            }

            const int opens = 2;
            for (int i = 0; i < opens; i++)
            {
                using Document document = new Document();
                document.NewPage();
            }

            Console.WriteLine("Opened " + opens + " document(s).");
            Console.WriteLine("The usage count is sent when this process exits.");
            Environment.Exit(0);
        }

        private static string ResolveVolumeKey(string[] args)
        {
            int keyFlag = Array.FindIndex(args, a => a is "--key" or "-k");
            if (keyFlag >= 0)
            {
                if (keyFlag + 1 >= args.Length)
                    throw new ArgumentException("Missing value for --key.");
                return args[keyFlag + 1].Trim();
            }

            string env = Environment.GetEnvironmentVariable("MUPDF_OFFICE_KEY");
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();

            string path = FindOfficeKeysFile();
            return path == null ? null : ReadNamedOfficeKey(path, "Activity");
        }

        private static string FindOfficeKeysFile()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                string direct = Path.Combine(dir, "TestDocuments", "office.keys");
                if (File.Exists(direct))
                    return direct;

                string sibling = Path.Combine(dir, "MuPDF.NET.Office", "TestDocuments", "office.keys");
                if (File.Exists(sibling))
                    return sibling;

                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        private static string ReadNamedOfficeKey(string path, string name)
        {
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                string keyName = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                if (string.Equals(keyName, name, StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                    return value;
            }

            return null;
        }
    }
}
