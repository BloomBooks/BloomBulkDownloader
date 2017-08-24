# BloomBulkDownloader
A simple command line program to download books from BloomLibrary for offline distribution

Running "BloomBulkDownloader --help" gives the following help screen:

BloomBulkDownloader 1.0.0.0
Copyright c  2017

  -b, --bucket            Required. S3 bucket to sync with (values are:
                          'sandbox', or 'production').

  -t, --trial             Just do a trial run of files for one user.

  -d, --dryrun            List files synced to console, but don't actually
                          download. Skips the second phase of copying filtered
                          files to final destination.

  --help                  Display this help screen.

  --version               Display version information.

  destination (pos. 0)    Required. Final filtered destination path for books.

BloomBulkDownloader requires command line arguments.
