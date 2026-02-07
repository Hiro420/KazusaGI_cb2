using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Command;

public class KazusaConsoleServer
{
    public static void StartLoop()
    {
        Logger logger = new("KazusaConsoleServer");
        CommandManager commandManager = new();

        while (true)
        {
            logger.LogWarning("Waiting for client connection...");
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
				using (var pipeServer = new NamedPipeServerStream("KazusaGI", PipeDirection.InOut, 1, PipeTransmissionMode.Message))
                {
                    pipeServer.WaitForConnection();
                    logger.LogSuccess("Client connected.");

                    using (StreamReader reader = new StreamReader(pipeServer))
                    using (StreamWriter writer = new StreamWriter(pipeServer) { AutoFlush = true })
                    {
                        writer.WriteLine("Connected to KazusaGI.");

                        string? clientMessage;
                        while ((clientMessage = reader.ReadLine()) != null)
                        {
                            logger.LogSuccess($"Recieved command: {clientMessage}");
                            commandManager.ProcessCommand(clientMessage);
                            writer.WriteLine("Command recieved.");
                        }
                    }
                }
#pragma warning restore CA1416 // Validate platform compatibility
			}
            catch (IOException)
            {
                logger.LogError("Client disconnected. Waiting for a new connection...");
            }
        }
    }
}