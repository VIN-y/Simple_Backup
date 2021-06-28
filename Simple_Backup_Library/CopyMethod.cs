using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Simple_Backup_Library
{
    public class CopyMethod
    {

        public static async Task CopyF(string sourceDirName, string destDirName, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            FileAttributes attr = File.GetAttributes(sourceDirName);

            List<Task> todo = new();
            string des;
            string Excep = ".849C9593-D756-4E56-8D6E-42412F2A707B";

            // Detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // Folder (directory) backup (do not overwrite what is already there)
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(sourceDirName);
                    DirectoryInfo[] dirs = dir.GetDirectories();

                    // If the source directory does not exist, throw an exception.
                    if (!dir.Exists)
                    {
                        throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
                    }
                    else
                    {
                        // Create folders and subfolders
                        if (!Directory.Exists(destDirName))
                        {
                            _ = Directory.CreateDirectory(destDirName);
                        }

                        foreach (DirectoryInfo subdir in dirs)
                        {
                            string Dirpath = Path.Combine(destDirName, subdir.Name);

                            if (!File.Exists(@Dirpath))
                            {
                                await CopyF(subdir.FullName, Dirpath, cancellation);
                            }
                        }

                        // Get the file contents of the directory to copy.
                        FileInfo[] files = dir.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            if (!(file.Name == Excep))
                            {
                                string Despath = Path.Combine(destDirName, file.Name);

                                if (!File.Exists(@Despath))
                                {
                                    todo.Add(Task.Run(() => file.CopyTo(Despath, false)));
                                }
                            }
                        }

                        // Run the copying task
                        await Task.WhenAll(todo);
                    }
                }
                catch (Exception) { }
            }

            else
            {
                // Specific file backup (setting to overwrite the old backup, until the next hour hit)
                des = Path.GetDirectoryName(destDirName);
                if (!Directory.Exists(des))
                {
                    _ = Directory.CreateDirectory(des);
                }

                try
                {
                    await Task.Run(() => File.Copy(sourceDirName, destDirName, true));
                }
                catch (Exception) { }
            }
        }
    }
}
