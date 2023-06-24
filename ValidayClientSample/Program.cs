using ValidayClient.Logging;
using ValidayClient.Logging.Interfaces;
using ValidayClient.Managers;
using ValidayClient.Network;
using ValidayClient.Network.Interfaces;

namespace ValidayClientSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IClient client = new Client();
            ILogger logger = new ConsoleLogger(LogType.Info);

            CommandHandlerManager commandHandler = new CommandHandlerManager(
                client,
                logger);

            client.Connect();

            while (client.IsRun)
            { };
        }
    }
}