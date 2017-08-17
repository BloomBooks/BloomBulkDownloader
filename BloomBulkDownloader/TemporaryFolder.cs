using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SIL.IO;

namespace BloomTemp
{
	// ENHANCE: Replace with TemporaryFolder implemented in Palaso. However, that means
	// refactoring some Palaso code and moving TemporaryFolder from Palaso.TestUtilities into
	// Palaso.IO
	public class TemporaryFolder : IDisposable
	{
		private string _path;

		public TemporaryFolder(string name)
		{
			_path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);
			if (Directory.Exists(_path))
			{
			   DeleteFolderThatMayBeInUse(_path);
			}
			Directory.CreateDirectory(_path);
		}

		public TemporaryFolder(TemporaryFolder parent, string name)
		{
			_path = parent.Combine(name);
			if (Directory.Exists(_path))
			{
			   DeleteFolderThatMayBeInUse(_path);
			}
			Directory.CreateDirectory(_path);
		}

		public string FolderPath
		{
			get { return _path; }
		}

		public void Dispose()
		{
			DeleteFolderThatMayBeInUse(_path);

			GC.SuppressFinalize(this);
		}

		public string GetPathForNewTempFile(bool doCreateTheFile)
		{
			string s = System.IO.Path.GetRandomFileName();
			s = System.IO.Path.Combine(_path, s);
			if (doCreateTheFile)
			{
				RobustFile.Create(s).Close();
			}
			return s;
		}

		public string GetPathForNewTempFile(bool doCreateTheFile, string extension)
		{
			extension = extension.TrimStart('.');
			var s = System.IO.Path.Combine(_path, System.IO.Path.GetRandomFileName() + "." + extension);

			if (doCreateTheFile)
			{
				RobustFile.Create(s).Close();
			}
			return s;
		}

		public TempFile GetNewTempFile(bool doCreateTheFile)
		{
			string s = System.IO.Path.GetRandomFileName();
			s = System.IO.Path.Combine(_path, s);
			if (doCreateTheFile)
			{
				RobustFile.Create(s).Close();
			}
			return TempFile.TrackExisting(s);
		}

		public string Combine(params string[] partsOfThePath)
		{
			string result = _path;
			foreach (var s in partsOfThePath)
			{
				result = System.IO.Path.Combine(result, s);
			}
			return result;
		}

		internal static void DeleteFolderThatMayBeInUse(string folder)
		{
			if (Directory.Exists(folder))
			{
				try
				{
					RobustIO.DeleteDirectory(folder, true);
				}
				catch (Exception e)
				{
					try
					{
						Debug.WriteLine(e.Message);
						//maybe we can at least clear it out a bit
						string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
						foreach (string s in files)
						{
							try
							{
								RobustFile.Delete(s);
							}
							catch (Exception)
							{
							}
						}
						//sleep and try again (in case some other thread will let go of them)
						Thread.Sleep(1000);
						RobustIO.DeleteDirectory(folder, true);
					}
					catch (Exception)
					{
					}
				}
			}
		}
	}
}
