﻿using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace GiftTasteHelper
{
    public class NPCGiftInfo
    {
        public struct ItemData
        {
            public string name;
            public int ID;
            public Rectangle tileSheetSourceRect;

            public override string ToString()
            {
                return "{ID: " + ID.ToString() + ", Name: " + name + "}";
            }
        }

        private string npcName;
        private ItemData[] favouriteGifts;
        private string longestGiftName; // Used for finding how wide to make the tooltip
        private int longestGiftNameLen;

        public string Name { get { return npcName; } }
        public ItemData[] FavouriteGifts { get { return favouriteGifts; } }
        public string LongestGiftName { get { return longestGiftName; } }

        public NPCGiftInfo(string name, string[] favourite)
        {
            npcName = name;
            longestGiftNameLen = 0;

            int[] favouriteGiftIDs = Utils.StringToIntArray(favourite);

            favouriteGifts = ParseGifts(favouriteGiftIDs);
        }

        private ItemData[] ParseGifts(int[] ids)
        {
            ItemData[] itemList = new ItemData[ids.Length];
            for (int i = 0; i < ids.Length; ++i)
            {
                if (!Game1.objectInformation.ContainsKey(ids[i]))
                {
                    Utils.DebugLog("Could not find item information for ID: " + ids[i]);
                    continue;
                }

                string objectInfo = Game1.objectInformation[ids[i]];
                string[] parts = objectInfo.Split(new char[] { '/' });

                ItemData itemData = new ItemData();
                itemData.name = parts[0];
                itemData.ID = ids[i];
                itemData.tileSheetSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, ids[i], 16, 16);
                itemList[i] = itemData;

                if (itemData.name.Length > longestGiftNameLen)
                {
                    longestGiftName = itemData.name;
                    longestGiftNameLen = itemData.name.Length;
                }
            }
            return itemList;
        }
    }
}