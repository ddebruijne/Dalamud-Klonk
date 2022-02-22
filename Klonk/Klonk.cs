using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using System;
using System.IO;
using Dalamud.Game;
using RJCP.IO.Ports;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Klonk
{
    public class Klonk : IDalamudPlugin
    {
        public string Name => "Klonk";
        private const string CommandName = "/klonk";

        private Configuration Configuration { get; init; }
        private KlonkUI UI { get; init; }
        private SerialPortStream port;
        private bool isConnected = false;
        private string lastKnownClockString = string.Empty;
        private string clockString = string.Empty;

        public Klonk([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            DalamudContainer.Initialize(pluginInterface);

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
            if (!clockString.Equals(lastKnownClockString))
            {
                lastKnownClockString = clockString;
                port.WriteLine(clockString);
            }
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

            port = new SerialPortStream(this.Configuration.SerialPort, 115200);
            try
            {
                port.Open();
                port.WriteLine("Hello from FFXIV");
                DalamudContainer.ChatGui.Print("Successfully connected to klonk.");
                isConnected = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                DalamudContainer.ChatGui.PrintError("Port " + port.ToString() + " is in use.");
                isConnected = false;
            }
            catch (IOException ex)
            {
                DalamudContainer.ChatGui.PrintError("Port " + port.ToString() + " does not exist.");
                isConnected = false;
            }
            catch (Exception ex)
            {
                DalamudContainer.ChatGui.PrintError("uart exception " + ex.ToString());
                isConnected = false;
            }
        }

        private void DeactivateKlonk()
        {
            if (!isConnected)
                return;

            DalamudContainer.ChatGui.Print("Disconnected from klonk.");
            string exit = string.Empty.PadLeft(Configuration.AmountTubes, '.');
            port.WriteLine(exit);
            port.Close();
            port.Dispose();
            port = null;
            isConnected = false;
        }
    }
}
