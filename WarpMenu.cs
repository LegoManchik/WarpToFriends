using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using WarpToFriends.Helpers;

namespace WarpToFriends;

public class WarpMenu : IClickableMenu
{
    private readonly List<Farmer> _farmers;
    private List<PlayerBar> _playerBars = new();
    private ClickableTextureComponent _optionsButton;
    private ClickableTextureComponent _upArrow;
    private ClickableTextureComponent _downArrow;

    private int _currentIndex = 0;
    private const int MaxPerPage = 4;

    public WarpMenu(int w = 720, int h = 420)
        : base(Game1.viewport.Width / 2 - w / 2, Game1.viewport.Height / 2 - h / 2, w, h, showUpperRightCloseButton: true)
    {
        // Prioritize other farmers: online first, then offline
        var onlineIds = Game1.getOnlineFarmers().Select(f => f.UniqueMultiplayerID).ToHashSet();
        _farmers = PlayerHelper.GetAllCreatedFarmers()
            .Where(f => f.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
            .OrderByDescending(f => onlineIds.Contains(f.UniqueMultiplayerID))
            .ThenBy(f => f.Name)
            .ToList();

        width = w;
        height = h;

        _optionsButton = new ClickableTextureComponent(
            "Options",
            new Rectangle(xPositionOnScreen + width - 64, yPositionOnScreen + height + 16, 64, 64),
            "",
            ModEntry.I18n.Get("menu.options_tooltip"),
            Game1.mouseCursors,
            new Rectangle(162, 440, 16, 16),
            4f
        );

        _upArrow = new ClickableTextureComponent(
            "up-arrow",
            new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen, 44, 48),
            "",
            "",
            Game1.mouseCursors,
            new Rectangle(421, 459, 11, 12),
            4f
        );

        _downArrow = new ClickableTextureComponent(
            "down-arrow",
            new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 48, 44, 48),
            "",
            "",
            Game1.mouseCursors,
            new Rectangle(421, 472, 11, 12),
            4f
        );

        setUpPlayerBars();
    }

    public void setUpPlayerBars()
    {
        _playerBars.Clear();
        int slotHeight = 90;

        int count = Math.Min(MaxPerPage, _farmers.Count - _currentIndex);
        for (int i = 0; i < count; i++)
        {
            int farmerIdx = _currentIndex + i;
            if (farmerIdx >= _farmers.Count)
                break;

            Farmer f = _farmers[farmerIdx];
            PlayerBar playerBar = new PlayerBar(f);

            int startX = xPositionOnScreen;
            int startY = yPositionOnScreen + slotHeight * i + 16;
            Rectangle bounds = new Rectangle(startX + 16, startY, width - 32, slotHeight - 6);
            playerBar.section = new ClickableComponent(bounds, f.Name);

            int iconX = startX + 30;
            int iconY = startY + 6;
            playerBar.icon = new ClickableComponent(new Rectangle(iconX, iconY, 80, 80), f.Name);

            // Warp button on the right side
            int btnWidth = 130;
            int btnHeight = 44;
            int btnX = startX + width - 32 - btnWidth - 16;
            int btnY = startY + (slotHeight - 6) / 2 - btnHeight / 2;
            playerBar.warpButton = new ClickableComponent(new Rectangle(btnX, btnY, btnWidth, btnHeight), f.Name);

            _playerBars.Add(playerBar);
        }
    }

    public override void draw(SpriteBatch b)
    {
        drawMenuBackground(b);
        drawMenuTitle(b);
        drawPlayerBars(b);
        drawOptionsButton(b);

        if (_farmers.Count > MaxPerPage)
        {
            if (_currentIndex > 0)
                _upArrow.draw(b);
            if (_currentIndex + MaxPerPage < _farmers.Count)
                _downArrow.draw(b);
        }

        if (_farmers.Count == 0)
        {
            string emptyMsg = ModEntry.I18n.Get("menu.no_friends");
            Vector2 sz = Game1.dialogueFont.MeasureString(emptyMsg);
            b.DrawString(Game1.dialogueFont, emptyMsg, new Vector2(xPositionOnScreen + width / 2f - sz.X / 2f, yPositionOnScreen + height / 2f - sz.Y / 2f), Game1.textColor);
        }

        base.draw(b);
        base.drawMouse(b);
    }

    private void drawMenuTitle(SpriteBatch b)
    {
        string title = ModEntry.I18n.Get("menu.title");
        SpriteText.drawStringWithScrollCenteredAt(b, title, xPositionOnScreen + width / 2, yPositionOnScreen - 72, "", 1f, null, 0, 0.88f, false);
    }

    private void drawOptionsButton(SpriteBatch b)
    {
        _optionsButton.draw(b);
        _optionsButton.tryHover(Game1.getMouseX(), Game1.getMouseY());
        if (_optionsButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
        {
            IClickableMenu.drawToolTip(b, _optionsButton.hoverText, _optionsButton.name, null);
        }
    }

    private void drawPlayerBars(SpriteBatch b)
    {
        foreach (PlayerBar playerBar in _playerBars)
        {
            playerBar.draw(b);
        }
    }

    private void drawMenuBackground(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
        IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), xPositionOnScreen, yPositionOnScreen, width, height + 16, Color.White, 4f, true);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        foreach (PlayerBar playerBar in _playerBars)
        {
            if (playerBar.online && playerBar.farmer != Game1.player && playerBar.warpButton.containsPoint(x, y))
            {
                PlayerHelper.WarpFarmerToPlayer(playerBar.farmer);
                return;
            }
        }

        if (_optionsButton.containsPoint(x, y))
        {
            Game1.playSound("drumkit6");
            exitThisMenuNoSound();
            Game1.activeClickableMenu = new OptionsMenu<ModConfig>(ModEntry.Helper, 520, 400, Game1.player.UniqueMultiplayerID, ModEntry.config, this);
            return;
        }

        if (_farmers.Count > MaxPerPage)
        {
            if (_upArrow.containsPoint(x, y) && _currentIndex > 0)
            {
                _currentIndex--;
                setUpPlayerBars();
                Game1.playSound("shwip");
                return;
            }

            if (_downArrow.containsPoint(x, y) && _currentIndex + MaxPerPage < _farmers.Count)
            {
                _currentIndex++;
                setUpPlayerBars();
                Game1.playSound("shwip");
                return;
            }
        }

        base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (_farmers.Count > MaxPerPage)
        {
            if (direction > 0 && _currentIndex > 0)
            {
                _currentIndex--;
                setUpPlayerBars();
                Game1.playSound("shwip");
            }
            else if (direction < 0 && _currentIndex + MaxPerPage < _farmers.Count)
            {
                _currentIndex++;
                setUpPlayerBars();
                Game1.playSound("shwip");
            }
        }
    }
}
