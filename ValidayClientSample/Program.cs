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
            CommandHandlerManager commandHandler = new CommandHandlerManager();

            client.RegistrationManager(commandHandler);

            client.Connect();

            while (client.IsRun)
            { };
        }
    }
}