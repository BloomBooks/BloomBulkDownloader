using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.Runtime.Internal.Auth;
using Amazon.S3;

namespace BloomBulkDownloader
{
	class Program
	{
		static void Main(string[] args)
		{
			var s3 = new AmazonS3Client();
			s3.GetBucketReplication()
		}
	}
}
