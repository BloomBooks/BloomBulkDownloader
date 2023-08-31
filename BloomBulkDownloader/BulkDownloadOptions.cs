﻿using System;
using System.IO;
using CommandLine;

namespace BloomBulkDownloader
{

	// Used with https://github.com/gsscoder/commandline, which we get via nuget.
	// (using the beta of commandline 2.0)
	public class BulkDownloadOptions
	{
		private const string SandboxBucketName = "BloomLibraryBooks-Sandbox";
		private const string ProductionBucketName = "BloomLibraryBooks";

		[Value(0, MetaName = "destination", HelpText = "Final filtered destination path for books.", Required = true)]
		public string FinalDestinationPath { get; set; }

		[Option('f', "syncfolder", HelpText = "Local destination path for the sync phase.", Required = true)]
		public string BaseSyncFolder { get; set; }

		[Option('b', "bucket", HelpText = "S3 bucket to sync with (values are: 'sandbox', or 'production').", Required = true)]
		public BucketCategory Bucket { get; set; }

		[Option('u', "user", HelpText = "Just do a trial run of files for one user. Provide their email.", Required = false)]
		public string UserEmail { get; set; }

		[Option('d', "dryrun", HelpText = "List files synced to console, but don't actually download. Skips the second phase of copying filtered files to final destination.", Required = false)]
		public bool DryRun { get; set; }

		[Option('s', "skipS3", HelpText = "Skip the S3 download (Phase 1). Only do the second phase of copying filtered files to final destination.", Required = false)]
		public bool SkipDownload { get; set; }

		[Option('i', "include", HelpText = "File filter", Required = false)]
		public string IncludeFilter { get; set; }

		public bool OneUserOnly { get { return !string.IsNullOrEmpty(UserEmail); } }
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
				if (string.IsNullOrEmpty(BaseSyncFolder))
				{
					// This is only necessary for testing, since in production processing the options
					// respects the 'require=true' flag.
					throw new ApplicationException("Please set a BaseSyncFolder for your test.");
				}
				switch (Bucket)
				{
					case BucketCategory.undefined:
						break;
					case BucketCategory.production:
						if (!string.IsNullOrEmpty(UserEmail))
						{
							return Path.Combine(BaseSyncFolder, UserEmail);
						}
						else
						{
							return BaseSyncFolder;
						}
					case BucketCategory.sandbox:
						var folder = BaseSyncFolder + "-sandbox";
						if (!string.IsNullOrEmpty(UserEmail))
						{
							return Path.Combine(folder, UserEmail);
						}
						else
						{
							return folder;
						}
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
	}
}
