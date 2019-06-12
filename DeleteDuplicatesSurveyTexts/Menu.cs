using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DeleteDuplicatesSurveyTexts
{
    class Menu
    {
        private readonly string _connection;
        private readonly bool _testMode;

        public Menu()
        {
            _connection = System.Configuration.ConfigurationManager.ConnectionStrings["Connection"].ConnectionString;
            _testMode = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["IsTestMode"]);
        }

        public void Render()
        {
            ConsoleKeyInfo key;
            do
            {
                Console.Clear();
                Console.WriteLine("1. Find databases with duplicates.");
                Console.WriteLine("2. Clear all databases.");
                Console.WriteLine("3. Clear database.");
                Console.WriteLine("0. Exit.");

                key = Console.ReadKey();
                Console.WriteLine();
                switch (char.ToLowerInvariant(key.KeyChar))
                {
                    case '1':
                        Console.Clear();
                        FindAllDatabases();
                        Console.ReadLine();
                        break;
                    case '2':
                        Console.Clear();
                        ClearAllDatabases();
                        Console.ReadLine();
                        break;
                    case '3':
                        Console.Clear();
                        Console.Write("Enter database name: ");
                        var database = Console.ReadLine();
                        int surveyId;
                        Console.Write("Enter survey id: ");
                        if (!int.TryParse(Console.ReadLine(), out surveyId))
                            surveyId = -1;
                        ClearDatabase(database, surveyId);
                        Console.ReadLine();
                        break;
                }
            } while (key.Key != ConsoleKey.D0 && key.Key != ConsoleKey.NumPad0);

            Console.WriteLine("Bye-Bye!");
        }

        private void ClearDatabase(string database, int surveyId = -1)
        {
            var sw = new Stopwatch();
            Console.WriteLine($"Start Clear Database: {database}");
            sw.Start();

            var p = new DatabaseProcessor(_connection, _testMode);
            try
            {
                p.ClearDatabase(database, surveyId);
            }
            catch (Exception ex)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Something go wrong with db: {database}");
                Console.WriteLine(ex);
                Console.ForegroundColor = color;
            }

            sw.Stop();
            Console.WriteLine($"End Clear Database {database}. Time: {sw.Elapsed}");
        }

        private void ClearAllDatabases()
        {
            var sw = new Stopwatch();
            Console.WriteLine("Start Clear All Databases");
            sw.Start();

            var p = new DatabaseProcessor(_connection, _testMode);
            var sw4Db = new Stopwatch();
            foreach (var db in p.SearchDatabases(out _))
            {
                sw4Db.Start();
                try
                {
                    Console.WriteLine($"Start Clear Database: {db.Database}");
                    p.ClearDatabase(db.Database);
                    Console.WriteLine("Success!");
                }
                catch (Exception ex)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"Something go wrong with db: {db.Database}");
                    Console.WriteLine(ex);
                    Console.ForegroundColor = color;
                }
                finally
                {
                    sw4Db.Stop();
                    Console.WriteLine($"End Clear Database: {db.Database}; Time: {sw4Db.Elapsed}");
                    sw4Db.Reset();
                }
            }

            sw.Stop();
            Console.WriteLine($"End Clear All Databases. Time: {sw.Elapsed}");
        }

        private void FindAllDatabases()
        {
            IList<DatabaseInfo> list = null;
            int processed = 0;
            var sw = new Stopwatch();
            try
            {
                sw.Start();
                var p = new DatabaseProcessor(_connection, _testMode);
                list = p.SearchDatabases(out processed);
                Console.WriteLine("Database list: ");
                foreach (var db in list)
                {
                    Console.WriteLine($"\t{db.Database}  --->  affected rows: {db.AffectedRows}");
                }
                File.WriteAllLines("FoundedDbs", list.Select(l => $"{l.Database} ----> affected rows: {l.AffectedRows}"));
            }
            catch (Exception ex)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"Something go wrong when database try to find");
                Console.WriteLine(ex);
                Console.ForegroundColor = color;
            }
            finally
            {
                sw.Stop();
                Console.WriteLine($"End database list. Processed: {processed}; Founded: {list?.Count ?? 0}; Time: {sw.Elapsed}");
            }
        }
    }
}