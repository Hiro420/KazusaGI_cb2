using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEnterSceneReadyReq
{
    [Packet.PacketCmdId(PacketId.EnterSceneReadyReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        EnterSceneReadyReq req = packet.GetDecodedBody<EnterSceneReadyReq>();
        EnterScenePeerNotify rsp = new EnterScenePeerNotify()
        {
            PeerId = session.player!.PeerId,
            HostPeerId = session.player!.PeerId,
            DestSceneId = session.player!.SceneId
        };

        session.SendPacket(rsp);
        session.SendPacket(new EnterSceneReadyRsp());
    }
}