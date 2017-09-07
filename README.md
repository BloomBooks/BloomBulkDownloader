# BloomBulkDownloader
A simple command line program to download books from BloomLibrary for offline distribution

Running "BloomBulkDownloader --help" gives the following help screen:

<p>BloomBulkDownloader {version}</p>
<p>Copyright c  2017</p>
<table>
<tr>
<td>-f, --syncfolder</td>
<td>Required.</td>
<td>Local destination path for the sync phase.</td>
</tr>
<tr>
<td>-b, --bucket</td><td>Required.</td><td>S3 bucket to sync with (values are: 'sandbox', or 'production').</td>
</tr>
<tr>
<td>-t, --trial</td><td></td><td>Just do a trial run of files for one user.</td>
</tr>
<tr>
<td>-d, --dryrun</td><td></td><td>List files synced to console, but don't actually download. Skips the second phase of copying filtered files to final destination.</td>
</tr>
<tr>
<td>-s, --skipS3</td><td></td><td>Skip the S3 download (Phase 1). Only do the second phase of copying filtered files to final destination.</td>
</tr>
<tr>
<td>--help</td><td></td><td>Display this help screen.</td>
</tr>
<tr>
<td>--version</td><td></td><td>Display version information.</td>
</tr>
<tr>
<td>destination (pos. 0)</td><td>Required.</td><td>Final filtered destination path for books.</td>
</tr>
</table>
<p>BloomBulkDownloader requires command line arguments.</p>