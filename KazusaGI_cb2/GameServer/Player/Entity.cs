using KazusaGI_cb2.Resource.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer;

public class Entity
{
    public uint _EntityId { get; set; }
    public Vector3 Position { get; set; }
    public Session session { get; set; }

    public SceneGroupLua? GetEntityGroup(uint groupId)
    {
        return session.player!.Scene.GetGroup((int)groupId);
    }

    public Entity(Session session, Vector3? position)
    {
        this.Position = position ?? session.player!.Pos;
        this.session = session;
    }
}
