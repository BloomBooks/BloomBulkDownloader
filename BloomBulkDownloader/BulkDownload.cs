using System;

namespace Bloom.WebLibraryIntegration
{
	/// <summary>
	/// This command syncs a local folder with an S3 bucket and then copies certain books to the specified destination.
	/// See DownloadBookOptions for the expected options.
	/// </summary>
	class BulkDownloadCommand
	{
		public static int Handle(BulkDownloadOptions options, IProgressDialog progress)
		{
			var opts = options;
			Console.WriteLine("Syncing local bucket repo");
			progress.Progress += 1;
			// This task will be all the program does. We need to do enough setup so that
			// the download code can work, then tear it down.
			//Program.SetUpErrorHandling();
			//try
			//{
			//	using (var applicationContainer = new ApplicationContainer())
			//	{
			//		Program.SetUpLocalization(applicationContainer);
			//		Browser.SetUpXulRunner();
			//		Browser.XulRunnerShutdown += Program.OnXulRunnerShutdown;
			//		LocalizationManager.SetUILanguage(Settings.Default.UserInterfaceLanguage, false);
			//		var transfer = new BookTransfer(new BloomParseClient(), ProjectContext.CreateBloomS3Client(),
			//			applicationContainer.BookThumbNailer, new BookDownloadStartingEvent()) /*not hooked to anything*/;
			//		// Since Bloom is not a normal console app, when run from a command line, the new command prompt
			//		// appears at once. The extra newlines here are attempting to separate this from our output.
			//		Console.WriteLine("\nstarting download");
			//		transfer.HandleDownloadWithoutProgress(options.Url, options.DestinationPath);
			//		Console.WriteLine(("\ndownload complete\n"));
			//	}
			//	return 0;
			//}
			//catch (Exception ex)
			//{
			//	Debug.WriteLine(ex.Message);
			//	Console.WriteLine(ex.Message);
			//	Console.WriteLine(ex.StackTrace);
			//	return 1;
			//}
			//var keys = AccessKeys.GetAccessKeys(BloomS3Client.SandboxBucketName);
			return 0;
		}
	}
}
