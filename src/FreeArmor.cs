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

[PluginMetadata(Id = "FreeArmor", Version = "1.0.0", Name = "FreeArmor", Author = "aga", Description = "Gives free armor on full buy rounds")]
public partial class FreeArmor : BasePlugin {
  private int _roundInCurrentHalf = 0;
  private IOptionsMonitor<ConfigModel>? _config;
  private ServiceProvider? _serviceProvider;
  private IConVar<bool>? _enabledCvar;

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

    ResetHalfRoundTracking();

    Core.Event.OnMapLoad += _ => ResetHalfRoundTracking();

    Core.GameEvent.HookPre<EventRoundStart>(_ => {
      _roundInCurrentHalf++;
      return HookResult.Continue;
    });

    Core.GameEvent.HookPre<EventStartHalftime>(_ => {
      ResetHalfRoundTracking();
      return HookResult.Continue;
    });

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

      var pawn = @event.UserIdPawn;
      if (pawn == null)
        return HookResult.Continue;

      var isPistolRound = _roundInCurrentHalf <= 1;

      var armorValue = isPistolRound ? 0 : 100;
      var hasHelmet = !isPistolRound;

      pawn.ArmorValue = armorValue;
      pawn.ArmorValueUpdated();

      var itemServices = pawn.ItemServices;
      if (itemServices != null)
      {
        itemServices.HasHelmet = hasHelmet;
        itemServices.HasHelmetUpdated();
      }

      return HookResult.Continue;
    });
  }

  public override void Unload() {
    _serviceProvider?.Dispose();
    _serviceProvider = null;
    _config = null;
    _enabledCvar = null;
  }

  private void ResetHalfRoundTracking()
  {
    _roundInCurrentHalf = 0;
  }
}