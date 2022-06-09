using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBDesign
{
    using Model;
    using Type;
    class Session
    {
        int UserID;
        Socket sClient;
        DBDesignEF entitys;
        async Task<int> Response(int sessionId, string data)
        {
            Response response = new Response();
            response.SessionId = sessionId;
            response.Data = data;
            string responseText = JsonConvert.SerializeObject(response);
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            return await sClient.SendAsync(buffer, SocketFlags.None);
        }

        async void NewMessage(int sessionId, int messageId)
        {
            List<int> sessionList = entitys.Friends.Where(f => f.User1.Id == this.UserID).Select(f => f.SessionId).ToList();

            var messages = entitys.UserMessages.Where(m => m.Id > messageId && sessionList.Contains(m.SessionID))
            .Include(m => m.Sender).Select(m => new { Id = m.Id, SenderName = m.Sender.Name, SenderId = m.Sender.Id, Content = m.Content, Time = m.SendTime }).ToList();
            if (messages != null)
            {
                await Response(sessionId, JsonConvert.SerializeObject(messages));
            }
            else
            {
                await Response(sessionId, null);
            }
        }

        async void FriendList(int SessionId)
        {
            await Response(SessionId, JsonConvert.SerializeObject(entitys.Friends.Where(f => f.User1.Id == this.UserID).Select(f => new { f.User2.Id, f.User2.Name, f.User2.State, f.SessionId }).ToList()));
        }

        bool Login(int Id, string Password)
        {
            User user = entitys.Users.FirstOrDefault(u => u.Id == Id);
            if (user.Password == Password)
            {
                UserID = Id;
                user.State = 1;
                entitys.SaveChanges();
                return true;
            }
            return false;
        }

        public async void Start()
        {
            byte[] buffer = new byte[1024];
            int length;
            string message;
            while (true)
            {
                try
                {
                    length = await sClient.ReceiveAsync(buffer, SocketFlags.None);
                    if (length == 0)
                        break;
                    message = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
                }
                catch (SocketException)
                {
                    break;
                }
                Request? request;
                Console.WriteLine(message);
                try
                {
                    request = JsonConvert.DeserializeObject<Request>(message);
                }
                catch (JsonException)
                {
                    Console.WriteLine("Invalid JSON");
                    await Response(0, "Error: Invalid JSON");
                    continue;
                }
                switch (request.Action)
                {
                    case "Login":
                        JObject obj = JObject.Parse(request.Data);
                        int Id = obj.Value<int>("Id");
                        if (Login(Id, obj.Value<string>("Password")))
                        {
                            this.UserID = Id;
                            await Response(request.SessionId, "Login Success");
                            Console.WriteLine(sClient.RemoteEndPoint + " Login Success");
                        }
                        else
                        {
                            await Response(request.SessionId, "Login Failed");
                            Console.WriteLine(sClient.RemoteEndPoint + " Login Failed");
                        }
                        break;
                    case "FriendList":
                        FriendList(request.SessionId);
                        break;
                    case "NewMessage":
                        int messageId = JsonConvert.DeserializeObject<int>(request.Data);
                        NewMessage(request.SessionId, messageId);
                        break;

                    default:
                        break;
                }
            }
            if (UserID != 0)
            {
                entitys.Users.FirstOrDefault(u => u.Id == UserID).State = 0;
                entitys.SaveChanges();
            }
            Console.WriteLine(sClient.RemoteEndPoint + " disconnected.");
            sClient.Close();
        }

        public Session(Socket socket, DBDesignEF ef)
        {
            this.sClient = socket;
            this.entitys = ef;
        }
    }
}
