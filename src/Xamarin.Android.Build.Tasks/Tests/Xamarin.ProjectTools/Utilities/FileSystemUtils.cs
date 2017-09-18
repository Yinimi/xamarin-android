using System;
using System.IO;

namespace Xamarin.ProjectTools
{
	public static class FileSystemUtils
	{
		public static void SetDirectoryWriteable (string directory)
		{
			if (!Directory.Exists (directory))
				return;

			var dirInfo = new DirectoryInfo (directory);
			if (dirInfo.Attributes.HasFlag (FileAttributes.ReadOnly)) {
				dirInfo.Attributes &= ~FileAttributes.ReadOnly;
				dirInfo.Refresh ();
			}

			foreach (var dir in Directory.GetDirectories (directory, "*", SearchOption.AllDirectories)) {
				dirInfo = new DirectoryInfo (dir);
				if (dirInfo.Attributes.HasFlag (FileAttributes.ReadOnly)) {
					dirInfo.Attributes &= ~FileAttributes.ReadOnly;
					dirInfo.Refresh ();
				}
			}

			foreach (var file in Directory.GetFiles (directory, "*", SearchOption.AllDirectories)) {
				SetFileWriteable (Path.GetFullPath (file));
			}
		}

		public static void SetFileWriteable (string source)
		{
			if (!File.Exists (source))
				return;

			var fileInfo = new FileInfo (source);
			if (fileInfo.IsReadOnly) {
				fileInfo.IsReadOnly = false;
				fileInfo.Refresh ();
			}
		}
	}
}

