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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using amp.UtilityClasses;
using TagLib;
using VPKSoft.LangLib;
using File = TagLib.File;

namespace amp.FormsUtility.Information;

/// <summary>
/// A form to display IDvX Tag information related to the music file.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormTagInfo : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormTagInfo"/> class.
    /// </summary>
    public FormTagInfo()
    {
        InitializeComponent();

        // ReSharper disable once StringLiteralTypo
        DBLangEngine.DBName = "lang.sqlite";

        if (Utils.ShouldLocalize() != null)
        {
            DBLangEngine.InitializeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
            return; // After localization don't do anything more.
        }

        DBLangEngine.InitializeLanguage("amp.Messages");
    }

    /// <summary>
    /// The <see cref="MusicFile"/> class instance of which information to display within the form.
    /// </summary>
    private MusicFile mf;

    /// <summary>
    /// A value to hold the pictures stored in a music file.
    /// </summary>
    private readonly List<IPicture> pictures = new List<IPicture>();

    private int picIndex = -1;

    /// <summary>
    /// Loads the IDvX Tag information from the music file.
    /// </summary>
    internal void LoadTag()
    {
        // IDisposable so using..
        using (File tagFile = File.Create(mf.FullFileName))
        {
            if (string.IsNullOrEmpty(tagFile.Tag.Title))
            {
                Text = Path.GetFileNameWithoutExtension(mf.FullFileName);
            }
            else
            {
                if (!string.IsNullOrEmpty(tagFile.Tag.FirstAlbumArtist))
                {
                    Text = tagFile.Tag.FirstAlbumArtist + @" - " + tagFile.Tag.Title;
                }
                else
                {
                    Text = tagFile.Tag.Title;
                }
            }

            tbAlbum.Text = tagFile.Tag.Album;
            tbArtists.Lines = tagFile.Tag.AlbumArtists;
            tbTitle.Text = tagFile.Tag.Title;
            tbPerformers.Lines = tagFile.Tag.Performers;
            tbComments.Text = tagFile.Tag.Comment;
            tbComposers.Lines = tagFile.Tag.Composers;
            tbCopyright.Text = tagFile.Tag.Copyright;
            tbGenres.Lines = tagFile.Tag.Genres;
            tbConductor.Text = tagFile.Tag.Conductor;
            tbTrack.Text = tagFile.Tag.Track.ToString();
            tbTrackCount.Text = tagFile.Tag.TrackCount.ToString();
            tbYear.Text = tagFile.Tag.Year.ToString();
            tbAudioBitrate.Text = tagFile.Properties.AudioBitrate.ToString();
            tbAudioChannels.Text = tagFile.Properties.AudioChannels.ToString();
            tbAudioSampleRate.Text = tagFile.Properties.AudioSampleRate.ToString();
            tbBitsPerSample.Text = tagFile.Properties.BitsPerSample.ToString();
            tbDuration.Text = tagFile.Properties.Duration.ToString(@"mm\:ss");
            tbFileName.Text = mf.FileNameNoPath;
            tbFullFileName.Text = mf.FullFileName;


            foreach (ICodec codec in tagFile.Properties.Codecs)
            {
                if (codec == null)
                {
                    continue;
                }

                tbCodecs.Text += codec.Description + @", ";
            }

            tbCodecs.Text = tbCodecs.Text.TrimEnd(' ', ',');

            pictures.AddRange(tagFile.Tag.Pictures);

            tbLyrics.Text = tagFile.Tag.Lyrics;

            if (mf.SongImage != null)
            {
                pbAlbum.Image = mf.SongImage;
            }
            else
            {
                ShowNextPic();
            }
        }
        btOK.Focus();
        btRemoveImage.Enabled = mf.SongImage != null;
    }

    /// <summary>
    /// Shows the next picture from the IDvX Tag information if any exists.
    /// </summary>
    internal void ShowNextPic()
    {
        picIndex++;
        if (picIndex >= pictures.Count)
        {
            picIndex = 0;
        }
        if (pictures.Count > 0)
        {
            MemoryStream ms = new MemoryStream(pictures[picIndex].Data.Data) { Position = 0 };
            Image im = Image.FromStream(ms);
            pbAlbum.Image = im;
        }
    }

    /// <summary>
    /// Shows the previous picture from the IDvX Tag information if any exists.
    /// </summary>
    internal void ShowPreviousPic()
    {
        picIndex--;
        if (picIndex < 0)
        {
            picIndex = 0;
        }
        if (pictures.Count > 0)
        {
            MemoryStream ms = new MemoryStream(pictures[picIndex].Data.Data) { Position = 0 };
            Image im = Image.FromStream(ms);
            pbAlbum.Image = im;
        }
    }

    /// <summary>
    /// Displays the dialog and shows the IDvX Tag information of the music file if any.
    /// </summary>
    /// <param name="musicFile">The <see cref="MusicFile"/> class instance of which IDvX Tag information to display.</param>
    /// <param name="parent">The parent <see cref="Form"/>.</param>
    public static void Execute(MusicFile musicFile, Form parent)
    {
        FormTagInfo frm = new FormTagInfo
        {
            mf = musicFile,
            Owner = parent
        };
        frm.ShowDialog();
    }

    // the user wants to see the next picture..
    private void tsbNext_Click(object sender, EventArgs e)
    {
        ShowNextPic();
    }

    // the user wants to see the previous picture..
    private void tsbPrevious_Click(object sender, EventArgs e)
    {
        ShowPreviousPic();
    }

    // when the form is shown, display the IDvX Tag information..
    private void FormTagInfo_Shown(object sender, EventArgs e)
    {
        LoadTag();
    }

    // displays the file in the windows explorer if the user clicks the full file name label..
    // https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
    private void lbFullFileName_Click(object sender, EventArgs e)
    {
        string argument = "/select, \"" + mf.FullFileName + "\"";

        Process.Start("explorer.exe", argument);
    }

    private void pbAlbum_Click(object sender, EventArgs e)
    {
        var formMain = (FormMain)Application.OpenForms[0];
        if (mf != null)
        {
            if (odImageFile.ShowDialog() == DialogResult.OK)
            {
                mf.SongImage = Image.FromFile(odImageFile.FileName);
                pbAlbum.Image = mf.SongImage;
                amp.SQLiteDatabase.Database.SaveImage(mf, FormMain.Connection);
                formMain.UpdateSongName();
                btRemoveImage.Enabled = mf.SongImage != null;
            }
        }
    }

    private void btRemoveImage_Click(object sender, EventArgs e)
    {
        var formMain = (FormMain)Application.OpenForms[0];
        mf.SongImage = null;
        btRemoveImage.Enabled = mf.SongImage != null;
        formMain.UpdateSongName();
        pbAlbum.Image = mf.SongImage;
        amp.SQLiteDatabase.Database.SaveImage(mf, FormMain.Connection);
        ShowNextPic();
    }
}