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
using amp.Remote.DataClasses;
using Microsoft.AspNetCore.Mvc;

namespace amp.Remote.RESTful;

/// <summary>
/// Initializes the RESTful API controllers.
/// </summary>
public static class RestInitializer
{
    /// <summary>
    /// Initializes the RESTful API with a specified base URL, port and an instance to a <paramref name="remoteProvider"/>.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    /// <param name="port">The port. If set to zero the port is assumed to be in the <paramref name="baseUrl"/></param>
    /// <param name="remoteProvider">The remote data provider.</param>
    public static void InitializeRest(string baseUrl, int port, RemoteProvider remoteProvider)
    {
        if (port > 0)
        {
            var builder = new UriBuilder(new Uri(baseUrl)) {Port = port};
            baseUrl = builder.Uri.AbsoluteUri;
        }

        RemoteProvider = remoteProvider;

        AmpRemoteController.CreateInstance(baseUrl);
    }

    /// <summary>
    /// Gets or sets the remote control provider.
    /// </summary>
    /// <value>The remote control provider.</value>
    public static RemoteProvider RemoteProvider { get; set; }
}

/// <summary>
/// A remote REST API for the amp# software.
/// </summary>
public class AlbumController: Controller
{
    #region Miscellaneous        
    /// <summary>
    /// A method to test the connection API connection.
    /// </summary>
    /// <returns>A string containing "OK".</returns>
    [Route("api/ok")]
    [HttpGet]
    public string TestConnection()
    {
        return "OK";
    }
    #endregion 

    #region AlbumSongs
    /// <summary>
    /// Gets the songs in the current album. This can take a while if there are thousands of songs in the album.
    /// </summary>
    /// <param name="queued">If true only the queued songs are returned.</param>
    /// <returns>A list of songs in the current album.</returns>
    [Route("api/songs/{queued}")]
    [HttpGet]
    public IEnumerable<AlbumSongRemote> GetAlbumSongs(bool queued)
    {
        return RestInitializer.RemoteProvider.GetAlbumSongs(queued);
    }

    /// <summary>
    /// Gets the songs in the current album. This can take a while if there are thousands of songs in the album.
    /// </summary>
    /// <returns>A list of songs in the current album.</returns>
    [Route("api/songs")]
    [HttpGet]
    public IEnumerable<AlbumSongRemote> GetAlbumSongs()
    {
        return RestInitializer.RemoteProvider.GetAlbumSongs();
    }

    /// <summary>
    /// Gets the ID, length and position of the currently playing song.
    /// </summary>
    /// <returns>A Tuple containing the ID, length and position of the currently playing song. If no song is playing the ID will be -1.</returns>
    [Route("api/currentSong")]
    [HttpGet]
    public Tuple<int, double, double> GetPlayingSong()
    {
        return RestInitializer.RemoteProvider.MFile == null
            ? new Tuple<int, double, double>(-1, 0, 0)
            : new Tuple<int, double, double>(RestInitializer.RemoteProvider.MFile.ID,
                RestInitializer.RemoteProvider.Seconds, RestInitializer.RemoteProvider.SecondsTotal);
    }

    /// <summary>
    /// Removes a song from the current album.
    /// </summary>
    /// <param name="albumSongRemote">A <see cref="AlbumSongRemote"/> class instance to remove from the album.</param>
    [HttpPost]
    [Route("api/removeSongFromAlbum")]
    public IActionResult RemoveSongFromAlbum([FromBody] AlbumSongRemote albumSongRemote)
    {
        RestInitializer.RemoteProvider.RemoveSongFromAlbum(albumSongRemote);
        return Ok();
    }

    /// <summary>
    /// Sets the playback position in seconds.
    /// </summary>
    /// <param name="seconds">The playback position in seconds.</param>
    /// <returns>HTTP result.</returns>
    [HttpPost]
    [Route("api/control/setPositionSeconds")]
    public IActionResult SetPositionSeconds([FromBody]double seconds)
    {
        RestInitializer.RemoteProvider.SetPositionSeconds(seconds);
        return Ok();
    }

    /// <summary>
    /// Sets the rating for the current song.
    /// </summary>
    /// <param name="rating">The new rating value.</param>
    /// <returns><c>true</c> if there is a song to set a rating for; otherwise <c>false</c>.</returns>
    [HttpPost]
    [Route("api/setRating/{rating}")]
    public bool SetRating(int rating)
    {
        return RestInitializer.RemoteProvider.SetRating(rating);
    }

    /// <summary>
    /// Sets a rating for multiple songs.
    /// </summary>
    /// <param name="songIdList">A list of song database ID numbers to set the rating for.</param>
    /// <param name="rating"></param>
    /// <returns><c>true</c> if the rating was set successfully; otherwise <c>false</c>.</returns>
    [HttpPost]
    [Route("api/setRatingByIds/{rating}")]
    public bool SetRating(int rating, [FromBody] List<int> songIdList)
    {
        return RestInitializer.RemoteProvider.SetRating(songIdList, rating);
    }

