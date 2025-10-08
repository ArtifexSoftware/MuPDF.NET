using System;
using System.IO;
using System.Text;

namespace ActiveQueryBuilder.Scripts
{
	class Preprocess
	{
		[STAThread]
		static public int Main(string[] args)
		{
			int result = 0;
			string errorMessage = String.Empty;

			ConsoleColor defaultColor = Console.ForegroundColor;

			if (args.Length != 1)
			{
				errorMessage = "Invalid command line parameter.";
				result = 1;
				goto exit;
			}

			try
			{
				if (Directory.Exists(args[0]))
				{
					string dir = Path.GetFullPath(args[0]);

					Console.WriteLine("Process folder <" + dir + ">? (y/n)");
				
					char c = (char) Console.Read();

					if (c != 'y' && c != 'Y')
					{
						result = 1;
						errorMessage = "Aborted.";
						goto exit;
					}

					var files = Directory.EnumerateFiles(args[0], "*.cs", SearchOption.AllDirectories);

					foreach (string file in files)
					{
						if (Path.GetFileName(file) != "preprocess.script.cs") // skip itself
						{
							ProcessFile(file);
						}
					}
				}
				else if (File.Exists(args[0]))
				{
					if (Path.GetExtension(args[0]).ToLower() == ".cs")
					{
						ProcessFile(args[0]);
					}
				}
				else
				{
					errorMessage = "Invalid command line parameter.";
					result = 1;
					goto exit;
				}
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				result = 1;
			}

exit:
			if (result != 0)
			{
				const string errorPerfix = "ERROR [preprocess.script.cs]: ";

				if (errorMessage.Length == 0)
				{
					errorMessage = "Unknown error";
				}

				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(errorPerfix + errorMessage);
				Console.ForegroundColor = defaultColor;
			}
			else
			{
				Console.WriteLine("Done.");
			}

#if DEBUG
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
#endif
			
			return result;
		}

		static private void ProcessFile(string file)
		{
			const string beginIfDef = "#if !PocketPC && !WindowsCE && !TARGETTING_FX_1_1";
			const string endIfDef = "#endif // !PocketPC && !WindowsCE && !TARGETTING_FX_1_1";
			const string beginComment = "#/*";
			const string endComment = "#*/";
			const string lineComment = "#//";

			StringBuilder sb = new StringBuilder();
			bool skip = false;
			bool needToWrap = false;
			Encoding encoding = Encoding.Unicode;

			Console.WriteLine("Processing " + Path.GetFileName(file) + "...");

			using (StreamReader sr = new StreamReader(file))
			{
				encoding = sr.CurrentEncoding;

				while (!sr.EndOfStream)
				{
					string line = sr.ReadLine();
					string trimmed = line.Trim();

					if (sb.Length == 0)
					{
						if (line != beginIfDef)
						{
							needToWrap = true;
						}
					}

					if (trimmed.StartsWith(lineComment))
					{
						continue;
					}

					if (skip)
					{
						int end = line.IndexOf(endComment);

						if (end != -1)
						{
							int pos = end + endComment.Length;
							line = line.Substring(pos, line.Length - pos);
							skip = false;
						}
						else
						{
							continue;
						}
					}
					else
					{
						while (true)
						{
							int pos = 0;
							int start = line.IndexOf(beginComment, pos), end;

							if (start != -1)
							{
								pos = start + beginComment.Length;
								end = line.IndexOf(endComment, pos);

								if (end != -1)
								{
									pos = end + endComment.Length;
									line = line.Substring(0, start) + line.Substring(pos, line.Length - pos);
								}
								else
								{
									line = line.Substring(0, start);
									skip = true;
									break;
								}
							}
							else
							{
								break;
							}
						}
					}
					
					line = line.Replace("public class ", "internal class ");
					line = line.Replace("public abstract class ", "internal abstract class ");
					line = line.Replace("public sealed class ", "internal sealed class ");
					line = line.Replace("public interface ", "internal interface ");
					line = line.Replace("public enum ", "internal enum ");

					sb.AppendLine(line);
				}

				sr.Close();
			}

			using (StreamWriter sw = new StreamWriter(file, false, encoding))
			{
				if (needToWrap)
				{
					sw.WriteLine(beginIfDef);
					sw.WriteLine();
				}

				sw.Write(sb.ToString());

				if (needToWrap)
				{
					sw.WriteLine();
					sw.WriteLine(endIfDef);
				}

				sw.Flush();
			}
		}
	}
}
