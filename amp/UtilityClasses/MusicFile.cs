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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using amp.Remote.DataClasses;
using TagLib;
using VPKSoft.ErrorLogger;
using VPKSoft.RandomizationUtils;
using File = TagLib.File;

namespace amp.UtilityClasses;

/// <summary>
/// Represents a single music file within the software.
/// </summary>
public class MusicFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MusicFile"/> class.
    /// </summary>
    /// <param name="fullPath">The full path of the music file.</param>
    /// <param name="id">The database ID number for the music file.</param>
    public MusicFile(string fullPath, int id)
    {
        ID = id;
        FileNameNoPath = Path.GetFileName(fullPath);
        filePath = Path.GetFullPath(fullPath);
        songName = Path.GetFileNameWithoutExtension(fullPath);
        fileExt = Path.GetExtension(fullPath);
        FileInfo fInfo = new FileInfo(fullPath);
        FileSize = fInfo.Length;
        tAlbum = string.Empty;
        tArtist = string.Empty;
        TagString = string.Empty;
        tYear = 0;
        tTitle = string.Empty;
        FullFileName = fullPath;
        tTrack = 0;
        Duration = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicFile"/> class.
    /// </summary>
    /// <param name="fullPath">The full path of the music file.</param>
    public MusicFile(string fullPath): this(fullPath, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicFile"/> class.
    /// </summary>
    /// <param name="mf">A <see cref="MusicFile"/> class instance to use as a base.</param>
    public MusicFile(MusicFile mf)
    {
        ID = mf.ID;
        FileNameNoPath = mf.FileNameNoPath;
        filePath = mf.filePath;
        songName = mf.songName;
        fileExt = mf.fileExt;
        FileSize = mf.FileSize;
        tAlbum = mf.tAlbum;
        tArtist = mf.tArtist;
        TagString = mf.TagString;
        tYear = mf.tYear;
        tTitle = mf.tTitle;
        FullFileName = mf.FullFileName;
        tTrack = mf.tTrack;
        Duration = mf.Duration;
    }

    /// <summary>
    /// Converts this instance to a <see cref="AlbumSongRemote"/> instance.
    /// </summary>
    /// <returns>An instance to a <see cref="AlbumSongRemote"/> class.</returns>
    public AlbumSongRemote ToAlbumSongRemote()
    {
        var musicFile = this;
        return new AlbumSongRemote
        {
            Id = musicFile.ID,
            Duration = musicFile.Duration,
            Volume = musicFile.Volume,
            QueueIndex = musicFile.QueueIndex,
            Rating = musicFile.Rating,
            SongName = musicFile.SongName,
            Album = musicFile.Album,
            Artist = musicFile.Artist,
            SongNameNoQueue = musicFile.SongNameNoQueue,
            OverrideName = musicFile.OverrideName,
            TagStr = musicFile.TagString,
            Title = musicFile.Title,
            Track = musicFile.Track,
            Year = musicFile.Year,
            FullFileName = musicFile.FullFileName,
        };
    }

    /// <summary>
    /// Gets a new <see cref="MusicFile"/> instance from a specified <see cref="SQLiteDataReader"/> instance.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="visualIndex">Index the visual for the <see cref="MusicFile"/> instance.</param>
    /// <returns>An instance to a <see cref="MusicFile"/>.</returns>
    public static MusicFile FromDataReader(SQLiteDataReader reader, int visualIndex)
    {
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

        if (!reader.IsDBNull(17))
        {
            byte[] imageData = new byte[reader.GetInt32(18)];
            reader.GetBytes(17, 0, imageData, 0, imageData.Length);

            using var memoryStream = new MemoryStream(imageData);
            mf.SongImage = Image.FromStream(memoryStream);
        }

        mf.TagString = reader.GetInt32(8) != 0 ? reader.GetString(6) : string.Empty;
        mf.VisualIndex = visualIndex;
        return mf;
    }

    private readonly string filePath;
    private readonly string fileExt;
    private readonly string songName;
    private string tAlbum;
    private string tArtist;
    private uint tYear;
    private uint tTrack;
    private string tTitle;
    private int queueIndex = -1;
    private int visualIndex;
    private bool picLoaded;
    private bool tagLoaded;
    private float volume = 1.0F;
    private int rating = 500;
    private Tag tag;
    private string overrideName = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the music file.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Gets or sets the pictures stored in the music file.
    /// </summary>
    public IPicture[] Pictures { get; set; }

    /// <summary>
    /// Gets or sets the song image stored in the database.
    /// </summary>
    /// <value>The song image stored in the database.</value>
    public Image SongImage { get; set; }

    /// <summary>
    /// Gets the file name without a path of the music file.
    /// </summary>
    public string FileNameNoPath { get; }

    /// <summary>
    /// Gets or sets the tag string (a combined string of data in a IDvX Tag) for searching purposes.
    /// </summary>
    public string TagString { get; set; }

    /// <summary>
    /// Loads the pictures containing in the music file's IDvX Tag.
    /// </summary>
    public void LoadPic()
    {
        if (picLoaded)
        {
            return;
        }
        try
        {
            using (File tagFile = File.Create(FullFileName))
            {
                Pictures = tagFile.Tag.Pictures;
            }
        }
        catch (Exception ex)
        {
            // log the exception..
            ExceptionLogger.LogError(ex);
        }
        picLoaded = true;
    }

    /// <summary>
    /// Loads the IDvX Tag data from the music file.
    /// </summary>
    /// <param name="force">if set to <c>true</c> the tag is tried read again even if it has been read already.</param>
    public void LoadTag(bool force = false)
    {
        if (tagLoaded && !force)
        {
            return;
        }

        try
        {                
            using (File tagFile = File.Create(FullFileName))
            {
                tAlbum = tagFile.Tag.Album;
                if (tagFile.Tag.AlbumArtists.Length > 0)
                {
                    tArtist = tagFile.Tag.AlbumArtists[0];
                }
                Duration = (int)Math.Round(tagFile.Properties.Duration.TotalSeconds);
                tYear = tagFile.Tag.Year;
                tTitle = tagFile.Tag.Title;
                tTrack = tagFile.Tag.Track;
                tag = tagFile.Tag;
            }
            TagString = GetTagString(tag);
        }
        catch (Exception ex)
        {
            // log the exception..
            ExceptionLogger.LogError(ex);
        }

        tagLoaded = true;
    }

    /// <summary>
    /// Gets the tag from a <see cref="SQLiteDataReader"/> instance.
    /// </summary>
    /// <param name="dr">The <see cref="SQLiteDataReader"/> instance to read the tag from.</param>
    public void GetTagFromDataReader(SQLiteDataReader dr)
    {
        tArtist = dr.GetString(9);
        tAlbum = dr.GetString(10);

        if (uint.TryParse(dr.GetString(11), out _))
        {
            tTrack = uint.Parse(dr.GetString(11));
        }

        if (uint.TryParse(dr.GetString(12), out _))
        {
            tYear = uint.Parse(dr.GetString(12));
        }

        tTitle = dr.GetString(13);
    }

    /// <summary>
    /// Gets the tag string by combining all the data in an IDvX Tag into a one string.
    /// </summary>
    /// <param name="strTag">The <see cref="Tag"/> instance to get the data from.</param>
    /// <param name="goDeep">if set to <c>true</c> a recursion is used to navigate through the IDvX Tag's tree structure.</param>
    /// <returns>A string representing the data in the tag if the operation was successful; otherwise <see cref="string.Empty"/>.</returns>
    private string GetTagString(Tag strTag, bool goDeep = false)
    {
        string result = string.Empty;

        PropertyInfo[] propertyInfos = strTag.GetType().GetProperties();

        foreach (PropertyInfo pi in propertyInfos)
        {
            if (pi.PropertyType != typeof(string) &&
                pi.PropertyType != typeof(uint) &&
                pi.PropertyType != typeof(string[]) && 
                pi.PropertyType != typeof(Tag[]))
            {
                continue;
            }

            try
            {
                if (pi.GetValue(tag) == null)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            if (pi.PropertyType == typeof(Tag[]) && goDeep)
            {
                foreach (Tag tmpTag in ((Tag[]) pi.GetValue(tag)))
                {
                    result += GetTagString(tmpTag);
                }
            }
            if (pi.PropertyType == typeof(string[]))
            {
                foreach (string str in ((string[]) pi.GetValue(tag)))
                {
                    result += str;
                }
            }
            else
            {
                result += pi.GetValue(tag).ToString();
            }
        }
        result = result.TrimStart("TagLib.Tag".ToCharArray());
        return result;
    }

    // a query counter for the SongChanged property..
    private int changeQueryCount;

    // a field for the SongChanged property..
    private bool valueChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the music file has changed.
    /// Note: This property auto-resets to false after querying the value.
    /// </summary>
    public bool SongChanged
    {
        set
        {
            valueChanged = value;
            if (value)
            {
                changeQueryCount = 0;
            }
        }

        get
        {
            changeQueryCount++;
            if (changeQueryCount >= 2)
            {
                bool tmp = valueChanged;
                valueChanged = false;
                return tmp;
            }

            return valueChanged;
        }
    }

    /// <summary>
    /// Gets or sets the override name for the music file.
    /// </summary>
    public string OverrideName
    {
        get => overrideName;

        set
        {
            if (value != string.Empty)
            {
                SongChanged = true;
                overrideName = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether rating of the music file has changed.
    /// </summary>
    public bool RatingChanged { get; set; }

    /// <summary>
    /// Gets or sets the rating of the music file. The range is between 0 to 1000.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">value</exception>
    public int Rating
    {
        get => rating;

        set
        {
            if (value < 0 || value > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            rating = value;
            SongChanged = true;
        }
    }

    /// <summary>
    /// Gets or sets the volume for the music file. The range is between 0 to 2.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">value</exception>
    public float Volume
    {
        get => volume;

        set
        {
            if (value < 0 || value > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            volume = value;
            SongChanged = true;
        }
    }

    /// <summary>
    /// Gets or sets the database ID number for the music file.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public int ID { get; set; }

    /// <summary>
    /// Gets the artist of the music file.
    /// </summary>
    public string Artist => (tArtist ?? string.Empty).Trim();

    /// <summary>
    /// Gets the album of the music file.
    /// </summary>
    public string Album => (tAlbum ?? string.Empty).Trim();

    /// <summary>
    /// Gets the title of the music file.
    /// </summary>
    public string Title => (tTitle ?? string.Empty).Trim();

    /// <summary>
    /// Gets the publishing year of the music file.
    /// </summary>
    public string Year => tYear == 0 ? "????" : tYear.ToString();

    /// <summary>
    /// Gets the track number of the music file.
    /// </summary>
    public string Track => tTrack == 0 ? string.Empty : tTrack.ToString();

    /// <summary>
    /// Gets the full name of the music file.
    /// </summary>
    public string FullFileName { get; }

    /// <summary>
    /// Gets the size of the music file.
    /// </summary>
    public long FileSize { get; }


    #region Queue
    /// <summary>
    /// Gets or sets the queue index of the music file.
    /// </summary>
    public int QueueIndex
    {
        get => queueIndex;
        set
        {
            queueIndex = value;
            queueChanged = true;
        }
    }

    /// <summary>
    /// Gets or sets the alternate queue index of music file.
    /// </summary>
    public int AlternateQueueIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the visual index of the music file (in the play list box).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int VisualIndex
    {
        get => visualIndex;

        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            visualIndex = value;
        }
    }

    /// <summary>
    /// Removes a music file from the given <paramref name="files"/> with a given database ID number.
    /// </summary>
    /// <param name="files">A reference to a list of <see cref="MusicFile"/>.</param>
    /// <param name="id">The database ID number used to remove the music file from the list.</param>
    public static void RemoveById(ref List<MusicFile> files, int id)
    {
        files.RemoveAll(f => f.ID == id);
    }

    // a field for the QueueChanged property..
    private static bool queueChanged;

    /// <summary>
    /// Gets a value indicating whether the queue has changed.
    /// Note: This property auto-resets it self to false after access.
    /// </summary>
    public static bool QueueChanged
    {
        get
        {
            bool tmp = queueChanged;
            queueChanged = false;
            return tmp;
        }
    }

    /// <summary>
    /// Inserts this music file to the top of the queue.
    /// </summary>
    /// <param name="files">A reference to a list of <see cref="MusicFile"/> class instances to which this instance should be queued on top.</param>
    /// <param name="filtered">if set to <c>true</c> the list is considered as filtered.</param>
    /// <param name="mfIndex">An optional index of the music file within the list.</param>
    public void QueueInsert(ref List<MusicFile> files, bool filtered, int mfIndex = -1) // 14.10.17
    {
        if (QueueIndex > 0)
        {
            Queue(ref files, false);
            //return; // added return at 2019/07/22, might cause a bug or fix one..
        }

        if ((filtered || mfIndex != -1) && QueueIndex == 0)
        {
            foreach (MusicFile mf in files)
            {
                if (mf.QueueIndex > 0)
                {
                    mf.QueueIndex++; //::QUEUE
                }
            }
            QueueIndex = 1; //::QUEUE
            return;
        }

        int iQueue = 0;

        int idx = files.IndexOf(this); // hope nobody does sort the list !!
        int idFirst = -1, idLast = -1;

        for (int i = 0; i < files.Count; i++)
        {
            if (i == idx)
            {
                continue;
            }

            if (files[i].QueueIndex > iQueue && i < idx)
            {
                iQueue = files[i].QueueIndex;
                idFirst = i;
            }
            else if (files[i].QueueIndex > iQueue && i > idx)
            {
                iQueue = files[i].QueueIndex;
                idLast = i;
            }
        }

        if (idLast < idx && idFirst > idx)
        {
            QueueIndex = files[idFirst].QueueIndex; //::QUEUE
        }
        else if (idFirst >= 0 && idFirst < idx)
        {
            QueueIndex = files[idFirst].QueueIndex + 1; //::QUEUE
        }
        else 
        {
            QueueIndex = QueueIndex == 0 ? 1 : iQueue; //::QUEUE
        }

        for (int i = 0; i < files.Count; i++)
        {
            if (files[i].QueueIndex >= QueueIndex && idx != i)
            {
                files[i].QueueIndex++; //::QUEUE
            }
        }
    }

    /// <summary>
    /// Queues this music file to the end of the queue of given music file list.
    /// </summary>
    /// <param name="files">A reference to a list of <see cref="MusicFile"/> class instances to which this instance should be queued to the end of.</param>
    /// <param name="stackQueueEnabled">A value indicating whether the stack queue (continuous queue playback with randomization is enabled).</param>
    public void Queue(ref List<MusicFile> files, bool stackQueueEnabled)
    {
        int iQueue = 0;
        foreach (MusicFile mf in files)
        {
            if (mf.QueueIndex > iQueue)
            {
                iQueue = mf.QueueIndex;
            }
        }

        if (QueueIndex >= 1)
        {
            int tmp = QueueIndex;
            QueueIndex = 0;
            foreach (MusicFile mf in files)
            {
                if (mf.QueueIndex > tmp)
                {
                    mf.QueueIndex--; //::QUEUE
                }
            }

            if (stackQueueEnabled) // the stack queue randomization..
            {
                QueueIndex = files.Max(f => f.QueueIndex) + 1;
                StackQueue(ref files, StackRandomPercentage);
            }

            return;
        }
        QueueIndex = iQueue + 1;
        //::QUEUE
    }

    /// <summary>
    /// Gets or sets the stack random percentage.
    /// </summary>
    /// <value>The stack random percentage.</value>
    internal static int StackRandomPercentage { get; set; } = 0;

    /// <summary>
    /// Loops the current queue so that it will not be consumed in the process. The previous song is put to the bottom of the queue and the end of the queue is re-randomized.
    /// </summary>
    /// <param name="files">The files which queue to adjust.</param>
    /// <param name="stackRandomPercentage">The stack random percentage.</param>
    /// <returns><c>true</c> if changes were made to the play list, <c>false</c> otherwise.</returns>
    internal bool StackQueue(ref List<MusicFile> files, int stackRandomPercentage)
    {
        // get the music files with a queue index value..
        var musicFiles = 
            files.Where(f => f.QueueIndex > 0).
                OrderByDescending(f => f.QueueIndex).ToList();


        // get the amount of music files in the queue to randomize to a new order..
        var randomizeCount = (int) (musicFiles.Count * (stackRandomPercentage / 100.0));

        // zero or amount of the queue leads to do-nothing-condition..
        if (randomizeCount == 0 || randomizeCount == musicFiles.Count || stackRandomPercentage == 0) 
        {
            return false; // no modifications were made..
        }

        // get the files which queue index to re-randomize..
        var reRandomFiles = musicFiles.Take(randomizeCount).ToList();

        List<int> usedQueueIndices = new List<int>();

        // get the min-max range for the randomization..
        var min = reRandomFiles.Min(f => f.QueueIndex);
        var max = reRandomFiles.Max(f => f.QueueIndex);

        // loop through the files which queue index is to be re-randomized..
        foreach (var reRandomFile in reRandomFiles)
        {
            // the last music file won't be re-randomized..
            if (reRandomFile.QueueIndex == max)
            {
                continue;
            }

            // random new unique queue indices..
            var newQueueIndex = Random.Next(min, max);
            while (usedQueueIndices.Contains(newQueueIndex))
            {
                newQueueIndex = Random.Next(min, max);
            }

            // add the new randomized queue index to the list..
            usedQueueIndices.Add(newQueueIndex);

            // save the queue index..
            reRandomFile.QueueIndex = newQueueIndex;
        }
            
        // modifications to the playlist were made..
        return true;
    }

    /// <summary>
    /// Moves the selected music files in the queue to the top of the queue.
    /// </summary>
    /// <param name="files">The music files.</param>
    /// <param name="selectedFiles">The selected music files.</param>
    /// <returns><c>true</c> if changes were made to the play list, <c>false</c> otherwise.</returns>
    internal static bool MoveQueueTop(ref List<MusicFile> files, params MusicFile[] selectedFiles)
    {
        bool affected = false; // if any songs in the play list was affected..

        var moveAmount = selectedFiles.Length;

        // if there is nothing queued do not continue the method execution..
        if (moveAmount == 0)
        {
            return false;
        }

        for (int i = 0; i < selectedFiles.Length; i++)
        {
            selectedFiles[i].QueueIndex = i + 1;
            affected = true;
        }

        var leftFiles = files.Where(f => f.QueueIndex > 0 && !selectedFiles.Contains(f)).OrderBy(x => x.QueueIndex)
            .ToList();

        for (int i = 0; i < leftFiles.Count; i++)
        {
            leftFiles[i].QueueIndex = i + moveAmount + 1;
            affected = true;
        }

        return affected;
    }

    /// <summary>
    /// Scrambles the queue with randomization to a new order for the selected songs.
    /// </summary>
    /// <param name="selectedFiles">The files in the playlist which queue to scramble.</param>
    /// <returns><c>true</c> if the <paramref name="selectedFiles"/> was affected, <c>false</c> otherwise.</returns>
    internal static bool ScrambleQueueSelected(params MusicFile[] selectedFiles)
    {
        bool affected = false; // if any songs in the play list was affected..
        List<int> queueIndices = new List<int>(); // the list of current queue indices..
        List<MusicFile> selectedMusicFiles = new List<MusicFile>(); // the list of selected music files in the UI..

        // get the current queue indices..
        foreach (MusicFile mf in selectedFiles)
        {
            if (mf.QueueIndex > 0)
            {
                queueIndices.Add(mf.queueIndex);
                selectedMusicFiles.Add(mf);
            }
        }

        if (selectedMusicFiles.Count <= 1)
        {
            return false;
        }

        int index = 0;
        var newIndex = queueIndices.Count == 1 ? 0 : Random.Next(queueIndices.Count);

        while (queueIndices.Count > 0)
        {
            if (selectedMusicFiles[index].QueueIndex == queueIndices[newIndex])
            {
                if (queueIndices.Count == 1)
                {
                    return affected;
                }
                newIndex = queueIndices.Count == 1 ? 0 : Random.Next(queueIndices.Count);
                continue;
            }

            selectedMusicFiles[index].QueueIndex = queueIndices[newIndex];
            queueIndices.Remove(queueIndices[newIndex]);
            index++;
            affected = true;

            newIndex = queueIndices.Count == 1 ? 0 : Random.Next(queueIndices.Count);
        }

        return affected;
    }

    /// <summary>
    /// Scrambles the queue with randomization to a new order.
    /// </summary>
    /// <param name="files">The files in the playlist which queue to scramble.</param>
    /// <returns><c>true</c> if the <paramref name="files"/> was affected, <c>false</c> otherwise.</returns>
    internal static bool ScrambleQueue(ref List<MusicFile> files)
    {
        bool affected = false; // if any songs in the play list was affected..
        List<int> queueIndices = new List<int>(); // the list of current queue indices..
        List<int> newQueueIndices = new List<int>(); // the list of new current queue indices..

        // get the current queue indices..
        foreach (MusicFile mf in files)
        {
            if (mf.QueueIndex > 0)
            {
                queueIndices.Add(mf.QueueIndex);
            }
        }

        // if there is nothing queued do not continue the method execution..
        if (queueIndices.Count == 0)
        {
            return false;
        }

        // randomize the new indices..
        for (int i = 0; i < queueIndices.Count; i++)
        {
            int newQueueIndex = Random.Next(queueIndices.Count) + 1;
            while ((queueIndices[i] == newQueueIndex && i != queueIndices.Count - 1) || newQueueIndices.Exists(f => f == newQueueIndex))
            {
                newQueueIndex = Random.Next(queueIndices.Count) + 1;
            }
            newQueueIndices.Add(newQueueIndex);
            affected = true;
        }

        int nextIndex = 0;
        foreach (MusicFile mf in files)
        {
            if (mf.QueueIndex > 0)
            {
                mf.QueueIndex = newQueueIndices[nextIndex++];
            }
        }
        return affected;
    }
    #endregion

    #region AlternateQueue        
    /// <summary>
    /// Inserts this music file to the top of the alternate queue.
    /// </summary>
    /// <param name="files">A reference to a list of <see cref="MusicFile"/> class instances to which this instance should be queued on top.</param>
    /// <param name="filtered">if set to <c>true</c> the list is considered as filtered.</param>
    /// <param name="mfIndex">An optional index of the music file within the list.</param>
    public void QueueInsertAlternate(ref List<MusicFile> files, bool filtered, int mfIndex = -1) // 14.10.17
    {
        if (AlternateQueueIndex > 0)
        {
            QueueAlternate(ref files);
            //return; // added return at 2019/07/22, might cause a bug or fix one..
        }

        if ((filtered || mfIndex != -1) && QueueIndex == 0)
        {
            foreach (MusicFile mf in files)
            {
                if (mf.AlternateQueueIndex > 0)
                {
                    mf.AlternateQueueIndex++; //::QUEUE
                }
            }
            AlternateQueueIndex = 1; //::QUEUE
            return;
        }

        int iQueue = 0;

        int idx = files.IndexOf(this); // hope nobody does sort the list !!
        int idFirst = -1, idLast = -1;

        for (int i = 0; i < files.Count; i++)
        {
            if (i == idx)
            {
                continue;
            }

            if (files[i].AlternateQueueIndex > iQueue && i < idx)
            {
                iQueue = files[i].AlternateQueueIndex;
                idFirst = i;
            }
            else if (files[i].AlternateQueueIndex > iQueue && i > idx)
            {
                iQueue = files[i].AlternateQueueIndex;
                idLast = i;
            }
        }

        if (idLast < idx && idFirst > idx)
        {
            AlternateQueueIndex = files[idFirst].AlternateQueueIndex; //::QUEUE
        }
        else if (idFirst >= 0 && idFirst < idx)
        {
            AlternateQueueIndex = files[idFirst].AlternateQueueIndex + 1; //::QUEUE
        }
        else
        {
            AlternateQueueIndex = AlternateQueueIndex == 0 ? 1 : iQueue; //::QUEUE
        }

        for (int i = 0; i < files.Count; i++)
        {
            if (files[i].AlternateQueueIndex >= AlternateQueueIndex && idx != i)
            {
                files[i].AlternateQueueIndex++; //::QUEUE
            }
        }
    }

    /// <summary>
    /// Queues this music file to the end of the alternate queue of given music file list.
    /// </summary>
    /// <param name="files">A reference to a list of <see cref="MusicFile"/> class instances to which this instance should be queued to the end of.</param>
    public void QueueAlternate(ref List<MusicFile> files)
    {
        int iQueue = 0;
        foreach (MusicFile mf in files)
        {
            if (mf.AlternateQueueIndex > iQueue)
            {
                iQueue = mf.AlternateQueueIndex;
            }
        }
        if (AlternateQueueIndex >= 1)
        {
            int tmp = AlternateQueueIndex;
            AlternateQueueIndex = 0;
            foreach (MusicFile mf in files)
            {
                if (mf.AlternateQueueIndex > tmp)
                {
                    mf.AlternateQueueIndex--; //::QUEUE
                }
            }
            return;
        }
        AlternateQueueIndex = iQueue + 1;
        //::QUEUE
    }
    #endregion

    #region PlayListNaming        
    /// <summary>
    /// Replaces a given formula part with with a given string value.
    /// </summary>
    /// <param name="formula">The formula of which part is to be replaced.</param>
    /// <param name="value">The value to replace the <paramref name="formula"/> part with.</param>
    /// <param name="formulaStr">The formula part to replace from the <paramref name="formula"/> string.</param>
    /// <returns>The <paramref name="formula"/> with replacements.</returns>
    public static string FormulaReplace(string formula, string value, string formulaStr)
    {
        int formulaStart = formula.IndexOf(formulaStr, StringComparison.Ordinal);

        if (formulaStr + "#" == "#ARTIST#" ||
            formulaStr + "#" == "#ALBUM#" ||
            formulaStr + "#" == "#TITLE#" ||
            formulaStr + "#" == "#QUEUE#" ||
            formulaStr + "#" == "#ALTERNATE_QUEUE#" ||
            formulaStr + "#" == "#RENAMED#" ||
            // ReSharper disable once StringLiteralTypo
            formulaStr + "#" == "#TRACKNO#")
        {
            formula = formula.Replace(formulaStr + "#", value);
            return formula;
        }
            
        int stringPos = -1;
        int formulaEnd = -1;
        string formulaPart = formula;
        for (int i = formulaStart + 1;  i < formulaPart.Length && (stringPos == -1 || formulaEnd == -1); i++)
        {
            if (formulaPart[i] == '?' && stringPos == -1)
            {
                stringPos = i;
            }

            if (formulaPart[i] == '#' && formulaEnd == -1)
            {
                formulaEnd = i;
            }
        }

        if (stringPos != -1 && formulaEnd != -1 && stringPos <= formulaEnd)
        {
            string stringPart = formulaPart.Substring(stringPos + 1, formulaEnd - stringPos - 1);
            string startPart = formula.Substring(0, formulaStart);
            string endPart = formula.Substring(formulaEnd + 1);

            string[] tmpParts = stringPart.Split('^');

            string stringPartStart = tmpParts.Length == 1 ? string.Empty :
                (tmpParts.Length > 1 ? tmpParts[0] : string.Empty);
            string stringPartEnd = tmpParts.Length > 1 ? tmpParts[1] : stringPart;

            formula = string.IsNullOrEmpty(value) ?
                startPart + endPart :
                startPart + stringPartStart + value + stringPartEnd + endPart;
        }           
        else
        {
            return string.Empty;
        }
        return formula;
    }

    /// <summary>
    /// An enumeration describing a part of a song naming formula.
    /// </summary>
    public enum FormulaType
    {
        /// <summary>
        /// There is no formula.
        /// </summary>
        None,

        /// <summary>
        /// The artist part.
        /// </summary>
        Artist,

        /// <summary>
        /// The album part.
        /// </summary>
        Album,

        /// <summary>
        /// The track number part.
        /// </summary>
        TrackNo,

        /// <summary>
        /// The title part.
        /// </summary>
        Title,

        /// <summary>
        /// The queue index part.
        /// </summary>
        QueueIndex,

        /// <summary>
        /// The alternate queue index part.
        /// </summary>
        AlternateQueueIndex,

        /// <summary>
        /// The override (renamed) name part.
        /// </summary>
        Renamed
    }

    /// <summary>
    /// Gets the next formula part of the given formula string.
    /// </summary>
    /// <param name="formula">The formula from which the next formula part to get.</param>
    /// <param name="formulaType">Type of the formula found; if nothing was found, then <see cref="FormulaType.None"/>.</param>
    /// <returns>The next formula part if found; otherwise a <see cref="string.Empty"/>.</returns>
    internal static string GetNextFormula(string formula, out FormulaType formulaType)
    {
        int startIndex = formula.IndexOf('#');
        int endIndex = -1;
        for (int i = startIndex + 1; i < formula.Length; i++)
        {
            if (formula[i] == '#')
            {
                endIndex = i;
                break;
            }
        }

        if (startIndex != -1 && endIndex != -1 && startIndex < endIndex)
        {
            string result = formula.Substring(startIndex, endIndex - startIndex);
            if (result.StartsWith("#ARTIST"))
            {
                formulaType = FormulaType.Artist;
            }
            else if (result.StartsWith("#ALBUM"))
            {
                formulaType = FormulaType.Album;
            }
            // ReSharper disable once StringLiteralTypo
            else if (result.StartsWith("#TRACKNO"))
            {
                formulaType = FormulaType.TrackNo;
            }
            else if (result.StartsWith("#TITLE"))
            {
                formulaType = FormulaType.Title;
            }
            else if (result.StartsWith("#QUEUE"))
            {
                formulaType = FormulaType.QueueIndex;
            }
            else if (result.StartsWith("#ALTERNATE_QUEUE"))
            {
                formulaType = FormulaType.AlternateQueueIndex;
            }
            else if (result.StartsWith("#RENAMED"))
            {
                formulaType = FormulaType.Renamed;
            }
            else
            {
                formulaType = FormulaType.None;
                return string.Empty;
            }
            return result;
        }

        formulaType = FormulaType.None;
        return string.Empty;
    }

    /// <summary>
    /// Gets a string based on a given formula and a the given parameters.
    /// </summary>
    /// <param name="formula">The formula to create string from.</param>
    /// <param name="artist">The artist.</param>
    /// <param name="album">The album.</param>
    /// <param name="trackNo">The track number.</param>
    /// <param name="title">The title of the song.</param>
    /// <param name="songName">Name of the song.</param>
    /// <param name="queueIndex">Index in the queue.</param>
    /// <param name="alternateQueueIndex">Index in the alternate queue.</param>
    /// <param name="overrideName">The overridden name.</param>
    /// <param name="onError">A value to return in case of an exception.</param>
    /// <param name="error">A value indicating if an error occurred while parsing the formula.</param>
    /// <returns>A string parsed from the given parameters.</returns>
    public static string GetString(string formula, string artist, string album, int trackNo, 
        string title, string songName, int queueIndex, int alternateQueueIndex, string overrideName, string onError, out bool error)
    {
        string FixPathExtension()
        {
            try
            {
                return Path.GetFileNameWithoutExtension(title);
            }
            catch
            {
                return title;
            }
        }

        try
        {
            string formulaStr;
            while ((formulaStr = GetNextFormula(formula, out var formulaType)) != string.Empty)
            {
                if (formulaType == FormulaType.Artist)
                {
                    formula = FormulaReplace(formula, artist == string.Empty ? string.Empty : artist, formulaStr);
                }
                else if (formulaType == FormulaType.Album)
                {
                    formula = FormulaReplace(formula, album == string.Empty ? string.Empty : album, formulaStr);
                }
                else if (formulaType == FormulaType.TrackNo)
                {
                    formula = FormulaReplace(formula, trackNo <= 0 ? string.Empty : trackNo.ToString(), formulaStr);
                }
                else if (formulaType == FormulaType.Title)
                {
                    formula = FormulaReplace(formula, string.IsNullOrWhiteSpace(title) ? songName : FixPathExtension(), formulaStr);
                }
                else if (formulaType == FormulaType.QueueIndex)
                {
                    formula = FormulaReplace(formula, queueIndex <= 0 ? string.Empty : queueIndex.ToString(), formulaStr);
                }
                else if (formulaType == FormulaType.AlternateQueueIndex)
                {
                    formula = FormulaReplace(formula, alternateQueueIndex <= 0 ? string.Empty : alternateQueueIndex.ToString(), formulaStr);
                }
                else if (formulaType == FormulaType.Renamed)
                {
                    formula = FormulaReplace(formula, overrideName == string.Empty ? songName : overrideName, formulaStr);
                }
            }
            error = false;
            return formula;
        }
        catch
        {
            error = true;
            return onError;
        }
    }

    /// <summary>
    /// Gets the music file track as an integer.
    /// </summary>
    /// <value>The track as an integer if successful; otherwise 0.</value>
    private int TrackInt
    {
        get
        {
            if (int.TryParse(Track, out _))
            {
                return int.Parse(Track);
            }

            return 0;
        }
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <param name="queue">if set to <c>true</c> the possible queue index should also be included in the result.</param>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public string ToString(bool queue)
    {
        if (overrideName != string.Empty)
        {
            return GetString(Program.Settings.AlbumNamingRenamed, Artist, Album, TrackInt, Title,
                songName, queue ? queueIndex : 0, AlternateQueueIndex, overrideName, ToStringOld(queue), out _);
        }

        return GetString(Program.Settings.AlbumNaming, Artist, Album, TrackInt, Title,
            songName, queue ? queueIndex : 0, AlternateQueueIndex, overrideName, ToStringOld(queue), out _);
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public override string ToString()
    {
        return ToString(true);
    }

    /// <summary>
    /// Returns a <see cref="System.String" /> that represents this instance (old version).
    /// </summary>
    /// <param name="queue">if set to <c>true</c> the possible queue index should also be included in the result.</param>
    /// <returns>A <see cref="System.String" /> that represents this instance (old version).</returns>
    private string ToStringOld(bool queue)
    {
        string alternateQueue = AlternateQueueIndex > 0 && queue ? " [*=" + AlternateQueueIndex + "]" : string.Empty;
        if (overrideName != string.Empty)
        {
            return overrideName + ((queueIndex >= 1 && queue) ? " [" + queueIndex + "]" : "") + alternateQueue;
        }

        return
            (Artist == string.Empty ? string.Empty : Artist + " - ") +
            (Album == string.Empty ? string.Empty : Album + " - ") +
            (Title.Length > 0 ? Title : songName) +
            (queueIndex >= 1 ? " [" + queueIndex + "]" : "") + alternateQueue;
    }

    /// <summary>
    /// Gets the name of the song.
    /// </summary>
    /// <value>The name of the song.</value>
    public string SongName => ToString();

    /// <summary>
    /// Gets the name of the song without a queue index.
    /// </summary>
    public string SongNameNoQueue => ToString(false);
    #endregion

    /// <summary>
    /// Gets the full name of the file of this music file instance.
    /// </summary>
    /// <returns>The full file name of this music file instance.</returns>
    public string GetFileName()
    {
        return FullFileName;
    }

    #region WeightedRandomization
    // ReSharper disable once InconsistentNaming
    internal int SKIPPED_EARLY { get; set; } = 0;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    internal int NPLAYED_RAND { get; set; } = 0;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    internal int NPLAYED_USER { get; set; } = 0;

    internal static Random Random = new Random();

    internal static bool InRange(double value, double randomValue, double min, double max, double tolerancePercentage)
    {
        double range = (max - min) / 100 * tolerancePercentage;
        return value <= randomValue + range && value >= randomValue - range;
    }

    internal static int RandomWeighted(List<MusicFile> musicFiles)
    {
        if (musicFiles == null || musicFiles.Count == 0)
        {
            return -1;
        }

        List<MusicFile> results = new List<MusicFile>();

        double valueMin = musicFiles.Min(f => f.Rating);
        double valueMax = musicFiles.Max(f => f.Rating);

        double biased = BiasedRandom.RandomBiased(valueMin, valueMax, Program.Settings.BiasedRating);

        if (Program.Settings.BiasedRatingEnabled)
        {
            results.AddRange(musicFiles.FindAll(f =>
                InRange(f.Rating, biased, valueMin, valueMax, Program.Settings.Tolerance)));
        }

        valueMin = musicFiles.Min(f => f.NPLAYED_USER);
        valueMax = musicFiles.Max(f => f.NPLAYED_USER);
        biased = BiasedRandom.RandomBiased(valueMin, valueMax, Program.Settings.BiasedPlayedCount);

        if (Program.Settings.BiasedPlayedCountEnabled)
        {
            results.AddRange(musicFiles.FindAll(f =>
                InRange(f.NPLAYED_USER, biased, valueMin, valueMax, Program.Settings.Tolerance)));
        }

        valueMin = musicFiles.Min(f => f.NPLAYED_RAND);
        valueMax = musicFiles.Max(f => f.NPLAYED_RAND);
        biased = BiasedRandom.RandomBiased(valueMin, valueMax, Program.Settings.BiasedRandomizedCount);

        if (Program.Settings.BiasedRandomizedCountEnabled)
        {
            results.AddRange(musicFiles.FindAll(f =>
                InRange(f.NPLAYED_RAND, biased, valueMin, valueMax, Program.Settings.Tolerance)));
        }

        valueMin = musicFiles.Min(f => f.SKIPPED_EARLY);
        valueMax = musicFiles.Max(f => f.SKIPPED_EARLY);
        biased = BiasedRandom.RandomBiased(valueMin, valueMax, Program.Settings.BiasedSkippedCount);

        if (Program.Settings.BiasedSkippedCountEnabled)
        {
            results.AddRange(musicFiles.FindAll(f =>
                InRange(f.SKIPPED_EARLY, biased, valueMin, valueMax, Program.Settings.Tolerance)));
        }

        int result = -1;

        if (results.Count > 0)
        {
            int tmpIndex = Random.Next(results.Count);
            result = musicFiles.FindIndex(f => f.ID == results[tmpIndex].ID);
        }

        if (result == -1)
        {
            result = Random.Next(musicFiles.Count);
        }
        return result;
    }

    #endregion

    /// <summary>
    /// Checks if the properties of this music file instance matches the given search string.
    /// </summary>
    /// <param name="search">The search string.</param>
    /// <returns><c>true</c> if one of the properties of this music file instance matches the search string, <c>false</c> otherwise.</returns>
    public bool Match(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        // a year match for the tag..
        if (search.StartsWith("y:") || search.StartsWith("Y:"))
        {
            var match = Regex.Match(search, "^(Y|y):(>|<|>=|<=|=)(\\d){4,}").Success;
            if (match)
            {
                if (int.TryParse(Year, out var year))
                {
                    var yearMatch = Regex.Matches(search, "\\d{4,}").FirstOrDefault()?.Value;
                    if (int.TryParse(yearMatch, out var yearCompare))
                    {
                        search = search.TrimStart('y', 'Y', ':');
                        if (search.StartsWith(">="))
                        {
                            return yearCompare >= year;
                        }

                        if (search.StartsWith(">"))
                        {
                            return yearCompare > year;
                        }

                        if (search.StartsWith("<="))
                        {
                            return yearCompare <= year;
                        }

                        if (search.StartsWith("<"))
                        {
                            return yearCompare < year;
                        }

                        if (search.StartsWith("="))
                        {
                            return yearCompare == year;
                        }
                    }

                    return false;
                }
            }
        }

        var digitMatch = search.StartsWith(">=") || search.StartsWith("<=") || search.StartsWith("<") ||
                         search.StartsWith(">");

        if (digitMatch)
        {
            if (search.Contains('&'))
            {
                try
                {
                    var searches = search.Split('&');
                    var match1 = YearMatch(searches[0], FullFileName);
                    var match2 = YearMatch(searches[1].TrimStart(), FullFileName);
                    if (match1 != null && match2 != null)
                    {
                        return (bool)match1 && (bool)match2;
                    }
                }
                catch
                {
                    // allow the search to continue..
                }
            }
            else
            {
                var match1 = YearMatch(search, FullFileName);
                if (match1 != null)
                {
                    return (bool)match1;
                }
            }
        }

        search = search.ToUpper().Trim();
        bool found1 = Artist.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Album.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Title.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Year.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Track.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      FullFileName.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      OverrideName.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      TagString.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1;

        string[] search2 = search.Split(' ');
        if (search2.Length <= 1 || found1)
        {
            return found1;
        }

        bool found2 = true;
        foreach(string str in search2)
        {
            var tmpStr = str.ToUpper();
            found2 &= Artist.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Album.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Title.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Year.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      Track.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      FullFileName.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      OverrideName.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                      TagString.IndexOf(tmpStr, StringComparison.InvariantCultureIgnoreCase) != -1;
        }
        return found2;
    }

    private static bool? YearMatch(string search, string fullFileName)
    {
        try
        {
            if (DateTime.TryParse(search.TrimStart('<', '>', '='),
                    CultureInfo.CurrentUICulture.DateTimeFormat,
                    DateTimeStyles.AllowInnerWhite | DateTimeStyles.AllowLeadingWhite |
                    DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowWhiteSpaces,
                    out var resultDateTime))
            {
                var info = new FileInfo(fullFileName);
                if (search.StartsWith(">="))
                {
                    return info.CreationTime >= resultDateTime;
                }

                if (search.StartsWith(">"))
                {
                    return info.CreationTime > resultDateTime;
                }

                if (search.StartsWith("<="))
                {
                    return info.CreationTime <= resultDateTime;
                }

                if (search.StartsWith("<"))
                {
                    return info.CreationTime < resultDateTime;
                }
            }
        }
        catch
        {
            return null;
        }

        return false;
    }
}