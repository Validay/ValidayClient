using System.Text;
using ValidayClient.Managers;
using ValidayClient.Network;

namespace ValidayClientSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();

            client.RegistrationManager<CommandHandlerManager>();

            client.Connect();

            while (client.IsRun)
            {
                string? message = Console.ReadLine();

                if (message != null)
                {
                    short id = 123;
                    byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                    List<byte> bytes = BitConverter.GetBytes(id)
                        .ToList();

                    bytes.AddRange(messageBytes);

                    client.SendToServer(bytes.ToArray());
                }
            }
        }
    }
}