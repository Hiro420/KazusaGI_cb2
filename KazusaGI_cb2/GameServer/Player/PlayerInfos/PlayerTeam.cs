using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.PlayerInfos;

public class PlayerTeam
{
    public TeamEntity? teamEntity;
    //public uint entityId;
    // private static ResourceManager resourceManager = MainApp.resourceManager;
    public PlayerAvatar? Leader { get; set; }
    public List<PlayerAvatar> Avatars { get; set; }
    // add affixes later TODO

    public PlayerTeam(Session session, PlayerAvatar leader) // in case it's only 1 character
        : this(session, new List<PlayerAvatar> { leader }, leader) { }

    public PlayerTeam(Session session, List<PlayerAvatar> avatars, PlayerAvatar leader)
    {
        this.Avatars = avatars;
        this.Leader = leader;
        this.teamEntity = new TeamEntity(session);
        //this.entityId = session.GetEntityId(ProtEntityType.ProtEntityTeam);
    }

    public PlayerTeam(Session session) 
    {
        this.Avatars = new();
        this.teamEntity = new TeamEntity(session);
		//this.entityId = session.GetEntityId(ProtEntityType.ProtEntityTeam);
	}

    public void RemoveAvatar(Session session, PlayerAvatar avatar)
    {
        if (this.Avatars.Count == 1)
        {
            session.c.LogError("Cannot remove the last avatar from the team");
            return;
        }
        this.Avatars.Remove(avatar);
        if (this.Leader == avatar)
        {
            this.Leader = this.Avatars[0];
        }
    }

    public void AddAvatar(Session session, PlayerAvatar avatar)
    {
        if (this.Avatars.Count == 4)
        {
            session.c.LogError("Cannot add more than 4 characters to the team"); // should never happen
            return;
        }
        this.Avatars.Add(avatar);
    }
}
