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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using amp.UtilityClasses;

namespace amp.SQLiteDatabase
{
    public class Album
    {
        public int Id { get; }

        public string AlbumName { get; }

        public Album(int id, string name)
        {
            AlbumName = name;
            Id = id;
        }
    }

    [Flags]
    public enum DatabaseEventType
    {
        Started = 0,
        Stopped = 1,
        GetSongTag = 2,
        UpdateSongDb = 4,
        InsertSongDb = 8,
        InsertSongAlbum = 16,
        GetSongId = 32,
        LoadMeta = 64,
        QueryDb = 128
    }

    public class DatabaseEventArgs: EventArgs
    {
        public int Progress { get; set; }
        public int ProgressEnd { get; set; }
        public DatabaseEventType EventType { get; set; }


        public DatabaseEventArgs(int progress, int progressEnd, DatabaseEventType eventType)
        {
            Progress = progress;
            ProgressEnd = progressEnd;
            EventType = eventType;
        }
    }


    public class Database
    {
        // initialize a System.Windows.Forms SynchronizationContext
        private static readonly SynchronizationContext Context = SynchronizationContext.Current ?? new SynchronizationContext();

        public delegate void OnDatabaseProgress(DatabaseEventArgs e);

        public static event OnDatabaseProgress DatabaseProgress;

        private static void OnDatabaseProgressThreadSafe(object state)
        {
            DatabaseProgress?.Invoke(state as DatabaseEventArgs);
        }

        private static void DatabaseProgressThreadSafe(DatabaseEventArgs e)
        {
            Context.Send(OnDatabaseProgressThreadSafe, e);
        }

        public static List<Album> GetAlbums(SQLiteConnection conn)
        {
            List<Album> retval = new List<Album>();
            using (SQLiteCommand command = new SQLiteCommand (conn))
            {
                command.CommandText = "SELECT ID, ALBUMNAME FROM ALBUM " +
                                      "WHERE ID >= 1 " +
                                      "ORDER BY ALBUMNAME COLLATE NOCASE ";
                SQLiteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    retval.Add(new Album(reader.GetInt32(0), reader.GetString(1)));
                }
            }
            return retval;
        }

