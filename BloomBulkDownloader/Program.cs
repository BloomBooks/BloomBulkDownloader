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
				return result;
			}
			return 1;
		}

	}
}
