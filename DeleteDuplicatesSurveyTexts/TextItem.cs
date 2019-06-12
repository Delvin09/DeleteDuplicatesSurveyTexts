namespace DeleteDuplicatesSurveyTexts
{
    class TextItem
    {
        public string Text { get; set; }
        public string Locale { get; set; }
        public int Id { get; set; }
        public string ItemColumnName { get; set; }
        public int ItemId { get; set; }
        public int? SurveyId { get; set; }
        public int? SurveyVersionId { get; set; }
        public int? VariableSetId { get; set; }
    }
}