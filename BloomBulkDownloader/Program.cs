using System;
using System.IO;
using CommandLine;

namespace BloomBulkDownloader
{
	class Program
	{
		private const string ErrorFile = "problems.txt";

		static int Main(string[] args)
		{
			if (args.Length > 0)
			{
				var parserResult = Parser.Default.ParseArguments<BulkDownloadOptions>(args);
				var parsed = parserResult as Parsed<BulkDownloadOptions>;
				if (parsed != null)
				{
					var result = BulkDownload.Handle(parsed.Value);
					Console.WriteLine(result == 0 ? "Success!" : "Failed!");
					var errorFile = Path.Combine(parsed.Value.FinalDestinationPath, ErrorFile);
					if (File.Exists(errorFile))
					{
						Console.WriteLine("There were problems. See " + errorFile);
					}
					WaitForInput();
					return result;
				}
			}
			Console.WriteLine("BloomBulkDownloader requires command line arguments.");
			WaitForInput();
			return 1;
		}

		private static void WaitForInput()
		{
			Console.Write("\nPlease press a key to continue...");
			Console.ReadKey(true);
		}
	}
}
