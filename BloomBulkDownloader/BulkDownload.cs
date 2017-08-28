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
	/// This class syncs a local folder with an S3 bucket and then copies certain books to the specified destination.
	/// See BulkDownloadOptions for the expected options.
	/// </summary>
	public class BulkDownload
	{
		/// <summary>
		/// The method defined by this delegate retrieves the "inCirculation" books from the parse db.
		/// Using a delegate allows for testing with a known set of books.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public delegate IEnumerable<DownloaderParseRecord> ParseDbDelegate(BulkDownloadOptions options);

		public static ParseDbDelegate GetParseDbBooks { private get; set; }

		public static int Handle(BulkDownloadOptions options)
		{
			// This task will be all the program does. We need to do enough setup so that
			// the download code can work.
			try
			{
				if (!options.SkipDownload)
				{
					Console.WriteLine("Commencing download from: " + options.S3BucketName +
					                  " to: " + options.FinalDestinationPath);
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
				}
				if (Directory.Exists(options.FinalDestinationPath))
				{
					RobustIO.DeleteDirectory(options.FinalDestinationPath, true);
				}
				Console.WriteLine("\nCreating clean empty directory at: " + options.FinalDestinationPath);
				Directory.CreateDirectory(options.FinalDestinationPath);

				GetParseDbBooks = GetParseDbBooksInCirculation; // Set production delegate

				var filteredListOfBooks = GetFilteredListOfBooksToCopy(options);
				Console.WriteLine("\nFiltering complete. " + filteredListOfBooks.Count + " books to copy.");

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

		/// <summary>
		/// public for testing
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public static Dictionary<string, Tuple<string, string>> GetFilteredListOfBooksToCopy(BulkDownloadOptions options)
		{
			// Get mongodb filtered by inCirculation flag
			var records = GetParseDbBooks(options);
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
			// The other string is the (disambiguated, if necessary) book Title, and uploader.
			// Disambiguation is by appending "_uploaderEmailAddress" to the Title.
			var filteredInstanceIds = new Dictionary<string, Tuple<string, string>>();
			foreach (var kvpTitle in filteredBooks)
			{
				var recordList = kvpTitle.Value;
				if (recordList.Count == 1)
				{
					filteredInstanceIds.Add(recordList[0].InstanceId, new Tuple<string, string>(recordList[0].Title, recordList[0].Uploader.Email));
				}
				else
				{
					foreach (var record in recordList)
					{
						filteredInstanceIds.Add(record.InstanceId, new Tuple<string, string>(record.Title + "_" + record.Uploader.Email, record.Uploader.Email));
					}
				}
			}

			return filteredInstanceIds;
		}

		private static int CopyMatchingBooksToFinalDestination(BulkDownloadOptions options, IDictionary<string, Tuple<string, string>> filteredInstanceIds)
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
				var destinationBookFolder = string.Empty;
				if (!emailAcctString.Contains("@"))
				{
					// This is a book without a separate uploader directory; use this guid string as the outer book folder
					// (Not sure yet if this occurs in the Production S3 bucket, but there are a couple of instances in the Sandbox.
					// In any case, we'll include it for completeness.)
					if (!filteredInstanceIds.ContainsKey(emailAcctString) || filteredInstanceIds[emailAcctString].Item2 != string.Empty)
						continue;

					destinationBookFolder = Path.Combine(destination, filteredInstanceIds[emailAcctString].Item1);
					fileCount = CopyOneBook(fileCount, directory, destinationBookFolder, ref bookCount);
				}
				else
				{
					foreach (var subDirectory in Directory.EnumerateDirectories(directory))
					{
						var sourceDirGuidString = Path.GetFileName(subDirectory);
						if (!filteredInstanceIds.ContainsKey(sourceDirGuidString) || filteredInstanceIds[sourceDirGuidString].Item2 != emailAcctString)
							continue;

						destinationBookFolder = Path.Combine(destination, filteredInstanceIds[sourceDirGuidString].Item1);
						fileCount = CopyOneBook(fileCount, subDirectory, destinationBookFolder, ref bookCount);
					}
				}
			}
			Console.WriteLine("\nBooks copied: " + bookCount + "  Files copied: " + fileCount);
			return 0;
		}

		private static int CopyOneBook(int fileCount, string directory, string destinationBookFolder, ref int bookCount)
		{
			var filesCopied = CopyOneBookFolder(directory, destinationBookFolder);
			if (filesCopied > 0)
			{
				Console.Write("."); // breadcrumb for each book copied
				bookCount++;
			}
			return fileCount + filesCopied;
		}

		private static int CopyOneBookFolder(string instanceIdFolder, string destinationBookFolder)
		{
			// This 'instanceIdFolder' contains a book we need to copy.
			var bookToCopyPath = Directory.EnumerateDirectories(instanceIdFolder).First();
			if (Directory.Exists(destinationBookFolder))
			{
				Debug.Fail("Failed to copy from folder " + Path.GetFileName(instanceIdFolder) + " because it already existed");
				return 0; // something is wrong here.
			}
			Directory.CreateDirectory(destinationBookFolder);
			var boolFileCount = 0;
			foreach (var fileToCopy in Directory.EnumerateFiles(bookToCopyPath))
			{
				var fileName = Path.GetFileName(fileToCopy);
				RobustFile.Copy(fileToCopy, Path.Combine(destinationBookFolder, fileName));
				//Console.WriteLine("  " + fileName + " copied to " + destinationFolderPath);
				boolFileCount++;
			}
			return boolFileCount;
		}

		private static IEnumerable<DownloaderParseRecord> GetParseDbBooksInCirculation(BulkDownloadOptions options)
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
