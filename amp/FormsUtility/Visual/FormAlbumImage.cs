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
using System.Drawing;
using System.IO;
using amp.Properties;
using amp.UtilityClasses;
using TagLib;
using VPKSoft.LangLib;

namespace amp.FormsUtility.Visual;

/// <summary>
/// A floating form to display the album image of the currently playing song.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormAlbumImage : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormAlbumImage"/> class.
    /// </summary>
    public FormAlbumImage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets a singleton instance of this form.
    /// </summary>
    public static FormAlbumImage ThisInstance;

    /// <summary>
    /// The main form of the application (for to activate the main form in case this form was activated).
    /// </summary>
    private static FormMain activateWindow;

    // a flag indicating whether the form was shown the first time..
    private static bool firstShow = true;

    /// <summary>
    /// Repositions the <see cref="ThisInstance"/> of this form.
    /// </summary>
    /// <param name="mw">The main form's instance.</param>
    /// <param name="top">The top position for the instance of this form.</param>
    public static void Reposition(FormMain mw, int top)
    {
        if (ThisInstance != null)
        {
            activateWindow = mw;
            ThisInstance.Left = mw.Left + mw.Width;
            ThisInstance.Top = top;
        }
    }

    /// <summary>
    /// Displays this form with a given music file at a given position.
    /// </summary>
    /// <param name="mw">The main form's instance.</param>
    /// <param name="mf">The <see cref="MusicFile"/> class instance for which album image to display on this form.</param>
    /// <param name="top">The top position for the instance of this form.</param>
    public static void Show(FormMain mw, MusicFile mf, int top)
    {
        if (ThisInstance == null)
        {
            ThisInstance = new FormAlbumImage {Owner = mw};
        }
        mf.LoadPic();
        try
        {
            if (mf.SongImage != null)
            {
                ThisInstance.pbAlbum.Image = mf.SongImage;
            }
            else if (mf.Pictures != null && mf.Pictures.Length > 0)
            {
                IPicture pic = mf.Pictures[0];
                MemoryStream ms = new MemoryStream(pic.Data.Data) {Position = 0};
                Image im = Image.FromStream(ms);
                ThisInstance.pbAlbum.Image = im;
            }
            else
            {
                if (Program.Settings.AutoHideAlbumImage)
                {
                    ThisInstance.Visible = false;
                    return;
                }
                ThisInstance.pbAlbum.Image = Resources.music_note;
            }
        }
        catch
        {
            if (Program.Settings.AutoHideAlbumImage)
            {
                ThisInstance.Visible = false;
                return;
            }
            ThisInstance.pbAlbum.Image = Resources.music_note;
        }

        ThisInstance.Visible = ThisInstance.pbAlbum.Image != null;
        if (firstShow && ThisInstance.Visible)
        {
            mw.BringToFront();
        }

        Reposition(mw, top);
    }

    // re-activate the main form in case this form was activated..
    private void FormAlbumImage_Activated(object sender, EventArgs e)
    {
        activateWindow?.Activate();
    }
}