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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using amp.SQLiteDatabase;
using amp.UtilityClasses;
using Ookii.Dialogs.WinForms;
using VPKSoft.ErrorLogger;
using VPKSoft.Utils;

namespace amp.DataMigrate;

/// <summary>
/// A helper class for moving the music library contents (collected data) to another computer.
/// </summary>
public class DatabaseDataMigrate: DatabaseHelpers
{
    /// <summary>
    /// Gets the directories of the SONG database table.
    /// </summary>
    /// <param name="connection">The <see cref="SQLiteConnection"/> instance to access the database.</param>
    /// <param name="maxDepth">The maximum depth.</param>
    /// <returns>A distinct list of directories in the software's SONG database table.</returns>
    public static List<string> GetDirectories(SQLiteConnection connection, int maxDepth = -1)
    {
        List<string> result = new List<string>();

        string sql =
            string.Join(Environment.NewLine,
                // ReSharper disable once StringLiteralTypo
                "SELECT DISTINCT SUBSTR(FILENAME, 1, LENGTH(FILENAME) - LENGTH(FILENAME_NOPATH) - 1)",
                // ReSharper disable once StringLiteralTypo
                "COLLATE NOCASE",
                "FROM SONG;");

        using (SQLiteCommand command = new SQLiteCommand(sql, connection))
        {
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var path = reader.GetString(0);
                    string addPath = SplitPathStringMax(path, maxDepth);
                    result.Add(addPath);
                }
            }
        }

        return result.Distinct(StringComparer.InvariantCultureIgnoreCase).OrderBy(f => f).ToList();
    }

    /// <summary>
    /// Deletes songs which doesn't exist in the file system from the database.
    /// </summary>
    /// <param name="connection">The <see cref="SQLiteConnection"/> instance to access the database.</param>
    /// <returns>An instance to a <see cref="BackgroundWorker"/> class to handle the operation.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static BackgroundWorker DeleteNonExistingSongs(SQLiteConnection connection)
    {
        string sql =
            string.Join(Environment.NewLine,
                    
                "SELECT ID, FILENAME, ARTIST, ALBUM,", 
                "TRACK, YEAR, LYRICS, RATING, NPLAYED_RAND,",
                "NPLAYED_USER, FILESIZE, VOLUME, OVERRIDE_NAME,",
                "TAGFINDSTR, TAGREAD, FILENAME_NOPATH,",
                "SKIPPED_EARLY, TITLE FROM SONG;");

        BackgroundWorker worker = new BackgroundWorker
            {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

        worker.DoWork += (sender, args) =>
        {
            List<Song> orphanSongs = new List<Song>();

            long songCount = GetScalar<long>(Song.GenerateCountSentence(false), connection);

            int progress = 0;
            int previousProgress = -1;

            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Song song = Song.FromSqLiteDataReader(reader);

                        var percentage = CalculatePercentage(progress, songCount, 50);

                        if (percentage > previousProgress)
                        {
                            worker.ReportProgress(percentage);
                            previousProgress = percentage;
                        }

                        if (!File.Exists(song.Filename))
                        {
                            orphanSongs.Add(song);
                        }

                        progress++;

                        if (worker.CancellationPending)
                        {
                            return;
                        }
                    }
                }
            }

            previousProgress = -1;
            songCount = orphanSongs.Count;
            progress = 0;
            foreach (var orphanSong in orphanSongs)
            {
                var percentage = CalculatePercentage(progress, songCount, 50, 50);

                if (percentage > previousProgress)
                {
                    worker.ReportProgress(percentage);
                    previousProgress = percentage;
                }

                ExecuteSql(orphanSong.GenerateDeleteSentence(false), connection);

                progress++;

                if (worker.CancellationPending)
                {
                    return;
                }
            }

            if (orphanSongs.Count > 0)
            {
                ExecuteSql(Song.GenerateDeleteSongsFromTable(orphanSongs, "ALBUMSONGS"), connection);

                ExecuteSql(Song.GenerateDeleteSongsFromTable(orphanSongs, "QUEUE_SNAPSHOT"), connection);
            }

            worker.ReportProgress(100, orphanSongs.Count);
        };

        return worker;
    }

    private static int CalculatePercentage(double currentValue, double maximumValue, double partsOfHundred = 100, double addToHundred = 0)
    {
        if (maximumValue > 0)
        {
            return (int) (addToHundred + currentValue * partsOfHundred / maximumValue);
        }

        return 0;
    }

    /// <summary>
    /// Creates a <see cref="BackgroundWorker"/> class instance to update the song locations with a user selected path.
    /// </summary>
    /// <param name="folderBrowserDialog">An instance to <see cref="Ookii.Dialogs.WinForms.VistaFolderBrowserDialog"/> for the user to select the path.</param>
    /// <param name="connection">A SQLite connection to use for the procedure.</param>
    /// <returns>An instance to a <see cref="BackgroundWorker"/> class to handle the operation.</returns>
    public static BackgroundWorker UpdateSongLocations(
        VistaFolderBrowserDialog folderBrowserDialog, SQLiteConnection connection)
    {
        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
            BackgroundWorker worker = new BackgroundWorker
                {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

            worker.DoWork += (sender, args) =>
            {
                List<FileEnumeratorFileEntry> files = new List<FileEnumeratorFileEntry>(FileEnumerator
                    .RecurseFiles(folderBrowserDialog.SelectedPath,
                        Constants.Extensions.ToArray()).ToArray());

                if (files.Count == 0)
                {
                    worker.ReportProgress(100);
                    return;
                }

                int fileCount = files.Count;

                long songCount = GetScalar<long>(Song.GenerateCountSentence(false), connection);

                int progress = 0;

                int previousProgress = -1;

                List<Song> songs = new List<Song>();
                using (SQLiteCommand command = new SQLiteCommand(Song.GenerateSelectSentence(false), connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var percentage = progress * 50 / (int) songCount;

                            if (percentage > previousProgress)
                            {
                                worker.ReportProgress(percentage);
                                previousProgress = percentage;
                            }

                            songs.Add(Song.FromSqLiteDataReader(reader));
                            progress++;

                            if (worker.CancellationPending)
                            {
                                return;
                            }
                        }
                    }
                }

                progress = 0;
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file.FileNameWithPath);
                        var song = songs.FirstOrDefault(f =>
                            f.FileSize == fileInfo.Length && f.FileNameNoPath == fileInfo.Name);

                        if (song == null)
                        {
                            song = songs.FirstOrDefault(f => f.Filename == fileInfo.FullName);
                            if (song != null)
                            {
                                song.FileSize = (int) fileInfo.Length;
                            }
                            else
                            {
                                song = songs.FirstOrDefault(f => f.FileNameNoPath == fileInfo.Name);
                                if (song != null)
                                {
                                    song.FileSize = (int) fileInfo.Length;
                                }
                            }
                        }

                        var percentage = 50 + progress * 50 / fileCount;

                        if (percentage > previousProgress)
                        {
                            worker.ReportProgress(percentage);
                            previousProgress = percentage;
                        }

                        if (song == null)
                        {
                            continue;
                        }

                        song.Filename = file.FileNameWithPath;
                        ExecuteSql(song.GenerateInsertUpdateSqlSentence(false), connection);
                        progress++;

                        if (worker.CancellationPending)
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // log the exception..
                        ExceptionLogger.LogError(ex);
                    }
                }

                worker.ReportProgress(100);
            };

            return worker;
        }

        return null;
    }

    /// <summary>
    /// Regexp: ^[a-zA-Z]:\\ (for etc: C:\).
    /// </summary>
    private static Regex NormalDirectory { get; set; } = new Regex(@"^[a-zA-Z]:\\", RegexOptions.Compiled);

    /// <summary>
    /// Regexp: ^\\\\ (for UNC-paths).
    /// </summary>
    private static Regex UncDirectory { get; set; } = new Regex(@"^\\\\", RegexOptions.Compiled);

    /// <summary>
    /// Splits a path string to a given <paramref name="max"/> amount of directories.
    /// </summary>
    /// <param name="path">The path to remove directories from the ending point.</param>
    /// <param name="max">The maximum amount of directories to leave.</param>
    /// <returns>A combined path string containing at most the <paramref name="max"/> amount of directories.</returns>
    public static string SplitPathStringMax(string path, int max)
    {
        if (max <= 0)
        {
            return path;
        }

        string pathStart = string.Empty;
        if (NormalDirectory.IsMatch(path))
        {
            pathStart = NormalDirectory.Match(path).Value;
        }
        else if (UncDirectory.IsMatch(path))
        {
            pathStart = UncDirectory.Match(path).Value;
        }

        string tmpString = path.Substring(pathStart.Length);

        List<string> pathParts = new List<string>(tmpString.Split(new[] {Path.DirectorySeparatorChar},
            StringSplitOptions.RemoveEmptyEntries));

        while (pathParts.Count > max)
        {
            pathParts.RemoveAt(pathParts.Count - 1);
        }


        string result = pathStart + string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.ToArray());

        return result;
    }
}