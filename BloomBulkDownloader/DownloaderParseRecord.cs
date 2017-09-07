using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BloomBulkDownloader
{
	class MainParseDownloaderObject
	{
		[JsonProperty("results")]
		public IEnumerable<DownloaderParseRecord> Results { get; set; }

		[JsonProperty("count")]
		public int Count { get; set; }
	}

	public class DownloaderParseRecord
	{
		private List<Tuple<string, string>> _tags;
		private string _title;

		[JsonProperty("objectId")]
		public string ObjectId { get; set; }

		[JsonProperty("inCirculation")]
		public bool InCirculation { get; set; }

		[JsonProperty("bookInstanceId")]
		public string InstanceId { get; set; }

		[JsonProperty("allTitles")]
		internal string AllTitlesInternal { get; set; }

		[JsonProperty("title")]
		public string Title
		{
			get { return _title; }
			set { _title = SanitizeTitle(value.Trim()); }
		}

		[JsonProperty("tags")]
		internal IEnumerable<string> TagsInternal { get; set; }

		[JsonProperty("langPointers")]
		public IEnumerable<ParseLanguage> Languages { get; set; }

		[JsonProperty("uploader")]
		public ParseUploader Uploader { get; set; }

		[JsonProperty("updatedAt")]
		public DateTime LastUpdated { get; set; }

		[JsonProperty("baseUrl")]
		public string BaseUrl { get; set; }

		public IEnumerable<Tuple<string, string>> Tags
		{
			get
			{
				if (_tags == null)
				{
					_tags = new List<Tuple<string, string>>();
					foreach (var tag in TagsInternal)
					{
						var parts = tag.Split(':');
						_tags.Add(new Tuple<string, string>(parts[0], parts[1]));
					}
				}
				return _tags;
			}
		}

		private static string SanitizeTitle(string title)
		{
			const char space = ' ';
			title = Path.GetInvalidFileNameChars().Aggregate(
				title, (current, character) => current.Replace(character, space));
			return title.Replace('&', space);
		}

	}

	public class ParseUploader
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("username")]
		public string Username { get; set; }

		[JsonProperty("administrator")]
		public bool IsAdministrator { get; set; }
	}

	public class ParseLanguage
	{
		[JsonProperty("isoCode")]
		public string IsoCode { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("ethnologueCode")]
		public string EthnologueCode { get; set; }
	}
}
