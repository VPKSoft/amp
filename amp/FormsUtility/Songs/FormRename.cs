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
using System.Windows.Forms;
using amp.UtilityClasses;
using VPKSoft.LangLib;

namespace amp.FormsUtility.Songs;

/// <summary>
/// A dialog to rename a song internally; No changes to the file system is done.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormRename : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormRename"/> class.
    /// </summary>
    public FormRename()
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

    // a value for a previous name of the song, so a change can be detected..
    string lastName = string.Empty;

    // enable/disable the OK button depending on the validity of the new name text box value..
    private void tbNewSongName_TextChanged(object sender, EventArgs e)
    {
        bOK.Enabled = tbNewSongName.Text.Trim() != string.Empty && tbNewSongName.Text != lastName;
    }

    /// <summary>
    /// Displays the dialog querying a new name for a song.
    /// </summary>
    /// <param name="mf">The <see cref="MusicFile"/> class instance of which name to change.</param>
    /// <returns>A <see cref="string"/> value for a new name for the music file if the user accepted the dialog; otherwise <see cref="string.Empty"/>.</returns>
    public static string Execute(MusicFile mf)
    {
        FormRename rename = new FormRename
        {
            tbNewSongName = {Text = mf.ToString(false)}, lastName = mf.ToString(false)
        };
        if (rename.ShowDialog() == DialogResult.OK)
        {
            return rename.tbNewSongName.Text;
        }

        return string.Empty;
    }

    // the dialog form is shown; focus and select all from the name text box..
    private void frmRename_Shown(object sender, EventArgs e)
    {
        bOK.Enabled = false;
        tbNewSongName.SelectAll();
        tbNewSongName.Focus();
    }
}