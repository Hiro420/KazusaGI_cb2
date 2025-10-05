using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleExecuteGadgetLuaReq
{
    [Packet.PacketCmdId(PacketId.ExecuteGadgetLuaReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        ExecuteGadgetLuaReq req = packet.GetDecodedBody<ExecuteGadgetLuaReq>();
        // todo: handle
        GameServer.Entity? targetEntity = session.player!.Scene.FindEntityByEntityId(req.SourceEntityId);
        if (targetEntity == null || targetEntity is not GameServer.GadgetEntity)
        {
            session.c.LogWarning($"[FUCKED EXECUTE GADGET LUA] Entity {req.SourceEntityId} not found for ExecuteGadgetLuaReq, or is not a gadget");
            return;
        }
        GadgetEntity gadgetEntity = (GameServer.GadgetEntity)targetEntity;
        Retcode ret = gadgetEntity.onClientExecuteRequest(req.Param1, req.Param2, req.Param3);
        session.SendPacket(new ExecuteGadgetLuaRsp() { Retcode = (int)ret });
    }
}