    /// <summary>
    /// Sets the volume of the currently playing song.
    /// </summary>
    /// <param name="volume">The new volume value.</param>
    /// <returns><c>true</c> if there is a song to set a volume for; otherwise <c>false</c>.</returns>
    [HttpPost]
    [Route("api/setSongVolume/{floatValue}")]
    public bool SetSongVolume(float volume)
    {
        return RestInitializer.RemoteProvider.SetSongVolume(volume);
    }

    /// <summary>
    /// Sets a volume for multiple songs.
    /// </summary>
    /// <param name="volume">The new volume value.</param>
    /// <param name="songIdList">A list of song database ID numbers to set the volume for.</param>
    /// <returns><c>true</c> if the volume was set successfully; otherwise <c>false</c>.</returns>
    [HttpPost]
    [Route("api/setSongVolumeByIds/{floatValue}")]
    public bool SetSongVolume(float volume, [FromBody] List<int> songIdList)
    {
        return RestInitializer.RemoteProvider.SetSongVolume(songIdList, volume);
    }

    /// <summary>
    /// Plays a song with a given database ID number or the next song if the given id is -1.
    /// </summary>
    /// <param name="id">The database ID number for the song to play.</param>
    [HttpPost]
    [Route("api/play/{id}")]
    public IActionResult Play(int id)
    {
        RestInitializer.RemoteProvider.Play(id);
        return Ok();
    }

    /// <summary>
    /// Scrambles the queue between the specified selected songs.
    /// </summary>
    /// <param name="scrambleIdList">A list of selected music file identifiers to scramble.</param>
    /// <returns>True if any songs were affected; otherwise false.</returns>
    [HttpPost]
    [Route("api/queueScrambleSelectedIds")]
    public bool ScrambleQueueSelected([FromBody] List<int> scrambleIdList)
    {
        return RestInitializer.RemoteProvider.ScrambleQueueSelected(scrambleIdList);
    }
    #endregion

    #region Album
    /// <summary>
    /// Gets the albums currently in the software database.
    /// </summary>
    /// <returns>A list of <see cref="AlbumRemote"/> class instances containing the album data.</returns>
    [HttpGet]
    [Route("api/getAlbums")]
    public List<AlbumRemote> GetAlbums()
    {
        return RestInitializer.RemoteProvider.GetAlbums();
    }

    /// <summary>
    /// Selects an album with a given name.
    /// </summary>
    /// <param name="name">The name of the album to select.</param>
    /// <returns><c>true</c> if the album was selected successfully; otherwise <c>false</c>.</returns>
    [HttpGet]
    [Route("api/selectAlbum/{albumName}")]
    public bool SelectAlbum(string name)
    {
        return RestInitializer.RemoteProvider.SelectAlbum(name);
    }

    /// <summary>
    /// Gets the current album.
    /// </summary>
    /// <returns>The current album name.</returns>
    [HttpGet]
    [Route("api/getCurrentAlbum")]
    public string GetCurrentAlbum()
    {
        return RestInitializer.RemoteProvider.CurrentAlbum;
    }

    /// <summary>
    /// Gets a list of songs which properties were changed (name, volume, rating).
    /// </summary>
    /// <returns>A list of songs which properties have been changed in the current album.</returns>
    [HttpGet]
    [Route("api/getChangedSongs")]
    public List<AlbumSongRemote> GetChangedSongs()
    {
        return RestInitializer.RemoteProvider.GetChangedSongs();
    }
    #endregion

    #region AmpSettings
    /// <summary>
    /// Sets the program volume.
    /// </summary>
    /// <param name="volume">The volume.</param>
    /// <returns>IHttpActionResult.</returns>
    [HttpPost]
    [Route("api/setAmpVolume/{floatValue}")]
    public IActionResult SetProgramVolume(float volume)
    {
        try
        {
            RestInitializer.RemoteProvider.ProgramVolume = volume;
            return Ok();
        }
        catch (Exception exception)
        {
            return BadRequest($"An exception occurred: '{exception.Message}'.");
        }
    }
    #endregion

    #region State
    /// <summary>
    /// Gets the current state of the amp# music player.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("api/state")]
    public PlayerStateRemote GetPlayerState()
    {
        return RestInitializer.RemoteProvider.GetPlayerState();
    }

    /// <summary>
    /// Gets a value if the queue was changed from the previous query.
    /// </summary>
    /// <returns>True if the queue was changed from the previous query, otherwise false.</returns>
    [HttpGet]
    [Route("api/queueChanged")]
    public bool QueueChanged()
    {
        return RestInitializer.RemoteProvider.QueueChanged();
    }

    /// <summary>
    /// Gets a value if the play list of the current album was changed from the previous query.
    /// </summary>
    /// <returns>True if the play list of the current album was changed from the previous query, otherwise false.</returns>
    [HttpGet]
    [Route("api/albumPlayListChanged")]
    public bool AlbumPlayListChanged()
    {
        return RestInitializer.RemoteProvider.AlbumPlayListChanged();
    }

