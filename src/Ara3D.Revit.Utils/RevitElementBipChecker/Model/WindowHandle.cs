using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RevitElementBipChecker.Model
{
    public static class WindowHandle
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

		/// <summary>
		/// Sets the given window's owner to Revit window.
		/// </summary>
		/// <param name="dialog">Target window.</param>
		public static void SetRevitAsWindowOwner(this Window dialog)
		{
			if (null == dialog) { return; }

			var helper = new WindowInteropHelper(dialog);
			helper.Owner = FindRevitWindowHandle();
		}

		/// <summary>
		/// Finds the Revit window handle.
		/// </summary>
		/// <returns>Revit window handle.</returns>
		private static IntPtr FindRevitWindowHandle()
		{
			try
			{
				var foundRevitHandle = IntPtr.Zero;
				var currentThreadID = GetCurrentThreadId();

				// Search for the Revit process with current thread ID.
				var revitProcesses = Process.GetProcessesByName("Revit");
				Process foundRevitProcess = null;
				foreach (var aRevitProcess in revitProcesses)
				{
					foreach (ProcessThread aThread in aRevitProcess.Threads)
					{
						if (aThread.Id == currentThreadID)
						{
							foundRevitProcess = aRevitProcess;
							break;
						}
					}  // For each thread in the process.

					// When we have found our Revit process, then stop searching.
					if (null != foundRevitProcess) { break; }
				}  // For each revit process found

				// Get the window handle of the Revit process found.
				if (null != foundRevitProcess)
				{
					foundRevitHandle = foundRevitProcess.MainWindowHandle;
				}

				return foundRevitHandle;
			}
			catch (Exception)
			{
				return IntPtr.Zero;
			}
		}
	}
}
