using System;
using CommandLine;

namespace BloomBulkDownloader
{

	// Used with https://github.com/gsscoder/commandline, which we get via nuget.
	// (using the beta of commandline 2.0)
	public class BulkDownloadOptions
	{
		public const string SandboxBucketName = "BloomLibraryBooks-Sandbox";
		public const string ProductionBucketName = "BloomLibraryBooks";

		[Value(0, MetaName = "destination", HelpText = "Final filtered destination path for books.", Required = true)]
		public string FinalDestinationPath { get; set; }

		[Option('b', "bucket", HelpText = "S3 bucket to sync with (values are: 'sandbox', or 'production').", Required = true)]
		public BucketCategory Bucket { get; set; }

		[Option('t', "trial", HelpText = "Just do a trial run of files for one user.", Required = false)]
		public bool TrialRun { get; set; }

		[Option('d', "dryrun", HelpText = "List files synced to console, but don't actually download. Skips the second phase of copying filtered files to final destination.", Required = false)]
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
				const string finalFolder = "C:\\BloomBulkDownloader-SyncFolder";
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

		/// <summary>
		/// Gets the appropriate Parse server (Production or Sandbox).
		/// Used for determining if a book is deleted or not (and other things).
		/// </summary>
		public string ParseServer
		{
			get
			{
				const string parseServerProd = "https://bloom-parse-server-production.azurewebsites.net";
				const string parseServerSandbox = "https://bloom-parse-server-develop.azurewebsites.net";
				switch (Bucket)
				{
					case BucketCategory.undefined:
						break;
					case BucketCategory.production:
						return parseServerProd;
					case BucketCategory.sandbox:
						return parseServerSandbox;
				}
				throw new ApplicationException("Trying to read ParseServer before Bucket category is defined...");
			}
		}

		/// <summary>
		/// Get the appropriate Parse AppId (Production or Sandbox).
		/// </summary>
		public string ParseAppId
		{
			get
			{
				const string appIdProd = "R6qNTeumQXjJCMutAJYAwPtip1qBulkFyLefkCE5";
				const string appIdSandbox = "yrXftBF6mbAuVu3fO6LnhCJiHxZPIdE7gl1DUVGR";
				switch (Bucket)
				{
					case BucketCategory.undefined:
						break;
					case BucketCategory.production:
						return appIdProd;
					case BucketCategory.sandbox:
						return appIdSandbox;
				}
				throw new ApplicationException("Trying to read ParseAppId before Bucket category is defined...");
			}
		}

		/// <summary>
		/// Get the appropriate Rest Api Key (Production or Sandbox).
		/// </summary>
		public string RestApiKey
		{
			get
			{
				const string restApiKeyProd = "P6dtPT5Hg8PmBCOxhyN9SPmaJ8W4DcckyW0EZkIx";
				const string restApiKeySandbox = "KZA7c0gAuwTD6kZHyO5iZm0t48RplaU7o3SHLKnj";

				switch (Bucket)
				{
					case BucketCategory.undefined:
						break;
					case BucketCategory.production:
						return restApiKeyProd;
					case BucketCategory.sandbox:
						return restApiKeySandbox;
				}
				throw new ApplicationException("Trying to read RestApiKey before Bucket category is defined...");
			}
		}

		/// <summary>
		/// Get the appropriate Email address for a trial run (Production or Sandbox).
		/// </summary>
		public string TrialEmail
		{
			get
			{
				const string trialEmail = "gordon_martin@sil.org";

				if (!TrialRun)
				{
					return string.Empty;
				}

				switch (Bucket)
				{
					case BucketCategory.undefined:
						break;
					case BucketCategory.production:
					case BucketCategory.sandbox:
						return trialEmail;
				}
				throw new ApplicationException("Trying to read TrialEmail before Bucket category is defined...");
			}
		}
	}
}
