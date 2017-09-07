using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BloomBulkDownloader;
using RestSharp.Extensions.MonoHttp;

namespace BloomBulkDownloaderTests
{
	[TestFixture]
	public class DownloaderTests
	{
		private List<DownloaderParseRecord> _testParseRecords;
		private readonly ParseUploader _testUploader1;
		private readonly ParseUploader _testUploader2;
		private readonly ParseLanguage _testLanguage;
		private readonly DateTime _oct102015;
		private readonly DateTime _jan012017;

		public DownloaderTests()
		{
			_oct102015 = new DateTime(2015, 10, 10);
			_jan012017 = new DateTime(2017, 01, 01);
			_testUploader1 = new ParseUploader {Email = "somebody@gmail.com"};
			_testUploader2 = new ParseUploader {Email = "somebodyelse@gmail.com"};
			_testLanguage = new ParseLanguage
			{
				EthnologueCode = "eng",
				IsoCode = "en",
				Name = "English"
			};
		}

		[SetUp]
		public void TestSetup()
		{
			_testParseRecords = new List<DownloaderParseRecord>();
		}

		[TearDown]
		public void TestTeardown()
		{
			_testParseRecords = null;
		}

		[Test]
		public void GetSyncCmdLineArgs_Sandbox()
		{
			var opts = new BulkDownloadOptions
			{
				Bucket = BulkDownloadOptions.BucketCategory.sandbox,
				BaseSyncFolder = "testFolder"
			};

			// Verify some initial settings
			Assert.IsFalse(opts.TrialRun);
			Assert.IsFalse(opts.DryRun);
			Assert.That(opts.TrialEmail, Is.EqualTo(string.Empty));
			Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-develop.azurewebsites.net"));

			// SUT
			var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

			// Verify
			Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks-Sandbox testFolder-sandbox"));
		}

		[Test]
		public void GetSyncCmdLineArgs_Production()
		{
			var opts = new BulkDownloadOptions
			{
				Bucket = BulkDownloadOptions.BucketCategory.production,
				BaseSyncFolder = "testFolder"
			};

			// Verify some initial settings
			Assert.IsFalse(opts.TrialRun);
			Assert.IsFalse(opts.DryRun);
			Assert.That(opts.TrialEmail, Is.EqualTo(string.Empty));
			Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-production.azurewebsites.net"));

			// SUT
			var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

			// Verify
			Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks testFolder"));
		}

		[Test]
		public void GetSyncCmdLineArgs_Sandbox_Trial_Dryrun()
		{
			var opts = new BulkDownloadOptions
			{
				Bucket = BulkDownloadOptions.BucketCategory.sandbox,
				DryRun = true, TrialRun = true,
				BaseSyncFolder = "testFolder"
			};

			// Verify some initial settings
			Assert.IsTrue(opts.TrialRun);
			Assert.IsTrue(opts.DryRun);
			Assert.That(opts.TrialEmail, Is.EqualTo("gordon_martin@sil.org"));
			Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-develop.azurewebsites.net"));

			// SUT
			var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

			// Verify
			Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks-Sandbox/gordon_martin@sil.org testFolder-sandbox\\gordon_martin@sil.org --dryrun"));
		}

		[Test]
		public void GetSyncCmdLineArgs_Production_Trial_Dryrun()
		{
			var opts = new BulkDownloadOptions
			{
				Bucket = BulkDownloadOptions.BucketCategory.production,
				DryRun = true, TrialRun = true,
				BaseSyncFolder = "testFolder"
			};

			// Verify some initial settings
			Assert.IsTrue(opts.TrialRun);
			Assert.IsTrue(opts.DryRun);
			Assert.That(opts.TrialEmail, Is.EqualTo("gordon_martin@sil.org"));
			Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-production.azurewebsites.net"));

			// SUT
			var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

			// Verify
			Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks/gordon_martin@sil.org testFolder\\gordon_martin@sil.org --dryrun"));
		}

