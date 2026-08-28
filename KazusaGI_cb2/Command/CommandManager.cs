using KazusaGI_cb2.GameServer;
using System.Reflection;
using System.Text;

namespace KazusaGI_cb2.Command;

public class CommandManager
{
	public static Logger logger = new("CommandManager");
	public Session? targetSession = null;

	[AttributeUsage(AttributeTargets.Class)]
	public class CommandAttribute : Attribute
	{
		public string Name { get; }
		public CommandAttribute(string name) => Name = name;
	}

	public CommandManager()
	{
	}

	public void ProcessCommand(string CommandStr)
	{
		var args = ParseArguments(CommandStr);
		if (args.Length == 0)
			return;
		string commandName = args[0];
		var commandArgs = args.Skip(1).ToArray();
		ExecuteCommand(commandName, commandArgs);
	}

	private void ExecuteCommand(string commandName, params object[] args)
	{
		if (commandName == "target" && args.Length > 0)
		{
			Session? newTargetSession = GameServerManager.sessions.FirstOrDefault(s => s._peer.ToString() == args[0].ToString());
			if (newTargetSession == null)
			{
				logger.LogWarning($"No session found for peer {args[0]}");
			}
			else
			{
				this.targetSession = newTargetSession;
				logger.LogSuccess($"Set target session to {args[0]}");
			}
			return;
		}
		else if (args.Length == 0 && GameServerManager.sessions.Count == 1)
		{
			this.targetSession = GameServerManager.sessions.First();
			logger.LogSuccess($"Set target session to {this.targetSession._peer}");
			return;
		}

		if (commandName == "sessions")
		{
			string sessionList = String.Join(", ", GameServerManager.sessions.Select(s => s._peer.ToString()));
			logger.LogWarning($"Sessions: {sessionList}");
			return;
		}


		var assembly = Assembly.GetExecutingAssembly();
		var commandTypes = assembly.GetTypes()
			.Where(t => t.Namespace == "KazusaGI_cb2.Command.Commands" &&
						t.GetCustomAttribute<CommandAttribute>() != null);

		List<string> availableCommandNames = new List<string>() { "sessions", "target" };

		foreach (var type in commandTypes)
		{
			var attribute = type.GetCustomAttribute<CommandAttribute>();
			availableCommandNames.Add(attribute!.Name);
			if (attribute.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase))
			{
				var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
				if (method != null)
				{
					var parameters = new object[] { args, targetSession! };
					method.Invoke(null, parameters);
					return;
				}
				else
				{
					logger.LogError($"Error: Class {type.Name} does not contain a public static Execute method.");
					return;
				}
			}
		}


		logger.LogWarning($"Unknown command {commandName}. Available commands: {String.Join(", ", availableCommandNames)}");
	}

	private string[] ParseArguments(string input)
	{
		var args = new List<string>();
		var currentArg = new StringBuilder();
		bool inQuotes = false;

		foreach (char c in input)
		{
			if (c == '\"')
			{
				inQuotes = !inQuotes;
				continue;
			}

			if (c == ' ' && !inQuotes)
			{
				if (currentArg.Length > 0)
				{
					args.Add(currentArg.ToString());
					currentArg.Clear();
				}
			}
			else
			{
				currentArg.Append(c);
			}
		}

		if (currentArg.Length > 0)
			args.Add(currentArg.ToString());

		return args.ToArray();
	}
}