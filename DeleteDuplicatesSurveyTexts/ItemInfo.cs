using System;
using System.Collections.Generic;

namespace DeleteDuplicatesSurveyTexts
{
    public class ItemInfo : IEquatable<ItemInfo>
    {
        public ItemInfo(int id, string name)
        {
            ItemId = id;
            ColumnName = name;
            TextItems = new HashSet<TextItem>();
        }

        public int ItemId { get; }
        public string ColumnName { get; }

        public HashSet<TextItem> TextItems { get; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ItemId;
                hashCode = (hashCode * 397) ^ (ColumnName != null ? ColumnName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public bool Equals(ItemInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ItemId == other.ItemId && string.Equals(ColumnName, other.ColumnName);
        }
    }
}