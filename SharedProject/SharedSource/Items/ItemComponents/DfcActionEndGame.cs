using System.Diagnostics;
using Barotrauma;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;

namespace DSSIFactionCraft.Items.Components
{
    internal class DfcActionEndGame : ItemComponent
    {
        public DfcActionEndGame(Item item, ContentXElement element) : base(item, element) { }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            if (GameMain.NetworkMember?.IsClient ?? false) { return; }
            switch (connection.Name)
            {
                case "signal_in":
                    GameMain.LuaCs.Timer.Wait(EndGame, 0);
                    static void EndGame(params object[] args) =>
#if SERVER
                    GameMain.Server.EndGame();
#else
                    GameMain.GameSession?.EndRound("");
#endif
                    break;
                default:
                    break;
            }
        }
    }
}