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
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using amp.SQLiteDatabase.DatabaseUtils;
using amp.UtilityClasses;
using TagLib;
using VPKSoft.LangLib;
using File = System.IO.File;

namespace amp.FormsUtility.QueueHandling
{
    public partial class FormModifySavedQueue : DBLangEngineWinforms
    {
        public FormModifySavedQueue()
        {
            InitializeComponent();
            colQueueIndex.Name = "colQueueIndex"; // the columns have only a design name.. LangLib doesn't apply on the nameless components 
            colSongName.Name = "colSongName"; // the columns have only a design name.. LangLib doesn't apply on the nameless components 

            DBLangEngine.DBName = "lang.sqlite";
            if (Utils.ShouldLocalize() != null)
            {
                DBLangEngine.InitalizeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
                return; // After localization don't do anything more.
            }
            DBLangEngine.InitalizeLanguage("amp.Messages");


            fbdDirectory.Description = DBLangEngine.GetMessage("msgQueueCopyFlat",
                "Copy songs into a single directory|A title to a folder select dialog indicating that files in a queue should be copied into a single directory.");
        }

        SQLiteConnection conn;
        int queueIndex = -1;

        List<MusicFile> queueFiles = new List<MusicFile>();
        List<MusicFile> deletedQueueFiles = new List<MusicFile>();

        private void GetQueue()
        {
            lvPlayList.Items.Clear();
            queueFiles.Clear();
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText =
                    "SELECT S.ID, Q.QUEUEINDEX, S.FILENAME " + Environment.NewLine +
                    "FROM " + Environment.NewLine +
                    "SONG S, QUEUE_SNAPSHOT Q " + Environment.NewLine +
                    "WHERE " + Environment.NewLine +
                    "S.ID = Q.SONG_ID AND " + Environment.NewLine +
                    "Q.ID = " + queueIndex + " " + Environment.NewLine +
                    "ORDER BY Q.QUEUEINDEX ";
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

        private void SaveQueue()
        {
            foreach (MusicFile mf in queueFiles)
            {
                string sql =
                    string.Format(
                    "UPDATE QUEUE_SNAPSHOT SET QUEUEINDEX = {0} WHERE SONG_ID = {1} AND ID = {2} ", mf.QueueIndex, mf.ID, queueIndex);
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }

            foreach(MusicFile mf in deletedQueueFiles)
            {
                string sql =
                    string.Format(
                    "DELETE FROM QUEUE_SNAPSHOT WHERE SONG_ID = {0} AND ID = {1} ", mf.ID, queueIndex);
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
        }

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

        private void tsbRemove_Click(object sender, EventArgs e)
        {
            int idx = lvPlayList.SelectedIndices[0];
            int queueDown = queueFiles[idx].QueueIndex;

            deletedQueueFiles.Add(queueFiles[idx]);

            queueFiles.RemoveAt(idx);

            for (int i = 0; i < queueFiles.Count; i++)
            {
                if (queueFiles[i].QueueIndex > queueDown)
                {
                    queueFiles[i].QueueIndex--;
                }
            }

            ReList();
        }

        private void TsbCopyAllFlat_Click(object sender, EventArgs e)
        {
            if (fbdDirectory.ShowDialog() == DialogResult.OK)
            {
                QueueUtilities.CopyQueueFiles(queueIndex, fbdDirectory.SelectedPath, conn);
            }
        }
    }
}