using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace DeleteDuplicatesSurveyTexts
{
    class DatabaseProcessor
    {
        private const int Timeout = 300;
        private readonly string connectionStr;
        private readonly string _fileName;
        private readonly bool _testMode;

        public DatabaseProcessor(string connectionStr, bool testMode)
        {
            this.connectionStr = connectionStr;
            _fileName = "Duplication_" + DateTime.Now.ToFileTime();
            _testMode = testMode;
        }

        /// <summary>
        /// Список всех баз данных в которых есть дубликаты
        /// </summary>
        /// <returns></returns>
        public IList<string> FindDatabases(out int processed)
        {
            ConsoleColor color = Console.ForegroundColor;
            var list = new List<string>();
            var result = new List<string>();
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                using (var connection = new SqlConnection(connectionStr))
                {
                    connection.Open();
                    //TODO: Переписать на получение из базы мета.
                    using (var cmd = new SqlCommand("SELECT name FROM master.dbo.sysdatabases;", connection) { CommandTimeout = Timeout })
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(reader[0].ToString());
                            }
                        }
                    }

                    foreach (var db in list)
                    {
                        connection.ChangeDatabase(db);
                        Console.WriteLine($"\t --- Process database {db} ---");
                        using (var checkTabExistCmd =
                            new SqlCommand(
                                @"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SurveyText'",
                                connection) { CommandTimeout = Timeout })
                        {
                            using (var reader = checkTabExistCmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Console.WriteLine($"\t --- Database {db} hasn't a SurveyText table ---");
                                    continue;
                                }
                            }

                            using (var checkDupCmd = new SqlCommand(Queries.CheckDups, connection) { CommandTimeout = Timeout })
                            {
                                using (var r = checkDupCmd.ExecuteReader())
                                {
                                    if (r.HasRows)
                                    {
                                        result.Add(db);
                                        r.Read();

                                        var count = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                                        Console.WriteLine($"\t --- Found database: {db}, count of affected rows: {count} ---");
                                    }
                                }
                            }
                        }
                    }
                    processed = list.Count;
                }

            }
            finally
            {
                Console.ForegroundColor = color;
            }

            return result;
        }

        private string GetDupItemsCommand(SqlConnection connection, string columnName, int surveyId = -1)
        {
            if (surveyId > -1)
            {
                var surveyVersionIds = new List<int>();
                using (var cmd = new SqlCommand(string.Format(Queries.GetSurveyVersions, surveyId), connection) { CommandTimeout = Timeout})
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            surveyVersionIds.Add(reader.GetInt32(0));
                        }
                    }
                }
                return string.Format(Queries.GetDupItemExt, columnName, surveyId, string.Join(",", surveyVersionIds));
            }

            var result = string.Format(Queries.GetDupItem, columnName);
            File.AppendAllText("Queries", result);
            return result;
        }

        /// <summary>
        /// Зачистка от дублей одной базы
        /// </summary>
        /// <param name="database"></param>
        public void ClearDatabase(string database, int surveyId = -1)
        {
            ConsoleColor color = Console.ForegroundColor;
            var dupItems = new Dictionary<int, List<TextItem>>();
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                using (var connection = new SqlConnection(connectionStr))
                {
                    connection.Open();
                    connection.ChangeDatabase(database);

                    foreach (var columnName in Queries.ColumnNames)
                    {
                        Console.WriteLine($"\t --- Start Process Column: {columnName} ---");
                        var ids = new HashSet<int>();
                        using (var cmd = new SqlCommand(GetDupItemsCommand(connection, columnName, surveyId), connection) { CommandTimeout = Timeout })
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    Console.WriteLine("\t <<<No duplicates for this items>>>");
                                    continue;
                                }

                                while (reader.Read())
                                {
                                    ids.Add(reader.GetInt32(0));
                                }
                            }
                        }

                        foreach (var id in ids)
                        {
                            using (var cmd = new SqlCommand(string.Format(Queries.GetDupGroupAllItem, columnName, id), connection)
                                { CommandTimeout = Timeout })
                            {
                                using (var reader = cmd.ExecuteReader())
                                {
                                    var list = new List<TextItem>();
                                    while (reader.Read())
                                    {
                                        list.Add(new TextItem()
                                        {
                                            Id = reader.GetInt32(0),
                                            Text = reader.IsDBNull(1) ? null : reader.GetString(1),
                                            Locale = reader.IsDBNull(2) ? null : reader.GetString(2),
                                            ItemColumnName = columnName,
                                            ItemId = id,
                                            SurveyId = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                                            SurveyVersionId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                                            VariableSetId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6)
                                        });
                                    }

                                    dupItems[id] = list;
                                }
                            }
                        }
                    }

                    int count = dupItems.SelectMany(i => i.Value).Count();
                    Console.WriteLine($"======== Items collected: {count} =========");
                    Clear(dupItems, connection);
                }
            }
            finally
            {
                Console.ForegroundColor = color;
            }
        }

        private void Clear(Dictionary<int, List<TextItem>> dupItems, SqlConnection connection)
        {
            var allDropedItems = new List<TextItem>();
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    foreach (var dupItem in dupItems)
                    {
                        var restItems = new List<TextItem>();
                        var dropedItems = new List<TextItem>();
                        var list = dupItem.Value;
                        foreach (var groupItem in list.GroupBy(i => i.Locale))
                        {
                            if (groupItem.Count() > 1)
                            {
                                StringBuilder sb = new StringBuilder();
                                bool isFirst = true;
                                restItems.Add(groupItem.OrderBy(i => i.Id).First());
                                foreach (var item in groupItem.OrderBy(i => i.Id).Skip(1))
                                {
                                    dropedItems.Add(item);
                                    if (isFirst)
                                    {
                                        sb.Append(item.Id);
                                        isFirst = false;
                                    }
                                    else
                                    {
                                        sb.Append(", " + item.Id);
                                    }
                                }

                                using (var cmd =
                                    new SqlCommand(string.Format(Queries.DeleteItem, sb), connection, transaction)
                                        {CommandTimeout = Timeout})
                                {
                                    var resultNonQuery = cmd.ExecuteNonQuery();
                                    Console.WriteLine(
                                        $"\t >>> Delete rows count: {resultNonQuery}; local: {groupItem.Key}; id: {dupItem.Key}");
                                }

                                using (var cmd =
                                    new SqlCommand(
                                            string.Format(Queries.FindTextItems,
                                                string.Join(",", restItems.Select(i => i.Id))), connection, transaction)
                                        {CommandTimeout = Timeout})
                                {
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        reader.Read();
                                        if (reader.GetInt32(0) != restItems.Count)
                                            throw new InvalidOperationException(
                                                $"Incorrect deletion (saved items incorrect): items - {string.Join(",", restItems.Select(i => i.Id))}, in db: {reader.GetInt32(0)}");
                                    }
                                }

                                using (var cmd =
                                    new SqlCommand(
                                            string.Format(Queries.FindTextItems,
                                                string.Join(",", dropedItems.Select(i => i.Id))), connection,
                                            transaction)
                                        {CommandTimeout = Timeout})
                                {
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        reader.Read();
                                        if (reader.GetInt32(0) > 0)
                                            throw new InvalidOperationException(
                                                $"Incorrect deletion (not all items deleted): items - {string.Join(", ", dropedItems.Select(i => i.Id))}, in db: {reader.GetInt32(0)}");
                                    }
                                }

                                allDropedItems.AddRange(dropedItems);
                            }
                            else
                            {
                                Console.WriteLine(
                                    $"\t >>>> Only one item - db: {connection.Database}; id: {dupItem.Key}; locale: {groupItem.Key}; ----- SKIP");
                            }
                        }
                    }

                    Console.WriteLine($"Clearing is finish. Summary deleted: {allDropedItems.Count}");
                    Console.WriteLine("Start Save in file");

                    File.AppendAllText(_fileName, $"=== START Dropped Items For Database {connection.Database} ===");
                    File.AppendAllLines(_fileName,
                        allDropedItems.OrderBy(i => i.ItemId).ThenBy(i => i.Locale).Select(i => string.Join(",", 
                            $"Id = {i.Id}", $"Locale: {i.Locale}", $"Text: {i.Text}",
                            $"{i.ItemColumnName} = {i.ItemId}", $"Survey_Id_Root: {i.SurveyId}",
                            $"SurveyVersion_Id_Root: {i.SurveyVersionId}")
                        ));

                    File.AppendAllText(_fileName, $"=== END Dropped Items For Database {connection.Database} ===");
                    Console.WriteLine("Saved in file.");

                    if (_testMode)
                        transaction.Rollback();
                    else
                        transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(ex);
                    Console.ForegroundColor = color;
                }
            }
        }
    }
}