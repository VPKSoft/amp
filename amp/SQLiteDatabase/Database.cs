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
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using amp.SQLiteDatabase.ContainerClasses;
using amp.UtilityClasses;
using VPKSoft.ErrorLogger;

namespace amp.SQLiteDatabase;

/// <summary>
/// A class representing an album in the database.
/// </summary>
public class Album
{
    /// <summary>
    /// Gets the database identifier of the album.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the name of the album.
    /// </summary>
    public string AlbumName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Album"/> class.
    /// </summary>
    /// <param name="id">The database identifier of the album.</param>
    /// <param name="name">The name of the album.</param>
    public Album(int id, string name)
    {
        AlbumName = name;
        Id = id;
    }
}

/// <summary>
/// An enumeration describing a type of a database event.
/// </summary>
[Flags]
public enum DatabaseEventType
{
    /// <summary>
    /// A database operation started.
    /// </summary>
    Started = 0,

    /// <summary>
    /// A database operation stopped.
    /// </summary>
    Stopped = 1,

    /// <summary>
    /// A song tag information is being fetched from the database.
    /// </summary>
    [Obsolete("No in use in the current design anymore.")]
    // ReSharper disable once UnusedMember.Global
    GetSongTag = 2,

    /// <summary>
    /// A song is being updated into the database.
    /// </summary>
    UpdateSongDb = 4,

    /// <summary>
    /// A song is being inserted into the database.
    /// </summary>
    InsertSongDb = 8,

    /// <summary>
    /// A song is being inserted into an album in the database.
    /// </summary>
    InsertSongAlbum = 16,

    /// <summary>
    /// An ID number is being fetched for a song in the database.
    /// </summary>
    GetSongId = 32,

    /// <summary>
    /// Meta data is being loaded from the database.
    /// </summary>
    LoadMeta = 64,

    /// <summary>
    /// The database is being queried.
    /// </summary>
    QueryDb = 128
}

/// <summary>
/// Event arguments for the <see cref="Database.DatabaseProgress"/> event.
/// Implements the <see cref="System.EventArgs" />
/// </summary>
/// <seealso cref="System.EventArgs" />
public class DatabaseEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the progress of the database operation.
    /// </summary>
    /// <value>The progress.</value>
    public int Progress { get; set; }

    /// <summary>
    /// Gets or sets the progress end value of the database operation.
    /// </summary>
    public int ProgressEnd { get; set; }

    /// <summary>
    /// Gets or sets the type of the database operation.
    /// </summary>
    public DatabaseEventType EventType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseEventArgs"/> class.
    /// </summary>
    /// <param name="progress">The current progress of a database operation.</param>
    /// <param name="progressEnd">The progress end value of a database operation.</param>
    /// <param name="eventType">Type of the database operation.</param>
    public DatabaseEventArgs(int progress, int progressEnd, DatabaseEventType eventType)
    {
        Progress = progress;
        ProgressEnd = progressEnd;
        EventType = eventType;
    }
}

/// <summary>
/// A class for most of the database handling for the software.
/// Implements the <see cref="T:amp.SQLiteDatabase.DatabaseHelpers"/>
/// </summary>
public class Database : DatabaseHelpers
{
    // initialize a System.Windows.Forms SynchronizationContext..
    private static readonly SynchronizationContext Context = SynchronizationContext.Current ?? new SynchronizationContext();

    /// <summary>
    /// A delegate for the <see cref="DatabaseProgress"/> event.
    /// </summary>
    /// <param name="e">The <see cref="DatabaseEventArgs"/> instance containing the event data.</param>
    public delegate void OnDatabaseProgress(DatabaseEventArgs e);

    /// <summary>
    /// Occurs when a database operation is in progress or ending.
    /// </summary>
    public static event OnDatabaseProgress DatabaseProgress;

    /// <summary>
    /// Raises the <see cref="DatabaseProgress"/> event.
    /// </summary>
    /// <param name="state">A <see cref="DatabaseEventArgs"/> class instance for the event.</param>
    private static void OnDatabaseProgressThreadSafe(object state)
    {
        DatabaseProgress?.Invoke(state as DatabaseEventArgs);
    }

