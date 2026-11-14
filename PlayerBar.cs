using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using WarpToFriends.Helpers;

namespace WarpToFriends;

internal class PlayerBar
{
    public Farmer farmer;
    public ClickableComponent icon;
    public ClickableComponent warpButton;
    public ClickableComponent section;
    public bool online;

    public long PlayerID { get; set; }

    public PlayerBar(Farmer f)
    {
        farmer = f;
        PlayerID = f.UniqueMultiplayerID;
        checkOnline();
    }

    public void checkOnline()
    {
        online = Game1.getOnlineFarmers().Any(o => o.UniqueMultiplayerID == farmer.UniqueMultiplayerID);
    }

    public void draw(SpriteBatch b)
    {
        checkOnline();
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), section.bounds.X, section.bounds.Y, section.bounds.Width, section.bounds.Height, Color.White, 4f, false);
        
        farmer.FarmerRenderer.drawMiniPortrat(b, new Vector2(icon.bounds.X, icon.bounds.Y), 0.5f, 5f, 1, farmer, 1f);

        string text = $"Player:\n{farmer.Name}";
        string locName = "Offline";
        if (online && farmer.currentLocation != null)
        {
            locName = farmer.currentLocation.DisplayName;
        }
        string text2 = $"Location:\n{locName}";

        Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(section.bounds.X + 120, section.bounds.Y + 22), Game1.textColor);
        Utility.drawTextWithShadow(b, text2, Game1.smallFont, new Vector2(section.bounds.X + 300, section.bounds.Y + 22), Game1.textColor);

        if (Game1.player != farmer)
        {
            string buttonText;
            int requiredStamina = PlayerHelper.GetRequiredStamina(Game1.player);
            if (ModEntry.ActiveEnergyPercent > 0)
            {
                buttonText = ModEntry.I18n.Get("button.warp-cost", new { percent = ModEntry.ActiveEnergyPercent, cost = requiredStamina });
            }
            else
            {
                buttonText = ModEntry.I18n.Get("button.warp");
            }

            Color btnColor;
            if (!online)
            {
                btnColor = Color.Gray;
            }
            else if (ModEntry.ActiveEnergyPercent > 0 && Game1.player.Stamina < requiredStamina && !ModEntry.ActiveAllowExhaustion)
            {
                btnColor = warpButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ? new Color(255, 150, 150) : new Color(220, 170, 170);
            }
            else
            {
                btnColor = warpButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ? Color.Wheat : Color.White;
            }

            UtilityPlus.drawButtonWithText(b, warpButton.bounds, btnColor, buttonText, Game1.smallFont, Game1.textColor);
        }
    }
}
