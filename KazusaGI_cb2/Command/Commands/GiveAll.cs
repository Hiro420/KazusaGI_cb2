using KazusaGI_cb2.GameServer;
using static KazusaGI_cb2.Command.CommandManager;

namespace KazusaGI_cb2.Command.Commands;

public class GiveAll
{
	[Command("giveall")]
	public class GiveAllCommand
	{
		public static void Execute(string[] args, Session? session)
		{
			if (session == null)
			{
				logger.LogError($"Please target a session first");
				return;
			}

			if (args.Length == 0)
			{
				logger.LogError("Usage: giveall <avatars|materials|weapons>");
				return;
			}

			string target = args[0].ToLower();

			switch (target)
			{
				case "avatars":
					uint level = 1;
					if (args.Length > 1 && args[1].StartsWith("lv"))
						level = UInt32.Parse(args[1].Substring(2));
					if (level > 90 || level < 1)
					{
						logger.LogError("Level must be between 1 and 90");
						return;
					}
					session.player.AddAllAvatars(level);
					break;
				case "materials":
					if (args.Length > 1 && args[1].ToLower() == "silent")
						session.player.AddAllMaterials(isSilent: true);
					else
						session.player.AddAllMaterials();
					break;
				case "weapons":
					session.player.AddAllWeapons();
					break;
				default:
					logger.LogError("Unknown argument. Usage: giveall <avatars|materials|weapons>");
					break;
			}

			session.player.SavePersistent();
			logger.LogSuccess($"Gave all {target} to player {session.player.Uid}");
		}
	}
}
