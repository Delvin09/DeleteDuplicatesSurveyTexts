namespace DeleteDuplicatesSurveyTexts
{
    internal class DatabaseInfo
    {
        public DatabaseInfo(string db, int count)
        {
            Database = db;
            AffectedRows = count;
        }

        public int AffectedRows { get; }

        public string Database { get; }
    }
}