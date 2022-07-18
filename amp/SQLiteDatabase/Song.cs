#region License
/*
MIT License

Copyright(c) 2020 Petteri Kautonen

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
using System.Linq;
using VPKSoft.ErrorLogger;

namespace amp.SQLiteDatabase;

/// <summary>
/// A class representing a single database entry in the SONG database table.
/// </summary>
public class Song: DatabaseHelpers
{
    /// <summary>
    /// Gets or sets the database ID number for the song.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the full file name for the song.
    /// </summary>
    public string Filename { get; set; }

    /// <summary>
    /// Gets opr sets the artist for the song.
    /// </summary>
    public string Artist { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the album of the song.
    /// </summary>
    public string Album { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the track number for the song.
    /// </summary>
    public string Track { get; set; } // can be NULL..
        
    /// <summary>
    /// Gets or sets the publishing year for the song.
    /// </summary>
    public string Year { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the lyrics for the song.
    /// </summary>
    public string Lyrics { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the rating of the song.
    /// </summary>
    public int? Rating { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the count of how many times a pseudo random generator has played the song.
    /// </summary>
    public int? PlayedRandomizedNumber { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the count of how many times a user has played the song.
    /// </summary>
    public int? PlayedByUserNumber { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the file size of the song.
    /// </summary>
    public int? FileSize { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the volume for the music file.
    /// </summary>
    public double Volume { get; set; }

    /// <summary>
    /// Gets or sets the override name for the song.
    /// </summary>
    public string OverrideName { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the tag string (a combined string of data in a IDvX Tag) for searching purposes.
    /// </summary>
    public string TagFindString { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets a value indicating whether the IDvX Tag has been read for the song.
    /// </summary>
    public bool? TagRead { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the file name without the path part for the song.
    /// </summary>
    public string FileNameNoPath { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets a value indicating how many times during playback the song was changed to another song.
    /// </summary>
    public int? SkippedEarly { get; set; } // can be NULL..

    /// <summary>
    /// Gets or sets the title for the song.
    /// </summary>
    public string Title { get; set; } // can be NULL..

    /// <summary>
    /// Gets a <see cref="Song"/> class instance from a given <see cref="SQLiteDataReader"/>.
    /// </summary>
    /// <param name="reader">A <see cref="SQLiteDataReader"/> class instance.</param>
    /// <returns>A song class instance read from the <paramref name="reader"/>.</returns>
    public static Song FromSqLiteDataReader(SQLiteDataReader reader)
    {
        try
        {
            var id = reader.GetInt64(0); // ID: 0
            var fileName = reader.GetString(1); // FILENAME: 1
            var artist = reader.IsDBNull(2) ? null : reader.GetString(2); // ARTIST: 2
            var album = reader.IsDBNull(3) ? null : reader.GetString(3); // ALBUM: 3
            var track = reader.IsDBNull(4) ? null : reader.GetString(4); // TRACK: 4
            var year = reader.IsDBNull(5) ? null : reader.GetString(5); // YEAR: 5
            var lyrics = reader.IsDBNull(6) ? null : reader.GetString(6); // LYRICS: 6
            var rating = reader.IsDBNull(7) ? null : (int?) reader.GetInt32(7); // RATING: 7
            // ReSharper disable once CommentTypo
            var playedRandomizedNumber = reader.IsDBNull(8) ? null : (int?) reader.GetInt32(8); // NPLAYED_RAND: 8
            // ReSharper disable once CommentTypo
            var playedByUserNumber = reader.IsDBNull(9) ? null : (int?) reader.GetInt32(9); // NPLAYED_USER: 9
            // ReSharper disable once CommentTypo
            var fileSize = reader.IsDBNull(10) ? null : (int?) reader.GetInt32(10); // FILESIZE: 10
            var volume = reader.GetFloat(11); // VOLUME: 11
            var overrideName = reader.IsDBNull(12) ? null : reader.GetString(12); // OVERRIDE_NAME: 12
            // ReSharper disable once CommentTypo
            var tagFindString = reader.IsDBNull(13) ? null : reader.GetString(13); // TAGFINDSTR: 13
            // ReSharper disable once CommentTypo
            var tagRead = reader.IsDBNull(14) ? null : (bool?) (reader.GetInt32(14) == 1); // TAGREAD: 14
            // ReSharper disable once CommentTypo
            var fileNameNoPath = reader.IsDBNull(15) ? null : reader.GetString(15); // FILENAME_NOPATH: 15
            var skippedEarly = reader.IsDBNull(16) ? null : (int?) reader.GetInt32(16); // SKIPPED_EARLY: 16
            var title = reader.IsDBNull(17) ? null : reader.GetString(15); // TITLE: 17

            Song result = new Song()
            {
                Id = id,
                Filename = fileName,
                Artist = artist,
                Album = album,
                Track = track,
                Year = year,
                Lyrics = lyrics,
                Rating = rating,
                PlayedRandomizedNumber = playedRandomizedNumber,
                PlayedByUserNumber = playedByUserNumber,
                FileSize = fileSize,
                Volume = volume,
                OverrideName = overrideName,
                TagFindString = tagFindString,
                TagRead = tagRead,
                FileNameNoPath = fileNameNoPath,
                SkippedEarly = skippedEarly,
                Title = title,
            };
            return result;
        }
        catch (Exception ex)
        {
            ExceptionLogger.LogError(ex);
            return null;
        }
    }

    /// <summary>
    /// Generates a SQL sentence to insert and update a song into the database.
    /// </summary>
    /// <param name="songTableName">The name of the database table containing the song.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    public string GenerateInsertUpdateSqlSentence(string songTableName)
    {
        return GenerateInsertUpdateSqlSentence(this, songTableName);
    }


    /// <summary>
    /// Generates a SQL sentence to insert and update a song into the database.
    /// </summary>
    /// <param name="historyTable">A value indicating whether the insert and update should be done into the SONG_HISTORY or the SONG database table.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    public string GenerateInsertUpdateSqlSentence(bool historyTable)
    {
        return GenerateInsertUpdateSqlSentence(this, historyTable ? "SONG_HISTORY" : "SONG");
    }


    /// <summary>
    /// Generates a SQL sentence to select all songs from the database.
    /// </summary>
    /// <param name="songTableName">The name of the database table containing the song.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static string GenerateSelectSentence(string songTableName)
    {
        string sql =
            string.Join(Environment.NewLine,
                "SELECT ID, FILENAME, ARTIST, ALBUM, TRACK, YEAR, LYRICS,",
                "RATING, NPLAYED_RAND, NPLAYED_USER, FILESIZE, VOLUME, OVERRIDE_NAME, " +
                "TAGFINDSTR, TAGREAD, FILENAME_NOPATH, SKIPPED_EARLY, TITLE",
                "FROM",
                $"{songTableName};");
        return sql;
    }

    /// <summary>
    /// Generates a SQL sentence to select all songs from the database.
    /// </summary>
    /// <param name="historyTable">A value indicating whether the use the SONG_HISTORY or the SONG database table.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    public static string GenerateSelectSentence(bool historyTable)
    {
        return GenerateSelectSentence(historyTable ? "SONG_HISTORY" : "SONG");
    }

    /// <summary>
    /// Generates a SQL sentence to select the count of all songs from the database.
    /// </summary>
    /// <param name="songTableName"></param>
    /// <returns></returns>
    public static string GenerateCountSentence(string songTableName)
    {
        string sql =
            string.Join(Environment.NewLine,
                $"SELECT COUNT(*) FROM {songTableName};");

        return sql;
    }

    /// <summary>
    /// Generates a SQL sentence to select the count of all songs from the database.
    /// </summary>
    /// <param name="historyTable">A value indicating whether the use the SONG_HISTORY or the SONG database table.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    public static string GenerateCountSentence(bool historyTable)
    {
        return GenerateCountSentence(historyTable ? "SONG_HISTORY" : "SONG");
    }

    /// <summary>
    /// Generates a SQL sentence to delete a given <see cref="Song"/> from the database.
    /// </summary>
    /// <param name="song">The song to delete from the database.</param>
    /// <param name="songTableName">The of the database table to delete the song from.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    public static string GenerateDeleteSentence(Song song, string songTableName)
    {
        string sql =
            string.Join(Environment.NewLine,
                "DELETE",
                "FROM",
                songTableName,
                "WHERE",
                $"ID = {song.Id};");

        return sql;
    }

    /// <summary>
    /// Generates a SQL sentence to delete a given <see cref="Song"/> from the database.
    /// </summary>
    /// <param name="song">The song to delete from the database.</param>
    /// <param name="historyTable">A value indicating whether the use the SONG_HISTORY or the SONG database table.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    public static string GenerateDeleteSentence(Song song, bool historyTable)
    {
        return GenerateDeleteSentence(song, historyTable ? "SONG_HISTORY" : "SONG");
    }

    /// <summary>
    /// Generates a SQL sentence to delete the <see cref="Song"/> instance from the database.
    /// </summary>
    /// <param name="historyTable">A value indicating whether the use the SONG_HISTORY or the SONG database table.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    public string GenerateDeleteSentence(bool historyTable)
    {
        return GenerateDeleteSentence(this, historyTable ? "SONG_HISTORY" : "SONG");
    }

    /// <summary>
    /// Generates a SQL sentence to delete the <see cref="Song"/> instance references from a given database table.
    /// </summary>
    /// <param name="songs">The songs which references to remove from a given database table.</param>
    /// <param name="tableName">The name of the table where the songs are identified via SONG_ID field.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    // ReSharper disable once StringLiteralTypo
    public static string GenerateDeleteSongsFromTable(List<Song> songs, string tableName)
    {
        string sql =
            string.Join(Environment.NewLine,
                "DELETE",
                "FROM",
                tableName,
                "WHERE",
                $"SONG_ID IN ({string.Join(", ", songs.Select(f => f.Id))});");

        return sql;
    }

    /// <summary>
    /// Generates a SQL sentence to insert and update a song into the database.
    /// </summary>
    /// <param name="song">An instance to a <see cref="Song"/> class.</param>
    /// <param name="songTableName">The name of the database table containing the song.</param>
    /// <returns>A SQL sentence formulated with the given parameters.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static string GenerateInsertUpdateSqlSentence(Song song, string songTableName)
    {
        string sql =
            string.Join(Environment.NewLine,
                $"INSERT INTO {songTableName}(FILENAME, ARTIST, ALBUM, TRACK, YEAR, LYRICS,",
                "RATING, NPLAYED_RAND, NPLAYED_USER, FILESIZE, OVERRIDE_NAME, TAGFINDSTR,",
                "TAGREAD, FILENAME_NOPATH, SKIPPED_EARLY, TITLE)",
                "SELECT", 
                $"{NI(song.Filename)},",
                $"{NI(song.Artist)},",
                $"{NI(song.Album)},",
                $"{NI(song.Track)},",
                $"{NI(song.Year)},",
                $"{NI(song.Lyrics)},",
                $"{NI(song.Rating)},",
                $"{NI(song.PlayedRandomizedNumber)},",
                $"{NI(song.PlayedByUserNumber)},",
                $"{NI(song.FileSize)},",
                $"{NI(song.OverrideName)},",
                $"{NI(song.TagFindString)},",
                $"{NI(song.TagRead)},",
                $"{NI(song.FileNameNoPath)},",
                $"{NI(song.SkippedEarly)},",
                $"{NI(song.Title)}",
                $"WHERE NOT EXISTS(SELECT * FROM {songTableName} WHERE ID = {song.Id});");

        sql +=
            string.Join(Environment.NewLine, Environment.NewLine,
                $"UPDATE {songTableName}",
                "SET",
                $"FILENAME = {NI(song.Filename)},",
                $"ARTIST = {NI(song.Artist)},",
                $"ALBUM = {NI(song.Album)},",
                $"TRACK = {NI(song.Track)},",
                $"YEAR = {NI(song.Year)},",
                $"LYRICS = {NI(song.Lyrics)},",
                $"RATING = {NI(song.Rating)},",
                $"NPLAYED_RAND = {NI(song.PlayedRandomizedNumber)},",
                $"NPLAYED_USER = {NI(song.PlayedByUserNumber)},",
                $"FILESIZE = {NI(song.FileSize)},",
                $"OVERRIDE_NAME = {NI(song.OverrideName)},",
                $"TAGFINDSTR = {NI(song.TagFindString)},",
                $"TAGREAD = {NI(song.TagRead)},",
                $"FILENAME_NOPATH = {NI(song.FileNameNoPath)},",
                $"SKIPPED_EARLY = {NI(song.SkippedEarly)},",
                $"TITLE = {NI(song.Title)}",
                $"WHERE ID = {song.Id}");

        return sql;
    }
}