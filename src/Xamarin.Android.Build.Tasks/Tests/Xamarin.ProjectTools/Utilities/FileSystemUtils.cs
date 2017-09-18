using System.IO;

namespace Xamarin.ProjectTools
{
	public static class FileSystemUtils
	{
		/// <summary>
		/// Recursively deletes a directory, even if a file is readonly
		/// </summary>
		public static void DeleteReadonly(string path)
		{
			DeleteReadonly (new DirectoryInfo (path));
		}

		static void DeleteReadonly (this FileSystemInfo fileInfo)
		{
			if (fileInfo is DirectoryInfo directoryInfo) {
				foreach (var child in directoryInfo.GetFileSystemInfos ()) {
					DeleteReadonly (child);
				}
			}

			fileInfo.Attributes = FileAttributes.Normal;
			fileInfo.Delete ();
		}
	}
}