        public static void GetIDsForSongs(ref List<MusicFile> noIdSongs, SQLiteConnection conn)
        {
            int count = noIdSongs.Count;
            DatabaseProgressThreadSafe(new DatabaseEventArgs(0, count, DatabaseEventType.Started));
            DatabaseProgressThreadSafe(new DatabaseEventArgs(0, count, DatabaseEventType.GetSongId));
            if (noIdSongs.Count == 0)
            {
                return;
            }

            var sql = "SELECT ID, FILENAME FROM SONG WHERE FILENAME IN(";
            foreach (MusicFile mf in noIdSongs)
            {
                sql += "'" + mf.FullFileName.Replace("'", "''") + "', ";
            }
            sql = sql.TrimEnd(", ".ToCharArray()) + ") ";

            List<KeyValuePair<int, string>> idFiles = new List<KeyValuePair<int, string>>();

            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = sql;
                using (SQLiteDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        idFiles.Add(new KeyValuePair<int, string>(dr.GetInt32(0), dr.GetString(1)));
                    }
                }
            }

            int counter = 0;

            foreach (MusicFile mf in noIdSongs)
            {
                for (int i = idFiles.Count - 1; i >= 0; i--)
                {
                    if (mf.FullFileName == idFiles[i].Value)
                    {
                        mf.ID = idFiles[i].Key;
                        idFiles.RemoveAt(i);
                        counter++;
                    }
                }
                if ((counter % 50) == 0)
                {
                    DatabaseProgressThreadSafe(new DatabaseEventArgs(counter, count, DatabaseEventType.GetSongId));
                }
                counter++;
            }
            DatabaseProgressThreadSafe(new DatabaseEventArgs(count, count, DatabaseEventType.GetSongId));
            DatabaseProgressThreadSafe(new DatabaseEventArgs(count, count, DatabaseEventType.Stopped));
        }

        private static bool albumChanged;

        public static bool AlbumChanged
        {
            get
            {
                bool btmp = albumChanged;
                albumChanged = false;
                return btmp;
            }
        }

        public static void AddSongToAlbum(string name, List<MusicFile> addSongs, SQLiteConnection conn)
        {
            string sql = string.Empty;
            if (addSongs.Count == 0)
            {
                return;
            }

            DatabaseProgressThreadSafe(new DatabaseEventArgs(0, addSongs.Count, DatabaseEventType.Started));

            object oAlbumId;

            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + name.Replace("'", "''") + "' ";
                oAlbumId = command.ExecuteScalar();
            }

            for (int i = 0; i < addSongs.Count; i++)
            {
                sql += "INSERT INTO ALBUMSONGS (ALBUM_ID, SONG_ID, QUEUEINDEX) " +
                       "SELECT " + oAlbumId + ", " + addSongs[i].ID + ", 0 " +
                       "WHERE NOT EXISTS(SELECT 1 FROM ALBUMSONGS WHERE " +
                       "ALBUM_ID = " + oAlbumId + " AND SONG_ID = " + addSongs[i].ID + "); ";
                if ((i % 200) == 0 && i != 0)
                {
                    ExecuteTransaction(sql, conn);
                    sql = string.Empty;
                    DatabaseProgressThreadSafe(new DatabaseEventArgs(i, addSongs.Count, DatabaseEventType.InsertSongAlbum));
                }
            }

            if (sql != string.Empty)
            {
                ExecuteTransaction(sql, conn);
            }
            DatabaseProgressThreadSafe(new DatabaseEventArgs(addSongs.Count, addSongs.Count, DatabaseEventType.InsertSongAlbum));
            DatabaseProgressThreadSafe(new DatabaseEventArgs(addSongs.Count, addSongs.Count, DatabaseEventType.Stopped));
            albumChanged = true;
        }

        public static void RemoveSongFromAlbum(string name, List<MusicFile> musicFiles, SQLiteConnection conn)
        {
            if (musicFiles.Count == 0)
            {
                return;
            }

            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + name.Replace("'", "''") + "' ";
                object oAlbumId = command.ExecuteScalar();
                string idList = string.Empty;
                foreach (MusicFile mf in musicFiles)
                {
                    idList += mf.ID + ", ";
                }
                if (idList != string.Empty)
                {
                    idList = idList.Substring(0, idList.Length - 2);
                    command.CommandText = "DELETE FROM ALBUMSONGS " +
                                          "WHERE " +
                                          "ALBUM_ID = " + oAlbumId + " AND SONG_ID IN(" + idList + ") ";
                    command.ExecuteNonQuery();
                }
            }
            albumChanged = true;
        }


        public static void ClearTmpAlbum(ref List<MusicFile> playList, SQLiteConnection conn)
        {
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                playList.Clear();
                command.CommandText = "DELETE FROM ALBUMSONGS WHERE ALBUM_ID = 0 ";
                command.ExecuteNonQuery();
            }
        }

        // ReSharper disable once InconsistentNaming
        public static string QS(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        public static string GetAlbumSql(string name)
        {
            string result =
                string.Join(Environment.NewLine,
                        "SELECT S.FILENAME, S.ID, S.VOLUME, S.RATING, A.QUEUEINDEX, IFNULL(S.OVERRIDE_NAME, '') AS OVERRIDE_NAME, ",
                        "NULLIF(S.TAGFINDSTR, '') AS TAGFINDSTR, IFNULL(S.TAGREAD, 0) AS TAGREAD, ",
                        "LENGTH(TAGFINDSTR) AS LEN, ", // 08
                        "IFNULL(S.ARTIST, '') AS ARTIST, ", // 09
                        "IFNULL(S.ALBUM, '') AS ALBUM, ", // 10
                        "IFNULL(S.TRACK, '') AS TRACK, ", // 11
                        "IFNULL(S.YEAR, '') AS YEAR, ", // 12
                        "IFNULL(S.TITLE, '') AS TITLE, ", // 13
                        "IFNULL(S.SKIPPED_EARLY, 0) AS SKIPPED_EARLY, ", // 14
                        "IFNULL(S.NPLAYED_RAND, 0) AS NPLAYED_RAND, ", // 15
                        "IFNULL(S.NPLAYED_USER, 0) AS NPLAYED_USER ", // 16
                        "FROM ",
                        "SONG S, ALBUMSONGS A ",
                        "WHERE ",
                        "S.ID = A.SONG_ID");

            if (name != string.Empty)
            {
                result += " AND " + 
                    string.Join(Environment.NewLine,
                        $"A.SONG_ID IN(SELECT SONG_ID FROM ALBUMSONGS WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)})) AND ",
                        $"A.ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)}) GROUP BY S.ID");
            }
            else
            {
                result += " GROUP BY S.ID ";
            }

            return result;
        }

        public static void GetAlbum(string name, ref List<MusicFile> playList, SQLiteConnection conn)
        {
            List<int> indices = new List<int>();
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                playList.Clear();

                command.CommandText = "SELECT COUNT(*) FROM ALBUMSONGS WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + name.Replace("'", "''") + "') ";
                var totalCnt = Convert.ToInt32(command.ExecuteScalar());

                command.CommandText = GetAlbumSql(name);
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    int counter = 0, counter2 = 1;
                    while (reader.Read())
                    {
                        counter2++;
                        if (!File.Exists(reader.GetString(0)))
                        {
                            continue;
                        }

                        MusicFile mf = new MusicFile(reader.GetString(0), reader.GetInt32(1))
                        {
                            Volume = reader.GetFloat(2),
                            Rating = reader.GetInt32(3),
                            QueueIndex = reader.GetInt32(4),
                            OverrideName = reader.GetString(5)
                        };
                        mf.GetTagFromDataReader(reader);

                        mf.SKIPPED_EARLY = reader.GetInt32(14);
                        mf.NPLAYED_RAND = reader.GetInt32(15);
                        mf.NPLAYED_USER = reader.GetInt32(16);

                        mf.TagString = reader.GetInt32(8) != 0 ? reader.GetString(6) : string.Empty;
                        mf.VisualIndex = counter++;
                        if (reader.GetInt32(7) == 0)
                        {
                            indices.Add(mf.VisualIndex);
                        }
                        playList.Add(mf);
                        DatabaseProgressThreadSafe(new DatabaseEventArgs(counter2, totalCnt, DatabaseEventType.QueryDb));
                    }
                }

                string sSql = string.Empty;
                foreach (MusicFile mf in playList)
                {
                    for (int i = 0; i < indices.Count; i++)
                    {
                        if (mf.VisualIndex == indices[i])
                        {
                            mf.LoadTag();
                            sSql += $"UPDATE SONG SET TAGFINDSTR = {QS(mf.TagString)}, TAGREAD = 1 WHERE ID = {mf.ID}; ";
                        }
                    }
                }
                if (sSql != string.Empty)
                {
                    ExecuteTransaction(sSql, conn);
                }
            }
        }

        public static void SaveVolume(MusicFile mf, SQLiteConnection conn)
        {
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = $"UPDATE SONG SET VOLUME = {mf.Volume.ToString(CultureInfo.InvariantCulture).Replace(',', '.')} WHERE ID = {mf.ID} ";
                command.ExecuteNonQuery();
            }
        }

        public static void SaveOverrideName(ref MusicFile mf, string newName, SQLiteConnection conn)
        {
            if (newName == string.Empty)
            {
                return;
            }
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = $"UPDATE SONG SET OVERRIDE_NAME = {QS(newName)} WHERE ID = {mf.ID} ";
                mf.OverrideName = newName;
                command.ExecuteNonQuery();
            }
        }

        public static void SaveQueue(List<MusicFile> files, SQLiteConnection conn, string albumName)
        {
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "UPDATE ALBUMSONGS SET QUEUEINDEX = 0 WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + albumName.Replace("'", "''") + "') ";
                command.ExecuteNonQuery();
            }
            foreach (MusicFile mf in files)
            {
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    if (mf.QueueIndex > 0)
                    {
                        command.CommandText = "UPDATE ALBUMSONGS SET QUEUEINDEX = " + mf.QueueIndex + " WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + albumName.Replace("'", "''") + "') AND SONG_ID = " + mf.ID + " ";
                        command.ExecuteNonQuery();
                    }
                }                
            }
        }

        public static void LoadQueue(ref List<MusicFile> files, SQLiteConnection conn, int queueIndex, bool append)
        {
            int qIdx = files.Max(f => f.QueueIndex);
            if (!append)
            {
                foreach (MusicFile mf in files)
                {
                    mf.QueueIndex = 0;
                }
            }

            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "SELECT SONG_ID, QUEUEINDEX FROM QUEUE_SNAPSHOT WHERE ID = " + queueIndex + " ORDER BY QUEUEINDEX ";
                using (SQLiteDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        try
                        {
                            MusicFile mf = files.Find(f => f.ID == dr.GetInt32(0));
                            if (append && mf.QueueIndex > 0)
                            {
                            }
                            else if (append)
                            {
                                mf.QueueIndex += ++qIdx;
                            }
                            else
                            {
                                mf.QueueIndex = dr.GetInt32(1);
                            }
                        }
                        catch
                        {
                            // The song does not exist anymore...
                        }
                    }
                }
            }
        }

        public static void SaveQueueSnapshot(List<MusicFile> files, SQLiteConnection conn, string albumName, string snapshotName)
        {
            string sql;

            int id;
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "SELECT IFNULL((SELECT MAX(ID) FROM QUEUE_SNAPSHOT), -1) + 1 ";
                id = Convert.ToInt32(command.ExecuteScalar());
            }

            if (files.Exists(f => f.AlternateQueueIndex > 0)) // the alternate queue will be saved if an alternate queue does exist..
            {
                foreach (MusicFile mf in files)
                {
                    using (SQLiteCommand command = new SQLiteCommand(conn))
                    {
                        if (mf.AlternateQueueIndex > 0)
                        {

                            sql =
                                "INSERT INTO QUEUE_SNAPSHOT (ID, ALBUM_ID, SONG_ID, QUEUEINDEX, SNAPSHOTNAME) VALUES( " + Environment.NewLine +
                                id + ", " + Environment.NewLine +
                                "(SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + albumName.Replace("'", "''") + "'), " + Environment.NewLine +
                                mf.ID + ", " + Environment.NewLine +
                                mf.AlternateQueueIndex + ", " + Environment.NewLine +
                                "'" + snapshotName.Replace("'", "''") + "') " + Environment.NewLine;

                            command.CommandText = sql;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            else
            {
                foreach (MusicFile mf in files)
                {
                    using (SQLiteCommand command = new SQLiteCommand(conn))
                    {
                        if (mf.QueueIndex > 0)
                        {

                            sql =
                                "INSERT INTO QUEUE_SNAPSHOT (ID, ALBUM_ID, SONG_ID, QUEUEINDEX, SNAPSHOTNAME) VALUES( " + Environment.NewLine +
                                id + ", " + Environment.NewLine +
                                "(SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + albumName.Replace("'", "''") + "'), " + Environment.NewLine +
                                mf.ID + ", " + Environment.NewLine +
                                mf.QueueIndex + ", " + Environment.NewLine +
                                "'" + snapshotName.Replace("'", "''") + "') " + Environment.NewLine;

                            command.CommandText = sql;
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public static int ListStringDownMatch(List<string> list1, List<string> list2)
        {
            int min = Math.Min(list1.Count, list2.Count);
            list1.Reverse();
            list2.Reverse();
            int retval = 0;
            for (int i = 0; i < min; i++)
            {
                if (list1[i] == list2[i])
                {
                    retval++;
                }
                else
                {
                    break;
                }
            }
            return retval;
        }

        public static bool SaveQueueSnapshotToFile(SQLiteConnection conn, int id, string queueFileName)
        {
            try
            {
                List<string> lines = new List<string>();
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    command.CommandText =
                        "SELECT " + Environment.NewLine +
                        "(SELECT ALBUMNAME FROM ALBUM WHERE ID = ALBUM_ID) AS ALBUMNAME, " + Environment.NewLine +
                        "(SELECT FILENAME FROM SONG WHERE ID = SONG_ID) AS FILENAME, " + Environment.NewLine +
                        "QUEUEINDEX, SNAPSHOTNAME, SNAPSHOT_DATE " + Environment.NewLine +
                        "FROM QUEUE_SNAPSHOT WHERE ID = " + id + " ";

                    lines.Add("amp# QueueSnapshot Export");
                    using (SQLiteDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (lines.Count == 1) // stupid..
                            {
                                lines.Add("NAME: " + dr.GetString(3));
                            }

                            if (lines.Count == 2) // also stupid..
                            {
                                lines.Add("ALBUMNAME:  " + dr.GetString(0));
                            }

                            if (lines.Count == 3) // getting there.. stupid..
                            {
                                lines.Add("DATE: " + dr.GetDateTime(4).ToString("yyyy-MM-dd HH':'mm':'ss", CultureInfo.InvariantCulture));
                            } // the end of stupid.. SNIFF..

                            try
                            {
                                lines.Add("SONG: QIDX=" + dr.GetInt32(2) + ": " + dr.GetString(1));
                            }
                            catch
                            {
                                // The song does not exist anymore...
                            }
                        }
                    }
                }

                File.WriteAllLines(queueFileName, lines.ToArray());

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetQueueSnapshotName(string queueFileName)
        {
            try
            {
                string[] lines = File.ReadAllLines(queueFileName);
                if (!lines[0].StartsWith("amp# QueueSnapshot Export"))
                {
                    return string.Empty;
                }
                return lines[1].Replace("NAME: ", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static bool RestoreQueueSnapshotFromFile(List<MusicFile> files, SQLiteConnection conn, string albumName, string queueFileName, string overrideName)
        {
            int id;
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "SELECT IFNULL((SELECT MAX(ID) FROM QUEUE_SNAPSHOT), -1) + 1 ";
                id = Convert.ToInt32(command.ExecuteScalar());
            }

            string snapShotDate;
            string snapshotName;

            List<List<string>> paths = new List<List<string>>();
            List<int> queueIndices = new List<int>();
            try
            {
                string[] lines = File.ReadAllLines(queueFileName);
                if (!lines[0].StartsWith("amp# QueueSnapshot Export"))
                {
                    return false;
                }

                snapshotName = lines[1].Replace("NAME: ", string.Empty);
                // lines[2] == useless information
                snapShotDate = lines[3].Replace("DATE: ", string.Empty).Trim();

                for (int i = 4; i < lines.Length; i++)
                {
                    queueIndices.Add(int.Parse(lines[i].Replace("SONG: QIDX=", string.Empty).Split(':')[0]));
                    lines[i] = lines[i].Replace("SONG: QIDX=", string.Empty);

                    // NOTE::Changed to actually mean something (watch out for a bug!)
                    lines[i] = lines[i].TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

                    lines[i] = lines[i].Substring(3);
                    lines[i] = lines[i].TrimStart();
                    lines[i] = Regex.Replace(lines[i], "^[A-Za-z][:]?", string.Empty);
                    lines[i] = lines[i].TrimStart('\\');
                    paths.Add(new List<string>(lines[i].Split('\\')));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }

            // select datetime('2017-08-01 10:12:11')

            List<string> queueFile;
            List<Tuple<int, int>> matches = new List<Tuple<int, int>>();
            List<MusicFile> foundSongs = new List<MusicFile>();
            foreach (MusicFile mf in files)
            {
                if (paths.Count == 0)
                {
                    break;
                }

                queueFile = new List<string>(Regex.Replace(mf.FullFileName, "^[A-Za-z][:]?", string.Empty).TrimStart('\\').Split('\\'));

                matches.Clear();
                for (int i = 0; i < paths.Count; i++)
                {
                    Tuple<int, int> addMatch = new Tuple<int, int>(i, ListStringDownMatch(new List<string>(paths[i]), new List<string>(queueFile)));
                    if (addMatch.Item2 > 0)
                    {
                        matches.Add(addMatch);
                    }
                }

                matches.Sort((x, y) => y.Item2.CompareTo(x.Item2));

                if (matches.Count > 0)
                {
                    MusicFile foundSong = new MusicFile(mf)
                    {
                        QueueIndex = queueIndices[matches[0].Item1]
                    };
                    foundSongs.Add(foundSong);
                    paths.RemoveAt(matches[0].Item1);
                    queueIndices.RemoveAt(matches[0].Item1);
                }
            }
            
            foundSongs.Sort((x, y) => x.QueueIndex.CompareTo(y.QueueIndex));

            if (overrideName != string.Empty)
            {
                snapshotName = overrideName;
            }

            int qIDx = 1;

            foreach (MusicFile mf in foundSongs)
            {
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    var sql = "INSERT INTO QUEUE_SNAPSHOT (ID, ALBUM_ID, SONG_ID, QUEUEINDEX, SNAPSHOTNAME, SNAPSHOT_DATE) VALUES( " + Environment.NewLine +
                                 id + ", " + Environment.NewLine +
                                 "(SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + albumName.Replace("'", "''") + "'), " + Environment.NewLine +
                                 mf.ID + ", " + Environment.NewLine +
                                 qIDx++ + ", " + Environment.NewLine +
                                 "'" + snapshotName.Replace("'", "''") + "', DATETIME('" + snapShotDate + "')) " + Environment.NewLine;

                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }

            return true;
        }

        public class MusicFileEntry
        {
            public string Path;
            public string Filename;
            public int Id;
            public MusicFileEntry(string path, string filename, int id)
            {
                Path = path;
                Filename = filename;
                Id = id;
            }
        }

        public static void AddFileToDb(List<MusicFile> addFiles, SQLiteConnection conn)
        {
            if (addFiles.Count == 0)
            {
                return;
            }

            DatabaseProgressThreadSafe(new DatabaseEventArgs(0, addFiles.Count, DatabaseEventType.Started));

            string sql = string.Empty;

            for (int i = 0; i < addFiles.Count; i++)
            {
                addFiles[i].LoadTag();
                if ((i % 50) == 0)
                {
                    DatabaseProgressThreadSafe(new DatabaseEventArgs(i, addFiles.Count, DatabaseEventType.LoadMeta));
                }
            }
            DatabaseProgressThreadSafe(new DatabaseEventArgs(addFiles.Count, addFiles.Count, DatabaseEventType.LoadMeta));


            for (int i = 0; i < addFiles.Count; i++)
            {
                sql += UpdateSongSql(addFiles[i]);
                if ((i % 200) == 0 && i != 0)
                {
                    ExecuteTransaction(sql, conn);
                    sql = string.Empty;
                    DatabaseProgressThreadSafe(new DatabaseEventArgs(i, addFiles.Count, DatabaseEventType.UpdateSongDb));
                }
            }
            if (sql != string.Empty)
            {
                ExecuteTransaction(sql, conn);
            }
            DatabaseProgressThreadSafe(new DatabaseEventArgs(addFiles.Count, addFiles.Count, DatabaseEventType.UpdateSongDb));

            sql = string.Empty;

            for (int i = 0; i < addFiles.Count; i++)
            {
//                addFiles[i].LoadTag();
                sql += InsertSongSql(addFiles[i]);
                if ((i % 200) == 0 && i != 0)
                {
                    ExecuteTransaction(sql, conn);
                    sql = string.Empty;
                    DatabaseProgressThreadSafe(new DatabaseEventArgs(i, addFiles.Count, DatabaseEventType.InsertSongDb));
                }
            }
            if (sql != string.Empty)
            {
                ExecuteTransaction(sql, conn);
            }
            DatabaseProgressThreadSafe(new DatabaseEventArgs(addFiles.Count, addFiles.Count, DatabaseEventType.InsertSongDb));
            DatabaseProgressThreadSafe(new DatabaseEventArgs(addFiles.Count, addFiles.Count, DatabaseEventType.Stopped));
        }

        private static string UpdateSongSql(MusicFile mf)
        {
            return $"UPDATE SONG SET FILENAME = {QS(mf.FullFileName)} WHERE FILENAME <> {QS(mf.FullFileName)} AND " +
                   $"FILENAME_NOPATH = {QS(mf.FileNameNoPath)} AND FILESIZE = {mf.FileSize}; ";
        }

        private static string InsertSongSql(MusicFile mf)
        {
            return
                string.Join(Environment.NewLine,
                    "INSERT INTO SONG(FILENAME, ARTIST, ALBUM, TRACK, YEAR, RATING, NPLAYED_RAND, ",
                    "NPLAYED_USER, FILESIZE, VOLUME, OVERRIDE_NAME, TAGFINDSTR, TAGREAD, FILENAME_NOPATH, TITLE) ",
                    $"SELECT {QS(mf.FullFileName)}, {QS(mf.Artist)}, ",
                    $"{QS(mf.Album)}, {QS(mf.Track)}, {QS(mf.Year)}, 500, 0, 0, {mf.FileSize}, 1.0, {QS(mf.OverrideName)}, ",
                    $"{QS(mf.TagString)}, 1, {QS(mf.FileNameNoPath)}, {QS(mf.Title)} ",
                    $"WHERE NOT EXISTS(SELECT 1 FROM SONG WHERE FILENAME = {QS(mf.FullFileName)}); ");
        }

        public static bool ExecuteTransaction(string sql, SQLiteConnection conn)
        {
            try
            {
                using (SQLiteTransaction trans = conn.BeginTransaction())
                {                   
                    using (SQLiteCommand command = new SQLiteCommand(conn))
                    {
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int AddDefaultAlbum(string name, SQLiteConnection conn)
        {
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "INSERT INTO ALBUM(ID, ALBUMNAME) " +
                                      "SELECT 1, '" + name.Replace("'", "''") + "' " +
                                      "WHERE NOT EXISTS(SELECT 1 FROM ALBUM WHERE ID = 1); " +
                                      "UPDATE ALBUM SET ALBUMNAME = '" + name.Replace("'", "''") + "' WHERE ID = 1; ";
                command.ExecuteNonQuery();

            }
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + name.Replace("'", "''") + "' ";
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    try
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("SQLite error: '" + ex.Message + "'.");
                    }
                }
            }
            return -1;
        }

        public static int AddNewAlbum(string name, SQLiteConnection conn)
        {
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "INSERT INTO ALBUM(ALBUMNAME) " +
                                      "SELECT '" + name.Replace("'", "''") + "' " +
                                      "WHERE NOT EXISTS(SELECT 1 FROM ALBUM WHERE ALBUMNAME = '" + name.Replace("'", "''") + "') ";
                command.ExecuteNonQuery();

            }
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText = "SELECT ID FROM ALBUM WHERE ALBUMNAME = '" + name.Replace("'", "''") + "' ";
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    try {
                        if (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }
                    } 
                    catch (Exception ex)
                    {
                        throw new Exception("SQLite error: '" + ex.Message + "'.");
                    }
                }
            }
            return -1;
        }
    }
}