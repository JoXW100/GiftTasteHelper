using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SDVSocialPage = StardewValley.Menus.SocialPage;

namespace GiftTasteHelper.Framework
{
    internal class SocialPage
    {
        /*********
        ** Properties
        *********/
        /// <summary>The underlying social menu.</summary>
        private SDVSocialPage? NativeSocialPage;

        /// <summary>Simplifies access to private game code.</summary>
        private IReflectionHelper? Reflection;

        private List<ClickableTextureComponent> FriendSlots = new();

        private int FirstCharacterIndex;
        private SVector2 SlotBoundsOffset = SVector2.Zero;
        private float SlotHeight;
        private Rectangle PageBounds;
        private int LastSlotIndex;

        /// <summary>Fires when the current slot index changes due to scrolling the list.</summary>
        public delegate void SlotIndexChangedDelegate();
        public event SlotIndexChangedDelegate? OnSlotIndexChanged;


        /*********
        ** Public methods
        *********/
        public void Init(SDVSocialPage nativePage, IReflectionHelper reflection)
        {
            this.Reflection = reflection;
            this.OnResize(nativePage);
        }

        public void OnResize(SDVSocialPage? nativePage)
        {
            this.NativeSocialPage = nativePage;
            if (this.NativeSocialPage is null)
            {
                return;
            }

            this.FriendSlots = this.Reflection?.GetField<List<ClickableTextureComponent>>(this.NativeSocialPage, "sprites").GetValue() ?? new();

            // Find the first NPC character slot
            this.FirstCharacterIndex = this.NativeSocialPage.SocialEntries.FindIndex(entry => !entry.IsPlayer);
            if (this.FriendSlots.Count == 0)
            {
                Utils.DebugLog("Failed to init SocialPage: No friend slots found.", LogLevel.Error);
                return;
            }

            // The slot bounds begin after a small margin on the top and left side, likely to make it easier to align
            // the slot contents. We need to offset by this margin so that when you mouse over where the slot actually begins
            // it's correctly detected.
            // These offset values are kind of magic based on what looked right as I couldn't find a nice way to get them.
            this.SlotBoundsOffset = new SVector2(Game1.tileSize / 4, Game1.tileSize / 8);
            this.SlotHeight = this.GetSlotHeight();
            this.PageBounds = this.MakePageBounds();
            LastSlotIndex = this.GetSlotIndex();
        }

        public void OnUpdate()
        {
            int slotIndex = this.GetSlotIndex();
            if (slotIndex != this.LastSlotIndex)
            {
                OnSlotIndexChanged?.Invoke();
                this.LastSlotIndex = slotIndex;
            }
        }

        public string GetCurrentlyHoveredNpc(SVector2 mousePos)
        {
            // Early out if the mouse isn't within the page bounds
            var mousePoint = mousePos.ToPoint();
            if (!PageBounds.Contains(mousePoint))
            {
                return string.Empty;
            }

            var slotIndex = GetSlotIndex();
            if (slotIndex < 0 || slotIndex >= FriendSlots.Count)
            {
                Utils.DebugLog($"Invalid social page slot index, was '{slotIndex}', expected range 0 - {FriendSlots.Count - 1}.", LogLevel.Error);
                return string.Empty;
            }

            if (NativeSocialPage == null)
            {
                Utils.DebugLog("¨Could not get native social page.", LogLevel.Error);
                return string.Empty;
            }

            var entries = NativeSocialPage.SocialEntries;
            // Find the slot containing the cursor among the currently visible slots
            for (int i = slotIndex; i < slotIndex + SDVSocialPage.slotsOnPage && i < FriendSlots.Count && i < entries.Count; ++i)
            {
                var friend = FriendSlots[i];
                var bounds = MakeSlotBounds(friend);

                if (bounds.Contains(mousePoint))
                {
                    return entries[i].InternalName ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private int GetSlotIndex()
        {
            if (NativeSocialPage is null || Reflection is null)
            {
                return 0;
            }

            try
            {
                return Reflection.GetField<int>(NativeSocialPage, "slotPosition").GetValue();
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        private float GetSlotHeight()
        {
            try
            {
                if (FirstCharacterIndex >= 0 && FriendSlots.Count > FirstCharacterIndex + 1)
                {
                    return FriendSlots[FirstCharacterIndex + 1].bounds.Y - FriendSlots[FirstCharacterIndex].bounds.Y;
                }

                Utils.DebugLog("SocialPage.GetSlotHeight out of range: index = " + this.FirstCharacterIndex, LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Utils.DebugLog($"Error occured while measuring social page character slot height. {ex.GetType().Name}: {ex.Message}", LogLevel.Error);
            }

            return 0f;
        }

        // Creates the bounds around all the slots on the screen within the page border.
        private Rectangle MakePageBounds()
        {
            var rect = MakeSlotBounds(this.FriendSlots[this.FirstCharacterIndex]);
            rect.Height = (int)this.SlotHeight * SDVSocialPage.slotsOnPage;
            return rect;
        }

        private Rectangle MakeSlotBounds(ClickableTextureComponent slot)
        {
            return Utils.MakeRect(
                (slot.bounds.X - this.SlotBoundsOffset.X),
                (slot.bounds.Y - this.SlotBoundsOffset.Y),
                (slot.bounds.Width - Game1.tileSize),
                this.SlotHeight - this.SlotBoundsOffset.Y // account for border between each slot
            );
        }
    }
}
