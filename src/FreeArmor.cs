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

namespace FreeArmor;

public class ConfigModel {
  public bool Enabled { get; set; } = true;

  public string AccessFlag { get; set; } = "";
}

[PluginMetadata(Id = "FreeArmor", Version = "1.0.1", Name = "FreeArmor", Author = "aga", Description = "Gives free armor on full buy rounds")]
public partial class FreeArmor : BasePlugin {
  private IOptionsMonitor<ConfigModel>? _config;
  private ServiceProvider? _serviceProvider;
  private IConVar<bool>? _enabledCvar;
  private CCSGameRulesProxy? _gameRulesProxy;
  private int _maxRounds = 30;

  public FreeArmor(ISwiftlyCore core) : base(core)
  {
  }

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager) {
  }

  public override void UseSharedInterface(IInterfaceManager interfaceManager) {
  }

  public override void Load(bool hotReload) {
    _enabledCvar = Core.ConVar.CreateOrFind<bool>("freearmor_enabled", "Enable/disable FreeArmor plugin", true);

    Core.Configuration
      .InitializeJsonWithModel<ConfigModel>("config.jsonc", "Main")
      .Configure(builder => {
        builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
      });

    ServiceCollection services = new();
    services
      .AddSwiftly(Core)
      .AddOptionsWithValidateOnStart<ConfigModel>()
      .BindConfiguration("Main");

    _serviceProvider = services.BuildServiceProvider();
    _config = _serviceProvider.GetRequiredService<IOptionsMonitor<ConfigModel>>();

    Core.Event.OnMapLoad += _ => {
      Core.Scheduler.DelayBySeconds(1.0f, () => {
        _gameRulesProxy = Core.EntitySystem.GetAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        var maxRoundsCvar = Core.ConVar.Find<int>("mp_maxrounds");
        if (maxRoundsCvar != null)
          _maxRounds = maxRoundsCvar.Value;
      });
    };

    if (hotReload)
    {
      _gameRulesProxy = Core.EntitySystem.GetAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
      var maxRoundsCvar = Core.ConVar.Find<int>("mp_maxrounds");
      if (maxRoundsCvar != null)
        _maxRounds = maxRoundsCvar.Value;
    }

    Core.GameEvent.HookPre<EventPlayerSpawn>((@event) => {
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
        int totalRounds = gameRules.TotalRoundsPlayed;

        if (totalRounds == 0 || totalRounds == _maxRounds / 2)
        {
          armorValue = 0;
          hasHelmet = false;
        }
      }

      Core.Scheduler.DelayBySeconds(0.1f, () => {
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

  public override void Unload() {
    _serviceProvider?.Dispose();
    _serviceProvider = null;
    _config = null;
    _enabledCvar = null;
    _gameRulesProxy = null;
  }
}