﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

	public static class FileSafety {
		public const int MAX_PATH_LENGTH = 248;

		public const string CLEANUP_DIRECTORY = @"_cleanup";

		public static string FilenameStrip (string entry) {
			foreach (char c in Path.GetInvalidFileNameChars())
				entry = entry.Replace (c.ToString (), string.Empty);
			return entry;
		}

		public static string PathStrip (string entry) {
			foreach (char c in Path.GetInvalidFileNameChars())
				entry = entry.Replace (c.ToString (), string.Empty);
			entry = entry.Replace (".", string.Empty);
			return entry;
		}

		public static void RemoveReadOnlyRecursive (string s) {
			foreach (string f in Directory.GetFiles(s)) {
				FileInfo myFile = new FileInfo (f);
				if ((myFile.Attributes & FileAttributes.ReadOnly) > 0)
					myFile.Attributes &= ~FileAttributes.ReadOnly;
			}

			foreach (string d in Directory.GetDirectories(s))
				RemoveReadOnlyRecursive (d);
		}

		public static bool FileMove (string src, string dest, bool overwrite = true) {
			src = PathSanitise (src);
			dest = PathSanitise (dest);
			if (src == dest)
				return true; //no move necessary

			try {
				if (overwrite)
					FileDelete (dest);
				System.IO.File.Move (src, dest);
			}
			catch {
				try {
					System.IO.File.Copy (src, dest);
					return FileDelete (src);
				}
				catch {
					return false;
				}
			}
			return true;
		}

        public static bool FileCopy (string src, string dest, bool overwrite = true)
        {
            src = PathSanitise(src);
            dest = PathSanitise(dest);

            if (src == dest)
                return true; //no copy necessary

            try {
                if (overwrite)
                    FileDelete(dest);
                System.IO.File.Copy(src, dest, true);
            }
            catch {
                try {
                    System.IO.File.Copy(src, dest);
                    return true;
                }
                catch {
                    return false;
                }
            }
            return true;
        }

		/// <summary>
		/// Converts all slashes and backslashes to OS-specific directory separator characters. Useful for sanitising user input.
		/// </summary>
		public static string PathSanitise (string path) {
			return path.Replace ('\\', Path.DirectorySeparatorChar).Replace ('/', Path.DirectorySeparatorChar).TrimEnd (Path.DirectorySeparatorChar);
		}

		/// <summary>
		/// Converts all OS-specific directory separator characters to '/'. Useful for outputting to a config file or similar.
		/// </summary>
		public static string PathStandardise (string path) {
			return path.Replace (Path.DirectorySeparatorChar, '/');
		}

		[Flags]
		internal enum MoveFileFlags
		{
			None = 0,
			ReplaceExisting = 1,
			CopyAllowed = 2,
			DelayUntilReboot = 4,
			WriteThrough = 8,
			CreateHardlink = 16,
			FailIfNotTrackable = 32,
		}

		internal static class NativeMethods {
			[DllImport ("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
			public static extern bool MoveFileEx (
				string lpExistingFileName,
				string lpNewFileName,
				MoveFileFlags dwFlags);
		}

		public static bool FileDeleteOnReboot (string filename) {
			filename = PathSanitise (filename);

			try {
				System.IO.File.Delete (filename);
				return true;
			}
			catch {
			}

			string deathLocation = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
			try {
				System.IO.File.Move (filename, deathLocation);
			}
			catch {
				deathLocation = filename;
			}

			return NativeMethods.MoveFileEx (deathLocation, null, MoveFileFlags.DelayUntilReboot);
		}

		public static bool FileDelete (string filename) {
			filename = PathSanitise (filename);

			try {
				if (!System.IO.File.Exists (filename))
					return true;

				System.IO.File.Delete (filename);
				return true;
			}
			catch {
			}

			try {
				//try alternative method: move to a cleanup folder and delete later.
				if (!Directory.Exists (@"_cleanup")) {
					DirectoryInfo di = Directory.CreateDirectory (@"_cleanup");
					di.Attributes |= FileAttributes.Hidden;
				}

				System.IO.File.Move (filename, CLEANUP_DIRECTORY + @"/" + Guid.NewGuid ());
				return true;
			}
			catch {
			}

			return false;
		}

		public static string AsciiOnly (string input) {
			if (input == null)
				return null;

			StringBuilder asc = new StringBuilder (input.Length);
			//keep only ascii chars
			foreach (char c in input)
				if (c <= 126)
					asc.Append (c);
			return asc.ToString ().Trim ();
		}

		public static void RecursiveMove (string oldDirectory, string newDirectory) {
			oldDirectory = PathSanitise (oldDirectory);
			newDirectory = PathSanitise (newDirectory);

			if (oldDirectory == newDirectory)
				return;

			foreach (string dir in Directory.GetDirectories(oldDirectory)) {
				string newSubDirectory = dir;
				newSubDirectory = Path.Combine (newDirectory, newSubDirectory.Remove (0, 1 + newSubDirectory.LastIndexOf (Path.DirectorySeparatorChar)));

				try {
					DirectoryInfo newDirectoryInfo = Directory.CreateDirectory (newSubDirectory);

					if ((new DirectoryInfo (dir).Attributes & FileAttributes.Hidden) > 0)
						newDirectoryInfo.Attributes |= FileAttributes.Hidden;
				}
				catch {
				}

				RecursiveMove (dir, newSubDirectory);
			}

			bool didExist = Directory.Exists (newDirectory);
			if (!didExist) {
				DirectoryInfo newDirectoryInfo = Directory.CreateDirectory (newDirectory);
				try {
					if ((new DirectoryInfo (oldDirectory).Attributes & FileAttributes.Hidden) > 0)
						newDirectoryInfo.Attributes |= FileAttributes.Hidden;
				}
				catch {
				}
			}

			foreach (string file in Directory.GetFiles(oldDirectory)) {
				string newFile = Path.Combine (newDirectory, Path.GetFileName (file));

				bool didMove = FileMove (file, newFile, didExist);
				if (!didMove) {
					try {
						System.IO.File.Copy (file, newFile);
					}
					catch {
					}
					System.IO.File.Delete (file);
				}
			}

			Directory.Delete (oldDirectory, true);
		}

		public static string GetExtension (string filename) { 
			return Path.GetExtension (filename).Trim ('.').ToLower ();
		}

		public static int GetMaxPathLength (string directory) {
			int highestPathLength = directory.Length;

			foreach (string file in Directory.GetFiles(directory)) {
				if (file.Length > highestPathLength)
					highestPathLength = file.Length;
			}

			foreach (string dir in Directory.GetDirectories(directory)) {
				int tempPathLength = GetMaxPathLength (dir);
				if (tempPathLength > highestPathLength)
					highestPathLength = tempPathLength;
			}

			return highestPathLength;
		}

		/// <summary>
		/// Returns the path without the extension of the file.
		/// Contrarily to Path.GetFileNameWithoutExtension, it keeps the path to the file ("path/triangle.png" becomes "path/triangle" and not "triangle")
		/// </summary>
		public static string StripExtension (string filepath) {
			int dotIndex = filepath.LastIndexOf ('.');
			return dotIndex == -1 ? filepath : filepath.Substring (0, dotIndex);
		}
	}
