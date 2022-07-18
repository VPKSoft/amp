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
using System.Reflection;
using System.Windows.Forms;
using VPKSoft.LangLib;

namespace amp.UtilityClasses.Settings;

/// <summary>
/// A form form creating instructions for how to display a music file in the playlist box.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormThemeSettings : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormAlbumNaming"/> class.
    /// </summary>
    public FormThemeSettings()
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
    /// Initializes a new instance of the <see cref="FormAlbumNaming"/> class.
    /// </summary>
    /// <param name="settings">The theme settings to edit.</param>
    public FormThemeSettings(ThemeSettings settings) : this()
    {
        ThemeSettings = settings;
    }

    /// <summary>
    /// Gets or sets the theme used with the software.
    /// </summary>
    private ThemeSettings ThemeSettings { get; set; }

    // the color selection has changed, set the color to a possibly selected item..
    private void colorWheel_ColorChanged(object sender, EventArgs e)
    {
        if (SuspendColorChange || listThemeColors.SelectedItem == null)
        {
            return;
        }

        colorEditor.Color = colorWheel.Color;
        pnColorDisplay.BackColor = colorWheel.Color;

        var item = (ColorStringProperty) listThemeColors.SelectedItem;
        item.Color = colorWheel.Color;

        var property = ThemeSettings.GetType().GetProperty(item.Name);
        property?.SetValue(ThemeSettings, item.Color);

        FormMain.ThemeMainForm(ThemeSettings);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the color change event should be ignored.
    /// </summary>
    internal bool SuspendColorChange { get; set; }

    // the color has been changed, update the other controls..
    private void colorEditor_ColorChanged(object sender, EventArgs e)
    {
        if (SuspendColorChange)
        {
            return;
        }

        SuspendColorChange = true;
        colorWheel.Color = colorEditor.Color;
        pnColorDisplay.BackColor = colorEditor.Color;
        SuspendColorChange = false;
    }

    /// <summary>
    /// A class to hold a property value with a color and a property name.
    /// </summary>
    internal class ColorStringProperty
    {
        /// <summary>
        /// Gets or sets the value of the color property.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// A class to hold a property value with an image and a property name.
    /// </summary>
    internal class ImageStringProperty
    {
        /// <summary>
        /// Gets or sets the value of the color property.
        /// </summary>
        public Image Image { get; set; }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Lists the theme colors and images to the GUI.
    /// </summary>
    private void ListThemeData()
    {
        listThemeColors.Items.Clear();
        listThemeImages.Items.Clear();

        var properties = ThemeSettings.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.PropertyType == typeof(Color))
            {
                listThemeColors.Items.Add(new ColorStringProperty
                    {Color = (Color)propertyInfo.GetValue(ThemeSettings), Name = propertyInfo.Name});
            }

            if (propertyInfo.PropertyType == typeof(Image))
            {
                listThemeImages.Items.Add(new ImageStringProperty
                    {Image = (Image)propertyInfo.GetValue(ThemeSettings), Name = propertyInfo.Name});
            }
        }
    }

    // the form is shown, show the data..
    private void FormTheSettings_Shown(object sender, EventArgs e)
    {
        ListThemeData();
    }

    // display the color when the list box selection changes..
    private void listThemeColors_SelectedValueChanged(object sender, EventArgs e)
    {
        if (SuspendColorChange)
        {
            return;
        }

        SuspendColorChange = true;
        var listBox = (ListBox) sender;
        var item = (ColorStringProperty) listBox.SelectedItem;
        colorWheel.Color = item.Color;
        pnColorDisplay.BackColor = item.Color;
        SuspendColorChange = false;
    }

    // the user clicked the color panel, so display a color dialog to pick a color..
    private void pnColorDisplay_Click(object sender, EventArgs e)
    {
        var panel = (Panel) sender;
        cdMain.Color = panel.BackColor;
        if (cdMain.ShowDialog() == DialogResult.OK)
        {
            colorWheel.Color = cdMain.Color;
            pnColorDisplay.BackColor = cdMain.Color;
        }
    }

    // the selected item in the theme image list box was changed, so do display the current item data..
    private void listThemeImages_SelectedValueChanged(object sender, EventArgs e)
    {
        var listBox = (ListBox) sender;
        var item = (ImageStringProperty) listBox.SelectedItem;

        if (item.Image == null)
        {
            pnImage.BackgroundImage = null;
            return;
        }

        if (item.Image.Width >= pnImage.Width || item.Image.Height >= pnImage.Height)
        {
            pnImage.BackgroundImageLayout = ImageLayout.Zoom;
        }
        else
        {
            pnImage.BackgroundImageLayout = ImageLayout.Center;
        }

        pnImage.BackgroundImage = item.Image;
    }

    // the user clicked the image panel, so do display an open file dialog to select a new image..
    private void pnImage_Click(object sender, EventArgs e)
    {
        if (listThemeImages.SelectedItem == null)
        {
            return;
        }

        if (fdOpenImage.ShowDialog() == DialogResult.OK)
        {
            var item = (ImageStringProperty) listThemeImages.SelectedItem;
            item.Image = Image.FromFile(fdOpenImage.FileName);

            if (item.Image.Width >= pnImage.Width || item.Image.Height >= pnImage.Height)
            {
                pnImage.BackgroundImageLayout = ImageLayout.Zoom;
            }
            else
            {
                pnImage.BackgroundImageLayout = ImageLayout.Center;
            }

            var property = ThemeSettings.GetType().GetProperty(item.Name);
            property?.SetValue(ThemeSettings, item.Image);

            pnImage.BackgroundImage = item.Image;

            FormMain.ThemeMainForm(ThemeSettings);
        }
    }

    // the user selected OK, so do save the default theme..
    private void bOK_Click(object sender, EventArgs e)
    {
        ThemeSettings.SaveAsDefaultTheme();
        DialogResult = DialogResult.OK;
    }

    // the user selected the default dark or light theme from the menu..
    private void mnuLightDarkTheme_Click(object sender, EventArgs e)
    {
        ThemeSettings = sender.Equals(mnuSetDefaultLightTheme)
            ? ThemeSettings.DefaultThemeLight
            : ThemeSettings.DefaultThemeDark;
        ListThemeData();
        FormMain.ThemeMainForm(ThemeSettings);
    }

    // saves the current theme as the default theme, same as the OK button press..
    private void mnuSaveAsDefaultTheme_Click(object sender, EventArgs e)
    {
        ThemeSettings.Save(ThemeSettings.DefaultFileName);
    }

    // the user wishes to load the theme..
    private void mnuLoadTheme_Click(object sender, EventArgs e)
    {
        if (fdOpenTheme.ShowDialog() == DialogResult.OK)
        {
            ThemeSettings.Load(fdOpenTheme.FileName);
            ListThemeData();
        }
    }

    // the user wishes to save the theme..
    private void mnuSaveThemeAs_Click(object sender, EventArgs e)
    {
        if (sdXmlTheme.ShowDialog() == DialogResult.OK)
        {
            ThemeSettings.Save(sdXmlTheme.FileName);
        }
    }

    // the user canceled the editing, reload the previous main form theme..
    private void bCancel_Click(object sender, EventArgs e)
    {
        FormMain.DefaultTheme();
        DialogResult = DialogResult.Cancel;
    }

    // the users selected the default theme settings to be shown..
    private void mnuSetSavedDefaultTheme_Click(object sender, EventArgs e)
    {
        ThemeSettings = ThemeSettings.LoadDefaultTheme();
        ListThemeData();
        FormMain.ThemeMainForm(ThemeSettings);
    }
}