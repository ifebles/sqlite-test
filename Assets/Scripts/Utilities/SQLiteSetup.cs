using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Data.Sqlite;
using UnityEngine;


namespace Utilities
{
  public class SQLiteSetup : IDisposable
  {
    /// <summary>
    /// Response code of failed script execution attempts
    /// </summary>
    public const int FailedScriptErrorCode = -1;

    /// <summary>
    /// Database URI
    /// </summary>
    private readonly string dbPath;
    /// <summary>
    /// Database connection
    /// </summary>
    private SqliteConnection conx;


    public SQLiteSetup()
    {
      dbPath = $"URI=file:{Application.persistentDataPath}/sqlite-test.db;version=3";
      conx = CreateConnection();
    }


    /// <summary>
    /// Create Database connection
    /// </summary>
    /// <param name="openConnection">Value indicating if the created connection should be opened by default</param>
    /// <returns>New Database connection</returns>
    private SqliteConnection CreateConnection(bool openConnection = true)
    {
      Debug.Log("Trying to create SQLite connection...");
      var conx = new SqliteConnection(dbPath);

      if (openConnection)
      {
        Debug.Log("Trying to open SQLite connection...");
        conx.Open();
      }

      Debug.Log("Returning connection...");
      return conx;
    }

    public void Dispose()
    {
      try { conx.Dispose(); }
      catch { }
    }


    #region EXECUTE COMMAND
    /// <summary>
    /// Execute the specified SQL command
    /// </summary>
    /// <param name="command">SQL command to execute</param>
    /// <returns>Value returned from the <c>ExecuteNonQuery</c> method</returns>
    public int Execute(string command)
    {
      return Execute(new CommandWithParameters { Text = command });
    }

    /// <summary>
    /// Execute the specified SQL command
    /// </summary>
    /// <param name="command">SQL command to execute</param>
    /// <returns>Value returned from the <c>ExecuteNonQuery</c> method</returns>
    public int Execute(CommandWithParameters command)
    {
      using (var sqlCommand = conx.CreateCommand())
        try
        {
          sqlCommand.CommandText = command.Text;
          sqlCommand.Parameters.Clear();

          if (command.Parameters != null)
            foreach (var key in command.Parameters.Keys)
              sqlCommand.Parameters.Add(new SqliteParameter
              {
                ParameterName = key,
                Value = command.Parameters[key],
              });

          Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>SQL command</color>:\n\n"
            + sqlCommand.CommandText);

          return sqlCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
          Debug.LogError(ex);
          Debug.LogError("SqliteException - failed command:\n\n" + sqlCommand.CommandText);
          return FailedScriptErrorCode;
        }
    }
    #endregion


    #region GET RAW COLLECTION
    /// <summary>
    /// Get the raw results from the specified SQL query
    /// </summary>
    /// <param name="query">SQL query to execute</param>
    /// <param name="getFieldNames">Value indicating if the first result set of the query result should contain the field names</param>
    /// <returns>Raw results</returns>
    public object[][] GetRawCollection(string query, bool getFieldNames = false)
    {
      return GetRawCollection(new CommandWithParameters { Text = query }, getFieldNames);
    }

    /// <summary>
    /// Get the raw results from the specified SQL query
    /// </summary>
    /// <param name="conx">Opened Database connection</param>
    /// <param name="query">SQL query to execute</param>
    /// <param name="getFieldNames">Value indicating if the first result set of the query result should contain the field names</param>
    /// <returns>Raw results</returns>
    public object[][] GetRawCollection(CommandWithParameters query, bool getFieldNames = false)
    {
      string[] fieldNameCollection = null;
      var resultSet = new List<object[]>();

      using (var sqlQuery = conx.CreateCommand())
      {
        sqlQuery.CommandText = query.Text;

        if (query.Parameters != null)
          foreach (var key in query.Parameters.Keys)
            sqlQuery.Parameters.Add(new SqliteParameter
            {
              ParameterName = key,
              Value = query.Parameters[key],
            });

        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>SQL command</color>:\n\n"
          + sqlQuery.CommandText);

        var reader = sqlQuery.ExecuteReader();

        fieldNameCollection = new string[reader.FieldCount]
          .Select((_entry, inx) => reader.GetName(inx))
          .ToArray();

        while (reader.Read())
          resultSet.Add(
            new object[reader.FieldCount]
              .Select((_entry, inx) => reader.GetValue(inx))
              .ToArray()
          );
      }

      if (getFieldNames)
        resultSet.Insert(0, fieldNameCollection);

      return resultSet.ToArray();
    }
    #endregion


    /// <summary>
    /// Class to associate a SQL Command with the specified Parameters for replacement
    /// </summary>
    /// <remarks>
    /// E.g.:
    /// <code>
    /// SELECT * FROM TABLE_NAME WHERE COLUMN_NAME = @ParamName
    /// </code>
    /// Here, <c>ParamName</c> is the name for the paramenter to add (without the `@` sign).
    /// </remarks>
    public class CommandWithParameters
    {
      /// <summary>
      /// SQL command
      /// </summary>
      public string Text { get; set; }
      /// <summary>
      /// Parameters to associate with the SQL command
      /// </summary>
      public Dictionary<string, object> Parameters { get; set; }
    }
  }
}
