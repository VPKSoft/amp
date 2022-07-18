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
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using amp.UtilityClasses;
using VPKSoft.LangLib;
using VPKSoft.ScriptRunner;

namespace amp.FormsUtility.Progress;

/// <summary>
/// A form for displaying progress for a lengthy operation with the database.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormDatabaseUpdatingProgress : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormDatabaseUpdatingProgress"/> class.
    /// </summary>
    public FormDatabaseUpdatingProgress()
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

    /// <summary>
    /// Sets the progress for the progress bar and for the item counter.
    /// </summary>
    /// <param name="percentage">The percentage.</param>
    /// <param name="maximumValue">The maximum value of updating items.</param>
    public void SetProgress(int percentage, int maximumValue)
    {
        pbUpdateProgress.Value = percentage;

        int current = maximumValue / 100 * percentage;
        lbItemIndex.Text = $@"({current}/{maximumValue})";
    }

    /// <summary>
    /// A field to hold <see cref="SQLiteConnection"/> connection.
    /// </summary>
    private SQLiteConnection conn;

    /// <summary>
    /// The count of entries in the SONG database table.
    /// </summary>
    private int maximum;

    // the form is shown, so start the update process..
    private void FormDatabaseUpdatingProgress_Shown(object sender, EventArgs e)
    {
        // ReSharper disable once StringLiteralTypo
        conn = new SQLiteConnection("Data Source=" + DBLangEngine.DataDir +
                                    "amp.sqlite;Pooling=true;FailIfMissing=false;Cache Size=10000;"); // PRAGMA synchronous=OFF;PRAGMA journal_mode=OFF
        conn.Open();

        // ReSharper disable once StringLiteralTypo
        if (!ScriptRunner.RunScript(DBLangEngine.DataDir + "amp.sqlite"))
        {
            MessageBox.Show(
                DBLangEngine.GetMessage("msgErrorInScript",
                    "A script error occurred on the database update|Something failed during running the database update script"),
                DBLangEngine.GetMessage("msgError", "Error|A message describing that some kind of error occurred."),
                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);

            // at this point there is no reason to continue the program's execution as the database might be in an invalid state..
            throw new Exception(DBLangEngine.GetMessage("msgErrorInScript",
                "A script error occurred on the database update|Something failed during running the database update script"));
        }
        bwDatabaseUpdate.RunWorkerAsync();
    }

    // run the update against the database..
    private void bwDatabaseUpdate_DoWork(object sender, DoWorkEventArgs e)
    {
        string sql = "SELECT COUNT(*) FROM SONG ";

        using (SQLiteCommand command = new SQLiteCommand(sql, conn))
        {
            maximum = (int)Convert.ToInt64(command.ExecuteScalar());
        }

        maximum *= 2;
        int maxValue = maximum;
        int currentValue = 0;

        List<MusicFile> updateFiles = new List<MusicFile>();

        sql = "SELECT FILENAME, ID FROM SONG ";

        using (SQLiteCommand command = new SQLiteCommand(sql, conn))
        {
            using (SQLiteDataReader dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    currentValue++;
                    updateFiles.Add(File.Exists(dr.GetString(0))
                        ? new MusicFile(dr.GetString(0), dr.GetInt32(1))
                        : null);

                    if ((currentValue % 100) == 0)
                    {
                        bwDatabaseUpdate.ReportProgress((int)(currentValue / (double)maxValue * 100));
                    }
                }
            }
        }

        sql = string.Empty;
        foreach (MusicFile mf in updateFiles)
        {
            if (mf != null)
            {
                mf.LoadTag(true);
                sql += $"UPDATE SONG SET TITLE = {amp.SQLiteDatabase.Database.QS(mf.Title)} WHERE ID = {mf.ID}; " + Environment.NewLine;
            }
            if ((currentValue % 100) == 0)
            {
                if (sql != string.Empty)
                {
                    using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                    sql = string.Empty;
                }
                bwDatabaseUpdate.ReportProgress((int)(currentValue / (double)maxValue * 100));
            }
            currentValue++;
        }

        bwDatabaseUpdate.ReportProgress(100);
    }

    // the update is finished; close the form
    private void bwDatabaseUpdate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        using (conn)
        {
            conn.Close();
        }
        canCloseForm = true;
        Close();
    }

    /// <summary>
    /// a value indicating whether the form can be closed to prevent interruption of the process.
    /// </summary>
    private bool canCloseForm;

    // indicate the progress..
    private void bwDatabaseUpdate_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        SetProgress(e.ProgressPercentage, maximum);
    }

    // validate whether the form can be closed..
    private void FormDatabaseUpdatingProgress_FormClosing(object sender, FormClosingEventArgs e)
    {
        e.Cancel = !canCloseForm;
        if (!canCloseForm)
        {
            BringToFront();
        }
    }
}