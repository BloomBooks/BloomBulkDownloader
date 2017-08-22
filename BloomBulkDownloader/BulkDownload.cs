using System;
using System.Diagnostics;
using System.IO;

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
			// This task will be all the program does. We need to do enough setup so that
			// the download code can work.
			try
			{
				Console.WriteLine("\nSyncing local bucket repo");
				progress.Progress += 1;
				var cmdline = GetSyncCommandFromOptions(opts);
				if (!Directory.Exists(opts.SyncFolder))
				{
					Directory.CreateDirectory(opts.SyncFolder);
				}
				Console.WriteLine("\naws " + cmdline);
				var process = Process.Start("aws", cmdline);
				Console.WriteLine("\nstarting filtered copy to " + opts.FinalDestinationPath);
				//transfer.HandleDownloadWithoutProgress(options.Url, options.DestinationPath);
				Console.WriteLine(("\nprocess complete\n"));
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				return 1;
			}
		}

		private static string GetSyncCommandFromOptions(BulkDownloadOptions opts)
		{
			var cmdLineArgs = "s3 sync s3://";
			cmdLineArgs += opts.S3BucketName;
			if (opts.TrialRun)
			{
				cmdLineArgs += "/gordon_martin@sil.org ";
			}
			cmdLineArgs += opts.SyncFolder;
			if (opts.DryRun)
			{
				cmdLineArgs += " --dryrun";
			}

			return cmdLineArgs;
		}
	}
}
