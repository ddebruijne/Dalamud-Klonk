using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Klonk
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public string SerialPort { get; set; } = "COM6";
        public bool SupportsText { get; set; } = true;
        public int AmountTubes { get; set; } = 8;

        /* Config ideas:
         * - In-Combat: Player resource + HP%, Player HP, Enemy HP, enemy casting warning (with blink)
         * - Out of combat: HP / player target / Eorzea Time / Server Time / Local Time
         */

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