		[Test]
		public void GetFilteredListOfBooksToCopy_SameGuidForTwoBooks()
		{
			// Setup
			var opts = new BulkDownloadOptions { Bucket = BulkDownloadOptions.BucketCategory.production };
			SetupParseRecordsTwoWithSameInstanceId();

			// Use our pre-determined set of DownloaderParseRecords
			BulkDownload.GetParseDbBooks = TestParseDbDelegate;

			// SUT
			var listOfBooks = BulkDownload.GetFilteredListOfBooksToCopy(opts);
			Assert.That(listOfBooks.Keys.Count, Is.EqualTo(3));
			Assert.That(listOfBooks.First().Value.Item1, Is.EqualTo("Box test"));
			Assert.That(listOfBooks.First().Value.Item2, Is.EqualTo(_testUploader1.Email));
			Assert.That(listOfBooks.First().Value.Item3, Is.EqualTo(_oct102015));
			var expected = new Tuple<string, string, DateTime>("Other test_somebody@gmail.com", "somebody@gmail.com", _oct102015);
			Assert.That(listOfBooks[CreateBaseUrl(_testUploader1.Email, _testParseRecords[1].InstanceId, _testParseRecords[1].Title)],
				Is.EqualTo(expected));
			Assert.That(listOfBooks.Last().Value.Item1, Is.EqualTo("Other test_somebodyelse@gmail.com"));
			Assert.That(listOfBooks.Last().Value.Item2, Is.EqualTo(_testUploader2.Email));
			Assert.That(listOfBooks.Last().Value.Item3, Is.EqualTo(_jan012017));
		}

		private IEnumerable<DownloaderParseRecord> TestParseDbDelegate(BulkDownloadOptions options)
		{
			return _testParseRecords;
		}

		private void SetupParseRecordsTwoWithSameInstanceId()
		{

			var parseRec1 = new DownloaderParseRecord
			{
				InCirculation = true,
				InstanceId = "09f5edbe-6259-471e-88ea-e409113ddfb3",
				Title = "\nBox\ntest\n", // Tests Title sanitization
				Uploader = _testUploader1,
				LastUpdated = _oct102015,
				Languages = new List<ParseLanguage> { _testLanguage }
			};
			parseRec1.BaseUrl = CreateBaseUrl(_testUploader1.Email, parseRec1.InstanceId, parseRec1.Title);
			_testParseRecords.Add(parseRec1);
			var parseRec2 = new DownloaderParseRecord
			{
				InCirculation = true,
				InstanceId = "0612f158-2143-4767-b8a0-b83b32266810",
				Title = "Other test",
				Uploader = _testUploader1,
				LastUpdated = _oct102015,
				Languages = new List<ParseLanguage> { _testLanguage }
			};
			parseRec2.BaseUrl = CreateBaseUrl(_testUploader1.Email, parseRec2.InstanceId, parseRec2.Title);
			_testParseRecords.Add(parseRec2);
			var parseRec3 = new DownloaderParseRecord
			{
				InCirculation = true,
				InstanceId = "0612f158-2143-4767-b8a0-b83b32266810", // same instance and title as record 2, different uploader
				Title = "Other test",
				Uploader = _testUploader2,
				LastUpdated = _oct102015,
				Languages = new List<ParseLanguage> { _testLanguage }
			};
			parseRec3.BaseUrl = CreateBaseUrl(_testUploader2.Email, parseRec3.InstanceId, parseRec3.Title);
			_testParseRecords.Add(parseRec3);
			var parseRec4 = new DownloaderParseRecord
			{
				InCirculation = true,
				InstanceId = "0612f158-2143-4767-b8a0-b83b32266810", // same instance, title, and uploader as record 3, later update time
				Title = "Other test",
				Uploader = _testUploader2,
				LastUpdated = _jan012017,
				Languages = new List<ParseLanguage> { _testLanguage }
			};
			parseRec4.BaseUrl = CreateBaseUrl(_testUploader2.Email, parseRec4.InstanceId, parseRec4.Title);
			_testParseRecords.Add(parseRec4);
		}

		private string CreateBaseUrl(string email, string instanceId, string title)
		{
			// Constructs a simulated BaseUrl for our test record

			const string baseUrlPrefix = "https://s3.amazonaws.com/BloomLibraryBooks/";

			return baseUrlPrefix + HttpUtility.UrlEncode(email + "/" + instanceId + "/" + title + "/");
		}
	}
}
