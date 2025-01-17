﻿using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

using Ninjacrab.PersistentWindows.Common;
using Ninjacrab.PersistentWindows.Common.Diagnostics;

namespace Ninjacrab.PersistentWindows.SystrayShell
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static readonly string ProjectUrl = "https://www.github.com/kangyu-california/PersistentWindows";
        public static System.Drawing.Icon IdleIcon = null;
        public static System.Drawing.Icon BusyIcon = null;

        static PersistentWindowProcessor pwp = null;    
        static SystrayForm systrayForm = null;
        static bool silent = false; //suppress all balloon tip & sound prompt
        static bool notification = false; //pop balloon when auto restore

        [STAThread]
        static void Main(string[] args)
        {
            bool splash = true;
            bool dry_run = false; //dry run mode without real restore, for debug purpose only
            bool fix_zorder = false;
            bool fix_zorder_specified = false;
            bool delay_start = false;
            bool redraw_desktop = false;
            bool redirect_appdata = false; // use "." instead of appdata/local/PersistentWindows to store db file
            bool offscreen_fix = true;
            bool enhanced_offscreen_fix = false;
            bool prompt_session_restore = false;
            bool check_upgrade = true;

            foreach (var arg in args)
            {
                if (delay_start)
                {
                    delay_start = false;
                    int seconds = Int32.Parse(arg);
                    Thread.Sleep(1000 * seconds);
                    continue;
                }

                switch(arg)
                {
                    case "-silent":
                        silent = true;
                        splash = false;
                        break;
                    case "-splash_off":
                    case "-splash=0":
                        splash = false;
                        break;
                    case "-notification_on":
                    case "-notification=1":
                        notification = true;
                        break;
                    case "-dry_run":
                        dry_run = true;
                        break;
                    case "-fix_zorder=0":
                        fix_zorder = false;
                        fix_zorder_specified = true;
                        break;
                    case "-fix_zorder":
                    case "-fix_zorder=1":
                        fix_zorder = true;
                        fix_zorder_specified = true;
                        break;
                    case "-delay_start":
                        delay_start = true;
                        break;
                    case "-redraw_desktop":
                        redraw_desktop = true;
                        break;
                    case "-redirect_appdata":
                        redirect_appdata = true;
                        break;
                    case "-enhanced_offscreen_fix":
                        enhanced_offscreen_fix = true;
                        break;
                    case "-disable_offscreen_fix":
                        offscreen_fix = false;
                        break;
                    case "-offscreen_fix=0":
                        offscreen_fix = false;
                        break;
                    case "-prompt_session_restore":
                        prompt_session_restore = true;
                        break;
                    case "-check_upgrade=0":
                        check_upgrade = false;
                        break;
                }
            }

            while (String.IsNullOrEmpty(PersistentWindowProcessor.GetDisplayKey()))
            {
                Thread.Sleep(5000);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string productName = System.Windows.Forms.Application.ProductName;
            string appDataFolder = redirect_appdata ? "." :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    productName);
#if DEBUG
            //avoid db path conflict with release version
            //appDataFolder = ".";
            appDataFolder = AppDomain.CurrentDomain.BaseDirectory;
#endif
            string icon_path = Path.Combine(appDataFolder, "pwIcon.ico");
            if (File.Exists(icon_path))
            {
                IdleIcon = new System.Drawing.Icon(icon_path);
            }
            else
            {
                IdleIcon = Properties.Resources.pwIcon;
            }

            icon_path = Path.Combine(appDataFolder, "pwIconBusy.ico");
            if (File.Exists(icon_path))
            {
                BusyIcon = new System.Drawing.Icon(icon_path);
            }
            else
            {
                BusyIcon = Properties.Resources.pwIconBusy;
            }

            systrayForm = new SystrayForm();
            systrayForm.enableUpgradeNotice = check_upgrade;
            if (check_upgrade)
                systrayForm.upgradeNoticeMenuItem.Text = "Disable upgrade notice";
            else
                systrayForm.upgradeNoticeMenuItem.Text = "Enable upgrade notice";

            pwp = new PersistentWindowProcessor();
            pwp.dryRun = dry_run;
            if (fix_zorder_specified)
            {
                if (fix_zorder)
                    pwp.fixZorder = 2; //force z-order recovery for all
                else
                    pwp.fixZorder = 0; //no z-order recovery at all
            }
            else
            {
                // pwp.fixZorder = 1 //do z-order recovery only for snapshot 
            }

            pwp.showRestoreTip = ShowRestoreTip;
            pwp.hideRestoreTip = HideRestoreTip;
            pwp.enableRestoreMenu = EnableRestoreMenu;
            pwp.redrawDesktop = redraw_desktop;
            pwp.redirectAppDataFolder = redirect_appdata;
            pwp.enhancedOffScreenFix = enhanced_offscreen_fix;
            pwp.enableOffScreenFix = offscreen_fix;
            pwp.promptSessionRestore = prompt_session_restore;

            if (!pwp.Start())
            {
                systrayForm.notifyIconMain.Visible = false;
                return;
            }

            if (splash)
            {
                StartSplashForm();
            }

            Application.Run();
        }

        static void ShowRestoreTip()
        {
            var thread = new Thread(() =>
            {
                systrayForm.notifyIconMain.Icon = BusyIcon;

                if (silent)
                    return;

                systrayForm.notifyIconMain.Visible = false;
                systrayForm.notifyIconMain.Visible = true;

                if (!notification)
                    return;

                systrayForm.notifyIconMain.ShowBalloonTip(10000);
            });

            thread.IsBackground = false;
            thread.Start();
        }

        static void HideRestoreTip()
        {
            systrayForm.notifyIconMain.Icon = IdleIcon;
            if (silent || !notification)
                return;
            systrayForm.notifyIconMain.Visible = false;
            systrayForm.notifyIconMain.Visible = true;
        }

        static void EnableRestoreMenu(bool enable)
        {
#if DEBUG
            LogEvent("start ui refresh timer");
#endif
            systrayForm.enableRestoreFromDB = enable;
            systrayForm.enableRefresh = true;
        }

        static public void TakeSnapshot(int id)
        {
            pwp.TakeSnapshot(id);
            if (!silent)
            {
                if (id == 0)
                    systrayForm.notifyIconMain.ShowBalloonTip(5000, $"snapshot {id} is captured", "click icon to restore the snapshot", ToolTipIcon.Info);
                else if (id == 1)
                    systrayForm.notifyIconMain.ShowBalloonTip(5000, $"snapshot {id} is captured", $"ctrl click icon to restore the snapshot", ToolTipIcon.Info);
                else if (id == 2)
                    systrayForm.notifyIconMain.ShowBalloonTip(5000, $"snapshot {id} is captured", $"ctrl click icon twice to restore the snapshot", ToolTipIcon.Info);
                else if (id == 3)
                    systrayForm.notifyIconMain.ShowBalloonTip(5000, $"snapshot {id} is captured", $"ctrl click icon {id} times to restore the snapshot", ToolTipIcon.Info);
            }
        }

        static void StartSplashForm()
        {
            var thread = new Thread(() =>
            {
                Application.Run(new SplashForm());
            });
            thread.IsBackground = false;
            thread.Priority = ThreadPriority.Highest;
            thread.Name = "StartSplashForm";
            thread.Start();
        }

        static public void ManageLayoutProfile()
        {
            var profileDlg = new LayoutProfile();
            if (profileDlg.ShowDialog(systrayForm) == DialogResult.OK)
            {

            }
        }

        static public void CaptureToDisk()
        {
            GetProcessInfo();
            pwp.BatchCaptureApplicationsOnCurrentDisplays(saveToDB : true);
        }

        static public void RestoreFromDisk()
        {
            pwp.restoringFromDB = true;
            pwp.StartRestoreTimer(milliSecond : 2000 /*wait mouse settle still for taskbar restore*/);
        }

        static public void RestoreSnapshot(int id)
        {
            pwp.RestoreSnapshot(id);
        }

        static public void PauseAutoRestore()
        {
            pwp.pauseAutoRestore = true;
            pwp.sessionActive = false; //disable capture as well
        }

        static public void ResumeAutoRestore()
        {
            pwp.pauseAutoRestore = false;
            pwp.restoringFromMem = true;
            pwp.StartRestoreTimer();
        }
        static void GetProcessInfo()
        {
            Process process = new Process();
            process.StartInfo.FileName = "wmic.exe";
            //process.StartInfo.Arguments = "process get caption,commandline,processid /format:csv";
            process.StartInfo.Arguments = "process get commandline,processid /format:csv";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = false;

            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);

            // Start process and handlers
            process.Start();
            process.BeginOutputReadLine();
            //process.BeginErrorReadLine();
            process.WaitForExit();
        }

        static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)
            //Console.WriteLine(outLine.Data);
            string line = outLine.Data;
            if (string.IsNullOrEmpty(line))
                return;
            //Log.Info("{0}", line);
            string[] fields = line.Split(',');
            if (fields.Length < 3)
                return;
            uint processId;
            if (uint.TryParse(fields[2], out processId))
            {
                if (!string.IsNullOrEmpty(fields[1]))
                {
                    pwp.processCmd[processId] = fields[1];
                }
            }
        }
        public static void LogEvent(string format, params object[] args)
        {
            Log.Event(format, args);
        }
    }
}
