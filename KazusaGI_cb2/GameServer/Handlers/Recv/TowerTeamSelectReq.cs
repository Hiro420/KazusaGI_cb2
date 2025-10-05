using KazusaGI_cb2.GameServer.Tower;
using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleTowerTeamSelectReq
{
    [Packet.PacketCmdId(PacketId.TowerTeamSelectReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        TowerTeamSelectReq towerTeamSelectReq = packet.GetDecodedBody<TowerTeamSelectReq>();
        session.player!.towerInstance = new TowerInstance(session, session.player);
        session.player!.towerInstance.HandleTowerTeamSelectReq(packet);
    }
}