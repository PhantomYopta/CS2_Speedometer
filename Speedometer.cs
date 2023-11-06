using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Speedometer;

public class Speedometer : BasePlugin
{
    public override string ModuleName => "Speedometer by phantom";
    public override string ModuleVersion => "1.0.0";

    public UsersSettings?[] _usersSettings = new UsersSettings?[65];

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnClientConnected>(((slot) =>
        {
            _usersSettings[slot + 1] = new UsersSettings { IsShowSpeed = true, CountJumps = 0 };
        }));
        RegisterListener<Listeners.OnClientDisconnectPost>(slot => _usersSettings[slot + 1] = null);
        RegisterListener<Listeners.OnTick>(() =>
        {
            for (var i = 1; i <= Server.MaxPlayers; ++i)
            {
                var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(i));

                if (player is { IsValid: true, IsBot: false, PawnIsAlive: true })
                {
                    var buttons = player.Pawn.Value.MovementServices!.ButtonState.Value;
                    var client = player.EntityIndex!.Value.Value;
                    if(client == IntPtr.Zero) return;
                    if(!_usersSettings[client]!.IsShowSpeed) return;
                    var gamerules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;
                    if ((buttons & PlayerButtons.Jump) != 0 && player.PlayerPawn.Value.GroundEntity.IsValid && !gamerules.FreezePeriod)
                        _usersSettings[client]!.CountJumps++;
                    player.PrintToCenter(
                        $"{Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D())}\n" +
                        $"Jumps: {_usersSettings[client]!.CountJumps}\n" +
                        $"{((buttons & PlayerButtons.Left) != 0 ? "←" : "_")} " +
                        $"{((buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                        $"{((buttons & PlayerButtons.Right) != 0 ? "→" : "_")}\n" +
                        $"{((buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                        $"{((buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                        $"{((buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} ");
                }
            }
        });
        RegisterEventHandler<EventPlayerDeath>(((@event, info) =>
        {
            if (@event.Userid.Handle == IntPtr.Zero || @event.Userid.UserId == null) return HookResult.Continue;

            var controller = @event.Userid;
            var client = controller.EntityIndex!.Value.Value;
            if (client == IntPtr.Zero) return HookResult.Continue;
            _usersSettings[client]!.CountJumps = 0;
            
            return HookResult.Continue;
        }));
        AddCommand("css_speed", "", ((player, info) =>
        {
            if (player == null) return;
            var client = player.EntityIndex!.Value.Value;
            _usersSettings[client]!.IsShowSpeed = !_usersSettings[client]!.IsShowSpeed;
            player.PrintToChat(_usersSettings[client]!.IsShowSpeed ? "Speedometer: \x06On" : "Speedometer: \x02Off");
        }));
    }
}

public class UsersSettings
{
    public int CountJumps { get; set; }
    public bool IsShowSpeed { get; set; }
}