using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace WarpToFriends;

internal class UtilityPlus : Utility
{
    public static void drawButtonWithText(SpriteBatch b, Rectangle bounds, Color boxColor, string text, SpriteFont font, Color color)
    {
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), bounds.X, bounds.Y, bounds.Width, bounds.Height, boxColor, 4f, true);
        float x = font.MeasureString(text).X;
        float y = font.MeasureString(text).Y;
        Vector2 vector = new Vector2(bounds.Width / 2f, bounds.Height / 2f);
        Vector2 position = new Vector2((float)bounds.X + vector.X - x / 2f, (float)bounds.Y + vector.Y - y / 2f);
        Utility.drawBoldText(b, text, font, position, color);
    }

    public static void drawTextBox(SpriteBatch b, Rectangle bounds, Color boxColor, string text, SpriteFont font, Color color)
    {
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), bounds.X, bounds.Y, bounds.Width, bounds.Height, boxColor, 4f, true);
    }
}
