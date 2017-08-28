using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BloomBulkDownloader;

namespace BloomBulkDownloaderTests
{
	[TestFixture]
    public class DownloaderTests
	{
		private List<DownloaderParseRecord> _testParseRecords;
		private readonly ParseUploader _testUploader1;
		private readonly ParseUploader _testUploader2;
		private readonly ParseLanguage _testLanguage;

		public DownloaderTests()
		{
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
		    var opts = new BulkDownloadOptions {Bucket = BulkDownloadOptions.BucketCategory.sandbox};

		    // Verify some initial settings
			Assert.IsFalse(opts.TrialRun);
			Assert.IsFalse(opts.DryRun);
			Assert.That(opts.TrialEmail, Is.EqualTo(string.Empty));
			Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-develop.azurewebsites.net"));

			// SUT
		    var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

			// Verify
			Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks-Sandbox C:\\BloomBulkDownloader-SyncFolder-sandbox"));
	    }

	    [Test]
	    public void GetSyncCmdLineArgs_Production()
	    {
		    var opts = new BulkDownloadOptions { Bucket = BulkDownloadOptions.BucketCategory.production };

		    // Verify some initial settings
		    Assert.IsFalse(opts.TrialRun);
		    Assert.IsFalse(opts.DryRun);
		    Assert.That(opts.TrialEmail, Is.EqualTo(string.Empty));
		    Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-production.azurewebsites.net"));

		    // SUT
		    var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

		    // Verify
		    Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks C:\\BloomBulkDownloader-SyncFolder"));
	    }

	    [Test]
	    public void GetSyncCmdLineArgs_Sandbox_Trial_Dryrun()
	    {
		    var opts = new BulkDownloadOptions
		    {
			    Bucket = BulkDownloadOptions.BucketCategory.sandbox,
			    DryRun = true, TrialRun = true
		    };

		    // Verify some initial settings
		    Assert.IsTrue(opts.TrialRun);
		    Assert.IsTrue(opts.DryRun);
		    Assert.That(opts.TrialEmail, Is.EqualTo("gordon_martin@sil.org"));
		    Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-develop.azurewebsites.net"));

		    // SUT
		    var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

		    // Verify
		    Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks-Sandbox/gordon_martin@sil.org C:\\BloomBulkDownloader-SyncFolder-sandbox\\gordon_martin@sil.org --dryrun"));
	    }

	    [Test]
	    public void GetSyncCmdLineArgs_Production_Trial_Dryrun()
	    {
		    var opts = new BulkDownloadOptions
		    {
			    Bucket = BulkDownloadOptions.BucketCategory.production,
			    DryRun = true, TrialRun = true
		    };

		    // Verify some initial settings
		    Assert.IsTrue(opts.TrialRun);
		    Assert.IsTrue(opts.DryRun);
		    Assert.That(opts.TrialEmail, Is.EqualTo("gordon_martin@sil.org"));
		    Assert.That(opts.ParseServer, Is.EqualTo("https://bloom-parse-server-production.azurewebsites.net"));

		    // SUT
		    var result = BulkDownload.GetSyncCommandLineArgsFromOptions(opts);

		    // Verify
		    Assert.That(result, Is.EqualTo("s3 sync s3://BloomLibraryBooks/gordon_martin@sil.org C:\\BloomBulkDownloader-SyncFolder\\gordon_martin@sil.org --dryrun"));
	    }

	    [Test]
	    public void GetFilteredListOfBooksToCopy_test()
	    {
			// Setup
		    var opts = new BulkDownloadOptions { Bucket = BulkDownloadOptions.BucketCategory.production };
		    SetupParseRecords();

			// Use our pre-determined set of DownloaderParseRecords
		    BulkDownload.GetParseDbBooks = TestParseDbDelegate;

			// SUT
		    var listOfBooks = BulkDownload.GetFilteredListOfBooksToCopy(opts);
			Assert.That(listOfBooks.Keys.Count, Is.EqualTo(2));
			Assert.That(listOfBooks.First().Value.Item1, Is.EqualTo(_testUploader1.Email));
		    Assert.That(listOfBooks.First().Value.Item2, Is.EqualTo("Box test"));
		    Assert.That(listOfBooks.Last().Value.Item1, Is.EqualTo(_testUploader2.Email));
		    Assert.That(listOfBooks.Last().Value.Item2, Is.EqualTo("Other test"));
	    }

		private IEnumerable<DownloaderParseRecord> TestParseDbDelegate(BulkDownloadOptions options)
	    {
		    return _testParseRecords;
	    }

		private void SetupParseRecords()
		{
			var parseRec1 = new DownloaderParseRecord
			{
				InCirculation = true,
				InstanceId = "09f5edbe-6259-471e-88ea-e409113ddfb3",
				Title = "\nBox\ntest\n",
				Uploader = _testUploader1,
				Languages = new List<ParseLanguage> { _testLanguage }
			};
			_testParseRecords.Add(parseRec1);
			var parseRec2 = new DownloaderParseRecord
			{
				InCirculation = true,
				InstanceId = "0612f158-2143-4767-b8a0-b83b32266810",
				Title = "Other test",
				Uploader = _testUploader1,
				Languages = new List<ParseLanguage> { _testLanguage }
			};
			_testParseRecords.Add(parseRec2);
			var parseRec3 = new DownloaderParseRecord
			{
				InCirculation = true,
				InstanceId = "0612f158-2143-4767-b8a0-b83b32266810", // same instance and title as record 2, different uploader
				Title = "Other test",
				Uploader = _testUploader2,
				Languages = new List<ParseLanguage> { _testLanguage }
			};
			_testParseRecords.Add(parseRec3);
		}
	}
}
