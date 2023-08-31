# BloomBulkDownloader

A simple command line program to download books from BloomLibrary for offline distribution

Requires that AWS CLI be installed, access keys be set up (`aws configure`).

Options:

```
 BloomBulkDownloader <options> destination-of-downloaded-books
  -f, --syncfolder        Required. Local destination path for the sync phase.

  -b, --bucket            Required. S3 bucket to sync with (values are: 'sandbox', or 'production').

  -u, --user              Just do a trial run of files for one user. Provide their email.

  -d, --dryrun            List files synced to console, but don't actually download. Skips the second phase of copying filtered files to final
                          destination.

  -s, --skipS3            Skip the S3 download (Phase 1). Only do the second phase of copying filtered files to final destination.

  -i, --include          File filter. E.g. --include "*/*/custom*Styles.css"

  --help                  Display this help screen.

  --version               Display version information.
```
