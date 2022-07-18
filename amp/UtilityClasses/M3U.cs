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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace amp.UtilityClasses;

/// <summary>
/// Represents a single entry in a music playlist file (*.m3u or *.m3u8).
/// </summary>
public class M3UEntry
{
    /// <summary>
    /// The description of the file.
    /// </summary>
    private string fileDesc;

    /// <summary>
    /// Gets or set the file name.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets or set the description of a file.
    /// </summary>
    public string FileDesc
    {
        get => fileDesc;

        set
        {
            if (value != string.Empty)
            {
                fileDesc = value;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:amp.UtilityClasses.M3UEntry"/> class.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="fileDesc">The description of the file.</param>
    public M3UEntry(string fileName, string fileDesc)
    {
        FileName = fileName;
        FileDesc = fileDesc;
    }
}

/// <summary>
/// A class representing a M3U playlist file.
/// </summary>
public class M3U
{
    private readonly Encoding enc;

    /// <summary>
    /// A list of file entries in a m3u/m3u8 file.
    /// </summary>
    public List<M3UEntry> M3UFiles = new List<M3UEntry>();

    /// <summary>
    /// Initializes a new instance of the <see cref="M3U"/> class.
    /// </summary>
    /// <param name="fileName">Name of the m3u/m3u8 file.</param>
    public M3U(string fileName)
    {
        List<string> fileLines = new List<string>();
        var fileDir = Path.GetDirectoryName(fileName)?.TrimEnd('\\') + "\\";
        if (Path.GetExtension(fileName)?.ToUpper() == ".M3U")
        {
            enc = Encoding.GetEncoding(1252);
        }
        else if (Path.GetExtension(fileName)?.ToUpper() == ".M3U8")
        {
            enc = new UTF8Encoding();
        }
        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        {
            using (StreamReader sr = new StreamReader(fs, enc))
            {
                while (!sr.EndOfStream)
                {
                    fileLines.Add(sr.ReadLine());
                }
            }
        }

        if (fileLines.Count == 0)
        {
            return;
        }

        for (int i = 0; i < fileLines.Count; i++)
        {
            if (fileLines[i] == string.Empty)
            {
            }
            else
            {
                string fileNameInM3U;
                // ReSharper disable once StringLiteralTypo
                if (fileLines[i].StartsWith("#EXTINF:"))
                {
                    if (i + 1 < fileLines.Count)
                    {
                        if (File.Exists(fileLines[i + 1]))
                        {
                            fileNameInM3U = fileLines[i + 1];
                        }
                        else if (File.Exists(fileDir + fileLines[i + 1]))
                        {
                            fileNameInM3U = fileDir + fileLines[i + 1];
                        }
                        else
                        {
                            continue;
                        }

                        if (File.Exists(fileNameInM3U))
                        {
                            string fileDesc;
                            try
                            {
                                fileDesc = fileLines[i].Split(',')[1];
                            }
                            catch
                            {
                                fileDesc = string.Empty;
                            }
                            M3UFiles.Add(new M3UEntry(fileNameInM3U, fileDesc));
                            i++;
                        }
                    }
                }
                else if (Directory.Exists(fileLines[i]) || Directory.Exists(fileDir + fileLines[i]))
                {
                    if (Directory.Exists(fileLines[i]))
                    {
                        fileNameInM3U = fileLines[i];
                    }
                    else if (Directory.Exists(fileDir + fileLines[i]))
                    {
                        fileNameInM3U = fileDir + fileLines[i];
                    }
                    else
                    {
                        fileNameInM3U = string.Empty;
                    }
                    foreach (string dirFileName in Directory.GetFiles(fileNameInM3U.TrimEnd('\\') + "\\", "*.*", SearchOption.AllDirectories).ToArray())
                    {
                        M3UFiles.Add(new M3UEntry(dirFileName, string.Empty));
                    }
                }
                else if (File.Exists(fileLines[i]) || File.Exists(fileDir + fileLines[i]))
                {
                    if (File.Exists(fileLines[i]))
                    {
                        fileNameInM3U = fileLines[i];
                    }
                    else if (File.Exists(fileDir + fileLines[i]))
                    {
                        fileNameInM3U = fileDir + fileLines[i];
                    }
                    else
                    {
                        fileNameInM3U = string.Empty;
                    }
                    M3UFiles.Add(new M3UEntry(fileNameInM3U, string.Empty));
                }
            }
        }
        for (int i = M3UFiles.Count - 1; i >= 0; i--)
        {
            if (!Constants.Extensions.Contains(Path.GetExtension(M3UFiles[i].FileName)?.ToUpper()))
            {
                M3UFiles.RemoveAt(i);
            }
        }
    }
}