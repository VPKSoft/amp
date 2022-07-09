﻿#region License
/*
MIT License

Copyright(c) 2022 Petteri Kautonen

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

using amp.Database.DataModel;
using amp.EtoForms.ExtensionClasses;
using amp.EtoForms.Forms;
using amp.EtoForms.Utilities;
using amp.Playback.Enumerations;
using Eto.Forms;
using EtoForms.Controls.Custom.EventArguments;

namespace amp.EtoForms;

partial class FormMain
{
    private async void FormMain_KeyDown(object? sender, KeyEventArgs e)
    {
        if (Equals(sender, tbSearch))
        {
            if (e.Key is Keys.Up or Keys.Down or Keys.PageDown or Keys.PageUp or Keys.Equal)
            {
                if (gvSongs.SelectedItem == null && gvSongs.DataStore.Any())
                {
                    gvSongs.SelectedRow = 0;
                }
                gvSongs.Focus();
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Keys.Add)
        {
            var selectedSongs = songs.Where(f => SelectedAlbumSongIds.Contains(f.Id)).Select(f => f.Id);
            await playbackOrder.ToggleQueue(songs, selectedSongs.ToArray());

            gvSongs.Invalidate();

            e.Handled = true;
            return;
        }

        if (e.Key == Keys.Enter)
        {
            if (gvSongs.SelectedItem != null)
            {
                var albumSong = (AlbumSong)gvSongs.SelectedItem;
                playbackManager.PlaySong(albumSong, true);
                e.Handled = true;
                return;
            }
        }

        if (e.Modifiers == Keys.None)
        {
            if (e.IsChar)
            {
                tbSearch.Text = tbSearch.Text.Insert(tbSearch.CaretIndex, e.KeyChar.ToString());
                tbSearch.CaretIndex++;
                tbSearch.Focus();
                e.Handled = true;
            }
        }
    }

    private long SelectedAlbumSongId
    {
        get
        {
            if (gvSongs.SelectedItem != null)
            {
                var songId = ((AlbumSong)gvSongs.SelectedItem).Id;
                return songId;
            }

            return 0;
        }
    }

    private IEnumerable<long> SelectedAlbumSongIds
    {
        get
        {
            foreach (var gvSongsSelectedItem in gvSongs.SelectedItems)
            {
                var songId = ((AlbumSong)gvSongsSelectedItem).Id;
                yield return songId;
            }
        }
    }

    private async void NextSongCommand_Executed(object? sender, EventArgs e)
    {
        await playbackManager.PlayNextSong(true);
    }

    private void CommandPlayPause_Executed(object? sender, EventArgs e)
    {
        if (SelectedAlbumSongId != 0)
        {
            var song = songs.First(f => f.Id == SelectedAlbumSongId);
            playbackManager.PlaySong(song, true);
        }
    }

    private void PlaybackPosition_ValueChanged(object? sender, ValueChangedEventArgs e)
    {
        playbackManager.PlaybackPositionPercentage = e.Value;
    }

    private async void GvSongsMouseDoubleClick(object? sender, MouseEventArgs e)
    {
        var song = songs.First(f => f.Id == SelectedAlbumSongId);
        if (song.Song?.PlayedByUser != null)
        {
            song.Song.PlayedByUser++;
            song.Song.ModifiedAtUtc = DateTime.UtcNow;
            context.AlbumSongs.Update(song);
            await context.SaveChangesAsync();
        }
        playbackManager.PlaySong(song, true);
    }

    private void FormMain_LocationChanged(object? sender, EventArgs e)
    {
        formAlbumImage.Reposition(this);
    }

    private void TbSearch_TextChanged(object? sender, EventArgs e)
    {
        var albumSongs = songs;

        if (!string.IsNullOrWhiteSpace(tbSearch.Text))
        {
            albumSongs = albumSongs.Where(f => f.Song!.Match(tbSearch.Text)).ToList();
        }

        gvSongs.DataStore = albumSongs;
    }

    private void FormMain_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        playbackManager.Dispose();
        formAlbumImage.Close();
        formAlbumImage.Dispose();
    }

    private async Task<AlbumSong?> GetNextSongFunc()
    {
        AlbumSong? result = null;
        await Application.Instance.InvokeAsync(async () =>
        {
            var nextSongData = await playbackOrder.NextSong(songs);
            result = songs[nextSongData.NextSongIndex];
            if (result.Song != null)
            {
                result.Song.PlayedByRandomize ??= 0;
                result.Song.PlayedByUser ??= 0;

                result.Song.PlayedByRandomize += nextSongData.PlayedByRandomize;
                result.Song.PlayedByUser += nextSongData.PlayedByUser;
                result.Song.ModifiedAtUtc = DateTime.UtcNow;

                context.Update(result);

                await context.SaveChangesAsync();
            }
        });

        return result;
    }

    private async void PlaybackManager_PlaybackStateChanged(object? sender, Playback.EventArguments.PlaybackStateChangedArgs e)
    {
        await Application.Instance.InvokeAsync(() =>
        {
            var song = songs.FirstOrDefault(f => f.SongId == e.SongId);
            lbSongsTitle.Text = song?.GetSongName() ?? string.Empty;
            btnPlayPause.CheckedChange -= PlayPauseToggle;
            btnPlayPause.Checked = e.PlaybackState == PlaybackState.Playing;
            btnPlayPause.CheckedChange += PlayPauseToggle;
        });
    }

    private void PlaybackManager_SongChanged(object? sender, Playback.EventArguments.SongChangedArgs e)
    {
        Application.Instance.Invoke(() =>
        {
            var song = songs.FirstOrDefault(f => f.SongId == e.SongId);
            songVolumeSlider.SuspendEventInvocation = true;
            songVolumeSlider.Value = song?.Song?.PlaybackVolume * 100 ?? 100;
            songVolumeSlider.SuspendEventInvocation = false;
            lbSongsTitle.Text = song?.GetSongName() ?? string.Empty;
            if (song != null)
            {
                formAlbumImage.Show(this, song);
            }
        });
    }

    private void PlaybackManager_SongSkipped(object? sender, Playback.EventArguments.SongSkippedEventArgs e)
    {
        Globals.LoggerSafeInvoke(async () =>
        {
            var albumSong = songs.FirstOrDefault(f => f.SongId == e.SongId);
            if (albumSong != null)
            {
                albumSong.Song!.SkippedEarlyCount = albumSong.Song.SkippedEarlyCount == null
                    ? 1
                    : albumSong.Song.SkippedEarlyCount + 1;
                albumSong.Song.ModifiedAtUtc = DateTime.UtcNow;
                context.Update(albumSong);
                await context.SaveChangesAsync();
            }
        });
    }

    private readonly FormAlbumImage formAlbumImage = new();

    private async void PlayPauseToggle(object? sender, CheckedChangeEventArguments e)
    {
        btnPlayPause.CheckedChange -= PlayPauseToggle;
        if (e.Checked)
        {
            await playbackManager.PlayOrResume();
        }
        else
        {
            if (playbackManager.PlaybackState == PlaybackState.Playing)
            {
                playbackManager.Pause();
            }
        }

        btnPlayPause.CheckedChange += PlayPauseToggle;
    }

    private void BtnShuffleToggle_CheckedChange(object? sender, CheckedChangeEventArguments e)
    {
        playbackManager.Shuffle = e.Checked;
    }

    private async void PlayNextSongClick(object? sender, EventArgs e)
    {
        await playbackManager.PlayNextSong(true);
    }

    private async Task<AlbumSong?> GetSongById(long songId)
    {
        return await Application.Instance.InvokeAsync(AlbumSong? () =>
        {
            return songs.FirstOrDefault(f => f.SongId == songId);
        });
    }

    private void PlaybackManager_PlaybackPositionChanged(object? sender, Playback.EventArguments.PlaybackPositionChangedArgs e)
    {
        Application.Instance.Invoke(() =>
        {
            playbackPosition.SuspendEventInvocation = true;
            playbackPosition.Value = e.CurrentPosition / e.PlaybackLength * 100;
            playbackPosition.SuspendEventInvocation = false;
            lbPlaybackPosition.Text =
                "-" + TimeSpan.FromSeconds(e.PlaybackLength - e.CurrentPosition).ToString(@"hh\:mm\:ss");
        });
    }

    // Eto.Forms localization.
    private void Instance_LocalizeString(object? sender, LocalizeEventArgs e)
    {
        try
        {
            e.LocalizedText = e.Text switch
            {
                null => null,
                "&File" => Shared.Localization.EtoForms.File,
                "&Help" => Shared.Localization.EtoForms.Help,
                "About" => Shared.Localization.EtoForms.About,
                "Hide amp.EtoForms" => Shared.Localization.Mac.HideAmpEtoForms,
                "Hide" => Shared.Localization.UI.Hide,
                "Hides the main amp.EtoForms window" => Shared.Localization.Mac.HidesTheMainAmpEtoFormsWindow,
                "Hide Others" => Shared.Localization.Mac.HideOthers,
                "Hides all other application windows" => Shared.Localization.Mac.HidesAllOtherApplicationWindows,
                "Show All" => Shared.Localization.Mac.ShowAll,
                "Show All Windows" => Shared.Localization.Mac.ShowAllWindows,
                "Minimize" => Shared.Localization.UI.Minimize,
                "Zoom" => Shared.Localization.UI.Zoom,
                "Close" => Shared.Localization.UI.Close,
                "Bring All To Front" => Shared.Localization.Mac.BringAllToFront,
                "Cut" => Shared.Localization.UI.Cut,
                "Copy" => Shared.Localization.UI.Copy,
                "Paste" => Shared.Localization.UI.Paste,
                "Paste and Match Style" => Shared.Localization.Mac.PasteAndMatchStyle,
                "Delete" => Shared.Localization.UI.Delete,
                "Select All" => Shared.Localization.UI.SelectAll,
                "Undo" => Shared.Localization.UI.Undo,
                "Redo" => Shared.Localization.UI.Redo,
                "Enter Full Screen" => Shared.Localization.Mac.EnterFullScreen,
                "Page Setup..." => Shared.Localization.UI.PageSetup,
                "Print..." => Shared.Localization.UI.Print,
                "&Edit" => Shared.Localization.UI.Edit,
                "&Window" => Shared.Localization.UI.Window,
                _ => throw new ArgumentOutOfRangeException(nameof(e.LocalizedText)),
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            Globals.Logger?.Information("Localization needed: '{text}'.", e.Text);
        }
    }

    private async void AddDirectoryToDatabase_Executed(object? sender, EventArgs e)
    {
        await AddDirectory(sender?.Equals(addDirectoryToAlbum) == true);
    }

    private async void AddFilesToDatabase_Executed(object? sender, EventArgs e)
    {
        await AddAudioFiles(sender?.Equals(addFilesToAlbum) == true);
    }
}