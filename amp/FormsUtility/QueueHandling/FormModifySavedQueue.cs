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
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;
using amp.SQLiteDatabase.DatabaseUtils;
using amp.UtilityClasses;
using VPKSoft.LangLib;

namespace amp.FormsUtility.QueueHandling;

/// <summary>
/// A form to modify a saved queue snapshot.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormModifySavedQueue : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormModifySavedQueue"/> class.
    /// </summary>
    public FormModifySavedQueue()
    {
        InitializeComponent();
        colQueueIndex.Name = "colQueueIndex"; // the columns have only a design name.. LangLib doesn't apply on the nameless components 
        colSongName.Name = "colSongName"; // the columns have only a design name.. LangLib doesn't apply on the nameless components 

        // ReSharper disable once StringLiteralTypo
        DBLangEngine.DBName = "lang.sqlite";
        if (Utils.ShouldLocalize() != null)
        {
            DBLangEngine.InitializeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
            return; // After localization don't do anything more.
        }
        DBLangEngine.InitializeLanguage("amp.Messages");


        fbdDirectory.Description = DBLangEngine.GetMessage("msgQueueCopyFlat",
            "Copy songs into a single directory|A title to a folder select dialog indicating that files in a queue should be copied into a single directory.");
    }

    /// <summary>
    /// A field to hold <see cref="SQLiteConnection"/> connection given in the <see cref="Execute(ref SQLiteConnection, int)"/> method call.
    /// </summary>
    private SQLiteConnection conn;

    /// <summary>
    /// The queue index (SQLite database ID field) for the queue snapshot being modified.
    /// </summary>
    private int queueIndex = -1;

    /// <summary>
    /// A list of the music files in the currently edited queue.
    /// </summary>
    private List<MusicFile> queueFiles = new List<MusicFile>();

    /// <summary>
    /// A list of the music files the user has deleted from the currently edited queue. This makes a cancel method possible.
    /// </summary>
    readonly List<MusicFile> deletedQueueFiles = new List<MusicFile>();

    /// <summary>
    /// Gets the queue and updates it into the GUI.
    /// </summary>
    private void GetQueue()
    {
        lvPlayList.Items.Clear();
        queueFiles.Clear();
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText =
                string.Join(Environment.NewLine,
                    "SELECT S.ID, Q.QUEUEINDEX, S.FILENAME",
                    "FROM",
                    "SONG S, QUEUE_SNAPSHOT Q",
                    "WHERE",
                    "S.ID = Q.SONG_ID AND",
                    $"Q.ID = {queueIndex}",
                    // ReSharper disable once StringLiteralTypo
                    "ORDER BY Q.QUEUEINDEX");
            using (SQLiteDataReader dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    queueFiles.Add(new MusicFile(dr.GetString(2), dr.GetInt32(0)) { QueueIndex = dr.GetInt32(1) });
                }
            }
        }
        foreach (MusicFile mf in queueFiles)
        {
            ListViewItem lvi = new ListViewItem(mf.QueueIndex.ToString());
            lvi.SubItems.Add(mf.SongNameNoQueue);
            lvPlayList.Items.Add(lvi);
            lvi.Tag = mf;
        }
    }

    /// <summary>
    /// Saves the modified queue snapshot into the database.
    /// </summary>
    private void SaveQueue()
    {
        foreach (MusicFile mf in queueFiles)
        {
            string sql =
                // ReSharper disable once StringLiteralTypo
                $"UPDATE QUEUE_SNAPSHOT SET QUEUEINDEX = {mf.QueueIndex} WHERE SONG_ID = {mf.ID} AND ID = {queueIndex} ";
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        foreach(MusicFile mf in deletedQueueFiles)
        {
            string sql =
                $"DELETE FROM QUEUE_SNAPSHOT WHERE SONG_ID = {mf.ID} AND ID = {queueIndex} ";
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Re-lists the currently edited queue snapshot to the GUI.
    /// </summary>
    /// <param name="selectIndex">Index to select from the list view containing the songs in a queue snapshot.</param>
    private void ReList(int selectIndex = -1)
    {
        queueFiles = queueFiles.OrderBy(f => f.QueueIndex).ToList();
        lvPlayList.Items.Clear();
        foreach (MusicFile mf in queueFiles)
        {
            ListViewItem lvi = new ListViewItem(mf.QueueIndex.ToString());
            lvi.SubItems.Add(mf.SongNameNoQueue);
            lvPlayList.Items.Add(lvi);
            lvi.Tag = mf;
        }
        lvPlayList.SelectedIndices.Clear();
        if (selectIndex != -1)
        {
            lvPlayList.SelectedIndices.Add(selectIndex);
        }
        bOK.Enabled = lvPlayList.Items.Count > 0;
    }

    /// <summary>
    /// Displays the dialog with a given <paramref name="queueIndex"/> (SQLite database ID number).
    /// </summary>
    /// <param name="conn">A reference to a <see cref="SQLiteConnection"/> class instance.</param>
    /// <param name="queueIndex">The index of the queue snapshot (SQLite database ID number).</param>
    /// <returns><c>true</c> if the user chose to accept the changes made to the queue snapshot, <c>false</c> otherwise.</returns>
    public static bool Execute(ref SQLiteConnection conn, int queueIndex)
    {
        FormModifySavedQueue frm = new FormModifySavedQueue
        {
            queueIndex = queueIndex,
            conn = conn
        };
        frm.GetQueue();

        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText = "SELECT SNAPSHOTNAME FROM QUEUE_SNAPSHOT WHERE ID = " + queueIndex + " ";
            frm.Text = DBLangEngine.GetStatMessage("msgModifyQueueCaption", "Modify saved queue [{0}]|A text to display in the window title where a saved queue is being modified",
                Convert.ToString(command.ExecuteScalar()));
        }

        if (frm.ShowDialog() == DialogResult.OK)
        {
            frm.SaveQueue();
            return true;
        }

        return false;
    }

    // enables/disables the control buttons based on state of the GUI..
    private void lvPlayList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lvPlayList.SelectedIndices.Count > 0)
        {
            tsbMoveUp.Enabled = lvPlayList.SelectedIndices[0] > 0;
            tsbMoveDown.Enabled = lvPlayList.SelectedIndices[0] < lvPlayList.Items.Count - 1;
            tsbRemove.Enabled = true;
        }
        else
        {
            tsbMoveUp.Enabled = false;
            tsbMoveDown.Enabled = false;
            tsbRemove.Enabled = false;
        }
    }

    // moves an item upwards in the queue list..
    private void tsbMoveUp_Click(object sender, EventArgs e)
    {
        int idx = lvPlayList.SelectedIndices[0];
        int idxToMove = idx - 1;
        MusicFile mf1 = (MusicFile)lvPlayList.Items[idx].Tag;
        MusicFile mf2 = (MusicFile)lvPlayList.Items[idxToMove].Tag;
        int tmpQueue = mf1.QueueIndex;
        mf1.QueueIndex = mf2.QueueIndex;
        mf2.QueueIndex = tmpQueue;
        ReList(idxToMove);
    }

    // moves an item downwards in the queue list..
    private void tsbMoveDown_Click(object sender, EventArgs e)
    {
        int idx = lvPlayList.SelectedIndices[0];
        int idxToMove = idx + 1;
        MusicFile mf1 = (MusicFile)lvPlayList.Items[idx].Tag;
        MusicFile mf2 = (MusicFile)lvPlayList.Items[idxToMove].Tag;
        int tmpQueue = mf1.QueueIndex;
        mf1.QueueIndex = mf2.QueueIndex;
        mf2.QueueIndex = tmpQueue;
        ReList(idxToMove);
    }

    // removes on item from the queue list..
    private void tsbRemove_Click(object sender, EventArgs e)
    {
        int idx = lvPlayList.SelectedIndices[0];
        int queueDown = queueFiles[idx].QueueIndex;

        deletedQueueFiles.Add(queueFiles[idx]);

        queueFiles.RemoveAt(idx);

        foreach (var queueFile in queueFiles)
        {
            if (queueFile.QueueIndex > queueDown)
            {
                queueFile.QueueIndex--;
            }
        }

        ReList();
    }

    // copies the queue to a single directory for to be burned to e.g. MP3 CD for a car usage..
    private void TsbCopyAllFlat_Click(object sender, EventArgs e)
    {
        if (fbdDirectory.ShowDialog() == DialogResult.OK)
        {
            bool convertToMp3 =
                MessageBox.Show(
                    DBLangEngine.GetMessage("msgQueryConvertToMP3",
                        "Convert non-MP3 files to MP3 format?|A query to ask whether to convert files other than MP3 to MP3 format."),
                    DBLangEngine.GetMessage("msgConfirmation",
                        "Confirm|Used in a dialog title to ask for a confirmation to do something"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1) == DialogResult.Yes;

            QueueUtilities.RunWithDialog(this, queueIndex, fbdDirectory.SelectedPath, conn, convertToMp3,
                DBLangEngine.GetMessage("msgProcessingFiles",
                    "Processing files...|A message describing a possible lengthy operation with files is running."),
                DBLangEngine.GetMessage("msgProgressPercentage",
                    "Progress: {0} %|A message describing some operation progress in percentage."));
        }
    }
}