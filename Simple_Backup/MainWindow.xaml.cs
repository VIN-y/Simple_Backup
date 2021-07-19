﻿using Microsoft.Win32;
using Simple_Backup_Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Simple_Backup
{
    /*
    * Build command for the dev terminal:
    * 
    *   dotnet publish Simple_Backup -c Release -r win-x64 --self-contained=true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
    *   
    */
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> cbItems { get; set; } // Binded for Destination ComboBox

        private Queue queue = new(); // Source Path Queue

        private string drivepath;
        private string backupfolder;
        private string backupdir;
        private string currentpath = Environment.CurrentDirectory;

        CancellationTokenSource cts = new();

        public MainWindow()
        {
            InitializeComponent();
            SetupDestinationOption();

            // Display the list
            PathList.ItemsSource = queue.Sources;

            // Block the Backup funtion by default
            cts.Cancel();
        }

        /*
         * Working methods
         */
        private void SetupDefaultSourceFolders()
        {
            /* Default Environment folder paths */
            string DesktopSource = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string DocumentsSource = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string MusicSource = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string OneDriveSource = Environment.GetEnvironmentVariable("OneDrive");
            string PicturesSource = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string VideosSource = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

            /* Adding the default folders to the list */
            queue.Sources.Add(new Source { Name = "Desktop", Path = DesktopSource });
            queue.Sources.Add(new Source { Name = "Documents", Path = DocumentsSource });
            queue.Sources.Add(new Source { Name = "Music", Path = MusicSource });
            queue.Sources.Add(new Source { Name = "OneDrive", Path = OneDriveSource });
            queue.Sources.Add(new Source { Name = "Pictures", Path = PicturesSource });
            queue.Sources.Add(new Source { Name = "Videos", Path = VideosSource });
        }

        private void SetupDestinationOption()
        {
            DataContext = this;
            cbItems = new ObservableCollection<string>();   // List of avaliable backup drive
            DriveInfo[] allDrives = DriveInfo.GetDrives();  // Get Destination Paths

            foreach (DriveInfo d in allDrives)
            {
                if (!(d.VolumeLabel == ""))
                {
                    Destination ite = new Destination { Name = d.Name, Path = d.VolumeLabel };
                    cbItems.Add(item: ite.Display);
                }
            }
        }

        public async Task CopyF(string sourceDirName, string destDirName, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            FileAttributes attr = File.GetAttributes(sourceDirName);
            List<Task> todo = new();
            string Sourpath;
            string Destpath;
            string Excep = ".849C9593-D756-4E56-8D6E-42412F2A707B";

            // Detect whether its a directory or file
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                // Folder (directory) backup (do not overwrite what is already there)
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(sourceDirName);
                    DirectoryInfo dirD = new DirectoryInfo(destDirName);
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
                            Sourpath = Path.Combine(sourceDirName, subdir.Name);
                            Destpath = Path.Combine(destDirName, subdir.Name);

                            if (!File.Exists(@Destpath))
                            {
                                await CopyF(subdir.FullName, Destpath, cancellation);
                                StatusReport.Text = await Task.Run(() => "Backing up: " + Sourpath);
                            }
                        }

                        FileInfo[] files = dir.GetFiles();
                        FileInfo[] fileDs = dirD.GetFiles();

                        foreach (FileInfo file in files)
                        {
                            if (!(file.Name == Excep))
                            {
                                Sourpath = Path.Combine(sourceDirName, file.Name);
                                Destpath = Path.Combine(destDirName, file.Name);

                                // Copy new files
                                if (!File.Exists(@Destpath))
                                {
                                    await Task.Run(() => File.Copy(Sourpath, Destpath, false), cancellation);
                                }
                                // overwrite modified files
                                else
                                {
                                    foreach (FileInfo fileD in fileDs)
                                    {
                                        if (fileD.Name == file.Name)
                                        {
                                            if (file.LastWriteTime > fileD.LastWriteTime)
                                            {
                                                await Task.Run(() => File.Copy(Sourpath, Destpath, true), cancellation);
                                            }
                                        }
                                    }
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
                // Specific file backup (setting to overwrite the old backup, until the next hour hit. Change to "false" in the "File.Copy..." to change this.)
                Destpath = Path.GetDirectoryName(destDirName);
                
                if (!Directory.Exists(Destpath))
                {
                    _ = Directory.CreateDirectory(Destpath);
                }

                try
                {
                    await Task.Run(() => File.Copy(sourceDirName, destDirName, true), cancellation);
                    StatusReport.Text = await Task.Run(() => "Backing up: " + sourceDirName);
                }
                catch (Exception) { }
            }
        }

        private void SaveLog()
        {
            StringBuilder sb = new StringBuilder();
            string logpath = Path.Combine(currentpath, "Simple_Backup_Log");

            if (File.Exists(logpath))
            {
                File.Delete(logpath);
            }

            foreach (Source s in PathList.ItemsSource)
            {
                _ = sb.Append("Source:*" + s.Name + "*Path:*" + s.Path + "*Queue_Status:*" + s.Queue + "\n");
            }

            _ = sb.Append("Destination: " + BackupDrive.Text + " Mode: " + BackupMode.Text + "\n");


            try
            {
                File.AppendAllText(logpath, sb.ToString());
                _ = sb.Clear();
            }
            catch (Exception Ex)
            {
                _ = MessageBox.Show(Ex.ToString());
            }
        }

        private void Load()
        {
            string logpath = Path.Combine(currentpath, "Simple_Backup_Log");
            string sourcename = " ";
            string sourcepath = " ";
            string[] lineread;
            bool sourcequeue = false;

            if (File.Exists(logpath))
            {
                lineread = File.ReadAllLines(logpath);

                foreach (string line in lineread)
                {
                    // Define source, from log.txt
                    if (line.Contains("Source:"))
                    {
                        foreach (string w in line.Split('*'))
                        {
                            if (!w.Contains(":") && !(w == "True") && !(w == "False"))
                            {
                                sourcename = w;
                            }
                            if (w.Contains(":") && !w.Contains("Source") && !w.Contains("Queue_Status") && !w.Contains("Path"))
                            {
                                sourcepath = w;
                            }
                            if (w.Contains("True"))
                            {
                                sourcequeue = true;
                            }
                            if (w.Contains("False"))
                            {
                                sourcequeue = false;
                            }
                        }
                        // Add the sources to the UI list
                        queue.Sources.Add(new Source { Name = sourcename, Path = sourcepath, Queue = sourcequeue });
                    }

                    // Define destination, from log.txt
                    if (line.Contains("Destination:"))
                    {
                        foreach (string s in cbItems)
                        {
                            if (line.Contains(s))
                            {
                                BackupDrive.SelectedItem = s;
                            }
                        }

                        if (line.Contains("Day"))
                        {
                            BackupMode.Text = "Day";
                        }
                        if (line.Contains("Month"))
                        {
                            BackupMode.Text = "Month";
                        }
                        if (line.Contains("Hour"))
                        {
                            BackupMode.Text = "Hour";
                        }
                    }
                }
            }
            else
            {
                SetupDefaultSourceFolders();
            }

            PathList.Items.Refresh();
        }

        private async Task Backup(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            if (BackupDrive.Text is not ("" or " "))
            {
                if (BackupMode.Text is not ("" or " "))
                {
                    foreach (Source s in queue.Sources)
                    {
                        if (s.Queue)
                        {
                            string DestinationFolder = Path.Combine(backupdir, s.Name);
                            try
                            {
                                await CopyF(s.Path, DestinationFolder, cts.Token);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }
                        }
                    }
                }
                else
                {
                    StatusReport.Text = "Please select a Backup Mode...";
                }
            }
            else
            {
                StatusReport.Text = "Please select a Backup Drive...";
            }
        }

        /*
         * UI events
         */
        private async void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset Cancelation token
            cts.Dispose();
            cts = new CancellationTokenSource();

            // Backup according to the settings
            if (BackupDrive.Text is not ("" or " "))
            {
                if (BackupMode.Text is not ("" or " "))
                {
                    BackupButton.Content = await Task.Run(() => "Running Backup");

                    try
                    {
                        await Backup(cts.Token);
                        await Task.Delay(1500);
                        StatusReport.Text = await Task.Run(() => " ");
                    }
                    catch (Exception) { }

                    BackupButton.Content = await Task.Run(() => "Start Backup");
                }
                else
                {
                    StatusReport.Text = "Please select a Backup Mode...";
                }
            }
            else
            {
                StatusReport.Text = "Please select a Backup Drive...";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(cts.IsCancellationRequested);
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();   // Throw Cancelation token
                StatusReport.Text = "Cancelling backup ...";
            }
        }

        private void BackupMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string thismonth = DateTime.Now.ToString("yy.MM") + ".__" + ".__";
                string today = DateTime.Now.ToString("yy.MM.dd") + ".__";
                string thishour = DateTime.Now.ToString("yy.MM.dd.HH");

                string SelectedMode = (e.AddedItems[0] as ComboBoxItem).Content as string;

                // "Day" mode
                if (SelectedMode == "Day")
                {
                    backupfolder = "Backups_" + today;
                    backupdir = Path.Combine(drivepath, "Simple Backup", backupfolder);
                }
                // "Month" mode
                if (SelectedMode == "Month")
                {
                    backupfolder = "Backups_" + thismonth;
                    backupdir = Path.Combine(drivepath, "Simple Backup", backupfolder);
                }
                // "Hour" mode
                if (SelectedMode == "Hour")
                {
                    backupfolder = "Backups_" + thishour;
                    backupdir = Path.Combine(drivepath, "Simple Backup", backupfolder);
                }

                BD_detail.Text = "Backup folder: " + backupdir;

                if (BackupDrive.Text is "" or " ")
                {
                    StatusReport.Text = "Please select a Backup Drive...";
                }
            }
            catch (Exception) { }
        }

        private void BackupDrive_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                string SelectedDrive = (string)e.AddedItems[0];

                foreach (DriveInfo d in allDrives)
                {
                    Destination ite = new Destination { Name = d.Name, Path = d.VolumeLabel };
                    if (ite.Display == SelectedDrive)
                    {
                        drivepath = d.Name;
                        backupdir = Path.Combine(drivepath, "Simple Backup", backupfolder);
                        BD_detail.Text = "Backup folder: " + backupdir;
                    }
                }

                if (BackupMode.Text is "" or " ")
                {
                    StatusReport.Text = "Please select a Backup Mode...";
                }
            }
            catch (Exception) { }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // F5 key pressed
            if (e.Key == Key.F5)
            {
                // Refresh backup drive list
                cbItems.Clear();
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    if (!(d.VolumeLabel == ""))
                    {
                        Destination item = new Destination { Name = d.Name, Path = d.VolumeLabel };
                        cbItems.Add(item: item.Display);
                    }
                }

                // Resetting Queue
                foreach (Source s in queue.Sources)
                {
                    s.Queue = false;
                }

                // Resetting Backup Mode selection
                BackupMode.Text = null;

                // Reset the backup folder indicator
                BD_detail.Text = null;
            }
        }

        private void PathList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Source c = (Source)PathList.SelectedItem;

            if (c.Queue)
            {
                c.Queue = false;
                PathList.Items.Refresh();
            }
            else
            {
                c.Queue = true;
                PathList.Items.Refresh();
            }

            // save the new settings
            SaveLog();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset settings to default
            string logpath = Path.Combine(currentpath, "log.txt");
            if (File.Exists(logpath))
            {
                File.Delete(logpath);
            }

            // Refresh backup drive list
            cbItems.Clear();
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (!(d.VolumeLabel == ""))
                {
                    Destination item = new Destination { Name = d.Name, Path = d.VolumeLabel };
                    cbItems.Add(item: item.Display);
                }
            }

            // Reset source list to default
            queue.Sources.Clear();
            SetupDefaultSourceFolders();
            PathList.Items.Refresh();

            // Resetting Backup Mode selection
            BackupMode.Text = null;

            // Reset the backup folder indicator
            BD_detail.Text = null;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string pat;
            string nam;

            //// Create OpenFileDialog
            OpenFileDialog folderBrowser = new();

            // Set validate names and check file exists to false otherwise windows will not let you select "Folder Selection."
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;

            // Set default selection to "(Backup this folder)"
            folderBrowser.FileName = "[Backup this folder]";

            Nullable<bool> result = folderBrowser.ShowDialog();
            if (result == true)
            {
                // If no file was selected, add the entire folder to source list
                if (folderBrowser.FileName.Contains("[Backup this folder]"))
                {
                    pat = Path.GetDirectoryName(folderBrowser.FileName);
                }
                // Otherwise, add the selected file
                else
                {
                    pat = folderBrowser.FileName;
                }

                // Identify the file name
                nam = Path.GetFileName(pat);

                // Add to source list
                queue.Sources.Add(new Source { Name = nam, Path = pat });
                PathList.Items.Refresh();
            }

            // save the new settings
            SaveLog();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Source a = (Source)PathList.SelectedItem;
            queue.Sources.Remove(a);
            PathList.Items.Refresh();
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveLog();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (SingleInstance.AlreadyRunning())
            {
                Application.Current.Shutdown(); // Just shutdown the current application, if any instance found.
            }
            else
            {
                Load();
            }
        }
    }
}