    /// <summary>
    /// Gets a value if current music album was changed.
    /// </summary>
    /// <returns>True if current music album was changed, otherwise false.</returns>
    [HttpGet]
    [Route("api/albumChanged")]
    public bool AlbumChanged()
    {
        return RestInitializer.RemoteProvider.AlbumChanged;
    }

    /// <summary>
    /// Gets a value whether songs in the album have changed.
    /// </summary>
    [HttpGet]
    [Route("api/songsChanged")]
    public bool SongsChanged()
    {
        return RestInitializer.RemoteProvider.SongsChanged;
    }

    /// <summary>
    /// Gets or sets a value whether the randomization is enabled in the main form.
    /// </summary>
    [HttpGet]
    [Route("api/randomizing")]        
    public bool Randomizing()
    {
        return RestInitializer.RemoteProvider.Randomizing;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the stack queue is enabled.
    /// </summary>
    [HttpGet]
    [Route("api/stackQueue")]        
    public bool StackQueue()
    {
        return RestInitializer.RemoteProvider.StackQueue;
    }

    /// <summary>
    /// Gets or sets a value whether shuffling is enabled in the main form.
    /// </summary>
    [HttpGet]
    [Route("api/shuffle")]
    public bool Shuffle()
    {
        return RestInitializer.RemoteProvider.Shuffle;
    }

    /// <summary>
    /// Gets a value whether it is possible to jump to the previously played song.
    /// </summary>
    [HttpGet]
    [Route("api/canGoPrevious")]
    public bool CanGoPrevious()
    {
        return RestInitializer.RemoteProvider.CanGoPrevious;
    }
    #endregion

    #region Controls
    /// <summary>
    /// Executes simple commands such as play, pause, next, previous and so on.
    /// </summary>
    /// <returns>HTTP result.</returns>
    [HttpPost]
    [Route("api/control/{command}")]
    public IActionResult RunCommand(string command)
    {
        if (command == "next")
        {
            RestInitializer.RemoteProvider.GetNextSong();
            return Ok();
        }

        if (command == "previous")
        {
            RestInitializer.RemoteProvider.GetPrevSong();
            return Ok();
        }

        if (command == "pause")
        {
            RestInitializer.RemoteProvider.Pause();
            return Ok();
        }

        if (command == "play")
        {
            RestInitializer.RemoteProvider.Play();
            return Ok();
        }

        return BadRequest($"Command not found: '{command}'.");
    }
    #endregion

    #region Queue        
    /// <summary>
    /// Gets the saved queues for a specified album.
    /// </summary>
    /// <param name="albumName">Name of the album.</param>
    /// <returns>A list of <see cref="SavedQueueRemote"/> class instances.</returns>
    [HttpGet]
    [Route("api/getSavedQueues/{albumName}")]
    public List<SavedQueueRemote> GetSavedQueues(string albumName)
    {
        return RestInitializer.RemoteProvider.GetQueueList(albumName);
    }

    /// <summary>
    /// Inserts or appends to the queue the given song list.
    /// </summary>
    /// <param name="insert">Whether to insert or append to the queue.</param>
    /// <param name="queueList">A list of songs to be appended or inserted into the queue.</param>
    /// <returns>A list of queued songs in the current album.</returns>
    [HttpPost]
    [Route("api/queue/{insert}")]
    public List<AlbumSongRemote> Queue(bool insert, [FromBody] List<AlbumSongRemote> queueList)
    {
        RestInitializer.RemoteProvider.Queue(insert, queueList);
        if (RestInitializer.RemoteProvider.Filtered == FilterType.QueueFiltered) // refresh the queue list if it's showing..
        {
            RestInitializer.RemoteProvider.ShowQueue();
        }

        RestInitializer.RemoteProvider.RefreshPlaylist();

        return RestInitializer.RemoteProvider.GetQueuedSongs();
    }

    /// <summary>
    /// Gets the queued songs.
    /// </summary>
    /// <returns>A list of queued songs in the current album.</returns>
    [HttpGet]
    [Route("api/getQueuedSongs")]
    public List<AlbumSongRemote> GetQueuedSongs()
    {
        return RestInitializer.RemoteProvider.GetQueuedSongs();
    }

    /// <summary>
    /// Loads the specified queue into the amp# playlist.
    /// </summary>
    /// <param name="queueIndex">The queue index (Database ID number) for the queue to load.</param>
    /// <param name="append">If set to <c>true</c> the queue is appended to the previous queue.</param>
    /// <returns>HTTP result.</returns>
    [HttpPost]
    [Route("api/queueLoad/{queueIndex}")]
    public IActionResult RefreshLoadQueueStats(int queueIndex, [FromBody] bool append)
    {
        RestInitializer.RemoteProvider.RefreshLoadQueueStats(queueIndex, append);
        return Ok();
    }
    #endregion
}