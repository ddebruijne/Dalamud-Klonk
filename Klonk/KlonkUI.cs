using ImGuiNET;
using System;
using System.Numerics;

namespace Klonk
{
    
    // It is good to have this be disposable in general, in case you ever need it to do any cleanup
    class KlonkUI : IDisposable
    {
        private Configuration configuration;

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }
        
        public KlonkUI(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Dispose()
        {
            
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.
            DrawSettingsWindow();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(400, 200), ImGuiCond.Always);
            if (ImGui.Begin("Klonk Config", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                string comport = this.configuration.SerialPort;
                if (ImGui.InputTextWithHint("Serial Port", "COM1", ref comport, 5))
                {
                    this.configuration.SerialPort = comport;
                    this.configuration.Save();
                }
            }
            ImGui.End();
        }
    }
}
