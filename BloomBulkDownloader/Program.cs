using System;
using CommandLine;

namespace BloomBulkDownloader
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length > 0)
			{
				var parserResult = Parser.Default.ParseArguments<BulkDownloadOptions>(args);
				var parsed = parserResult as Parsed<BulkDownloadOptions>;
				if (parsed != null)
				{
					Console.WriteLine("Commencing download from: "+ parsed.Value.S3BucketName +
						" to: "+parsed.Value.FinalDestinationPath);
					var result = BulkDownloadCommand.Handle(parsed.Value);
					Console.WriteLine(result == 0 ? "Success!" : "Failed!");
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
