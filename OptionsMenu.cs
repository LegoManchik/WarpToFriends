using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace WarpToFriends;

public class OptionsMenu<TOptions> : IClickableMenu where TOptions : class, new()
{
    private readonly IModHelper _helper;
    private readonly long _originPlayerId;
    private TOptions _config;
    private IClickableMenu _returnMenu;

    private static int BorderWidth = 16;
    private ClickableTextureComponent Scrollbar;
    private ClickableTextureComponent UpArrow;
    private ClickableTextureComponent DownArrow;
    private Rectangle ScrollbarRail;
    private int CurrentOptionIndex;
    private int OptionCount;
    private readonly int OptionsPerPage;
    private ClickableTextureComponent SliderHeld;
    private bool Sliding;

    private static int OptionHeight => 75;

    private List<ClickableTextureComponent> Checkboxes { get; set; } = new();
    private List<ClickableTextureComponent> SetButtons { get; set; } = new();
    private List<ClickableTextureComponent> Sliders { get; set; } = new();

    public OptionsMenu(IModHelper helper, int w, int h, long originPlayerId, TOptions config, IClickableMenu returnMenu = null)
        : base(Game1.viewport.Width / 2 - w / 2, Game1.viewport.Height / 2 - h / 2, w, h, showUpperRightCloseButton: true)
    {
        _helper = helper;
        _originPlayerId = originPlayerId;
        _config = config;
        _returnMenu = returnMenu;
        OptionsPerPage = (height - BorderWidth * 2) / OptionHeight;

        UpArrow = new ClickableTextureComponent("up-arrow", new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen, 44, 48), "", "", Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
        DownArrow = new ClickableTextureComponent("down-arrow", new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 48, 44, 48), "", "", Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        Scrollbar = new ClickableTextureComponent("scrollbar", new Rectangle(UpArrow.bounds.X + 12, UpArrow.bounds.Y + UpArrow.bounds.Height + 4, 24, 40), "", "", Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
        ScrollbarRail = new Rectangle(Scrollbar.bounds.X, Scrollbar.bounds.Y, Scrollbar.bounds.Width, height - UpArrow.bounds.Height - DownArrow.bounds.Height - 8);

        exitFunction = () =>
        {
            _helper.WriteConfig(_config);
            ModEntry.UpdateActiveHostSettings();
            ModEntry.BroadcastHostSettings();
        };
    }

    private List<PropertyInfo> GetAllowedProperties()
    {
        var list = new List<PropertyInfo>();
        foreach (var p in _config.GetType().GetProperties())
        {
            var attr = p.GetCustomAttributes(inherit: true).FirstOrDefault(a => a is OptionDisplayAttribute) as OptionDisplayAttribute;
            if (attr != null && attr.HostOnly && Context.IsWorldReady && !Context.IsMainPlayer)
            {
                // Host-only options are hidden for non-hosts in co-op
                continue;
            }
            list.Add(p);
        }
        return list;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), xPositionOnScreen, yPositionOnScreen, width, height, Color.White, 4f, true);

        List<PropertyInfo> properties = GetAllowedProperties();
        OptionCount = properties.Count;
        Checkboxes.Clear();
        SetButtons.Clear();
        Sliders.Clear();

        int num = CurrentOptionIndex;
        int num2 = 0;
        while (num2 < Math.Min(OptionCount, OptionsPerPage) && num < properties.Count)
        {
            PropertyInfo propertyInfo = properties[num];
            object value = propertyInfo.GetValue(_config, null);
            Type propertyType = propertyInfo.PropertyType;
            string name = propertyInfo.Name;
            int maxValue = 100;

            object obj = propertyInfo.GetCustomAttributes(inherit: true).FirstOrDefault(a => a is OptionDisplayAttribute);
            if (obj != null)
            {
                name = ((OptionDisplayAttribute)obj).Name;
                maxValue = ((OptionDisplayAttribute)obj).MaxValue;
            }

            if (propertyType == typeof(bool))
            {
                DrawOption(num2, name, b);
                DrawBoolOption(num2, (bool)(value ?? false), propertyInfo.Name, b);
            }
            else if (propertyType == typeof(string) || propertyType == typeof(KeybindList))
            {
                string displayVal = value?.ToString() ?? "";
                DrawOption(num2, name, b, displayVal);
                DrawKeyOption(num2, displayVal, propertyInfo.Name, b);
            }
            else if (propertyType == typeof(int))
            {
                DrawOption(num2, name, b, (value?.ToString() ?? "0") + "%");
                DrawSliderOption(num2, (int)(value ?? 0), propertyInfo.Name, maxValue, b);
            }

            num2++;
            num++;
        }

