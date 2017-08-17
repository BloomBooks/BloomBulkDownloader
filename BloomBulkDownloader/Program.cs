using System;
using CommandLine;

namespace Bloom.WebLibraryIntegration
{
	class Program
	{
		private const string syncFolder = "BloomBulkDownloader-SyncFolder";

		static int Main(string[] args)
		{
			if (args.Length > 0 && new[] { "--help", "hydrate", "download", "getfonts" }.Contains(args[0])) //restrict using the commandline parser to cases were it should work
			{
				var exitCode = Parser.Default.ParseArguments(args,
						new[] { typeof(HydrateParameters), typeof(DownloadBookOptions), typeof(GetUsedFontsParameters) })
					.MapResult(
						(HydrateParameters opts) => HandlePrepareCommandLine(opts),
						(DownloadBookOptions opts) => DownloadBookCommand.HandleSilentDownload(opts),
						(GetUsedFontsParameters opts) => GetUsedFontsCommand.Handle(opts),
						errors =>
						{
							var code = 0;
							foreach (var error in errors)
							{
								if (!(error is HelpVerbRequestedError))
								{
									Console.WriteLine(error.ToString());
									code = 1;
								}
							}
							return code;
						});
				return exitCode; // we're done
			}
			var keys = AccessKeys.GetAccessKeys(BloomS3Client.SandboxBucketName);
			return 0;
		}
	}
}
