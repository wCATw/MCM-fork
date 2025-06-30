using System;
using System.Linq;
using Barotrauma;
using Barotrauma.Networking;

namespace MultiplayerCrewManager
{
    partial class McmMod
    {
        private static McmMod _instance;
        public static McmMod Instance => _instance ?? (_instance = new McmMod());

        public McmClientManager Manager { get; private set; }
        public McmControl Control { get; private set; }

        public void InitServer()
        {
            LuaCsSetup.PrintCsMessage("[MCM-SERVER] Initializing...");

            Manager = new McmClientManager();
            Control = new McmControl();

            // networking
            GameMain.LuaCs.Networking.Receive("server-mcm", (object[] args) =>
            {
                (IReadMessage msg, Client client) = (args[0] as IReadMessage, args[1] as Client);
                Int32.TryParse(msg.ReadString(), out int characterID);
                Control.TryGiveControl(client, characterID);
            });

            LuaCsSetup.PrintCsMessage("[MCM-SERVER] Initialization complete");
        }
    }
}
