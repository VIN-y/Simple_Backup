using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple_Backup_Library
{
    public sealed class SingleInstance
    {
        public static bool AlreadyRunning()
        {
            bool running = false;
            try
            {
                // Getting collection of process  
                Process currentProcess = Process.GetCurrentProcess();

                // Check with other process already running   
                foreach (var p in Process.GetProcesses())
                {
                    if (p.Id != currentProcess.Id) // Check running process   
                    {
                        if (p.ProcessName.Equals(currentProcess.ProcessName) == true)
                        {
                            running = true;
                            IntPtr hFound = p.MainWindowHandle;
                            if (User32API.IsIconic(hFound)) // If application is in ICONIC mode then
                            {
                                _ = User32API.ShowWindow(hFound, User32API.SW_RESTORE);
                            }

                            _ = User32API.SetForegroundWindow(hFound); // Activate the window, if process is already running  
                            break;
                        }
                    }
                }
            }
            catch { }
            return running;
        }
    }
    }
