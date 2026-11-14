using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace WarpToFriends;

public class OptionsKeyListener<TOptions> : IClickableMenu where TOptions : class, new()
{
    private TOptions _config;
    private string _property;
    private readonly string _message = "Press New Key...";
    private IClickableMenu _returnMenu;

    public OptionsKeyListener(TOptions config, string property, IClickableMenu returnMenu)
    {
        _config = config;
        _property = property;
        _returnMenu = returnMenu;
    }

    public override void draw(SpriteBatch b)
    {
        _returnMenu.draw(b);
        int num = 2;
        float num2 = Game1.smallFont.MeasureString(_message).X / 2f * num;
        float num3 = Game1.smallFont.MeasureString(_message).Y / 2f * num;
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);
        b.DrawString(Game1.dialogueFont, _message, new Vector2((float)(Game1.viewport.Width / 2) - num2, (float)(Game1.viewport.Height / 2) - num3), Color.White);
    }

    public override void receiveKeyPress(Keys key)
    {
        PropertyInfo propertyInfo = _config.GetType().GetProperties().FirstOrDefault(p => p.Name == _property);
        if (propertyInfo != null)
        {
            if (propertyInfo.PropertyType == typeof(KeybindList))
            {
                propertyInfo.SetValue(_config, KeybindList.Parse(key.ToString()));
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                propertyInfo.SetValue(_config, key.ToString());
            }
        }
        exitThisMenu();
        Game1.activeClickableMenu = _returnMenu;
    }
}
