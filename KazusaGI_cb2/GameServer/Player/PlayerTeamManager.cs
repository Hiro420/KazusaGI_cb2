using KazusaGI_cb2.GameServer.PlayerInfos;

namespace KazusaGI_cb2.GameServer;

/// <summary>
/// Team management functionality for Player
/// </summary>
public class PlayerTeamManager
{
    private readonly Player player;
    
    public PlayerTeamManager(Player player)
    {
        this.player = player;
    }
    
    /// <summary>
    /// Get the current avatar entity (leader of current team)
    /// </summary>
    /// <returns>The AvatarEntity for the current avatar, or null if not found</returns>
    public AvatarEntity? GetCurrentAvatarEntity()
    {
        var currentTeam = player.GetCurrentLineup();
        if (currentTeam?.Leader == null) return null;
        
        return player.FindEntityByPlayerAvatar(player.Session, currentTeam.Leader);
    }
}