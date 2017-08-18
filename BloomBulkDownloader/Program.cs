using System;
using CommandLine;
using System.Linq;

namespace Bloom.WebLibraryIntegration
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
					using (var consoleProgress = new ConsoleProgress())
					{
						consoleProgress.ProgressRangeMaximum = 50;
						Console.WriteLine("Commencing download from: "+ parsed.Value.S3BucketName +
							" to: "+parsed.Value.FinalDestinationPath);
						var result = BulkDownloadCommand.Handle(parsed.Value, consoleProgress);
						Console.WriteLine(result == 0 ? "Success!" : "Failed!");
						return result;
					}
				}
			}
			return -1;
		}
	}
}
