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
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using amp.FormsUtility.Progress;
using Ionic.Zip;
using VPKSoft.ErrorLogger;
using VPKSoft.LangLib;
using VPKSoft.Utils;
using Utils = VPKSoft.LangLib.Utils;

namespace amp.DataMigrate.GUI;

/// <summary>
/// A form to relocate file entries within the database if moved in the file system.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormDatabaseMigrate : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormDatabaseMigrate"/> class.
    /// </summary>
    public FormDatabaseMigrate()
    {
        InitializeComponent();

        // ReSharper disable once StringLiteralTypo
        DBLangEngine.DBName = "lang.sqlite";

        if (Utils.ShouldLocalize() != null)
        {
            DBLangEngine.InitializeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
            return; // After localization don't do anything more.
        }

        DBLangEngine.InitializeLanguage("amp.Messages");
    }

    private SQLiteConnection Connection { get; set; }

    /// <summary>
    /// Shows the dialog.
    /// </summary>
    /// <param name="connection">A <see cref="SQLiteConnection"/> instance.</param>
    /// <returns><c>true</c> if changes were made to the database, <c>false</c> otherwise.</returns>
    public static bool ShowDialog(SQLiteConnection connection)
    {
        var form = new FormDatabaseMigrate {Connection = connection};

        using (form)
        {
            return form.ShowDialog() == DialogResult.OK;
        }
    }

    private void FormFileRelocate_Shown(object sender, EventArgs e)
    {
        ListPaths((int) nudDirectoryDepth.Value);
    }

    private void ListPaths(int depth)
    {
        lbPathsUsedList.Items.Clear();
        var paths = DatabaseDataMigrate.GetDirectories(Connection, depth);
        // ReSharper disable once CoVariantArrayConversion
        lbPathsUsedList.Items.AddRange(paths.ToArray());
    }

    private void BtUpdateFileLocation_Click(object sender, EventArgs e)
    {
        FormProgressBackground.Execute(this,
            DatabaseDataMigrate.UpdateSongLocations(fbdDirectory, Connection),
            DBLangEngine.GetMessage("msgDatabaseUpdate", "Database update|A message describing that the software is updating it's database."),
            DBLangEngine.GetMessage("msgProgressPercentage",
                "Progress: {0} %|A message describing some operation progress in percentage."));

        // indicate to the user that a software restart is required..
        tbRestartRequired.Visible = true;

        // indicate that the application should be restarted after the operation..
        FormMain.RestartRequired = true;
    }

    private void BtDeleteNonExistingSongs_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show(
                DBLangEngine.GetMessage("msgQueryUserDeleteNonExistingSongs",
                    "Really delete non-existing files from the database?|A message requesting for user confirmation to delete non-existing files from the database."),
                DBLangEngine.GetMessage("msgConfirmation",
                    "Confirm|Used in a dialog title to ask for a confirmation to do something"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) ==
            DialogResult.Yes)
        {
            // indicate to the user that a software restart is required..
            tbRestartRequired.Visible = true;

            FormProgressBackground.Execute(this,
                DatabaseDataMigrate.DeleteNonExistingSongs(Connection), 
                DBLangEngine.GetMessage("msgDatabaseUpdate", "Database update|A message describing that the software is updating it's database."),
                DBLangEngine.GetMessage("msgProgressPercentage",
                    "Progress: {0} %|A message describing some operation progress in percentage."));

            // indicate that the application should be restarted after the operation..
            FormMain.RestartRequired = true;
        }
    }

    private void NudDirectoryDepth_ValueChanged(object sender, EventArgs e)
    {
        ListPaths((int) nudDirectoryDepth.Value);
    }

    private void BtExportUserData_Click(object sender, EventArgs e)
    {
        sdZip.FileName = "amp_" + DateTime.Now.ToString("yyyy'-'MM'-'dd HH'_'mm") + "_backup";
        if (sdZip.ShowDialog() == DialogResult.OK)
        {
            try
            {
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddFile(Path.Combine(Paths.GetAppSettingsFolder(Misc.AppType.Winforms), "amp.sqlite"), "");
                    // Removed as useless: if a new version is installed and a backup is restored the
                    // localization will revert to the moment of the backup:
                    // ReSharper disable once CommentTypo
                    // zip.AddFile(Path.Combine(Paths.GetAppSettingsFolder(), "lang.sqlite"),"");

                    zip.AddFile(Path.Combine(Paths.GetAppSettingsFolder(Misc.AppType.Winforms), "settings.vnml"), "");
                    zip.AddFile(Path.Combine(Paths.GetAppSettingsFolder(Misc.AppType.Winforms), "position.vnml"), "");
                    zip.Save(sdZip.FileName);
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogError(ex);
                MessageBox.Show(
                    DBLangEngine.GetMessage("msgUserDataExportError",
                        "An error occurred while exporting the user data.|Something failed during compressing the user data to a ZIP file"),
                    DBLangEngine.GetMessage("msgError", "Error|A message describing that some kind of error occurred."),
                    MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }
    }

    private void BtImportUserData_Click(object sender, EventArgs e)
    {
        if (odZip.ShowDialog() == DialogResult.OK)
        {
            Program.RunProgramOnExit = Application.ExecutablePath;
            var args = "--restoreBackup=" + odZip.FileName;
            args = "\"" + args + "\"";

            Program.RunProgramOnExitArguments = args;

            // indicate that the application should be restarted after the operation..
            FormMain.RestartRequired = true;

            // indicate to the user that a software restart is required..
            tbRestartRequired.Visible = true;

            // other operations aren't allowed before restart..
            DialogResult = DialogResult.OK;
            /*

            MessageBox.Show(
                DBLangEngine.GetMessage("msgQueryUserShutdownProgram",
                    "Please close the software so the backup can be restored.|A message informing the user that the close the software that the backup zip file can be restored."),
                DBLangEngine.GetMessage("msgInformation",
                    "Information|Some information is given to the user, do add more definitive message to make some sense to the 'information'..."),
                MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
            */
        }
    }

    private void FormDatabaseMigrate_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (FormMain.RestartRequired)
        {
            MessageBox.Show(
                DBLangEngine.GetMessage("msgSoftwareWillRestart",
                    "The software will now restart for the changes to take affect.|A message informing the user that the software will restart for the changes to take affect."),
                DBLangEngine.GetMessage("msgInformation",
                    "Information|Some information is given to the user, do add more definitive message to make some sense to the 'information'..."),
                MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
        }
    }

    private void BtOKRestartClose_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
    }
}