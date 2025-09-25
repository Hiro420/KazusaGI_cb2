using KazusaGI_cb2.GameServer;

namespace KazusaGI_cb2.GameServer;

/// <summary>
/// Represents the game world containing scenes and global state
/// </summary>
public class World
{
    public Scene? Scene { get; set; }
    public Player? Host { get; set; }
    
    public World(Scene scene, Player host)
    {
        Scene = scene;
        Host = host;
    }
}