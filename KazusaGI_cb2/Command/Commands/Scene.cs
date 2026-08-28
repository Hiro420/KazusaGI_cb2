using KazusaGI_cb2.GameServer;
using static KazusaGI_cb2.Command.CommandManager;

namespace KazusaGI_cb2.Command.Commands;

public class Scene
{
	[Command("scene")]
	public class SceneCommand
	{
		public static void Execute(string[] args, Session? session)
		{
			if (session == null)
			{
				logger.LogError($"Please target a session first");
				return;
			}

			if (args.Length == 0 || !uint.TryParse(args[0], out uint targetSceneId))
			{
				logger.LogError($"Please enter a valid scene id");
				return;
			}

			session.player!.EnterScene(session, targetSceneId);
		}
	}
}
