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
        // Mirror hk4e: validate enter_scene_token for PostEnterSceneReq.
        if (req.EnterSceneToken != session.player!.EnterSceneToken)
        {
            session.SendPacket(new PostEnterSceneRsp
            {
                Retcode = (int)Retcode.RetEnterSceneTokenInvalid
            });
            return;
        }

        session.SendPacket(new PostEnterSceneRsp { Retcode = 0 });
    }
}