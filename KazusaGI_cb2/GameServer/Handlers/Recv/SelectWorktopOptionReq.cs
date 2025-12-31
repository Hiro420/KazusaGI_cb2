using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.GameServer.Lua;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSelectWorktopOptionReq
{
    [Packet.PacketCmdId(PacketId.SelectWorktopOptionReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        var req = packet.GetDecodedBody<SelectWorktopOptionReq>();

        var rsp = new SelectWorktopOptionRsp
        {
            Retcode = (int)Retcode.RetSucc,
            GadgetEntityId = req.GadgetEntityId,
            OptionId = req.OptionId
        };

        var player = session.player;
        if (player == null || player.Scene == null)
        {
            rsp.Retcode = (int)Retcode.RetFail;
            session.SendPacket(rsp);
            return;
        }

        if (!player.Scene.EntityManager.TryGet(req.GadgetEntityId, out var entity) || entity is not GadgetEntity gadget)
        {
            rsp.Retcode = (int)Retcode.RetEntityNotExist;
            session.SendPacket(rsp);
            return;
        }

        uint optionId = req.OptionId;
        if (!gadget.WorktopOptions.Contains(optionId))
        {
            rsp.Retcode = (int)Retcode.RetWorktopOptionNotExist;
            session.SendPacket(rsp);
            return;
        }

        session.SendPacket(rsp);

        if (gadget._gadgetLua == null)
            return;

        int groupId = (int)gadget._gadgetLua.group_id;
        var group = player.Scene.GetGroup(groupId);
        if (group == null)
            return;

        var args = new ScriptArgs(groupId,
            (int)EventType.EVENT_SELECT_OPTION,
            (int)gadget._gadgetLua.config_id,
            (int)optionId)
        {
            source_eid = (int)gadget._EntityId
        };

        LuaManager.executeTriggersLua(session, group, args);
    }
}
