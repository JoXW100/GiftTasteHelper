using System.Diagnostics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using SDVSocialPage = StardewValley.Menus.SocialPage;

namespace GiftTasteHelper.Framework
{
    internal class SocialPageGiftHelper : GiftHelper
    {
        /*********
        ** Properties
        *********/
        private readonly SocialPage SocialPage = new();
        private string LastHoveredNpc = string.Empty;


        /*********
        ** Public methods
        *********/
        public SocialPageGiftHelper(IGiftDataProvider dataProvider, GiftConfig config, IReflectionHelper reflection, ITranslationHelper translation)
            : base(GiftHelperType.SocialPage, dataProvider, config, reflection, translation)
        {
            SocialPage.OnSlotIndexChanged += OnSlotIndexChanged;
        }

        public override bool OnOpen(IClickableMenu menu)
        {
            // reset
            LastHoveredNpc = string.Empty;

            SDVSocialPage? nativeSocialPage = this.GetNativeSocialPage(menu);
            if (nativeSocialPage != null)
            {
                SocialPage.Init(nativeSocialPage, this.Reflection);
            }
            return base.OnOpen(menu);
        }

        public override void OnResize(IClickableMenu menu)
        {
            base.OnResize(menu);
            SocialPage.OnResize(this.GetNativeSocialPage(menu));
        }

        /*
        public override bool CanTick()
        {
            // we don't have a tab-changed event so don't tick when the social tab isn't open
            return this.IsCorrectMenuTab(Game1.activeClickableMenu) && base.CanTick();
        }
        */

        public override void OnCursorMoved(CursorMovedEventArgs e)
        {
            if (!Utils.Ensure(SocialPage != null, "Social Page is null!"))
            {
                return;
            }

            UpdateHoveredNPC(GetAdjustedCursorPosition(e.NewPosition.ScreenPixels.X, e.NewPosition.ScreenPixels.Y));
        }

        public override bool WantsUpdateEvent()
        {
            return true;
        }

        public override void OnPostUpdate(UpdateTickedEventArgs e)
        {
            if (IsCorrectMenuTab(Game1.activeClickableMenu))
            {
                SocialPage.OnUpdate();
            }
            else
            {
                OnClose();
            }
        }

        /*********
        ** Protected methods
        *********/
        protected override void AdjustTooltipPosition(ref int x, ref int y, int width, int height, int viewportW, int viewportHeight)
        {
            // Prevent the tooltip from going off screen if we're at the edge
            if (x + width > viewportW)
            {
                x = viewportW - width;
            }
        }

        private static bool IsCorrectMenuTab(IClickableMenu menu)
        {
            return menu is GameMenu gameMenu && gameMenu.currentTab == GameMenu.socialTab;
        }

        private SDVSocialPage? GetNativeSocialPage(IClickableMenu menu)
        {
            try
            {
                var tabs = Reflection.GetField<List<IClickableMenu>>(menu, "pages").GetValue();
                IClickableMenu tab = tabs[GameMenu.socialTab];
                return (SDVSocialPage)tab;
            }
            catch (Exception ex)
            {
                Utils.DebugLog("Failed to get native social page: " + ex, LogLevel.Error);
                return null;
            }
        }

        private void UpdateHoveredNPC(SVector2 mousePos)
        {
            string hoveredNpc = string.Empty;
            try
            {
                hoveredNpc = SocialPage.GetCurrentlyHoveredNpc(mousePos);
            }
            catch (Exception e)
            {
                Utils.DebugLog($"Error occured when updating hovered NPC. {e.GetType().Name}: {e.Message}", LogLevel.Error);
            }

            if (hoveredNpc == string.Empty)
            {
                DrawCurrentFrame = false;
                return;
            }

            if (hoveredNpc != LastHoveredNpc)
            {
                if (GiftDrawDataProvider.HasDataForNpc(hoveredNpc) && SetSelectedNPC(hoveredNpc))
                {
                    DrawCurrentFrame = true;
                    LastHoveredNpc = hoveredNpc;
                }
                else
                {
                    DrawCurrentFrame = false;
                    LastHoveredNpc = string.Empty;
                }
            }
            else
            {
                LastHoveredNpc = string.Empty;
            }
        }

        private void OnSlotIndexChanged()
        {
            // We currently only check if the hovered npc changed during mouse move events, so if the user
            // scrolls the list without moving the mouse the tooltip won't change and it will be incorrect.
            // Listening for when the slot index changes fixes this.
            UpdateHoveredNPC(GetAdjustedCursorPosition(Game1.getMouseX(), Game1.getMouseY()));
        }
    }
}
