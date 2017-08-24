using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RestSharp;
using SIL.IO;

namespace BloomBulkDownloader
{
	/// <summary>
	/// This command syncs a local folder with an S3 bucket and then copies certain books to the specified destination.
	/// See DownloadBookOptions for the expected options.
	/// </summary>
	class BulkDownloadCommand
	{
		public static int Handle(BulkDownloadOptions options, IProgressDialog progress)
		{
			// This task will be all the program does. We need to do enough setup so that
			// the download code can work.
			try
			{
				Console.WriteLine("\nSyncing local bucket repo");
				var cmdline = GetSyncCommandFromOptions(options);
				if (!Directory.Exists(options.SyncFolder))
				{
					Directory.CreateDirectory(options.SyncFolder);
				}
				Console.WriteLine("\naws " + cmdline);
				var process = Process.Start(new ProcessStartInfo
				{
					Arguments = cmdline,
					FileName = "aws",
					UseShellExecute = false,
					RedirectStandardError = true,
					//RedirectStandardOutput = true
				});
				Console.WriteLine("\nSyncing folders...\n");
				// Read the error stream first and then wait.
				var error = process.StandardError.ReadToEnd();
				//var output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					throw new ApplicationException("\nSync process failed.\n" + error);
				}
				//if (string.IsNullOrWhiteSpace(output))
				//{
				//	Console.WriteLine(options.SyncFolder + " was already up-to-date.\n");
				//}
				//else
				//{
				//	Console.WriteLine(output);
				//}
				if (options.DryRun)
				{
					Console.WriteLine("\nDry run completed... Can't continue to phase 2 with dry run.");
					return 0;
				}
				if (Directory.Exists(options.FinalDestinationPath))
				{
					RobustIO.DeleteDirectory(options.FinalDestinationPath, true);
				}
				Console.WriteLine("Creating clean empty directory at: " + options.FinalDestinationPath);
				Directory.CreateDirectory(options.FinalDestinationPath);
				return FilteredCopy(options, progress);
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
				cmdLineArgs += "/" + opts.TrialEmail;
			}
			cmdLineArgs += " " + opts.SyncFolder;
			if (opts.DryRun)
			{
				cmdLineArgs += " --dryrun";
			}
			return cmdLineArgs;
		}

		private static int FilteredCopy(BulkDownloadOptions options, IProgressDialog progress)
		{
			var records = GetParseDbInCirculationJson(options);
			var filteredInstanceIds = new Dictionary<string, string>();

			foreach (var parseRecord in records)
			{
				if (options.TrialRun && parseRecord.Uploader.Email != options.TrialEmail)
					continue;
				filteredInstanceIds.Add(parseRecord.InstanceId, parseRecord.Uploader.DisambName);
			}

			CopyMatchingBooksToFinalDestination(options, filteredInstanceIds);

			Console.WriteLine("\nFiltered copy complete\n");
			return 0;
		}

		private static void CopyMatchingBooksToFinalDestination(BulkDownloadOptions options, IDictionary<string, string> filteredInstanceIds)
		{
			// Copy to 'destination' folder all folders inside of folders whose names match one of the 'filteredInstanceIds'.
			var destination = options.FinalDestinationPath;
			Console.WriteLine("\nStarting filtered copy to " + destination);
			var sourceDirectories = Directory.GetDirectories(options.SyncFolder);
			// TODO: Need to make 2 passes
			// 1 - determine which folders (books) will get copied and whether there are duplicates
			//     part of duplicate check will record name of destination book
			// 2 - copy the ones that make the cut
			var fileCount = 0;
			var bookCount = 0;
			foreach (var directory in sourceDirectories)
			{
				var sourceDirGuidString = Path.GetFileName(directory);
				if (!filteredInstanceIds.ContainsKey(sourceDirGuidString))
					continue;

				// This directory contains a book we need to copy.
				var bookToCopyPath = Directory.EnumerateDirectories(directory).First();
				var sourceFolderName = Path.GetFileName(bookToCopyPath);
				var destinationFolderPath = Path.Combine(destination, sourceFolderName);
				Directory.CreateDirectory(destinationFolderPath);
				foreach (var fileToCopy in Directory.EnumerateFiles(bookToCopyPath))
				{
					var fileName = Path.GetFileName(fileToCopy);
					RobustFile.Copy(fileToCopy, Path.Combine(destinationFolderPath, fileName));
					Console.WriteLine("  " + fileName + " copied to " + destinationFolderPath);
					fileCount++;
				}
				bookCount++;
			}
			Console.WriteLine("\nBooks copied: " + bookCount + "  Files copied: " + fileCount);
		}

		private static IEnumerable<DownloaderParseRecord> GetParseDbInCirculationJson(BulkDownloadOptions options)
		{
			const int limitParam = 2000;
			var client = new RestClient(options.ParseServer);
			var request = new RestRequest("parse/classes/books", Method.GET);
			request.AddHeader("X-Parse-Application-Id", options.ParseAppId);
			request.AddHeader("X-Parse-REST-API-Key", options.RestApiKey);
			request.AddParameter("limit", limitParam.ToString());
			request.AddParameter("count", "1");
			request.AddParameter("include", "langPointers");
			request.AddParameter("include", "uploader");
			request.AddParameter("where", "{\"inCirculation\":true}");
			request.RequestFormat = DataFormat.Json;

			var response = client.Execute(request);
			var mainObject = JsonConvert.DeserializeObject<MainParseDownloaderObject>(response.Content);
			var records = mainObject.Results;
			var count = mainObject.Count;
			if (count == limitParam)
			{
				throw new ApplicationException("Need to update our program to handle more than 2000 records.");
			}
			return records;
		}
	}
}
