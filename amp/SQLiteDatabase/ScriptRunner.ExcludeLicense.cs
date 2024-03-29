﻿#region license
/*
This file is public domain.
You may freely do anything with it.
Copyright (c) VPKSoft 2019
*/
#endregion

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;
using System.IO;
// ReSharper disable CommentTypo

// ReSharper disable once CheckNamespace
namespace VPKSoft.ScriptRunner;

/// <summary>
/// A class to run updates on a program's database if updated.
/// </summary>
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class ScriptRunner
{
    /// <summary>
    /// A class representing a "block" in the script file. A such block should start with SQL comment '--VER n' and end with '--ENDVER n', where n is the version number.
    /// </summary>
    public class DbScriptBlock
    {
        /// <summary>
        /// A list of lines in a SQL script file which should start with SQL comment '--VER n' and end with '--ENDVER n', where n is the version number of the database.
        /// </summary>
        public List<string> SqlBlock = new List<string>();

        /// <summary>
        /// The database version number.
        /// </summary>
        public int DbVer;
    }

    /// <summary>
    /// Checks the SQL script for version blocks and if the there are version blocks not already run they are executed against the SQLite database.
    /// </summary>
    /// <param name="sqliteDataSource">A file name for a SQLite database.</param>
    /// <param name="scriptFile">A database script file location (optional).</param>
    /// <returns>True if the script was run successfully, otherwise false. 
    /// <note type="note">This has no indication to an issue whether an commands were actually executed.</note>
    /// </returns>
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static bool RunScript(string sqliteDataSource, string scriptFile = "")
    {
        try // we try..
        {
            // construct a list of database version blocks..
            List<DbScriptBlock> sqlBlocks = new List<DbScriptBlock>();

            // indicates if any of the database version block executions failed..
            bool noBlockExecError = true;

            // construct a SQLite database connection..
            using (SQLiteConnection conn = new SQLiteConnection("Data Source=" + sqliteDataSource + ";Pooling=true;FailIfMissing=false"))
            {
                conn.Open(); // open the SQLite database connection..

                int dbVer = 0; // assume that a version block in the SQL script file is at version 0..

                try // again it is required to try as there might be syntax errors in the script file with the
                    // '--VER n' and '--ENDVER n' blocks..
                {
                    // start reading the script file.. it is assumed to be in the application's 
                    // executable directory by the name of script.sql_script..

                    // if the script file location has been set then use that; otherwise use the default location..
                    scriptFile = scriptFile == string.Empty ?
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "script.sql_script") :
                        scriptFile;

                    using (StreamReader sr = new StreamReader(scriptFile))
                    {
                        while (!sr.EndOfStream) // read until all lines are read..
                        {
                            // keep reading until a first '--VER n' line is found..
                            string line; // a line in the SQL script file..
                            // ReSharper disable once PossibleNullReferenceException
                            while (!(line = sr.ReadLine()).StartsWith("--VER " + dbVer)) { }

                            // start building an instance of DBScriptBlock class from the "block"..
                            if (line != null)
                            {
                                DbScriptBlock scriptBlock = new DbScriptBlock
                                {
                                    DbVer = Convert.ToInt32(line.Split(' ')[1])
                                };

                                // and lines to the block, until a line like '--ENDVER n' is found..
                                // ReSharper disable once PossibleNullReferenceException
                                while (!(line = sr.ReadLine()).StartsWith("--ENDVER " + dbVer))
                                {
                                    // add the lines to the DBScriptBlock class instance..
                                    scriptBlock.SqlBlock.Add(line);
                                }
                                dbVer++; // increase the database version by one..
                                sqlBlocks.Add(scriptBlock); // add the DBScriptBlock class instance to the list..
                            }
                        }
                    }
                }
                catch
                {
                    // possible to many lines at the end of the script (last line must end with '--ENDVER n')
                }

                // as the SQL script file should always start with script block such as:
                // --VER 0
                // CREATE TABLE IF NOT EXISTS DBVERSION(DBVERSION INTEGER NOT NULL);
                // --ENDVER 0

                // an assumption is made that the version of the SQLite database can now be checked..
                int dbVersion; // assume that the database it at version 0..
                using (SQLiteCommand command = new SQLiteCommand(conn))
                {
                    try
                    {
                        // check the current SQLite database version..
                        command.CommandText = "SELECT IFNULL(MAX(DBVERSION), 0) AS VER FROM DBVERSION; ";
                        using (SQLiteDataReader dr = command.ExecuteReader())
                        {
                            // if anything was returned..
                            dbVersion = dr.Read() ? dr.GetInt32(0) : 0;
                        }
                    }
                    catch // an exception occurred..
                    {
                        dbVersion = 0; // ..so assume the version as 0..
                    }
                }

                // avoid to run the last block multiple times..
                if (dbVersion > 0)
                {
                    // ..if the database version is larger than 0..
                    sqlBlocks.RemoveAt(0);
                }

                // loop through the list of DBScriptBlock class instances starting from the next SQL script version..
                for (int i = dbVersion; i < sqlBlocks.Count; i++)
                {
                    string exec = string.Empty; // build an SQLite "transaction" block of lines in the block
                    foreach (string sqLine in sqlBlocks[i].SqlBlock)
                    {
                        exec += sqLine + Environment.NewLine;
                    }
                    try // keep trying..
                    {
                        // execute the SQLite "transaction" script block..
                        using (SQLiteCommand command = new SQLiteCommand(conn))
                        {
                            command.CommandText = exec;
                            command.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        // indicate that a block execution failed..
                        noBlockExecError = false;
                        break; // do nothing as the database wouldn't get fully updated..
                    }

                    // construct a SQL sentence to update the SQLite database version..
                    exec =
                        string.Join(Environment.NewLine,
                            "INSERT INTO DBVERSION(DBVERSION)",
                            $"SELECT {sqlBlocks[i].DbVer}",
                            $"WHERE NOT EXISTS(SELECT * FROM DBVERSION WHERE DBVERSION = {sqlBlocks[i].DbVer});");
                    // update the SQLite database version (DBVERSION table)..
                    using (SQLiteCommand command = new SQLiteCommand(conn))
                    {
                        command.CommandText = exec;
                        command.ExecuteNonQuery();
                    }
                }
            }

            // return the value indicating if the SQLite database was updated successfully..
            return true & noBlockExecError;
        }
        catch
        {
            // return false indicating that the SQLite database wasn't updated successfully..
            return false;
        }
    }
}