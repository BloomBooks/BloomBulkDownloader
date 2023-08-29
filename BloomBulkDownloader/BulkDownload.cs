using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Extensions.MonoHttp;
using SIL.IO;
using Spart.Parsers;

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

		private const string ErrorFile = "problems.txt";
		private static string _pubFolder;

		public static int Handle(BulkDownloadOptions options)
		{
			// This task will be all the program does. We need to do enough setup so that
			// the download code can work.
			try
			{
				_pubFolder = options.FinalDestinationPath;
				if (!options.SkipDownload)
				{
					Console.WriteLine("Commencing download from: " + options.S3BucketName + " to: " + _pubFolder);
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
				if (Directory.Exists(_pubFolder))
				{
					Console.WriteLine("\nEmptying out previously used directory: " + _pubFolder);
					SIL.IO.RobustIO.DeleteDirectory(_pubFolder, true);
				}
				Console.WriteLine("\nCreating clean empty directory at: " + _pubFolder);
				Directory.CreateDirectory(_pubFolder);

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
			cmdLineArgs += opts.S3BucketName; // source
			if (opts.OneUserOnly)
			{
				cmdLineArgs += "/" + opts.UserEmail;
			}
			cmdLineArgs += " " + opts.SyncFolder; // target
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
		public static Dictionary<string, Tuple<string, string, DateTime>> GetFilteredListOfBooksToCopy(BulkDownloadOptions options)
		{
			// Get mongodb filtered by inCirculation flag
			var records = GetParseDbBooks(options);
			// dictionary keyed on Title, so we can disambiguate different books with the same Title
			var filteredBooks = new Dictionary<string, List<DownloaderParseRecord>>();

			// Collect up all the records from the mongodb of books to be copied.
			// Further filter if we are doing a trial run.
			foreach (var parseRecord in records)
			{
				if (options.OneUserOnly && parseRecord.Uploader.Email != options.UserEmail)
					continue;
				// Use Title.ToLowerInvariant() since Windows foldernames are case insensitive.
				if (filteredBooks.TryGetValue(parseRecord.Title.ToLowerInvariant(), out List<DownloaderParseRecord> listMatchingThisTitle))
				{
					listMatchingThisTitle.Add(parseRecord);
				}
				else
				{
					filteredBooks.Add(parseRecord.Title.ToLowerInvariant(), new List<DownloaderParseRecord> { parseRecord });
				}
			}

			// Now disambiguate any book Titles that have been uploaded by more than one email address.
			// 'filteredBaseUrlsToCopy' is keyed on part of the BaseUrl of the book to copy over.
			// (GJM--This gets around an email mismatch problem I was having where the uploader email in the mongodb was
			// different than the email that S3 had the book filed under.)
			// The tuple is the (disambiguated, if necessary) book Title, uploader, and lastupdate datestamp.
			// Disambiguation is by appending "_uploaderEmailAddress" to the Title.
			var filteredBaseUrlsToCopy = new Dictionary<string, Tuple<string, string, DateTime>>();
			foreach (var kvpTitle in filteredBooks)
			{
				var booksWithThisTitle = kvpTitle.Value;
				Tuple<string, string, DateTime> bookInfoTuple;
				if (booksWithThisTitle.Count == 1)
				{
					var bookParseRecord = booksWithThisTitle[0];
					var bookBaseUrl = bookParseRecord.BaseUrl;
					if (string.IsNullOrWhiteSpace(bookBaseUrl))
					{
						ReportMissingBookOnS3(bookParseRecord);
						continue;
					}
					bookInfoTuple = new Tuple<string, string, DateTime>(
						bookParseRecord.Title, bookParseRecord.Uploader.Email, bookParseRecord.LastUpdated);
					UpdateDictWithMostRecentMatch(filteredBaseUrlsToCopy, bookParseRecord, bookInfoTuple);
				}
				else
				{
					foreach (var bookParseRecord in booksWithThisTitle)
					{
						if (string.IsNullOrWhiteSpace(bookParseRecord.BaseUrl))
						{
							ReportMissingBookOnS3(bookParseRecord);
							continue;
						}
						// Disambiguate title of new folder by appending underscore plus email address.
						bookInfoTuple = new Tuple<string, string, DateTime>(
							bookParseRecord.Title + "_" + bookParseRecord.Uploader.Email, bookParseRecord.Uploader.Email, bookParseRecord.LastUpdated);
						UpdateDictWithMostRecentMatch(filteredBaseUrlsToCopy, bookParseRecord, bookInfoTuple);
					}
				}
			}

			return filteredBaseUrlsToCopy;
		}

		private static void UpdateDictWithMostRecentMatch(Dictionary<string, Tuple<string, string, DateTime>> filteredBaseUrlsToCopy,
			DownloaderParseRecord bookParseRecord, Tuple<string, string, DateTime> bookInfoTuple)
		{
			var bookBaseUrl = bookParseRecord.BaseUrl;
			if (filteredBaseUrlsToCopy.TryGetValue(bookBaseUrl, out Tuple<string, string, DateTime> existingBookByUrl))
			{
				ReportSameBaseUrlFound(bookParseRecord);
				var existingDate = existingBookByUrl.Item3;
				if (existingDate >= bookInfoTuple.Item3)
				{
					return;
				}
				filteredBaseUrlsToCopy.Remove(bookBaseUrl);
			}
			filteredBaseUrlsToCopy.Add(bookBaseUrl, bookInfoTuple);
		}

		private static void ReportSameBaseUrlFound(DownloaderParseRecord parseRecord)
		{
			AddErrorMessageToProblemFile("Duplicate book '" + parseRecord.Title + "' uploaded by " + parseRecord.Uploader.Email +
							  " was found by baseUrl on S3.\n  Copying only the most recently one.");
		}

		private static void ReportMissingBookOnS3(DownloaderParseRecord parseRecord)
		{
			AddErrorMessageToProblemFile("Book '" + parseRecord.Title + "' uploaded by " + parseRecord.Uploader.Email +
							  " has no BaseUrl and can't be copied.");
		}

		private static int CopyMatchingBooksToFinalDestination(BulkDownloadOptions options, IDictionary<string, Tuple<string, string, DateTime>> filteredBaseUrlsToCopy)
		{
			// Copy to '_pubFolder' folder all folders inside of folders whose names match one of the 'filteredBaseUrlsToCopy'.
			Console.WriteLine("\nStarting filtered copy to " + _pubFolder);
			var notFoundDirs = new List<string>();
			var fileCount = 0;
			var bookCount = 0;
			foreach (var key in filteredBaseUrlsToCopy.Keys)
			{
				// key is the base url to copy
				var decodedKey = DecodeBaseUrl(key); // decode base url into [email, instanceId, title] array
													 // In the case of a TrialRun, the SyncFolder already includes the email address
				var baseSourcePath = options.OneUserOnly ? options.SyncFolder : Path.Combine(options.SyncFolder, decodedKey[0]);
				var sourceDirGuidString = Path.Combine(baseSourcePath, decodedKey[1]);
				if (!Directory.Exists(sourceDirGuidString))
				{
					// For some unknown reason, the parsedb record exists, but the book is missing on Amazon S3?
					// We will report these after copying the books we find.
					notFoundDirs.Add(sourceDirGuidString);
					continue;
				}
				var destinationBookFolder = Path.Combine(_pubFolder, filteredBaseUrlsToCopy[key].Item1);
				fileCount = CopyOneBook(fileCount, sourceDirGuidString, destinationBookFolder, ref bookCount);
			}
			Console.WriteLine("\nBooks copied: " + bookCount + "  Files copied: " + fileCount);
			if (notFoundDirs.Count > 0)
			{
				var errorMsg = "\nThe following books were found inCirculation on the parsedb, but Amazon S3 didn't have them:\n";
				foreach (var missingDir in notFoundDirs)
				{
					errorMsg += "  " + missingDir + "\n";
				}
				AddErrorMessageToProblemFile(errorMsg);
			}
			return 0;
		}

		private static void AddErrorMessageToProblemFile(string errorMsg)
		{
			if (string.IsNullOrEmpty(_pubFolder))
			{
				// happens in tests
				return;
			}
			var filename = Path.Combine(_pubFolder, ErrorFile);
			File.AppendAllText(filename, errorMsg + "\n");
		}

		/// <summary>
		/// Public for testing
		/// </summary>
		/// <param name="baseUrl"></param>
		/// <returns></returns>
		public static string[] DecodeBaseUrl(string baseUrl)
		{
			// decode base url into an array -> [email, instanceId, title]
			var strippedResult = HttpUtility.UrlDecode(baseUrl.Substring(baseUrl.LastIndexOf('/') + 1));

			return strippedResult.Split('/');
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
				AddErrorMessageToProblemFile("When copying from folder " + Path.GetFileName(instanceIdFolder) +
								  " destination " + destinationBookFolder + " already existed; disambiguating...");
				try
				{
					destinationBookFolder = GetUniqueDestinationFolder(destinationBookFolder);
				}
				catch (ApplicationException)
				{
					return 0;
				}
			}
			Directory.CreateDirectory(destinationBookFolder);
			var boolFileCount = 0;
			foreach (var fileToCopy in Directory.EnumerateFiles(bookToCopyPath))
			{
				var fileName = Path.GetFileName(fileToCopy);
				if (fileName == null)
					continue;
				try
				{
					RobustFile.Copy(fileToCopy, Path.Combine(destinationBookFolder, fileName));
				}
				// The one case we've found that causes this is an empty thumbnail.png file in the book.
				catch (ArgumentException e)
				{
					AddErrorMessageToProblemFile("ArgumentException copying '" + fileToCopy + "'. " + e.Message);
					continue; // We've reported the problem, skip the file
				}
				boolFileCount++;
			}
			return boolFileCount;
		}

		private static string GetUniqueDestinationFolder(string destinationBookFolder)
		{
			var digit = 1;
			while (digit < 10 && Directory.Exists(destinationBookFolder + "_" + digit))
			{
				digit++;
			}
			if (digit == 10)
			{
				AddErrorMessageToProblemFile("Too many identically titled books by the same uploader: " + destinationBookFolder);
				throw new ApplicationException("Too many identical books"); // caught above
			}
			return destinationBookFolder + "_" + digit;
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
			request.AddQueryParameter("where", "{\"inCirculation\":{\"$ne\":false}}");
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
