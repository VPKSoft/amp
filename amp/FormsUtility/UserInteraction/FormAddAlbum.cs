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
using VPKSoft.LangLib;

namespace amp.FormsUtility.UserInteraction;

/// <summary>
/// A dialog for adding new albums to the database.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormAddAlbum : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormAddAlbum"/> class.
    /// </summary>
    public FormAddAlbum()
    {
        InitializeComponent();
        DBLangEngine.DBName = "lang.sqlite";
        if (Utils.ShouldLocalize() != null)
        {
            DBLangEngine.InitializeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
            return; // After localization don't do anything more.
        }

        DBLangEngine.InitializeLanguage("amp.Messages");

        if (DefaultAlbumName == default)
        {
            DefaultAlbumName = amp.SQLiteDatabase.Database.GetDefaultAlbumName(FormMain.Connection);
        }
    }

    /// <summary>
    /// Gets or sets the default album name.
    /// </summary>
    private static string DefaultAlbumName { get; set; }

    /// <summary>
    /// Gets or sets the previous name of the album in case of a rename situation.
    /// </summary>
    /// <value>The name of the previous.</value>
    private string PreviousName { get; set; }

    /// <summary>
    /// Displays the dialog and with an optional album name.
    /// </summary>
    /// <param name="name">The optional name for the album.</param>
    /// <param name="dialogTitle">The title (Text property) for the dialog.</param>
    /// <returns>A name for an album in case the user accepted the dialog; otherwise string.Empty.</returns>
    public static string Execute(string dialogTitle, string name = "")
    {
        FormAddAlbum form = new FormAddAlbum { tbAlbumName = { Text = name }, PreviousName = name, Text = dialogTitle };
        if (form.ShowDialog() == DialogResult.OK)
        {
            return form.tbAlbumName.Text;
        }

        return string.Empty;
    }

    private void tbAlbumName_TextChanged(object sender, EventArgs e)
    {
        // not ok if empty or only white space..
        bOK.Enabled = tbAlbumName.Text.Trim().Length > 0 && tbAlbumName.Text != DefaultAlbumName &&
                      tbAlbumName.Text != PreviousName;
    }
}