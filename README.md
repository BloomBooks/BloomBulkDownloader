# BloomBulkDownloader
A simple command line program to download books from BloomLibrary for offline distribution

Requires that AWS CLI be installed and "S3" be available from the command line.

Running "BloomBulkDownloader --help" gives the following help screen:

Options:
```
  -f, --syncfolder        Required. Local destination path for the sync phase.

  -b, --bucket            Required. S3 bucket to sync with (values are: 'sandbox', or 'production').

  -u, --user              Just do a trial run of files for one user. Provide their email.

  -d, --dryrun            List files synced to console, but don't actually download. Skips the second phase of copying filtered files to final
                          destination.

  -s, --skipS3            Skip the S3 download (Phase 1). Only do the second phase of copying filtered files to final destination.

  --help                  Display this help screen.

  --version               Display version information.

  destination (pos. 0)    Required. Final filtered destination path for books.
  ```