    /// <summary>
    /// Raises the <see cref="DatabaseProgress"/> event thread-safely.
    /// </summary>
    /// <param name="e">The <see cref="DatabaseEventArgs"/> instance containing the event data.</param>
    private static void DatabaseProgressThreadSafe(DatabaseEventArgs e)
    {
        // send via threading context..
        Context.Send(OnDatabaseProgressThreadSafe, e);
    }

    /// <summary>
    /// Gets the albums from the database.
    /// </summary>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <returns>A list of <see cref="Album"/> instances.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static List<Album> GetAlbums(SQLiteConnection conn)
    {
        List<Album> result = new List<Album>();
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText =
                string.Join(Environment.NewLine,
                    "SELECT ID, ALBUMNAME FROM ALBUM",
                    "WHERE ID >= 1",
                    "ORDER BY ALBUMNAME COLLATE NOCASE");
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Album(reader.GetInt32(0), reader.GetString(1)));
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the album by it's identifier.
    /// </summary>
    /// <param name="connection">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <param name="identifier">The album identifier.</param>
    /// <returns>The name of the album.</returns>
    public static string GetAlbumByIdentifier(SQLiteConnection connection, int identifier)
    {
        // ReSharper disable once StringLiteralTypo, database field..
        var result = GetScalar<string>($"SELECT ALBUMNAME FROM ALBUM WHERE ID = {identifier}", connection);
        return result;
    }

    /// <summary>
    /// Gets the name of the album by it's identifier.
    /// </summary>
    /// <param name="connection">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <param name="albumName">The name of the album.</param>
    /// <returns>The identifier of the album.</returns>
    public static int GetAlbumIdentifierByName(SQLiteConnection connection, string albumName)
    {
        // ReSharper disable once StringLiteralTypo, database field..
        var result = (int)GetScalar<long>($"SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)}", connection);
        return result;
    }

    /// <summary>
    /// Gets the name of the of the default album.
    /// </summary>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <returns>A name of the default album.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static string GetDefaultAlbumName(SQLiteConnection conn)
    {
        string sql =
            string.Join(Environment.NewLine,
                "SELECT ALBUMNAME FROM ALBUM WHERE ID = 1;");

        using (SQLiteCommand command = new SQLiteCommand(sql, conn))
        {
            return (string)command.ExecuteScalar();
        }
    }

    /// <summary>
    /// Deletes an album from the database with a given <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the album to delete.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <returns><c>true</c> if the album was successfully deleted, <c>false</c> otherwise.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static bool DeleteAlbum(string name, SQLiteConnection conn)
    {
        string sql =
            string.Join(Environment.NewLine,
                $"SELECT COUNT(*) FROM ALBUM WHERE ALBUMNAME = {QS(name)} AND ID NOT IN (0, 1);");

        using (SQLiteCommand command = new SQLiteCommand(sql, conn))
        {
            if ((long)command.ExecuteScalar() == 0)
            {
                return false;
            }
        }

        sql =
            string.Join(Environment.NewLine,
                $"DELETE FROM ALBUMSONGS WHERE ALBUM_ID IN (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)} AND ID NOT IN (0, 1));",
                $"DELETE FROM ALBUM WHERE ALBUMNAME = {QS(name)} AND ID NOT IN (0, 1);");

        using (SQLiteCommand command = new SQLiteCommand(sql, conn))
        {
            command.ExecuteNonQuery();
        }

        return true;
    }

    /// <summary>
    /// Gets the ID numbers for songs without an ID number from the database.
    /// </summary>
    /// <param name="noIdSongs">A list of songs without a database ID number.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
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
            sql += QS(mf.FullFileName) + ", ";
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

    // a field for the AlbumChanged property..
    private static bool albumChanged;

