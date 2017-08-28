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
	public class BulkDownloadCommand
	{
		public static int Handle(BulkDownloadOptions options)
		{
			// This task will be all the program does. We need to do enough setup so that
			// the download code can work.
			try
			{
				Console.WriteLine("\nPreparing to sync local bucket repo");
				var cmdline = GetSyncCommandLineArgsFromOptions(options);
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
					RedirectStandardError = true
				});
				Console.WriteLine("\nSyncing folders...\n");
				// Read the error stream first and then wait.
				var error = process.StandardError.ReadToEnd();
				// I (gjm) tried out redirecting standard output as well, but it left the user with
				// no feedback that the program was actually doing something.
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					throw new ApplicationException("\nSync process failed.\n" + error);
				}
				if (options.DryRun)
				{
					Console.WriteLine("\nDry run completed... Can't continue to phase 2 with dry run.");
					return 0;
				}
				if (Directory.Exists(options.FinalDestinationPath))
				{
					RobustIO.DeleteDirectory(options.FinalDestinationPath, true);
				}
				Console.WriteLine("\nCreating clean empty directory at: " + options.FinalDestinationPath);
				Directory.CreateDirectory(options.FinalDestinationPath);

				var filteredListOfBooks = GetFilteredListOfBooksToCopy(options);

				return CopyMatchingBooksToFinalDestination(options, filteredListOfBooks);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				return 1;
			}
		}

		/// <summary>
		/// public for testing
		/// </summary>
		/// <param name="opts"></param>
		/// <returns></returns>
		public static string GetSyncCommandLineArgsFromOptions(BulkDownloadOptions opts)
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

		private static Dictionary<string, string> GetFilteredListOfBooksToCopy(BulkDownloadOptions options)
		{
			// Get mongodb filtered by inCirculation flag
			var records = GetParseDbInCirculationJson(options);
			var filteredBooks = new Dictionary<string, List<DownloaderParseRecord>>();

			// Collect up all the records from the mongodb of books to be copied.
			// Further filter if we are doing a trial run.
			foreach (var parseRecord in records)
			{
				if (options.TrialRun && parseRecord.Uploader.Email != options.TrialEmail)
					continue;
				if (filteredBooks.TryGetValue(parseRecord.Title, out List<DownloaderParseRecord> listMatchingThisTitle))
				{
					listMatchingThisTitle.Add(parseRecord);
				}
				else
				{
					filteredBooks.Add(parseRecord.Title, new List<DownloaderParseRecord> {parseRecord});
				}
			}

			// Now disambiguate any book Titles that have been uploaded by more than one email address.
			// 'filteredInstanceIds' is keyed on the instance id to copy over
			// The other string is the (disambiguated, if necessary) book Title.
			// Disambiguation is by appending "_uploaderEmailAddress" to the Title.
			var filteredInstanceIds = new Dictionary<string, string>();
			foreach (var kvpTitle in filteredBooks)
			{
				var recordList = kvpTitle.Value;
				if (recordList.Count == 1)
				{
					filteredInstanceIds.Add(recordList[0].InstanceId, recordList[0].Title);
				}
				else
				{
					foreach (var record in recordList)
					{
						filteredInstanceIds.Add(record.InstanceId, record.Title + "_" + record.Uploader.Email);
					}
				}
			}

			Console.WriteLine("\nFiltering complete. " + filteredInstanceIds.Count + " books to copy.\n");
			return filteredInstanceIds;
		}

		private static int CopyMatchingBooksToFinalDestination(BulkDownloadOptions options, IDictionary<string, string> filteredInstanceIds)
		{
			// Copy to 'destination' folder all folders inside of folders whose names match one of the 'filteredInstanceIds'.
			var destination = options.FinalDestinationPath;
			Console.WriteLine("\nStarting filtered copy to " + destination);
			var sourceDirectories = Directory.GetDirectories(options.SyncFolder);
			var fileCount = 0;
			var bookCount = 0;
			foreach (var directory in sourceDirectories)
			{
				var emailAcctString = Path.GetFileName(directory);
				if (!emailAcctString.Contains("@"))
				{
					// This is a book without a separate uploader directory; use this guid string as the outer book folder
					if (!filteredInstanceIds.ContainsKey(emailAcctString))
						continue;
					fileCount += CopyOneBookFolder(directory, destination);
					bookCount++;
					Console.Write("."); // breadcrumb for each book copied
				}
				else
				{
					foreach (var subDirectory in Directory.EnumerateDirectories(directory))
					{
						var sourceDirGuidString = Path.GetFileName(subDirectory);
						if (!filteredInstanceIds.ContainsKey(sourceDirGuidString))
							continue;

						fileCount += CopyOneBookFolder(subDirectory, destination);
						bookCount++;
						Console.Write("."); // breadcrumb for each book copied
					}
				}
			}
			Console.WriteLine("\nBooks copied: " + bookCount + "  Files copied: " + fileCount);
			return 0;
		}

		private static int CopyOneBookFolder(string instanceIdFolder, string destination)
		{
			// This 'instanceIdFolder' contains a book we need to copy.
			var bookToCopyPath = Directory.EnumerateDirectories(instanceIdFolder).First();
			var sourceFolderName = Path.GetFileName(bookToCopyPath);
			var destinationFolderPath = Path.Combine(destination, sourceFolderName);
			Directory.CreateDirectory(destinationFolderPath);
			var boolFileCount = 0;
			foreach (var fileToCopy in Directory.EnumerateFiles(bookToCopyPath))
			{
				var fileName = Path.GetFileName(fileToCopy);
				RobustFile.Copy(fileToCopy, Path.Combine(destinationFolderPath, fileName));
				//Console.WriteLine("  " + fileName + " copied to " + destinationFolderPath);
				boolFileCount++;
			}
			return boolFileCount;
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
