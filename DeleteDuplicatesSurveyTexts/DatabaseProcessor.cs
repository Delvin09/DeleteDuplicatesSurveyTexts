using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        /// Return list of all databases with duplicates
        /// </summary>
        public List<DatabaseInfo> SearchDatabases(out int processed)
        {
            ConsoleColor color = Console.ForegroundColor;
            var result = new List<DatabaseInfo>();
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                using (var connection = new SqlConnection(connectionStr))
                {
                    connection.Open();
                    var list = GetAllAccountDatabases(connection);

                    foreach (var db in list)
                    {
                        connection.ChangeDatabase(db);
                        Console.WriteLine($"\t --- Process database {db} ---");
                        using (var checkDupCmd = new SqlCommand(Queries.CheckDups, connection) {CommandTimeout = Timeout})
                        {
                            using (var reader = checkDupCmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    if (!reader.IsDBNull(0))
                                    {
                                        var count = reader.GetInt32(0);
                                        result.Add(new DatabaseInfo(db, count));
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

        /// <summary>
        /// Return list of all account databases
        /// </summary>
        private List<string> GetAllAccountDatabases(SqlConnection connection)
        {
            List<string> metaList = new List<string>();
            List<string> list = new List<string>();

            using (var cmd = new SqlCommand("SELECT name FROM master.dbo.sysdatabases", connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dbName = reader.GetString(0);
                        if (dbName.Contains(".Meta"))
                            metaList.Add(dbName);
                        if (dbName.Contains(".Trial"))
                            list.Add(dbName);
                    }
                }
            }

            foreach (var metaDb in metaList)
            {
                connection.ChangeDatabase(metaDb);
                using (var cmd = new SqlCommand(Queries.GetAccountConnections, connection) {CommandTimeout = Timeout})
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var accountConnectionString = reader[0].ToString();
                            try
                            {
                                var sqlConnectionStringBuilder =
                                    new SqlConnectionStringBuilder(accountConnectionString);
                                list.Add(sqlConnectionStringBuilder.InitialCatalog);
                            }
                            catch (Exception e)
                            {
                                var color = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine(e);
                                Console.ForegroundColor = color;
                            }
                        }
                    }
                }
            }
#if DEBUG
            list.Add("Survalyzer.Trial");
            list.Add(@"zSulaPNL_20180413104126_intelligencegroup_1eced1591d9743b5913cd95ffb7be73a");
#endif
            return list;
        }

        /// <summary>
        /// Clear duplicate items in database
        /// </summary>
        /// <param name="database"></param>
        public void ClearDatabase(string database, int surveyId = -1)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.CursorVisible = false;
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                using (var connection = new SqlConnection(connectionStr))
                {
                    var duplicateItems = new Dictionary<int, List<TextItem>>();
                    connection.Open();
                    connection.ChangeDatabase(database);

                    foreach (var columnName in Queries.ColumnNames)
                    {
                        Console.WriteLine($"\t --- Start Process Column: {columnName} ---");
                        var itemIds = new Dictionary<int, HashSet<string>>();
                        using (var cmd = new SqlCommand(GetDuplicateItemsCommand(connection, columnName, surveyId), connection) { CommandTimeout = Timeout })
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
                                    var itemId = reader.GetInt32(0);
                                    if (itemIds.TryGetValue(itemId, out HashSet<string> locales))
                                    {
                                        locales.Add(reader.GetString(1));
                                    }
                                    else
                                    {
                                        itemIds[itemId] = new HashSet<string>
                                        {
                                            reader.GetString(1)
                                        };
                                    }
                                }

                                Console.WriteLine($"\t<< Items collected: {itemIds.Count} and duplicates locales text in: {itemIds.SelectMany(x => x.Value).Count()} >>");
                            }
                        }

                        Console.WriteLine($"\t>>> Start Process Survey Text Collection for {columnName} <<<");
                        int counter = 0;
                        foreach (var id in itemIds)
                        {
                            using (var cmd = new SqlCommand(string.Format(Queries.GetDupGroupAllItem, columnName, id.Key, string.Join(",", id.Value.Select(i => $"'{i}'"))), connection)
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
                                            ItemId = id.Key,
                                            SurveyId = reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                                            SurveyVersionId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                                            VariableSetId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6)
                                        });
                                    }

                                    duplicateItems[id.Key] = list;
                                    Console.Write($"\t>><< Processed {++counter} of {itemIds.Count} => for item {id.Key} found {list.Count} >><<");
                                    Console.SetCursorPosition(0, Console.CursorTop);
                                }
                            }
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine($"======== Items collected: {duplicateItems.SelectMany(i => i.Value).Count()} =========");
                    Console.Write("Press any key for continue...");
                    Console.Read();

                    Clear(duplicateItems, connection);
                }
            }
            finally
            {
                Console.ForegroundColor = color;
                Console.CursorVisible = true;
            }
        }

        private void Clear(Dictionary<int, List<TextItem>> dupItems, SqlConnection connection)
        {
            var allDroppedItems = new List<TextItem>();
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    int counter = 0, all = dupItems.SelectMany(i => i.Value).Count();
                    foreach (var dupItem in dupItems)
                    {
                        var restItems = new List<TextItem>();
                        var droppedItems = new List<TextItem>();
                        var list = dupItem.Value;
                        var processMessage = string.Empty;

                        foreach (var groupItem in list.GroupBy(i => i.Locale))
                        {
                            if (groupItem.Count() > 1)
                            {
                                restItems.Add(groupItem.OrderBy(i => i.Id).First());

                                var itemsForDrop = groupItem.OrderBy(i => i.Id).Skip(1).ToArray();
                                var constraint = string.Join(",", itemsForDrop.Select(i => i.Id));

                                droppedItems.AddRange(itemsForDrop);

                                using (var cmd = new SqlCommand(string.Format(Queries.DeleteItem, constraint), connection, transaction) {CommandTimeout = Timeout})
                                {
                                    var resultNonQuery = cmd.ExecuteNonQuery();
                                    processMessage = $"Delete rows count: {resultNonQuery}; local: {groupItem.Key}; id: {dupItem.Key}";
                                }

                                using (var cmd = new SqlCommand(string.Format(Queries.FindTextItems, 
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

                                using (var cmd = new SqlCommand(string.Format(Queries.FindTextItems, 
                                            string.Join(",", droppedItems.Select(i => i.Id))), connection, transaction)
                                        {CommandTimeout = Timeout})
                                {
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        reader.Read();
                                        if (reader.GetInt32(0) > 0)
                                            throw new InvalidOperationException(
                                                $"Incorrect deletion (not all items deleted): items - {string.Join(", ", droppedItems.Select(i => i.Id))}, in db: {reader.GetInt32(0)}");
                                    }
                                }

                                allDroppedItems.AddRange(droppedItems);
                            }
                            else
                            {
                                Console.WriteLine($"Only one item - db: {connection.Database}; id: {dupItem.Key}; locale: {groupItem.Key}; ----- SKIP");
                            }

                            Console.Write($"\t >>> Processed {++counter} of {all}. {processMessage}");
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Clearing is finish. Summary deleted: {allDroppedItems.Select(i => i.Id).Distinct().Count()}");
                    Console.WriteLine("Start Save in file");

                    File.AppendAllText(_fileName, $"=== START Dropped Items For Database {connection.Database} ===");
                    File.AppendAllLines(_fileName,
                        allDroppedItems.Distinct(new TextItemIdComparer()).OrderBy(i => i.ItemId).ThenBy(i => i.Locale).Select(i =>
                            string.Format(Queries.InsertPattern
                                , i.ItemColumnName
                                , string.Join(",", i.Locale, i.Text, i.ItemId, i.SurveyId, i.SurveyVersionId, i.VariableSetId)
                                , i.Id
                            )));

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

        /// <summary>
        /// Return selection duplicate items string query.
        /// </summary>
        /// <param name="surveyId">If specified, than return query with filter by survey and survey version</param>
        private string GetDuplicateItemsCommand(SqlConnection connection, string columnName, int surveyId = -1)
        {
            if (surveyId > -1)
            {
                var surveyVersionIds = new List<int>();
                using (var cmd = new SqlCommand(string.Format(Queries.GetSurveyVersions, surveyId), connection) { CommandTimeout = Timeout })
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

            return string.Format(Queries.GetDupItem, columnName);
        }
    }

    internal class TextItemIdComparer : IEqualityComparer<TextItem>
    {
        public bool Equals(TextItem x, TextItem y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(TextItem obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}