        base.draw(b);
        drawScrollbar(b);
        base.drawMouse(b);
    }

    private void DrawOption(int idx, string optionName, SpriteBatch b, string value = null)
    {
        int num = xPositionOnScreen + BorderWidth;
        int num2 = yPositionOnScreen + BorderWidth + idx * OptionHeight;
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), num, num2, width - 2 * BorderWidth, OptionHeight, Color.White, 4f, false);
        float y = Game1.smallFont.MeasureString(optionName).Y;
        float y2 = (float)num2 + (float)OptionHeight / 2f - y / 2f;
        Utility.drawTextWithShadow(b, optionName + ((value != null) ? $" : {value}" : ""), Game1.smallFont, new Vector2(num + BorderWidth, y2), Color.Black);
    }

    private void DrawSliderOption(int idx, int value, string propertyName, int maxValue, SpriteBatch b)
    {
        int railWidth = 100;
        int railHeight = 24;
        int num3 = xPositionOnScreen + width - 2 * BorderWidth - railWidth;
        int num4 = yPositionOnScreen + BorderWidth + idx * OptionHeight + OptionHeight / 2 - railHeight / 2;
        Rectangle rectangle = new Rectangle(403, 383, 6, 6);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, rectangle, num3, num4, railWidth, railHeight, Color.White, 4f, false);

        int sliderWidth = 24;
        int sliderHeight = 24;
        float frac = maxValue > 0 ? Math.Clamp((float)value / maxValue, 0f, 1f) : 0f;
        int sliderX = num3 + (int)(frac * (railWidth - sliderWidth));

        ClickableTextureComponent clickableTextureComponent = new ClickableTextureComponent(
            sourceRect: new Rectangle(420, 441, 10, 6),
            name: propertyName,
            bounds: new Rectangle(sliderX, num4, sliderWidth, sliderHeight),
            label: "",
            hoverText: "",
            texture: Game1.mouseCursors,
            scale: 4f,
            drawShadow: true
        );
        Sliders.Add(clickableTextureComponent);
        clickableTextureComponent.draw(b);
    }

    private void DrawKeyOption(int idx, string value, string propertyName, SpriteBatch b)
    {
        int num = 84;
        int num2 = 44;
        int x = xPositionOnScreen + width - 2 * BorderWidth - num;
        int y = yPositionOnScreen + BorderWidth + idx * OptionHeight + OptionHeight / 2 - num2 / 2;
        ClickableTextureComponent clickableTextureComponent = new ClickableTextureComponent(sourceRect: new Rectangle(294, 428, 21, 11), name: propertyName, bounds: new Rectangle(x, y, num, num2), label: "", hoverText: "", texture: Game1.mouseCursors, scale: 4f, drawShadow: true);
        SetButtons.Add(clickableTextureComponent);
        clickableTextureComponent.draw(b);
    }

    private void DrawBoolOption(int idx, bool value, string propertyName, SpriteBatch b)
    {
        int num = 36;
        int x = xPositionOnScreen + width - 3 * BorderWidth - num;
        int y = yPositionOnScreen + BorderWidth + idx * OptionHeight + OptionHeight / 2 - num / 2;
        Rectangle rectangle = new Rectangle(227, 425, 9, 9);
        Rectangle rectangle2 = new Rectangle(236, 425, 9, 9);
        Rectangle sourceRect = (value ? rectangle2 : rectangle);
        ClickableTextureComponent clickableTextureComponent = new ClickableTextureComponent(propertyName, new Rectangle(x, y, num, num), "", "", Game1.mouseCursors, sourceRect, 4f);
        Checkboxes.Add(clickableTextureComponent);
        clickableTextureComponent.draw(b);
    }

    private void drawScrollbar(SpriteBatch b)
    {
        if (OptionCount <= OptionsPerPage)
            return;

        UpArrow.draw(b);
        DownArrow.draw(b);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), ScrollbarRail.X, ScrollbarRail.Y, ScrollbarRail.Width, ScrollbarRail.Height, Color.White, 4f, false);
        Scrollbar.bounds.Y = ScrollbarRail.Height / Math.Max(1, OptionCount - OptionsPerPage + 1) * CurrentOptionIndex + ScrollbarRail.Y;
        if (CurrentOptionIndex >= OptionCount - OptionsPerPage)
        {
            Scrollbar.bounds.Y = ScrollbarRail.Bottom - Scrollbar.bounds.Height;
        }
        Scrollbar.draw(b);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (ClickableTextureComponent checkbox in Checkboxes)
        {
            if (checkbox.containsPoint(x, y))
            {
                ToggleOption(checkbox.name);
                Game1.playSound("drumkit6");
                return;
            }
        }

        foreach (ClickableTextureComponent setButton in SetButtons)
        {
            if (setButton.containsPoint(x, y))
            {
                Game1.playSound("drumkit6");
                Game1.activeClickableMenu = new OptionsKeyListener<TOptions>(_config, setButton.name, this);
                return;
            }
        }

        foreach (ClickableTextureComponent slider in Sliders)
        {
            int railWidth = 100;
            int railX = xPositionOnScreen + width - 2 * BorderWidth - railWidth;
            Rectangle railRect = new Rectangle(railX - 10, slider.bounds.Y - 10, railWidth + 20, slider.bounds.Height + 20);
            if (slider.containsPoint(x, y) || railRect.Contains(x, y))
            {
                SliderHeld = slider;
                Sliding = true;
                setSliderValue(slider, x, y);
                Game1.playSound("drumkit6");
                return;
            }
        }

        if (UpArrow.containsPoint(x, y) && CurrentOptionIndex > 0)
        {
            CurrentOptionIndex--;
            Game1.playSound("shwip");
            return;
        }

        if (DownArrow.containsPoint(x, y) && CurrentOptionIndex + OptionsPerPage < OptionCount)
        {
            CurrentOptionIndex++;
            Game1.playSound("shwip");
            return;
        }

        if (upperRightCloseButton.containsPoint(x, y))
        {
            _helper.WriteConfig(_config);
            ModEntry.UpdateActiveHostSettings();
            ModEntry.BroadcastHostSettings();
            base.receiveLeftClick(x, y, playSound);
            Game1.activeClickableMenu = _returnMenu;
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (direction > 0 && CurrentOptionIndex > 0)
        {
            CurrentOptionIndex--;
        }
        else if (direction < 0 && CurrentOptionIndex + OptionsPerPage < OptionCount)
        {
            CurrentOptionIndex++;
        }
    }

    public override void leftClickHeld(int x, int y)
    {
        if (Sliding && SliderHeld != null)
        {
            setSliderValue(SliderHeld, x, y);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        SliderHeld = null;
        Sliding = false;
    }

    public override void emergencyShutDown()
    {
        exitFunction?.Invoke();
        _returnMenu?.emergencyShutDown();
    }

    private void ToggleOption(string checkboxName)
    {
        PropertyInfo propertyInfo = _config.GetType().GetProperties().FirstOrDefault(p => p.Name == checkboxName);
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(_config, !(bool)propertyInfo.GetValue(_config, null));
        }
    }

    private void setSliderValue(ClickableTextureComponent slider, int x, int y)
    {
        PropertyInfo propertyInfo = _config.GetType().GetProperties().FirstOrDefault(p => p.Name == slider.name);
        if (propertyInfo != null)
        {
            int maxValue = 100;
            int minValue = 0;
            var attr = propertyInfo.GetCustomAttributes(inherit: true).OfType<OptionDisplayAttribute>().FirstOrDefault();
            if (attr != null)
            {
                maxValue = attr.MaxValue;
                minValue = attr.MinValue;
            }

            int railWidth = 100;
            int railX = xPositionOnScreen + width - 2 * BorderWidth - railWidth;
            float percent = Math.Clamp((float)(x - railX) / railWidth, 0f, 1f);
            int val = minValue + (int)Math.Round(percent * (maxValue - minValue));
            propertyInfo.SetValue(_config, val);
        }
    }
}
