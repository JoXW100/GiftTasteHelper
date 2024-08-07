﻿using System.Diagnostics.CodeAnalysis;
using StardewValley;
using StardewValley.TokenizableStrings;

namespace GiftTasteHelper.Framework
{
    internal struct ItemCategory
    {
        public const string InvalidId = "-";

        public string Name;
        public string ID;

        public bool Valid => ID != InvalidId;
    }

    internal struct ItemData
    {
        public const int InEdible = -300;

        // Indices of data in yaml file: http://stardewvalleywiki.com/Modding:Object_data#Format
        public const int NameIndex = 0;
        public const int PriceIndex = 1;
        public const int EdibilityIndex = 2;
        public const int TypeIndex = 3; // Type and category
        public const int DisplayNameIndex = 4;
        public const int DescriptionIndex = 5;

        /*********
        ** Accessors
        *********/
        public string Name; // Always english        
        public string DisplayName; // Localized display name
        public int Price;
        public int Edibility;
        public ItemCategory Category;
        public string ID;
        public int SpriteIndex;
        public SVector2 NameSize;

        public readonly bool Edible => Edibility > InEdible;
        public readonly bool TastesBad => Edibility < 0;

        /*********
        ** Public methods
        *********/
        public override readonly string ToString()
        {
            return $"{{ID: {this.ID}, Name: {this.DisplayName}}}";
        }

        public static ItemCategory GetCategory(string itemId)
        {
            if (!Game1.objectData.TryGetValue(itemId, out var objectInfo)) 
            {
                return new ItemCategory { Name = "", ID = ItemCategory.InvalidId };
            }
            return new ItemCategory { Name = objectInfo.Name, ID = itemId };
        }

        public static ItemData MakeItem(string itemId)
        {
            if (!Game1.objectData.TryGetValue(itemId, out var objectInfo))
            {
                throw new ArgumentException($"Tried creating an item with an invalid id: {itemId}");
            }

            if (objectInfo is null)
            {
                throw new NullReferenceException($"Registered item with invalid info: {itemId}, possibly caused by another mod.");
            }

            string tokenizedName = TokenParser.ParseText(objectInfo.DisplayName);
            return new ItemData
            {
                Name = objectInfo.Name,
                DisplayName = tokenizedName,
                Price = objectInfo.Price,
                Edibility = objectInfo.Edibility,
                ID = itemId,
                Category = ItemData.GetCategory(itemId),
                SpriteIndex = objectInfo.SpriteIndex,
                NameSize = SVector2.MeasureString(tokenizedName, Game1.smallFont)
            };
        }

        public static bool TryMakeItem(string itemId, out ItemData item)
        {
            item = default;
            if (!Game1.objectData.TryGetValue(itemId, out var objectInfo) || objectInfo is null)
            {
                return false;
            }

            string tokenizedName = TokenParser.ParseText(objectInfo.DisplayName);
            item = new ItemData
            {
                Name = objectInfo.Name,
                DisplayName = tokenizedName,
                Price = objectInfo.Price,
                Edibility = objectInfo.Edibility,
                ID = itemId,
                Category = ItemData.GetCategory(itemId),
                SpriteIndex = objectInfo.SpriteIndex,
                NameSize = SVector2.MeasureString(tokenizedName, Game1.smallFont)
            };
            return true;
        }

        public static bool Validate([NotNullWhen(true)] string? itemId)
        {
            return itemId is not null && Game1.objectData.TryGetValue(itemId, out var objectInfo) && objectInfo is not null;
        }

        public static IEnumerable<ItemData> MakeItemsFromIds(IEnumerable<string> itemIds)
        {
            foreach (string itemId in itemIds)
            {
                if (ItemData.TryMakeItem(itemId, out var item))
                {
                    yield return item;
                } 
                else
                {
                    Utils.DebugLog($"Failed to make item from item id: {itemId}, Ignoring.");
                }
            }
        }
    }
}
