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
using System.Drawing;
using System.Windows.Forms;
using VPKSoft.LangLib;

namespace amp.UtilityClasses.Settings;

/// <summary>
/// A form form creating instructions for how to display a music file in the playlist box.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormAlbumNaming : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormAlbumNaming"/> class.
    /// </summary>
    public FormAlbumNaming()
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

        ListItems(); // list the "tag" items to the list box..

        activeTextBox = tbAlbumNaming;
    }

    /// <summary>
    /// A class to hold the "tag" and description pairs in the list box.
    /// </summary>
    private class TagDescriptionPair
    {
        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { private get; set; } = string.Empty;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            return Tag + $" ({Description})";
        }
    }

    /// <summary>
    /// Lists the localized items to the "tag" list box.
    /// </summary>
    private void ListItems()
    {
        lbDragItems.Items.Clear();
        lbDragItems.Items.Add(new TagDescriptionPair
        {
            Tag = "#ARTIST? - #", // the artist..
            Description = DBLangEngine.GetMessage("msgArtists", "Artist|As in an artist(s) of a music file")
        });

        lbDragItems.Items.Add(new TagDescriptionPair
        {
            Tag = "#ALBUM? - #", // the album..
            Description = DBLangEngine.GetMessage("msgAlbum", "Album|As in a name of an album of a music file")
        });

        lbDragItems.Items.Add(new TagDescriptionPair
        {
            // ReSharper disable once StringLiteralTypo
            Tag = @"#TRACKNO?(^) #", // the title..
            Description = DBLangEngine.GetMessage("msgTrackNO", "Track number|As a track number of a song in a music album")
        });

        lbDragItems.Items.Add(new TagDescriptionPair
        {
            Tag = "#TITLE?#", // the title..
            Description = DBLangEngine.GetMessage("msgTitle", "Title|As in a title of a music file")
        });

        lbDragItems.Items.Add(new TagDescriptionPair
        {
            Tag = "#QUEUE? [^]#", // the queue index number..
            Description = DBLangEngine.GetMessage("msgQueueTag", "Queue index|As a queue index of a music file")
        });

        lbDragItems.Items.Add(new TagDescriptionPair
        {
            Tag = "#ALTERNATE_QUEUE?[ *=^]#", // the alternate queue index..
            Description = DBLangEngine.GetMessage("msgAlternateQueue", "Alternate queue index|As an alternate queue index of a music file")
        });

        lbDragItems.Items.Add(new TagDescriptionPair
        {
            Tag = "#RENAMED?#", // a renamed song name..
            Description = DBLangEngine.GetMessage("msgMusicFileRenamed", "I named this song my self|The user has renamed the song him self")
        });
    }

    /// <summary>
    /// "Initialize" drag and drop operation from a list box.
    /// </summary>
    /// <param name="sender">The sender from an event. This will be cast into a ListBox class.</param>
    /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
    /// <param name="index">An index of a list box item at the mouse coordinates.</param>
    /// <param name="lb">The <paramref name="sender"/> cast into a ListBox.</param>
    /// <param name="item">An item of the list box at the mouse coordinates..</param>
    /// <returns>True if an item was found in the current mouse coordinates; otherwise false.</returns>
    private bool InitDragDropListBox(object sender, MouseEventArgs e, out int index, out ListBox lb, out object item)
    {
        // cast the sender parameter into a ListBox..
        lb = (ListBox)sender;

        // get an item's index at the current mouse coordinates..
        index = lb.IndexFromPoint(e.X, e.Y);

        if (!e.Button.HasFlag(MouseButtons.Right))
        {
            item = null; // ..set the item to null and..
            return false;
        }

        // if the index is invalid..
        if (index < 0 || index >= lb.Items.Count)
        {
            item = null; // ..set the item to null and..
            return false; // ..return false..
        }

        item = lb.Items[index]; // ..set the item's value and..
        return true; // ..return true..
    }

    private void lbDragItems_MouseDown(object sender, MouseEventArgs e)
    {
        if (!InitDragDropListBox(sender, e, out _, out ListBox lb, out object dragDrop))
        {
            return; // the click was "invalid" so just return..
        }

        // create a copy effect to allow dragging tags from the list box containing all the
        // tags in the database to the current photo's tag list..
        lb.DoDragDrop(dragDrop, DragDropEffects.Copy);
    }

    private void tbCommon_Dragging(object sender, DragEventArgs e)
    {
        // TagDescriptionPairs will be..
        if (e.Data.GetDataPresent(typeof(TagDescriptionPair)))
        {
            e.Effect = DragDropEffects.Copy; // ..copied..

            TextBox textBox = (TextBox)sender;
            if (!textBox.Focused)
            {
                textBox.Focus();
            }

            Point point = textBox.PointToClient(new Point(e.X, e.Y));
            int idx1 = textBox.GetCharIndexFromPosition(point);

            // pseudo detection of the last "character", i.e. the end position..
            int idx2 = textBox.GetCharIndexFromPosition(new Point(point.X + 15, point.Y));

            if (idx1 >= 0 && idx2 >= 0 && idx1 != idx2)
            {
                textBox.SelectionStart = idx1;
                textBox.SelectionLength = 0;
            }
            else
            {
                textBox.SelectionStart = textBox.Text.Length;
                textBox.SelectionLength = 0;
            }
        }
        else
        {
            // indicate an "invalid" drag & drop operation..
            e.Effect = DragDropEffects.None;
        }
    }

    // an event handler for drag &amp; drop for the naming instructions..
    private void tbCommon_DragDrop(object sender, DragEventArgs e)
    {
        // if the data is of type of string set the effect to move..
        if (e.Data.GetDataPresent(typeof(TagDescriptionPair)) &&
            sender.Equals(activeTextBox)) // the sender must be one of the two formula text boxes..
        {
            e.Effect = DragDropEffects.Move; // ensure a move effect..
            TagDescriptionPair dropPair = (TagDescriptionPair)e.Data.GetData(typeof(TagDescriptionPair));

            // set the text of the dropped item..
            activeTextBox.SelectedText = dropPair.Tag;
        }
    }

    // adds a selected item to the song naming instructions..
    private void btAddToNaming_Click(object sender, EventArgs e)
    {
        if (lbDragItems.SelectedIndex >= 0)
        {
            activeTextBox.SelectedText = ((TagDescriptionPair)lbDragItems.SelectedItem).Tag;
        }
    }

    // resets the song naming rules to default values..
    private void btDefaultNaming_Click(object sender, EventArgs e)
    {
        // ReSharper disable once StringLiteralTypo
        tbAlbumNaming.Text = @"    #ARTIST? - ##ALBUM? - ##TRACKNO?(^) ##TITLE?##QUEUE? [^]##ALTERNATE_QUEUE?[ *=^]#";
        tbAlbumNamingRenamed.Text = @"    #RENAMED?##QUEUE? [^]##ALTERNATE_QUEUE?[ *=^]#";
    }

    // validates the "recipe" for the song naming..
    private void tbCommonNaming_TextChanged(object sender, EventArgs e)
    {
        // avoid replicating the code..
        Label label = sender.Equals(tbAlbumNaming) ? lbNamingSampleValue : lbNamingSampleRenamedValue;
        TextBox textBox = (TextBox)sender;
        label.Text =
            MusicFile.GetString(textBox.Text,
                DBLangEngine.GetMessage("msgArtists", "Artist|As in an artist(s) of a music file"),
                DBLangEngine.GetMessage("msgAlbum", "Album|As in a name of an album of a music file"),
                1,
                DBLangEngine.GetMessage("msgTitle", "Title|As in a title of a music file"),
                DBLangEngine.GetMessage("msgMusicFile", "A01 Song Name|A sample file name without path of a music file name"),
                1, 2,
                DBLangEngine.GetMessage("msgMusicFileRenamed", "I named this song my self|The user has renamed the song him self"),
                DBLangEngine.GetStatMessage("msgError", "Error|A common error that should be defined in another message"), out bool error);

        bool formulaInText = // check that the formula has been parsed properly..
            label.Text.Contains("#ARTIST") ||
            label.Text.Contains("#ALBUM") ||
            // ReSharper disable once StringLiteralTypo
            label.Text.Contains("#TRACKNO") ||
            label.Text.Contains("#QUEUE") ||
            label.Text.Contains("#ALTERNATE_QUEUE") ||
            label.Text.Contains("#RENAMED");

        error |= formulaInText;


        // indicate invalid naming formula..
        textBox.ForeColor = error || string.IsNullOrWhiteSpace(Text) ? Color.Red : SystemColors.WindowText;
               

        bOK.Enabled = !error && textBox.Text.Trim() != string.Empty;
    }

    // a click handler for the OK button; the settings are also saved..
    private void bOK_Click(object sender, EventArgs e)
    {
        // save the settings..
        Program.Settings.AlbumNaming = tbAlbumNaming.Text;
        Program.Settings.AlbumNamingRenamed = tbAlbumNamingRenamed.Text;
        DialogResult = DialogResult.OK;
    }

    // a field to save the active text box so the text boxes can be used in common events..
    private TextBox activeTextBox;

    // a text box was focused..
    private void tbCommon_Enter(object sender, EventArgs e)
    {
        activeTextBox = (TextBox)sender;
        if (sender.Equals(tbAlbumNaming))
        {
            tbAlbumNaming.BackColor = Color.PaleTurquoise;
            tbAlbumNamingRenamed.BackColor = SystemColors.Window;
        }
        else if (sender.Equals(tbAlbumNamingRenamed))
        {
            tbAlbumNaming.BackColor = SystemColors.Window;
            tbAlbumNamingRenamed.BackColor = Color.PaleTurquoise;
        }
    }

    // the dialog form is shown so focus to the upper-most text box and set the
    // text for the naming rule text boxes..
    private void FormAlbumNaming_Shown(object sender, EventArgs e)
    {
        tbAlbumNaming.Focus();
        tbAlbumNaming.Text = Program.Settings.AlbumNaming;
        tbAlbumNamingRenamed.Text = Program.Settings.AlbumNamingRenamed;
    }

    // select the corresponding text from the active formula text box if the selected item 
    // is contained within the text in the text box..
    private void lbDragItems_SelectedIndexChanged(object sender, EventArgs e)
    {
        ListBox lb = (ListBox)sender; // the list box..
        if (lb.SelectedItem != null) // check if an item is selected..
        {
            TagDescriptionPair pair = (TagDescriptionPair)lb.SelectedItem; // get the selected item..
            if (activeTextBox.Text.Contains(pair.Tag)) // if the text is there..
            {
                // ..select the text..
                activeTextBox.Select(activeTextBox.Text.IndexOf(pair.Tag, StringComparison.Ordinal), pair.Tag.Length);
                activeTextBox.Focus(); // set the focus to the text box..
            }
        }
    }
}