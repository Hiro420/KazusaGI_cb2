using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandlePostEnterSceneReq
{
    [Packet.PacketCmdId(PacketId.PostEnterSceneReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        PostEnterSceneReq req = packet.GetDecodedBody<PostEnterSceneReq>();
        session.SendPacket(new PostEnterSceneRsp());

        foreach (var entity in session.entityMap.Values)
        {
            if (entity.abilityManager != null)
                entity.abilityManager.IsInited = true;
        }
    }
}