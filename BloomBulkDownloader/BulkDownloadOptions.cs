using System;
using CommandLine;

namespace Bloom.WebLibraryIntegration
{

	// Used with https://github.com/gsscoder/commandline, which we get via nuget.
	// (using the beta of commandline 2.0)
	public class BulkDownloadOptions
	{
		public const string SandboxBucketName = "BloomLibraryBooks-Sandbox";
		public const string ProductionBucketName = "BloomLibraryBooks";

		[Value(0, MetaName = "destination", HelpText = "Final filtered destination path for books", Required = true)]
		public string FinalDestinationPath { get; set; }

		[Option('b', "bucket", HelpText = "S3 bucket to sync with (values are: 'sandbox', or 'production')", Required = true)]
		public BucketCategory Bucket { get; set; }

		[Option('t', "trial", HelpText = "Just do a trial run of 3 files", Required = false)]
		public bool TrialRun { get; set; }

		[Option('d', "dryrun", HelpText = "List files synced to console, but don't actually download", Required = false)]
		public bool DryRun { get; set; }

		public string S3BucketName
		{
			get
			{
				switch (Bucket)
				{
					case BucketCategory.undefined:
						break;
					case BucketCategory.production:
						return ProductionBucketName;
					case BucketCategory.sandbox:
						return SandboxBucketName;
				}
				throw new ApplicationException("Trying to read S3BucketName before Bucket category is defined...");
			}
		}

		/// <summary>
		/// The result of the -b command line option. Used in switches to simplify processing.
		/// </summary>
		public enum BucketCategory
		{
			undefined,
			sandbox,
			production,
		}

		/// <summary>
		/// Gets the destination folder for the initial sync phase.
		/// N.B. Check for existence before accessing.
		/// </summary>
		public string SyncFolder
		{
			get
			{
				const string finalFolder = "BloomBulkDownloader-SyncFolder";
				switch (Bucket)
				{
					case BucketCategory.undefined:
						break;
					case BucketCategory.production:
						return finalFolder;
					case BucketCategory.sandbox:
						return finalFolder + "-sandbox";
				}
				throw new ApplicationException("Trying to read SyncFolder before Bucket category is defined...");
			}
		}
	}
}
