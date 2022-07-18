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
using System.Windows.Forms;
using amp.FormsUtility.Progress;
using amp.UtilityClasses;
using VPKSoft.ErrorLogger;

namespace amp.SQLiteDatabase.DatabaseUtils;

/// <summary>
/// A class containing some utilities dealing with the saved queues.
/// </summary>
public class QueueUtilities
{
    /// <summary>
    /// Gets the files in queue and their total count.
    /// </summary>
    /// <param name="queueIdentifier">The database ID number for the queue.</param>
    /// <param name="toFolder">The folder to copy the files to.</param>
    /// <param name="connection">A reference to a <see cref="SQLiteConnection"/> class instance.</param>
    /// <returns>A list of file names and their locations and to where to copy them to.</returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static IEnumerable<(int currentIndex, int countTotal, string fileNameFrom, string fileNameTo)>
        GetQueueFiles(int queueIdentifier,
            string toFolder,
            SQLiteConnection connection)
    {
        string sql =
            string.Join(Environment.NewLine,
                "SELECT S.ID, Q.QUEUEINDEX, S.FILENAME,",
                $"(SELECT COUNT(*) FROM QUEUE_SNAPSHOT Q WHERE Q.ID = {queueIdentifier}) AS COUNT",
                "FROM",
                "SONG S, QUEUE_SNAPSHOT Q",
                "WHERE",
                "S.ID = Q.SONG_ID AND",
                $"Q.ID = {queueIdentifier}",
                "ORDER BY Q.QUEUEINDEX;");

        using (SQLiteCommand command = new SQLiteCommand(sql, connection))
        {
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                int fileCount = 0;

                while (reader.Read())
                {
                    string sourceFile = reader.GetString(2);
                    string fileName = Path.GetFileName(reader.GetString(2));
                    if (fileName != null)
                    {
                        string destinationFile = Path.Combine(toFolder,
                            fileName);

                        yield return (++fileCount, (int)reader.GetInt64(3), sourceFile, destinationFile);
                    }
                }
            }
        }
    }


    private static BackgroundWorker worker;
    private static int queueIndex;
    private static string toPath;
    private static SQLiteConnection conn;
    private static bool toMp3;
    private static int bitRate;
    private static Form form;


    /// <summary>
    /// Copies the queue files into a given directory and optionally converts the files to MP3 format.
    /// </summary>
    /// <param name="queueIdentifier">The database ID number for the queue.</param>
    /// <param name="toFolder">The folder to copy the files to.</param>
    /// <param name="connection">A reference to a <see cref="SQLiteConnection"/> class instance.</param>
    /// <param name="convertToMp3">A value indicating whether to convert the file to a MP3 format.</param>
    /// <param name="staticStatusText">A text to be shown as a static title describing the process.</param>
    /// <param name="statusLabelText">A text to display the progress percentage in the status label of the form. One place holder for the progress percentage must be reserved (i.e. {0}).</param>
    /// <param name="outputBitRate">A bit rate for an MP3 file if a file is requested to be converted into a MP3 format.</param>
    /// <param name="parentForm">the parent form to which the progress dialog should be aligned to the center of.</param>
    public static void RunWithDialog(Form parentForm, int queueIdentifier, string toFolder,
        SQLiteConnection connection, bool convertToMp3, string staticStatusText, string statusLabelText,
        int outputBitRate = 256000)
    {
        worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
        queueIndex = queueIdentifier;
        toPath = toFolder;
        conn = connection;
        toMp3 = convertToMp3;
        bitRate = outputBitRate;
        form = parentForm;
        worker.DoWork += Worker_DoWork;
        FormProgressBackground.Execute(form, worker, staticStatusText, statusLabelText);

        worker.DoWork -= Worker_DoWork;

        using (worker)
        {
            worker = null;
        }
    }

    private static int GetPercentage(int value, int max)
    {
        return max == 0 ? 0 : value * 100 / max;
    }

    private static void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        try
        {
            // start the NAudio.MediaFoundation.MediaFoundationApi..
            AudioConvert.IsMediaFoundationApiStarted = true;

            foreach (var fileEntry in GetQueueFiles(queueIndex, toPath, conn))
            {
                if (worker.CancellationPending)
                {
                    break;
                }

                string sourceFile = fileEntry.fileNameFrom;
                string fileName = Path.GetFileName(fileEntry.fileNameFrom);
                if (fileName != null)
                {
                    string destinationFile = Path.Combine(toPath,
                        fileName);


                    if (toMp3 && !Constants.FileIsMp3(sourceFile))
                    {
                        AudioConvert.DesiredBitRate = bitRate;
                        AudioConvert.ToMp3(sourceFile, Path.ChangeExtension(destinationFile, ".mp3"), true);
                    }
                    else
                    {
                        File.Copy(sourceFile, destinationFile, true);
                    }

                    worker.ReportProgress(GetPercentage(fileEntry.currentIndex, fileEntry.countTotal));
                }
            }

            // stop the NAudio.MediaFoundation.MediaFoundationApi..
            AudioConvert.IsMediaFoundationApiStarted = false;
        }
        catch (Exception ex)
        {
            // log the exception..
            ExceptionLogger.LogError(ex);
        }
    }
}