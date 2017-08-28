﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using BloomBulkDownloader;

namespace BloomBulkDownloaderTests
{
	[TestFixture]
    public class DownloaderTests
	{
		private IEnumerable<DownloaderParseRecord> _testParseRecords;

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

			// Use our pre-determined set of DownloaderParseRecords
		    BulkDownload.GetParseDbBooks = TestParseDbDelegate;

			// SUT
		    var listOfBooks = BulkDownload.GetFilteredListOfBooksToCopy(opts);
	    }

	    private IEnumerable<DownloaderParseRecord> TestParseDbDelegate(BulkDownloadOptions options)
	    {
		    return _testParseRecords;
	    }
    }
}
