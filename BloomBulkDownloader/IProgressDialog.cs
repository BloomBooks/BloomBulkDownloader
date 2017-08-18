using System;

namespace Bloom.WebLibraryIntegration
{
	/// <summary>
	/// Interface that we wish ProgressDialog implemented
	/// </summary>
	public interface IProgressDialog
	{
		int ProgressRangeMaximum { get; set; }
		int Progress { get; set; }
		// Note: the ConsoleProgress implementation just runs the delegate on the current thread.
		object Invoke(Delegate method);
	}

	// We're running from a console, so we pass this.
	class ConsoleProgress : IProgressDialog, IDisposable
	{
		private int _progress;
		public int ProgressRangeMaximum { get; set; }

		public int Progress
		{
			get { return _progress; }
			set
			{
				if (value > _progress)
				{
					Console.Write(".");
				}
				_progress = value;
			}
		}

		public object Invoke(Delegate method)
		{
			return method.Method.Invoke(method.Target, new object[0]);
		}

		public void Dispose()
		{
		}
	}
}
