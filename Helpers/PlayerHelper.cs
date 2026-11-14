using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace WarpToFriends.Helpers;

public static class PlayerHelper
{
    public static List<Farmer> GetAllCreatedFarmers()
    {
        return Game1.getAllFarmers()
            .Where(f => !string.IsNullOrEmpty(f.Name))
            .ToList();
    }

    public static int GetRequiredStamina(Farmer player)
    {
        if (ModEntry.ActiveEnergyPercent <= 0)
            return 0;

        int cost = (int)Math.Round((player.MaxStamina * ModEntry.ActiveEnergyPercent) / 100f);
        return Math.Max(1, cost);
    }

    public static bool WarpFarmerToPlayer(Farmer toFarmer, Farmer player = null)
    {
        if (player == null)
            player = Game1.player;

        if (toFarmer == null || toFarmer.currentLocation == null)
        {
            Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("hud.location-offline"), HUDMessage.error_type));
            Game1.playSound("cancel");
            return false;
        }

        // Energy consumption check (applies only to the local player executing the warp)
        int requiredStamina = GetRequiredStamina(player);
        if (player == Game1.player && requiredStamina > 0)
        {
            if (player.Stamina < requiredStamina && !ModEntry.ActiveAllowExhaustion)
            {
                string msg = ModEntry.I18n.Get("hud.not-enough-energy", new { percent = ModEntry.ActiveEnergyPercent, cost = requiredStamina });
                Game1.addHUDMessage(new HUDMessage(msg, HUDMessage.error_type));
                Game1.playSound("cancel");
                return false;
            }

            player.Stamina = Math.Max(ModEntry.ActiveAllowExhaustion ? -20f : 0f, player.Stamina - requiredStamina);
            if (player.Stamina <= 0f)
            {
                player.exhausted.Value = true;
            }
        }

        Game1.playSound("wand");

        if (Game1.activeClickableMenu is WarpMenu)
        {
            Game1.activeClickableMenu.exitThisMenuNoSound();
        }

        string text = toFarmer.currentLocation.NameOrUniqueName;
        int targetTileX = (int)(toFarmer.Position.X + 16f) / 64;
        int targetTileY = (int)(toFarmer.Position.Y + 16f) / 64;

        player.warpFarmer(new Warp(0, 0, text, targetTileX, targetTileY, false));
        return true;
    }
}
