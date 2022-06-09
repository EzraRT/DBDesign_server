using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Net;

namespace DBDesign // Note: actual namespace depends on the project name.
{

    using Model;
    internal class Program
    {
        async static Task Main(string[] args)
        {
            Test.TestQuery();
            await MainLoop();
        }

        async static Task MainLoop()
        {
            DBDesignEF ef = new DBDesignEF();

            TcpListener listener = new TcpListener(IPAddress.Any, 9090);
            listener.Start();
            while (true)
            {
                Socket client = await listener.AcceptSocketAsync();
                Session session = new Session(client, ef);

                Console.WriteLine(client.RemoteEndPoint + " connected.");
                session.Start();
            }
        }
    }
}
