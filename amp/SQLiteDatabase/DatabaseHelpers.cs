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
using System.Data.SQLite;
using System.Globalization;
using VPKSoft.ErrorLogger;

namespace amp.SQLiteDatabase;

/// <summary>
/// A helper class for formulating straightforward written SQL.
/// </summary>
public class DatabaseHelpers
{
    /// <summary>
    /// Quotes a given string suitable for raw SQL.
    /// </summary>
    /// <param name="value">The string value to quote.</param>
    /// <returns>A quoted string suitable for raw SQL.</returns>
    // ReSharper disable once InconsistentNaming
    public static string QS(string value)
    {
        return "'" + value.Replace("'", "''") + "'";
    }

    /// <summary>
    /// Converts a double value into a string value understood by SQL.
    /// </summary>
    /// <param name="value">The double value to convert into a string.</param>
    /// <returns>A string converted from the give <paramref name="value"/>.</returns>
    // ReSharper disable once InconsistentNaming
    public static string DS(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a decimal value into a string value understood by SQL.
    /// </summary>
    /// <param name="value">The double value to convert into a string.</param>
    /// <returns>A string converted from the give <paramref name="value"/>.</returns>
    // ReSharper disable once InconsistentNaming
    public static string DS(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the given value to a SQL formatted string considering the SQL NULL possibility.
    /// </summary>
    /// <typeparam name="T">A value to convert into a SQL formatted string.</typeparam>
    /// <param name="value">The value to convert into a SQL formatted string.</param>
    /// <returns>A value to convert into a SQL formatted string. A NULL is returned in case the <paramref name="value"/> is <c>null</c>.</returns>
    // ReSharper disable once InconsistentNaming
    public static string NI<T>(T value)
    {
        if (value == null)
        {
            return "NULL";
        }

        if (value is string)
        {
            return QS(value.ToString());
        }

        if (value is double || value is float)
        {
            return DS(Convert.ToDouble(value));
        }

        if (value is decimal)
        {
            return DS(Convert.ToDecimal(value));
        }

        if (value is bool)
        {
            return Convert.ToBoolean(value) ? "1" : "0";
        }

        return value.ToString();
    }

    /// <summary>
    /// Executes a scalar SQL sentence against the database.
    /// </summary>
    /// <typeparam name="T">The return type of the scalar SQL sentence.</typeparam>
    /// <param name="sql">A scalar SQL sentence to be executed against the database.</param>
    /// <param name="connection">A <see cref="SQLiteConnection"/> connection to use with the scalar SQL.</param>
    /// <returns>The value of type T if the operation was successful; otherwise a default value of T is returned.</returns>
    public static T GetScalar<T>(string sql, SQLiteConnection connection)
    {
        try
        {
            // execute a scalar SQL sentence against the database..
            using (SQLiteCommand command = new SQLiteCommand(connection))
            {
                command.CommandText = sql;

                // ..and return the value casted into a typeof(T)..

                var value = command.ExecuteScalar();

                if (value == null)
                {
                    return default;
                }

                return (T)value;
            }
        }
        catch (Exception ex)
        {
            // log the exception if the action has a value..
            ExceptionLogger.LogError(ex);
            // failed, return default(T).. 
            return default;
        }
    }

    /// <summary>
    /// Executes a SQL sentence against the database.
    /// </summary>
    /// <param name="sql">A SQL sentence to be executed against the database.</param>
    /// <param name="connection">A <see cref="SQLiteConnection"/> connection to use with the SQL.</param>
    /// <returns><c>true</c> if sentence was successfully executed, <c>false</c> otherwise.</returns>
    public static bool ExecuteSql(string sql, SQLiteConnection connection)
    {
        try
        {
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }

            return true;
        }
        catch (Exception ex)
        {
            // log the exception if the action has a value..
            ExceptionLogger.LogError(ex);
            // failed, return default(T).. 
            return false;
        }
    }
}