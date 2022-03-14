using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using System;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using System.Net.Sockets;
using System.Text;

namespace Klonk
{
    public class Klonk : IDalamudPlugin
    {
        public string Name => "Klonk";
        private const string CommandName = "/klonk";

        private Configuration Configuration { get; init; }
        private KlonkUI UI { get; init; }
        private UdpClient udpClient;

        private bool isConnected = false;
        private string lastKnownClockString = string.Empty;
        private string clockString = string.Empty;
        private double timeSinceLastKeepalive = 0;
        private double timeSinceLastSendString = 0;

        public Klonk([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            DalamudContainer.Initialize(pluginInterface);
            udpClient = new UdpClient();

            this.Configuration = DalamudContainer.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(DalamudContainer.PluginInterface);
            
            this.UI = new KlonkUI(this.Configuration);
            
            DalamudContainer.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Toggles klonk activity"
            });

            DalamudContainer.PluginInterface.UiBuilder.Draw += DrawUI;
            DalamudContainer.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            DalamudContainer.Framework.Update += OnUpdate;
            DalamudContainer.ClientState.Login += OnLogin;
            DalamudContainer.ClientState.Logout += OnLogout;

            if (DalamudContainer.ClientState.IsLoggedIn)
                ActivateKlonk();
        }

        public void Dispose()
        {
            this.UI.Dispose();
            DeactivateKlonk();
            DalamudContainer.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            if (!isConnected)
                ActivateKlonk();
            else
                DeactivateKlonk();
        }

        private string GetPaddedHP(PlayerCharacter p)
        {
            return p.CurrentHp.ToString().PadLeft(Configuration.AmountTubes, '0');
        }

        private unsafe void OnUpdate(Framework framework)
        {
            if (!isConnected)
                return;

            if (timeSinceLastKeepalive >= 1000)
            {
                Byte[] hello = Encoding.ASCII.GetBytes("KEEPALIVE|FinalFantasyXIV");
                try
                {
                    udpClient.Send(hello, hello.Length, "127.0.0.1", 11001);
                }
                catch (Exception e)
                {
                    DalamudContainer.ChatGui.PrintError(e.ToString());
                }

                timeSinceLastKeepalive = 0;
            }
            timeSinceLastKeepalive += framework.UpdateDelta.TotalMilliseconds;

            // Separate events based on whether we're in combat or not.
            PlayerCharacter localPlayer = DalamudContainer.ClientState.LocalPlayer;
            GameObject currentTarget = DalamudContainer.TargetManager.FocusTarget != null ? DalamudContainer.TargetManager.FocusTarget : DalamudContainer.TargetManager.Target;
            if ((localPlayer.StatusFlags & StatusFlags.InCombat) != 0)
            {
                if (localPlayer.TargetObject != null && currentTarget.ObjectKind == ObjectKind.BattleNpc)
                {
                    BattleChara bc = (BattleChara)currentTarget;
                    if(bc == null)
                    {
                        DalamudContainer.ChatGui.PrintError("OnUpdate: Could not cast currentTarget to BattleChara!");
                        DeactivateKlonk();
                    }

                    if (bc.IsCasting)
                        clockString = (bc.TotalCastTime - bc.CurrentCastTime).ToString("0.00");
                    else
                        clockString = GetPaddedHP(localPlayer);
                }
                else
                    clockString = GetPaddedHP(localPlayer);
            }
            else
            {
                if (Configuration.SupportsText && currentTarget != null)
                    clockString = currentTarget.Name.TextValue;
                else
                    clockString = GetPaddedHP(localPlayer);
            }

            // To not spam the device, only send an update when the text differs.
            if (!clockString.Equals(lastKnownClockString) || timeSinceLastSendString >= 1000)
            {
                lastKnownClockString = clockString;

                String s = "SENDDATA|FinalFantasyXIV|" + clockString;
                Byte[] senddata = Encoding.ASCII.GetBytes(s);
                try
                {
                    udpClient.Send(senddata, senddata.Length, "127.0.0.1", 11001);
                    Console.WriteLine(s);
                }
                catch (Exception e)
                {
                    DalamudContainer.ChatGui.PrintError(e.ToString());
                }

                timeSinceLastSendString = 0;
            }
            timeSinceLastSendString += framework.UpdateDelta.TotalMilliseconds;
        }

        private unsafe void OnLogin(object sender, System.EventArgs e)
        {
            ActivateKlonk();
        }

        private unsafe void OnLogout(object sender, System.EventArgs e)
        {
            DeactivateKlonk();
        }

        private void DrawUI()
        {
            this.UI.Draw();
        }

        private void DrawConfigUI()
        {
            this.UI.SettingsVisible = true;
        }

        private void ActivateKlonk()
        {
            if (isConnected)
                return;

            Byte[] hello = Encoding.ASCII.GetBytes("HELLO|FinalFantasyXIV");
            try
            {
                udpClient.Send(hello, hello.Length, "127.0.0.1", 11001);
            }
            catch (Exception e)
            {
                DalamudContainer.ChatGui.PrintError(e.ToString());
            }

            isConnected = true;
        }

        private void DeactivateKlonk()
        {
            if (!isConnected)
                return;

            DalamudContainer.ChatGui.Print("Disconnected from klonk.");
            Byte[] hello = Encoding.ASCII.GetBytes("GOODBYE|FinalFantasyXIV");
            try
            {
                udpClient.Send(hello, hello.Length, "127.0.0.1", 11001);
            }
            catch (Exception e)
            {
                DalamudContainer.ChatGui.PrintError(e.ToString());
            }

            lastKnownClockString = string.Empty;
            clockString = string.Empty;
            isConnected = false;
        }
    }
}
