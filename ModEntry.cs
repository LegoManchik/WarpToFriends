using System;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using WarpToFriends.Helpers;

namespace WarpToFriends;

public class ModEntry : Mod
{
    public static ModConfig config = null!;
    public new static IModHelper Helper { get; private set; } = null!;
    public new static IMonitor Monitor { get; private set; } = null!;
    public static ITranslationHelper I18n { get; private set; } = null!;

    public static int ActiveEnergyPercent { get; set; } = 10;
    public static bool ActiveAllowExhaustion { get; set; } = false;

    private int _lastProcessedMessageCount = 0;

    public override void Entry(IModHelper helper)
    {
        Helper = helper;
        Monitor = base.Monitor;
        I18n = helper.Translation;

        config = helper.ReadConfig<ModConfig>();
        UpdateActiveHostSettings();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;

        helper.Events.Multiplayer.PeerContextReceived += OnPeerContextReceived;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;

        helper.ConsoleCommands.Add("warp_to_friend", "Warp to an online friend.\n\nUsage: warp_to_friend <friend_name>", CommandWarpToFriend);
    }

    public static void UpdateActiveHostSettings()
    {
        if (Context.IsMainPlayer || !Context.IsWorldReady)
        {
            ActiveEnergyPercent = config.EnergyCostPercent;
            ActiveAllowExhaustion = config.AllowExhaustion;
        }
    }

    public static void BroadcastHostSettings()
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        var message = new EnergySyncMessage
        {
            EnergyCostPercent = config.EnergyCostPercent,
            AllowExhaustion = config.AllowExhaustion
        };

