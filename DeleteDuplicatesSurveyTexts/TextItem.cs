using System;

namespace DeleteDuplicatesSurveyTexts
{
    public class TextItem : IEquatable<TextItem>
    {
        public string Text { get; set; }
        public string Locale { get; set; }
        public int Id { get; set; }
        public string ItemColumnName { get; set; }
        public int ItemId { get; set; }
        public int? SurveyId { get; set; }
        public int? SurveyVersionId { get; set; }
        public int? VariableSetId { get; set; }

        public bool Equals(TextItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextItem) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}