    /// <summary>
    /// Gets a value indicating whether the album changed during a database operation.
    /// Note: The property resets to false once its value is read.
    /// </summary>
    /// <value><c>true</c> if the album changed during a database operation; otherwise, <c>false</c>.</value>
    public static bool AlbumChanged
    {
        get
        {
            bool tmp = albumChanged;
            albumChanged = false;
            return tmp;
        }
    }

    /// <summary>
    /// Adds songs to album.
    /// </summary>
    /// <param name="name">The name of the album to add the songs to.</param>
    /// <param name="addSongs">A list of <see cref="MusicFile"/> class instances to add to an album.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
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
            command.CommandText = $"SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)} ";
            oAlbumId = command.ExecuteScalar();
        }

        for (int i = 0; i < addSongs.Count; i++)
        {
            sql += string.Join(Environment.NewLine,
                "INSERT INTO ALBUMSONGS (ALBUM_ID, SONG_ID, QUEUEINDEX)",
                $"SELECT {oAlbumId}, {addSongs[i].ID}, 0 ",
                "WHERE NOT EXISTS(SELECT * FROM ALBUMSONGS WHERE " +
                $"ALBUM_ID = {oAlbumId} AND SONG_ID = {addSongs[i].ID}); ");
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

    /// <summary>
    /// Removes songs from album.
    /// </summary>
    /// <param name="name">The name of the album to add the songs to.</param>
    /// <param name="musicFiles">A list of <see cref="MusicFile"/> class instances to add to an album.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
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

    /// <summary>
    /// Clears the temporary album from the database.
    /// </summary>
    /// <param name="playList">A reference to list of <see cref="MusicFile"/> class instances to be cleared also.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    public static void ClearTmpAlbum(ref List<MusicFile> playList, SQLiteConnection conn)
    {
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            playList.Clear();
            command.CommandText = "DELETE FROM ALBUMSONGS WHERE ALBUM_ID = 0 ";
            command.ExecuteNonQuery();
        }
    }



    /// <summary>
    /// Generates a SQL sentence for getting an album with a given <paramref name="name"/> from the database.
    /// </summary>
    /// <param name="name">The name of the album.</param>
    /// <param name="savedQueueId">An optional saved queue id to which the songs need to belong to.</param>
    /// <returns>A SQL sentence generated with the given parameters.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static string GetAlbumSql(string name, int savedQueueId = -1)
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
                "IFNULL(S.NPLAYED_USER, 0) AS NPLAYED_USER, ", // 16
                "SongImage, ", // 17
                "(SELECT LENGTH(CAST(SongImage AS BLOB))) AS ImageSize ", // 18
                "FROM ",
                "SONG S, ALBUMSONGS A ",
                "WHERE ",
                "S.ID = A.SONG_ID");

        if (savedQueueId != -1)
        {
            result += " AND " +
                      string.Join(Environment.NewLine,
                          $"A.SONG_ID IN (SELECT SONG_ID FROM QUEUE_SNAPSHOT WHERE ID = {savedQueueId})");
        }

        if (!string.IsNullOrEmpty(name))
        {
            result += " AND " +
                      string.Join(Environment.NewLine,
                          $"A.SONG_ID IN(SELECT SONG_ID FROM ALBUMSONGS WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)})) AND ",
                          $"A.ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)})");
        }

        result += " GROUP BY S.ID ";

        return result;
    }

    /// <summary>
    /// Gets an album from the database.
    /// </summary>
    /// <param name="name">The name of the album.</param>
    /// <param name="playList">A reference to list of <see cref="MusicFile"/> class instances to fill with the results from the database.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static void GetAlbum(string name, ref List<MusicFile> playList, SQLiteConnection conn)
    {
        List<int> indices = new List<int>();
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            playList.Clear();

            command.CommandText = $"SELECT COUNT(*) FROM ALBUMSONGS WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)})";
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

                    MusicFile mf = MusicFile.FromDataReader(reader, counter++);

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
                foreach (var index in indices)
                {
                    if (mf.VisualIndex == index)
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

    /// <summary>
    /// Saves the volume (playback) to the database of a given <see cref="MusicFile"/>.
    /// </summary>
    /// <param name="mf">The <see cref="MusicFile"/> which volume to update to the database.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    public static void SaveVolume(MusicFile mf, SQLiteConnection conn)
    {
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText = $"UPDATE SONG SET VOLUME = {DS(mf.Volume)} WHERE ID = {mf.ID} ";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Updates the image of a specified musing file into the database.
    /// </summary>
    /// <param name="mf">The <see cref="MusicFile"/> which image to update to the database.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    public static void SaveImage(MusicFile mf, SQLiteConnection conn)
    {
        using var command = new SQLiteCommand(conn)
        {
            CommandText = $"UPDATE SONG SET SongImage = @IMAGE WHERE ID = {mf.ID} "
        };

        using var memoryStream = new MemoryStream();

        if (mf.SongImage == null) // if null the DBNull..
        {
            command.Parameters.Add("@IMAGE", System.Data.DbType.Binary).Value = null;
        }
        else
        {
            // save the image to a memory stream..
            mf.SongImage.Save(memoryStream, ImageFormat.Png);

            // ..and ad it's data to the parameter as a byte array..
            command.Parameters.Add("@IMAGE", System.Data.DbType.Binary).Value = memoryStream.ToArray();
        }

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Sets an override name for a given <see cref="MusicFile"/> reference.
    /// </summary>
    /// <param name="mf">The reference to a <see cref="MusicFile"/> which override name to set.</param>
    /// <param name="newName">The new override name for the song.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
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

    /// <summary>
    /// Saves the current queue into the database so it will be remembered next time the software starts.
    /// </summary>
    /// <param name="files">A list of <see cref="MusicFile"/> containing the album.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <param name="albumName">The name of the album the music files belong to.</param>
    public static void SaveQueue(List<MusicFile> files, SQLiteConnection conn, string albumName)
    {
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText =
                $"UPDATE ALBUMSONGS SET QUEUEINDEX = 0 WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)})";
            command.ExecuteNonQuery();
        }
        foreach (MusicFile mf in files)
        {
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                if (mf.QueueIndex > 0)
                {
                    command.CommandText =
                        $"UPDATE ALBUMSONGS SET QUEUEINDEX = {mf.QueueIndex} WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)}) AND SONG_ID = {mf.ID}";
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    /// <summary>
    /// Loads a queue snapshot with a given <paramref name="queueIndex"/> (SQL ID field) from the database.
    /// </summary>
    /// <param name="files">A reference to list of <see cref="MusicFile"/> class instances to fill with the queue snapshot contents with.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <param name="queueIndex">The queue index (SQL ID number in the database) for the queue snapshot</param>
    /// <param name="append">A value indicating whether the queue snapshot should be appended to the <paramref name="files"/>.</param>
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
            command.CommandText = $"SELECT SONG_ID, QUEUEINDEX FROM QUEUE_SNAPSHOT WHERE ID = {queueIndex} ORDER BY QUEUEINDEX";
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

    /// <summary>
    /// Saves a given list of music files to a queue snapshot.
    /// </summary>
    /// <param name="files">A list of <see cref="MusicFile"/> class instances save to a queue snapshot.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <param name="albumName">The name of the album the queue snapshot belongs to.</param>
    /// <param name="snapshotName">The name for the queue snapshot to be saved into the database.</param>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
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
                            string.Join(Environment.NewLine,
                                "INSERT INTO QUEUE_SNAPSHOT (ID, ALBUM_ID, SONG_ID, QUEUEINDEX, SNAPSHOTNAME) VALUES(",
                                $"{id},",
                                $"(SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)}),",
                                $"{mf.ID}, {mf.AlternateQueueIndex},",
                                $"{QS(snapshotName)})");

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
                            string.Join(Environment.NewLine,
                                "INSERT INTO QUEUE_SNAPSHOT (ID, ALBUM_ID, SONG_ID, QUEUEINDEX, SNAPSHOTNAME) VALUES(",
                                $"{id},",
                                $"(SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)}),",
                                $"{mf.ID}, {mf.QueueIndex},",
                                $"{QS(snapshotName)})");

                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Matches a two lists of strings downwards using the length of the smaller list.
    /// </summary>
    /// <param name="list1">The first list of strings.</param>
    /// <param name="list2">The second list of strings.</param>
    /// <returns>A count of how many strings with the lists matched with each other in reversed order.</returns>
    public static int ListStringDownMatch(List<string> list1, List<string> list2)
    {
        int min = Math.Min(list1.Count, list2.Count);
        list1.Reverse();
        list2.Reverse();
        int result = 0;
        for (int i = 0; i < min; i++)
        {
            if (list1[i] == list2[i])
            {
                result++;
            }
            else
            {
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Saves a queue snapshot into a file.
    /// </summary>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <param name="id">The SQL ID number of the queue snapshot.</param>
    /// <param name="queueFileName">The file name to which the queue snapshot should be saved into.</param>
    /// <returns>True if the operation was successful; otherwise false.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static bool SaveQueueSnapshotToFile(SQLiteConnection conn, int id, string queueFileName)
    {
        try
        {
            List<string> lines = new List<string>();
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                command.CommandText =
                    string.Join(Environment.NewLine,
                        "SELECT",
                        "(SELECT ALBUMNAME FROM ALBUM WHERE ID = ALBUM_ID) AS ALBUMNAME,",
                        "(SELECT FILENAME FROM SONG WHERE ID = SONG_ID) AS FILENAME,",
                        "QUEUEINDEX, SNAPSHOTNAME, SNAPSHOT_DATE",
                        $"FROM QUEUE_SNAPSHOT WHERE ID = {id}");

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

    /// <summary>
    /// Gets a queue snapshot name from a given file.
    /// </summary>
    /// <param name="queueFileName">The file name containing the queue snapshot to get the snapshot name from.</param>
    /// <returns>A queue snapshot name if the operation was successful; otherwise <see cref="string.Empty"/>.</returns>
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

    /// <summary>
    /// Restores a queue snapshot from a file into the database.
    /// </summary>
    /// <param name="files">A list of <see cref="MusicFile"/> entries (an album contents) to compare the entries in the <paramref name="queueFileName"/> file.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <param name="albumName">The name of the album.</param>
    /// <param name="queueFileName">The name of the file containing the queue snapshot.</param>
    /// <param name="overrideName">An override for the snap shot name in the file. This value can be a empty string.</param>
    /// <returns>True if the snapshot was successfully inserted into the database; otherwise false.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
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
            ExceptionLogger.LogError(ex);
            return false;
        }

        // select datetime('2017-08-01 10:12:11')

        List<Tuple<int, int>> matches = new List<Tuple<int, int>>();
        List<MusicFile> foundSongs = new List<MusicFile>();
        foreach (MusicFile mf in files)
        {
            if (paths.Count == 0)
            {
                break;
            }

            var queueFile = new List<string>(Regex.Replace(mf.FullFileName, "^[A-Za-z][:]?", string.Empty)
                .TrimStart('\\').Split('\\'));

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

        if (overrideName.Trim() != string.Empty)
        {
            snapshotName = overrideName;
        }

        int qIDx = 1;

        foreach (MusicFile mf in foundSongs)
        {
            using (SQLiteCommand command = new SQLiteCommand(conn))
            {
                var sql =
                    string.Join(Environment.NewLine,
                        "INSERT INTO QUEUE_SNAPSHOT (ID, ALBUM_ID, SONG_ID, QUEUEINDEX, SNAPSHOTNAME, SNAPSHOT_DATE) VALUES(",
                        $"{id},",
                        $"(SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)}),",
                        $"{mf.ID}, {qIDx++},{QS(snapshotName)},",
                        $"DATETIME('{snapShotDate}'))");


                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        return true;
    }

    /// <summary>
    /// Adds music files into the database.
    /// </summary>
    /// <param name="addFiles">A list of <see cref="MusicFile"/> entries to add to the database.</param>
    /// <param name="conn">An instance to a <see cref="SQLiteConnection"/> for the query.</param>
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

        // some (invalid) encoding in the file name seems to create duplicates..
        ExecuteTransaction(CleanUpDuplicates, conn);

        DatabaseProgressThreadSafe(new DatabaseEventArgs(addFiles.Count, addFiles.Count, DatabaseEventType.InsertSongDb));
        DatabaseProgressThreadSafe(new DatabaseEventArgs(addFiles.Count, addFiles.Count, DatabaseEventType.Stopped));
    }

    /// <summary>
    /// Generates an update SQL sentence for a given <see cref="MusicFile"/>.
    /// </summary>
    /// <param name="mf">The music file instance for which to generate the SQL sentence for.</param>
    /// <returns>A generated SQL sentence with the given parameter(s).</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static string UpdateSongSql(MusicFile mf)
    {
        string sql =
            $"UPDATE SONG SET FILENAME = {QS(mf.FullFileName)} WHERE FILENAME <> {QS(mf.FullFileName)} AND " +
            $"FILENAME_NOPATH = {QS(mf.FileNameNoPath)} AND FILESIZE = {mf.FileSize}; ";

        return sql;
    }

    /// <summary>
    /// Gets a SQL sentence to clean up duplicates from the database.
    /// </summary>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static string CleanUpDuplicates
    {
        get
        {
            string sql =
                string.Join(Environment.NewLine,
                    "CREATE TABLE IF NOT EXISTS TMPSONG AS",
                    "SELECT FILENAME, MIN(ID) AS ID FROM SONG",
                    "GROUP BY FILENAME",
                    "HAVING COUNT(FILENAME) > 1;",
                    "",
                    "DELETE FROM SONG",
                    "WHERE FILENAME =",
                    "(SELECT FILENAME FROM TMPSONG WHERE TMPSONG.FILENAME = SONG.FILENAME AND TMPSONG.ID <> SONG.ID);",
                    "",
                    "DELETE FROM ALBUMSONGS WHERE SONG_ID NOT IN (SELECT ID FROM SONG);",
                    "",
                    "DELETE FROM QUEUE_SNAPSHOT WHERE SONG_ID NOT IN (SELECT ID FROM SONG);",
                    "",
                    "DROP TABLE IF EXISTS TMPSONG;");
            return sql;
        }
    }

    /// <summary>
    /// Generates an insert SQL sentence for a given <see cref="MusicFile"/>.
    /// </summary>
    /// <param name="mf">The music file instance for which to generate the SQL sentence for.</param>
    /// <returns>A generated SQL sentence with the given parameter(s).</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static string InsertSongSql(MusicFile mf)
    {
        string sql =
            string.Join(Environment.NewLine,
                "INSERT INTO SONG(FILENAME, ARTIST, ALBUM, TRACK, YEAR, RATING, NPLAYED_RAND, ",
                "NPLAYED_USER, FILESIZE, VOLUME, OVERRIDE_NAME, TAGFINDSTR, TAGREAD, FILENAME_NOPATH, TITLE) ",
                $"SELECT {QS(mf.FullFileName)}, {QS(mf.Artist)}, ",
                $"{QS(mf.Album)}, {QS(mf.Track)}, {QS(mf.Year)}, 500, 0, 0, {mf.FileSize}, 1.0, {QS(mf.OverrideName)}, ",
                $"{QS(mf.TagString)}, 1, {QS(mf.FileNameNoPath)}, {QS(mf.Title)} ",
                $"WHERE NOT EXISTS(SELECT * FROM SONG WHERE FILENAME = {QS(mf.FullFileName)}); ");

        return sql;
    }

    /// <summary>
    /// Executes a given SQL sentence against the database.
    /// </summary>
    /// <param name="sql">The SQL sentence to execute.</param>
    /// <param name="conn">An open <see cref="SQLiteConnection"/> class instance to be used with the database operation.</param>
    /// <returns>True if the operation was successful; otherwise false.</returns>
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
        catch (Exception ex)
        {
            // log the exception..
            ExceptionLogger.LogError(ex);
            return false;
        }
    }

    /// <summary>
    /// Inserts the default album (ID = 1) into the database with a given name.
    /// </summary>
    /// <param name="name">The localized name of the default database.</param>
    /// <param name="conn">An instance to a <see cref="SQLiteConnection"/> for the query.</param>
    /// <returns></returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static int AddDefaultAlbum(string name, SQLiteConnection conn)
    {
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText =
                string.Join(Environment.NewLine,
                    "INSERT INTO ALBUM(ID, ALBUMNAME)",
                    $"SELECT 1, {QS(name)}",
                    "WHERE NOT EXISTS(SELECT * FROM ALBUM WHERE ID = 1);",
                    $"UPDATE ALBUM SET ALBUMNAME = {QS(name)} WHERE ID = 1;");
            command.ExecuteNonQuery();

        }
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText =
                $"SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)} ";
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

    /// <summary>
    /// Adds a new album with a given <paramref name="name"/> into the database.
    /// </summary>
    /// <param name="name">The name of the album to add.</param>
    /// <param name="conn">An instance to a <see cref="SQLiteConnection"/> for the query.</param>
    /// <returns>A SQL ID number for the album if the operation was successful; otherwise -1.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static int AddNewAlbum(string name, SQLiteConnection conn)
    {
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText =
                string.Join(Environment.NewLine,
                    "INSERT INTO ALBUM(ALBUMNAME) ",
                    $"SELECT {QS(name)}",
                    $"WHERE NOT EXISTS(SELECT * FROM ALBUM WHERE ALBUMNAME = {QS(name)})");
            command.ExecuteNonQuery();

        }
        using (SQLiteCommand command = new SQLiteCommand(conn))
        {
            command.CommandText =
                $"SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(name)}";

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
                    // log the exception..
                    ExceptionLogger.LogError(ex);
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the saved queues belonging to a specified album.
    /// </summary>
    /// <param name="albumName">Name of the album.</param>
    /// <param name="conn">The SQLite connection.</param>
    /// <returns>A List&lt;SavedQueue&gt; class instances.</returns>
    public static List<SavedQueue> GetAlbumQueues(string albumName, SQLiteConnection conn)
    {
        var result = new List<SavedQueue>();

        using SQLiteCommand command = new SQLiteCommand(conn);
        command.CommandText =
            string.Join(Environment.NewLine,
                "SELECT COUNT(DISTINCT ID) FROM",
                // ReSharper disable once StringLiteralTypo
                $"QUEUE_SNAPSHOT WHERE ALBUM_ID = (SELECT ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)})");

        var countTotal = Convert.ToInt32(command.ExecuteScalar());

        if (countTotal == 0)
        {
            return result;
        }

        command.CommandText =
            string.Join(Environment.NewLine,
                // ReSharper disable once StringLiteralTypo
                "SELECT ID, SNAPSHOTNAME, MAX(SNAPSHOT_DATE) AS SNAPSHOT_DATE",
                // ReSharper disable once StringLiteralTypo
                $"FROM QUEUE_SNAPSHOT WHERE ALBUM_ID = (SELECT ALBUM_ID FROM ALBUM WHERE ALBUMNAME = {QS(albumName)})",
                // ReSharper disable once StringLiteralTypo
                "GROUP BY ID, SNAPSHOTNAME",
                "ORDER BY MAX(SNAPSHOT_DATE)");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new SavedQueue
            {
                Id = reader.GetInt32(0),
                AlbumName = albumName,
                QueueName = reader.GetString(1),
                CountTotal = countTotal,
                CreteDate = DateTime.ParseExact(reader.GetString(2), "yyyy-MM-dd HH':'mm':'ss", CultureInfo.InvariantCulture),
                QueueSongs = new List<MusicFile>(),
            });
        }


        foreach (var savedQueue in result)
        {
            using var songCommand = new SQLiteCommand(GetAlbumSql(albumName, savedQueue.Id), conn);
            using var songReader = songCommand.ExecuteReader();
            while (songReader.Read())
            {
                savedQueue.QueueSongs.Add(MusicFile.FromDataReader(songReader, 0));
            }
        }

        return result;
    }
}