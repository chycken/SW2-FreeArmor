using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.SchemaDefinitions;
using System;
using System.Linq;

namespace FreeArmor;

public class ConfigModel
{
  public bool Enabled { get; set; } = true;
  public string AccessFlag { get; set; } = "";
}

[PluginMetadata(Id = "FreeArmor", Version = "1.0.2", Name = "FreeArmor", Author = "aga (fixed)", Description = "Gives free armor on full buy rounds")]
public partial class FreeArmor : BasePlugin
{
  private IOptionsMonitor<ConfigModel>? _config;
  private ServiceProvider? _serviceProvider;
  private IConVar<bool>? _enabledCvar;
  private CCSGameRulesProxy? _gameRulesProxy;

  // fallback default if we can't retrieve anything
  private int _maxRounds = 30;

  public FreeArmor(ISwiftlyCore core) : base(core) { }

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager) { }
  public override void UseSharedInterface(IInterfaceManager interfaceManager) { }

  public override void Load(bool hotReload)
  {
    _enabledCvar = Core.ConVar.CreateOrFind<bool>("freearmor_enabled", "Enable/disable FreeArmor plugin", true);

    Core.Configuration
      .InitializeJsonWithModel<ConfigModel>("config.jsonc", "Main")
      .Configure(builder =>
      {
        builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
      });

    ServiceCollection services = new();
    services
      .AddSwiftly(Core)
      .AddOptionsWithValidateOnStart<ConfigModel>()
      .BindConfiguration("Main");

    _serviceProvider = services.BuildServiceProvider();
    _config = _serviceProvider.GetRequiredService<IOptionsMonitor<ConfigModel>>();

    void RefreshGameRulesAndMaxRounds()
    {
      _gameRulesProxy = Core.EntitySystem.GetAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();

      // mp_maxrounds sometimes 0 / unavailable in some modes -> only if used > 0
      var maxRoundsCvar = Core.ConVar.Find<int>("mp_maxrounds");
      if (maxRoundsCvar != null && maxRoundsCvar.Value > 0)
        _maxRounds = maxRoundsCvar.Value;
    }

    Core.Event.OnMapLoad += _ =>
    {
      Core.Scheduler.DelayBySeconds(1.0f, () => RefreshGameRulesAndMaxRounds());
    };

    if (hotReload)
    {
      RefreshGameRulesAndMaxRounds();
    }

    Core.GameEvent.HookPre<EventPlayerSpawn>((@event) =>
    {
      var player = @event.UserIdPlayer;
      if (player == null || !player.IsValid)
        return HookResult.Continue;

      if (_enabledCvar != null && !_enabledCvar.Value)
        return HookResult.Continue;

      if (_config != null)
      {
        var config = _config.CurrentValue;
        if (!config.Enabled)
          return HookResult.Continue;

        var accessFlag = config.AccessFlag?.Trim();
        if (!string.IsNullOrEmpty(accessFlag))
        {
          if (!Core.Permission.PlayerHasPermissions(player.SteamID, new[] { accessFlag }))
            return HookResult.Continue;
        }
      }

      var controller = player.Controller;
      if (controller == null || !controller.IsValid)
        return HookResult.Continue;

      var pawnHandle = controller.PlayerPawn;
      if (!pawnHandle.IsValid)
        return HookResult.Continue;

      var pawn = pawnHandle.Value;
      if (pawn == null || !pawn.IsValid)
        return HookResult.Continue;

      if (_gameRulesProxy == null)
        return HookResult.Continue;

      var gameRules = _gameRulesProxy.GameRules;
      if (gameRules == null)
        return HookResult.Continue;

      int armorValue = 100;
      bool hasHelmet = true;

      if (!gameRules.WarmupPeriod)
      {
        // TotalRoundsPlayed = number of rounds already played
        var totalRounds = gameRules.TotalRoundsPlayed;

       // Calculate half: if mp_maxrounds is valid, use it, otherwise fallback _maxRounds.
// (The point is: it should not be 0 or negative.)
        var maxRounds = _maxRounds;
        var cvar = Core.ConVar.Find<int>("mp_maxrounds");
        if (cvar != null && cvar.Value > 0)
          maxRounds = cvar.Value;

        var half = maxRounds / 2;

// ✅ PISTOL ROUND detection:
// - first round: totalRounds == 0
// - first round after halftime (and beginning of overtime halves): totalRounds % half == 0
// (but only if half > 0)
        var isPistolRound =
            totalRounds == 0 ||
            (half > 0 && totalRounds > 0 && (totalRounds % half) == 0);

        if (isPistolRound)
        {
          armorValue = 0;
          hasHelmet = false;
        }
      }

      Core.Scheduler.DelayBySeconds(0.1f, () =>
      {
        if (pawn == null || !pawn.IsValid)
          return;

        pawn.ArmorValue = armorValue;
        pawn.ArmorValueUpdated();

        var itemServices = pawn.ItemServices;
        if (itemServices != null)
        {
          itemServices.HasHelmet = hasHelmet;
          itemServices.HasHelmetUpdated();
        }
      });

      return HookResult.Continue;
    });
  }

  public override void Unload()
  {
    _serviceProvider?.Dispose();
    _serviceProvider = null;
    _config = null;
    _enabledCvar = null;
    _gameRulesProxy = null;
  }
}