        Helper.Multiplayer.SendMessage(
            message: message,
            messageType: "SyncEnergyConfig",
            modIDs: new[] { Helper.ModRegistry.ModID }
        );
    }

    private void OnPeerContextReceived(object sender, PeerContextReceivedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            BroadcastHostSettings();
        }
    }

    private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID == ModManifest.UniqueID && e.Type == "SyncEnergyConfig")
        {
            var msg = e.ReadAs<EnergySyncMessage>();
            ActiveEnergyPercent = msg.EnergyCostPercent;
            ActiveAllowExhaustion = msg.AllowExhaustion;
            Monitor.Log($"[WarpToFriends] Synced energy settings from host: {ActiveEnergyPercent}% max stamina, AllowExhaustion: {ActiveAllowExhaustion}", LogLevel.Debug);
        }
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        // Register Generic Mod Config Menu if installed
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm != null)
        {
            gmcm.Register(
                mod: ModManifest,
                reset: () =>
                {
                    config = new ModConfig();
                    Helper.WriteConfig(config);
                    UpdateActiveHostSettings();
                    BroadcastHostSettings();
                },
                save: () =>
                {
                    Helper.WriteConfig(config);
                    UpdateActiveHostSettings();
                    BroadcastHostSettings();
                }
            );

            gmcm.AddKeybindList(
                mod: ModManifest,
                getValue: () => config.OpenMenuKey,
                setValue: val => config.OpenMenuKey = val,
                name: () => I18n.Get("config.open-menu-key.name"),
                tooltip: () => I18n.Get("config.open-menu-key.desc")
            );

            gmcm.AddNumberOption(
                mod: ModManifest,
                getValue: () => Context.IsWorldReady && !Context.IsMainPlayer ? ActiveEnergyPercent : config.EnergyCostPercent,
                setValue: val =>
                {
                    if (Context.IsMainPlayer || !Context.IsWorldReady)
                    {
                        config.EnergyCostPercent = val;
                        UpdateActiveHostSettings();
                        BroadcastHostSettings();
                    }
                },
                name: () => I18n.Get("config.energy-cost.name") + (Context.IsWorldReady && !Context.IsMainPlayer ? " (" + I18n.Get("config.host_only_tag") + ")" : ""),
                tooltip: () => (Context.IsWorldReady && !Context.IsMainPlayer ? I18n.Get("config.host_only_tooltip") + " " : "") + I18n.Get("config.energy-cost.desc"),
                min: 0,
                max: 100,
                interval: 5,
                formatValue: val => $"{val}%"
            );

            gmcm.AddBoolOption(
                mod: ModManifest,
                getValue: () => Context.IsWorldReady && !Context.IsMainPlayer ? ActiveAllowExhaustion : config.AllowExhaustion,
                setValue: val =>
                {
                    if (Context.IsMainPlayer || !Context.IsWorldReady)
                    {
                        config.AllowExhaustion = val;
                        UpdateActiveHostSettings();
                        BroadcastHostSettings();
                    }
                },
                name: () => I18n.Get("config.allow-exhaustion.name") + (Context.IsWorldReady && !Context.IsMainPlayer ? " (" + I18n.Get("config.host_only_tag") + ")" : ""),
                tooltip: () => (Context.IsWorldReady && !Context.IsMainPlayer ? I18n.Get("config.host_only_tooltip") + " " : "") + I18n.Get("config.allow-exhaustion.desc")
            );

            gmcm.AddBoolOption(
                mod: ModManifest,
                getValue: () => config.canConsoleWarp,
                setValue: val => config.canConsoleWarp = val,
                name: () => I18n.Get("config.console-warp.name"),
                tooltip: () => I18n.Get("config.console-warp.desc")
            );
        }
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        _lastProcessedMessageCount = Game1.chatBox?.messages?.Count ?? 0;
        UpdateActiveHostSettings();
        if (Context.IsMainPlayer)
        {
            BroadcastHostSettings();
        }
    }

    private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
    {
        _lastProcessedMessageCount = 0;
        UpdateActiveHostSettings();
    }

    private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (config.OpenMenuKey.JustPressed())
        {
            if (Game1.activeClickableMenu is WarpMenu)
            {
                Game1.activeClickableMenu.exitThisMenu();
            }
            else if (Context.IsPlayerFree && Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new WarpMenu();
            }
        }
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (!config.canConsoleWarp || !Context.IsWorldReady || Game1.chatBox?.messages == null)
            return;

        var messages = Game1.chatBox.messages;
        if (messages.Count == _lastProcessedMessageCount)
            return;

        if (messages.Count < _lastProcessedMessageCount)
        {
            _lastProcessedMessageCount = messages.Count;
            return;
        }

        for (int i = _lastProcessedMessageCount; i < messages.Count; i++)
        {
            var chatMsg = messages[i];
            string text = ChatMessage.makeMessagePlaintext(chatMsg.message, false);
            ProcessChatMessage(text);
        }

        _lastProcessedMessageCount = messages.Count;
    }

    private void ProcessChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        string[] parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        // Command: !warp <FriendName>
        if (parts[0].Equals("!warp", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length == 2)
            {
                string targetName = parts[1];
                Farmer target = Game1.getOnlineFarmers().FirstOrDefault(f => f.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
                if (target != null)
                {
                    PlayerHelper.WarpFarmerToPlayer(target, Game1.player);
                }
            }
            else if (parts.Length >= 3)
            {
                // Host/admin warping player1 to player2: !warp Player1 Player2
                string player1Name = parts[1];
                string player2Name = parts[2];
                Farmer p1 = Game1.getOnlineFarmers().FirstOrDefault(f => f.Name.Equals(player1Name, StringComparison.OrdinalIgnoreCase));
                Farmer p2 = Game1.getOnlineFarmers().FirstOrDefault(f => f.Name.Equals(player2Name, StringComparison.OrdinalIgnoreCase));
                if (p1 != null && p2 != null)
                {
                    PlayerHelper.WarpFarmerToPlayer(p2, p1);
                }
            }
        }
    }

    private void CommandWarpToFriend(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("You must load a save first!", LogLevel.Warn);
            return;
        }

        if (args.Length == 0)
        {
            Monitor.Log("Please specify a friend's name: warp_to_friend <name>", LogLevel.Warn);
            return;
        }

        string targetName = string.Join(" ", args);
        Farmer target = Game1.getOnlineFarmers().FirstOrDefault(f => f.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            Monitor.Log($"Could not find an online player named '{targetName}'.", LogLevel.Error);
            return;
        }

        PlayerHelper.WarpFarmerToPlayer(target);
    }
}
