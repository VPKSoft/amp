﻿#region License
/*
MIT License

Copyright(c) 2019 Petteri Kautonen

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
using System.Globalization;
using System.Windows.Forms;
using amp.SQLiteDatabase;
using amp.SQLiteDatabase.DatabaseUtils;
using VPKSoft.LangLib;

namespace amp.FormsUtility.QueueHandling
{
    /// <summary>
    /// A form for managing saved queue snapshots.
    /// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
    /// </summary>
    /// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
    public partial class FormSavedQueues : DBLangEngineWinforms
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormSavedQueues"/> class.
        /// </summary>
        public FormSavedQueues()
        {
            InitializeComponent();
            colQueueSaveTime.Name = "colQueueSaveTime"; // the columns have only a design name.. LangLib doesn't apply on the nameless components 
            colQueueName.Name = "colQueueName"; // the columns have only a design name.. LangLib doesn't apply on the nameless components

            // ReSharper disable once StringLiteralTypo
            DBLangEngine.DBName = "lang.sqlite";
            if (Utils.ShouldLocalize() != null)
            {
                DBLangEngine.InitalizeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
                return; // After localization don't do anything more.
            }
            DBLangEngine.InitalizeLanguage("amp.Messages");

            try // as this can be translated to a invalid format :-)
            {
                odExportQueue.Filter = DBLangEngine.GetMessage("msgFileExt_amp_qex", "amp# queue export files (*.amp#_qex)|*.amp#_qex|as in the combo box to select file type from a dialog");
                sdExportQueue.Filter = DBLangEngine.GetMessage("msgFileExt_amp_qex", "amp# queue export files (*.amp#_qex)|*.amp#_qex|as in the combo box to select file type from a dialog");
            }
            catch
            {
                // ignored..
            }

            odExportQueue.Title = DBLangEngine.GetMessage("msgImportQueueFrom", "Import queue from file|As in import a queue snapshot from a file");
            sdExportQueue.Title = DBLangEngine.GetMessage("msgExportQueueTo", "Export queue to file|As in export a queue snapshot to a file");

            fbdDirectory.Description = DBLangEngine.GetMessage("msgQueueCopyFlat",
                "Copy songs into a single directory|A title to a folder select dialog indicating that files in a queue should be copied into a single directory.");
        }

        /// <summary>
        /// A field to hold <see cref="SQLiteConnection"/> connection given in the <see cref="Execute(string, ref SQLiteConnection, out bool)"/> method call.
        /// </summary>
        private SQLiteConnection conn;

        /// <summary>
        /// The album name.
        /// </summary>
        private string albumName = string.Empty;

        /// <summary>
        /// A field to hold the previous queue snapshot name in case the user cancels the rename.
        /// </summary>
        private string lastText = string.Empty;

        /// <summary>
        /// A field indicating whether the user chose to append to a selected queue snapshot to the current queue of the album.
        /// </summary>
        private bool appendQueue;

        /// <summary>
        /// Refreshes the list of saved queue snapshots discarding the previous changes.
        /// </summary>
        private void RefreshList()
        {
            lvQueues.Items.Clear();
            tsbSave.Enabled = false;
            tsbRemove.Enabled = false;
            tsbCopyAllFlat.Enabled = false;
            lastText = string.Empty;
            bOK.Enabled = false;
            btAppendQueue.Enabled = false;
            tsbModifySavedQueue.Enabled = false;
            tsbExportQueue.Enabled = false;
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText =
                    string.Join(Environment.NewLine,
                        "SELECT COUNT(DISTINCT ID) FROM",
                        // ReSharper disable once StringLiteralTypo
                        $"QUEUE_SNAPSHOT WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {Database.QS(albumName)})");

                if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                {
                    return;
                }

                command.CommandText =
                    string.Join(Environment.NewLine,
                        // ReSharper disable once StringLiteralTypo
                        "SELECT ID, SNAPSHOTNAME, MAX(SNAPSHOT_DATE) AS SNAPSHOT_DATE",
                        // ReSharper disable once StringLiteralTypo
                        $"FROM QUEUE_SNAPSHOT WHERE ALBUM_ID = (SELECT ALBUM_ID FROM ALBUM WHERE ALBUMNAME = {Database.QS(albumName)})",
                                // ReSharper disable once StringLiteralTypo
                                "GROUP BY ID, SNAPSHOTNAME",
                                "ORDER BY MAX(SNAPSHOT_DATE)");


                using (SQLiteDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        ListViewItem lvi = new ListViewItem(dr.GetString(1))
                        {
                            Tag = dr.GetInt32(0)
                        };
                        DateTime dt = DateTime.ParseExact(dr.GetString(2), "yyyy-MM-dd HH':'mm':'ss", CultureInfo.InvariantCulture);
                        lvi.SubItems.Add(dt.ToShortDateString() + " " + dt.ToShortTimeString());
                        lvQueues.Items.Add(lvi);
                    }
                }
            }
        }

        /// <summary>
        /// Displays the dialog for the user to edit the saved queue snapshots.
        /// </summary>
        /// <param name="albumName">Name of the album which queue snapshots to edit.</param>
        /// <param name="conn">A reference to a <see cref="SQLiteConnection"/> class instance.</param>
        /// <param name="append">if set to <c>true</c> the user chose to append the selected queue to the current one in the album.</param>
        /// <returns>The queue index the user selected from the dialog; otherwise -1.</returns>
        public static int Execute(string albumName, ref SQLiteConnection conn, out bool append)
        {
            FormSavedQueues frm = new FormSavedQueues
            {
                conn = conn,
                albumName = albumName
            };
            frm.RefreshList();
            if (frm.lvQueues.Items.Count == 0)
            {
                append = frm.appendQueue;
                return -1;
            }
            if (frm.ShowDialog() == DialogResult.OK)
            {
                append = frm.appendQueue;
                if (frm.lvQueues.SelectedIndices.Count > 0)
                {
                    return Convert.ToInt32(frm.lvQueues.Items[frm.lvQueues.SelectedIndices[0]].Tag);
                }
                return -1;
            }

            append = frm.appendQueue;
            return -1;
        }

        // enables/disables the control buttons based on state of the GUI..
        private void lvQueues_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView lv = (ListView)sender;
            bOK.Enabled = lv.SelectedIndices.Count > 0;
            tsbRemove.Enabled = lv.SelectedIndices.Count > 0;
            tsbModifySavedQueue.Enabled = lv.SelectedIndices.Count > 0;
            tsbCopyAllFlat.Enabled = lv.SelectedIndices.Count > 0;
            tsbExportQueue.Enabled = lv.SelectedIndices.Count > 0;
            btAppendQueue.Enabled = lv.SelectedIndices.Count > 0;
        }

        // saves the changes to the queue snapshots..
        private void tsbSave_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lvQueues.Items)
            {
                if (lvi.Name != @"MODIFIED")
                {
                    continue;
                }
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText =
                        $"UPDATE QUEUE_SNAPSHOT SET SNAPSHOTNAME = '{lvi.Text}' WHERE ID = {lvi.Tag} ";
                    command.ExecuteNonQuery();
                    lvi.Name = string.Empty;
                }
            }
            tsbSave.Enabled = false;
        }

        // the user discarded the changes and refreshed the list of queue snapshots..
        private void tsbRefresh_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        // if the name of a queue snapshot was modified by the user, indicate the change by setting the item's name..
        private void lvQueues_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label != lastText)
            {
                tsbSave.Enabled = true;
                lvQueues.Items[e.Item].Name = @"MODIFIED";
            }
            lastText = string.Empty;
        }

        // save the name of the queue snapshot before the user edits it to detect a change..
        private void lvQueues_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            lastText = e.Label;
        }

        // the user wants to remove a selected queue snapshot..
        private void tsbRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                DBLangEngine.GetMessage("msgRemoveQueueSnapshot", "Are you sure you want to remove queue snapshot {0}?|A confirmation question if a queue snapshot can be removed when asked from the user", lvQueues.Items[lvQueues.SelectedIndices[0]].Text),
                DBLangEngine.GetMessage("msgConfirmation", "Confirm|Used in a dialog title to ask for a confirmation to do something"), MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText = "DELETE FROM QUEUE_SNAPSHOT WHERE ID = " + Convert.ToInt32(lvQueues.Items[lvQueues.SelectedIndices[0]].Tag) + " ";
                    command.ExecuteNonQuery();
                }
                RefreshList();
            }
        }

        // the user wants to modify the selected queue in detail..
        private void tsbModifySavedQueue_Click(object sender, EventArgs e)
        {
            FormModifySavedQueue.Execute(ref conn, Convert.ToInt32(lvQueues.Items[lvQueues.SelectedIndices[0]].Tag));
        }

        // the user wants to export the queue into a file..
        private void tsbExportQueue_Click(object sender, EventArgs e)
        {
            if (sdExportQueue.ShowDialog() == DialogResult.OK)
            {
                Database.SaveQueueSnapshotToFile(conn, Convert.ToInt32(lvQueues.Items[lvQueues.SelectedIndices[0]].Tag), sdExportQueue.FileName);
            }
        }

        // the user wants to import a saved queue from a file..
        private void tsbImportQueue_Click(object sender, EventArgs e)
        {
            if (odExportQueue.ShowDialog() == DialogResult.OK)
            {                
                FormMain wnd = Application.OpenForms[0] as FormMain;

                string queueName = Database.GetQueueSnapshotName(odExportQueue.FileName);

                queueName = FormQueueSnapshotName.Execute(queueName, true);

                if (wnd != null && Database.RestoreQueueSnapshotFromFile(wnd.PlayList, conn, wnd.CurrentAlbum,
                        odExportQueue.FileName, queueName))
                {
                    RefreshList();
                }
            }
        }

        // the user wants to append the selected queue snapshot to the current album queue..
        private void btAppendQueue_Click(object sender, EventArgs e)
        {
            appendQueue = true;
            DialogResult = DialogResult.OK;
        }

        // copies the selected queue snapshot to a single directory for to be burned to e.g. MP3 CD for a car usage..
        private void TsbCopyAllFlat_Click(object sender, EventArgs e)
        {
            if (fbdDirectory.ShowDialog() == DialogResult.OK)
            {
                QueueUtilities.CopyQueueFiles(Convert.ToInt32(lvQueues.Items[lvQueues.SelectedIndices[0]].Tag),
                    fbdDirectory.SelectedPath, conn);
            }
        }
    }
}
