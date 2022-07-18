#region License
/*
MIT License

Copyright(c) 2021 Petteri Kautonen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using amp.DataMigrate.GUI;
using amp.FormsUtility.Help;
using amp.FormsUtility.Information;
using amp.FormsUtility.Progress;
using amp.FormsUtility.QueueHandling;
using amp.FormsUtility.Random;
using amp.FormsUtility.Songs;
using amp.FormsUtility.UserInteraction;
using amp.IpcUtils;
using amp.UtilityClasses.Settings;
using Ionic.Zip;
using RpcSelf;
using VPKSoft.ErrorLogger;
using VPKSoft.LangLib;
using VPKSoft.PosLib;
using VPKSoft.Utils;
using Utils = VPKSoft.LangLib.Utils;
// (C): http://www.vpksoft.net/, GNU Lesser General Public License Version 3

namespace amp;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        string[] args = Environment.GetCommandLineArgs();

        // not obsolete for the migration to the new settings..
#pragma warning disable 618
        SettingsOld.FromOldSettings(Settings);
#pragma warning restore 618

        Settings.Load(Settings.SettingFileName);

        // wait for the possible install process to finnish..
        VPKSoft.WaitForProcessUtil.WaitForProcess.WaitForProcessArguments(args, 30);

        foreach (var arg in args)
        {
            if (arg.StartsWith("--restoreBackup"))
            {
                var restoreParameters = arg.Split('=');
                if (restoreParameters.Length == 2)
                {
                    if (File.Exists(restoreParameters[1]))
                    {
                        while (AppRunning.CheckIfRunningNoAdd("VPKSoft.amp.sharp#"))
                        {
                            Thread.Sleep(100);
                        }

                        try
                        {
                            using (ZipFile zip = ZipFile.Read(restoreParameters[1]))
                            {
                                zip.ExtractAll(Paths.GetAppSettingsFolder(Misc.AppType.Winforms),
                                    ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                        catch (Exception ex)
                        {
                            // log the exception..
                            ExceptionLogger.LogError(ex);
                        }
                    }
                }
            }
        }


        Process localizeProcess = Utils.CreateDBLocalizeProcess(Paths.AppInstallDir);
        // localizeProcess..

        if (localizeProcess != null)
        {
            localizeProcess.Start();
            return;
        }

        if (!Debugger.IsAttached)
        {
            ExceptionLogger.Bind(); // bind before any visual objects are created
        }

        ExceptionLogger.ApplicationCrashData += ExceptionLogger_ApplicationCrashData;


        // Save languages
        if (Utils.ShouldLocalize() != null)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new FormMain();
            // ReSharper disable once ObjectCreationAsStatement
            new FormPsycho();
            // ReSharper disable once ObjectCreationAsStatement
            new FormProgressBackground();
            // ReSharper disable once ObjectCreationAsStatement
            new FormAddAlbum();
            // ReSharper disable once ObjectCreationAsStatement
            new FormRename();
            // ReSharper disable once ObjectCreationAsStatement
            new FormQueueSnapshotName(); // 16.10.17
            // ReSharper disable once ObjectCreationAsStatement
            new FormSavedQueues(); // 18.10.17
            // ReSharper disable once ObjectCreationAsStatement
            new FormModifySavedQueue(); // 19.10.17
            // ReSharper disable once ObjectCreationAsStatement
            new FormSettings(); // 21.10.17
            // ReSharper disable once ObjectCreationAsStatement
            new FormTagInfo(); // 11.02.18
            // ReSharper disable once ObjectCreationAsStatement
            new FormAlbumNaming(); // 27.10.18
            // ReSharper disable once ObjectCreationAsStatement
            new FormDatabaseUpdatingProgress(); // 27.10.18
            // ReSharper disable once ObjectCreationAsStatement
            new FormHelp(); // 28.10.18
            // ReSharper disable once ObjectCreationAsStatement
            new FormRandomizePriority(); // 30.10.18
            // ReSharper disable once ObjectCreationAsStatement
            new FormDatabaseMigrate(); // 01.09.19
            // ReSharper disable once ObjectCreationAsStatement
            new FormThemeSettings();

            ExceptionLogger.ApplicationCrashData -= ExceptionLogger_ApplicationCrashData;
            ExceptionLogger.UnBind(); // unbind so the truncate thread is stopped successfully
            return;
        }
        // End save languages

#pragma warning disable 618
        // required for history reasons..
        if (Settings.DbUpdateRequiredLevel < 1)
#pragma warning restore 618
        {
            if (AppRunning.CheckIfRunningNoAdd("VPKSoft.amp.DBUpdate.sharp#"))
            {
                ExceptionLogger.ApplicationCrashData -= ExceptionLogger_ApplicationCrashData;
                ExceptionLogger.UnBind(); // unbind so the truncate thread is stopped successfully        
                return;
            }
            DBLangEngine.UseCulture = Settings.Culture; // set the localization value..
            Application.Run(new FormDatabaseUpdatingProgress());
            ExceptionLogger.ApplicationCrashData -= ExceptionLogger_ApplicationCrashData;
            ExceptionLogger.UnBind(); // unbind so the truncate thread is stopped successfully        
#pragma warning disable 618
            // required for history reasons..
            Settings.DbUpdateRequiredLevel = 1;
#pragma warning restore 618
            Process.Start(new ProcessStartInfo(Path.Combine(Paths.AppInstallDir, "amp.exe"))); // the database is updated..
            return;
        }

        if (AppRunning.CheckIfRunningNoAdd("VPKSoft.amp.sharp#"))
        {
            ExceptionLogger.LogMessage($"Application is running. Checking for open file requests. The current directory is: '{Environment.CurrentDirectory}'.");
            try
            {
                RpcSelfClient<string> ipcClient = new RpcSelfClient<string>(50671);

                // only send the existing files to the running instance, don't send the executable
                // file name thus the start from 1..
                for (int i = 1; i < args.Length; i++)
                {
                    string file = args[i];
                        
                    ExceptionLogger.LogMessage($"Request file open: '{file}'.");
                    bool exists;

                    if ((exists = File.Exists(file)) || 
                        file == ArgumentNext ||
                        file == ArgumentPrevious ||
                        file == ArgumentPlayPause)
                    {
                        if (exists)
                        {
                            ExceptionLogger.LogMessage($"File exists: '{file}'. Send open request.");
                        }
                        else
                        {
                            file = file.TrimStart('-');
                            file = file[0].ToString().ToUpperInvariant() + file.Substring(1);
                            ExceptionLogger.LogMessage($"Send playback request: '{file}'.");
                        }

                        ipcClient.SendData(file);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogError(ex);
                // just in case something fails with the IPC communication..
            }
            ExceptionLogger.ApplicationCrashData -= ExceptionLogger_ApplicationCrashData;
            ExceptionLogger.UnBind(); // unbind so the truncate thread is stopped successfully..
            return;
        }

        AppRunning.CheckIfRunning("VPKSoft.amp.sharp#");

        // create an IPC server at localhost, the port was randomized in the development phase..
        IpcServer = new RpcSelfHost<string>(50671);

        // subscribe to the IPC event if the application receives a message from another instance of this application..
        IpcServer.MessageReceived += MessageReceived;

        PositionCore.Bind(ApplicationType.WinForms); // attach the PosLib to the application
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        DBLangEngine.UseCulture = Settings.Culture; // set the localization value..
        Application.Run(new FormMain());
        FormHelp.DisposeSingleton(); // release the help form if any..
        PositionCore.UnBind(ApplicationType.WinForms); // release the event handlers used by the PosLib and save the default data

        // unsubscribe the IpcClientServer MessageReceived event handler..
        IpcServer.MessageReceived -= MessageReceived;
        IpcServer.Dispose();

        ExceptionLogger.ApplicationCrashData -= ExceptionLogger_ApplicationCrashData;
        ExceptionLogger.UnBind(); // unbind so the truncate thread is stopped successfully        
        AppRunning.DisposeMutexByName("VPKSoft.amp.sharp#");

        // run a possible software (as of the time of the writing the software is this one.)..
        if (File.Exists(RunProgramOnExit))
        {
            Process.Start(RunProgramOnExit, RunProgramOnExitArguments);
        }
        else if (FormMain.RestartRequired)
        {
            Process.Start(Application.ExecutablePath);
        }
    }

    // an event handler for the IPC channel to add files to the temporary album via user shell interaction..
    private static void MessageReceived(object sender, IpcExchangeEventArgs<string> e)
    {
        // if a playback command was received, perform different handling..
        switch (e.Object)
        {
            case @"Next": FormMain.TaskBarPlaybackCommand = TaskBarPlaybackCommand.Next; return;
            case @"PlayPause": FormMain.TaskBarPlaybackCommand = TaskBarPlaybackCommand.PausePlayToggle; return;
            case @"Previous": FormMain.TaskBarPlaybackCommand = TaskBarPlaybackCommand.Previous; return;
        }

        if (FormMain.RemoteFileBeingProcessed)
        {
            TemporaryRemoteFiles.Add(e.Object);
        }
        else
        {
            FormMain.StopIpcTimer = true;
            FormMain.RemoteFiles.Add(e.Object);
            FormMain.RemoteFiles.AddRange(TemporaryRemoteFiles);
            TemporaryRemoteFiles.Clear();
            FormMain.StopIpcTimer = false;
        }

        bool threadWait = false;

        while (TemporaryRemoteFiles.Count > 0 && FormMain.RemoteFileBeingProcessed)
        {
            threadWait = true;
            Thread.Sleep(100);
        }

        if (threadWait)
        {
            FormMain.StopIpcTimer = true;
            FormMain.RemoteFiles.AddRange(TemporaryRemoteFiles);
            TemporaryRemoteFiles.Clear();
            FormMain.StopIpcTimer = false;
        }
    }

    // the application is crashing without exception handling..
    private static void ExceptionLogger_ApplicationCrashData(ApplicationCrashEventArgs e)
    {
        // unsubscribe this event handler..
        ExceptionLogger.ApplicationCrashData -= ExceptionLogger_ApplicationCrashData;

        if (IpcServer != null)
        {
            IpcServer.MessageReceived -= MessageReceived;
            IpcServer.Dispose();
        }

        ExceptionLogger.UnBind(); // unbind the exception logger..

        AppRunning.DisposeMutexByName("VPKSoft.amp.sharp#");

        // kill self as the native inter-op libraries may have some issues of keeping the process alive..
        Process.GetCurrentProcess().Kill();

        // This is the end..
    }

    /// <summary>
    /// The IPC channel pushes the existing files to be added to the the list to be added to the temporary album.
    /// </summary>
    private static readonly List<string> TemporaryRemoteFiles = new List<string>();

    /// <summary>
    /// Gets or sets the program to run upon an application exit.
    /// </summary>
    internal static string RunProgramOnExit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments for the program to run upon an application exit.
    /// </summary>
    internal static string RunProgramOnExitArguments { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the program settings.
    /// </summary>
    /// <value>The program settings.</value>
    internal static Settings Settings { get; set; } = new Settings();

    /// <summary>
    /// Gets or sets the IPC server to communicate between amp# application instances.
    /// </summary>
    internal static RpcSelfHost<string> IpcServer { get; set; }

    /// <summary>
    /// An argument for IPC to play the next song.
    /// </summary>
    internal const string ArgumentNext = @"--next";

    /// <summary>
    /// An argument for IPC to play the previous song.
    /// </summary>
    internal const string ArgumentPrevious = @"--previous";

    /// <summary>
    /// An argument for IPC to toggle play / pause state of the song.
    /// </summary>
    internal const string ArgumentPlayPause = @"--playPause";
}