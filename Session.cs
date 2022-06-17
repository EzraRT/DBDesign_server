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
        async Task<int> Response(int sessionId, string data)
        {
            Response response = new Response();
            response.SessionId = sessionId;
            response.Data = data;
            string responseText = JsonConvert.SerializeObject(response);
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            try
            {
                return await sClient.SendAsync(buffer, SocketFlags.None);
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        async Task SendMessage(int sessionId, int talkSessionId, string content)
        {
            UserMessage message = new UserMessage();
            message.SenderId = UserID;
            message.SessionID = talkSessionId;
            message.Content = content;
            message.SendTime = DateTime.Now;

            DBDesignEF entitys = new DBDesignEF();

            await entitys.UserMessages.AddAsync(message);
            await entitys.SaveChangesAsync();

            await Response(sessionId, JsonConvert.SerializeObject("Send Success"));
        }

        async Task DeleteFriend(int sessionId, int friendId)
        {
            DBDesignEF entitys = new DBDesignEF();

            var list = entitys.Friends.Where(f => f.User1Id == UserID && f.User2Id == friendId).ToList();
            list.AddRange(entitys.Friends.Where(f => f.User1Id == friendId && f.User2Id == UserID).ToList());

            if (list.Count == 0)
            {
                await Response(sessionId, JsonConvert.SerializeObject("No Friend"));
                return;
            }

            entitys.Friends.RemoveRange(list);
            await entitys.SaveChangesAsync();

            await Response(sessionId, "Success");
        }
        async Task NewFriend(int sessionId, int friendId)
        {
            if (UserID == friendId)
            {
                await Response(sessionId, "You can't add yourself as a friend");
                return;
            }

            Friend friend = new Friend();

            DBDesignEF entitys = new DBDesignEF();
            User me = await entitys.Users.FindAsync(UserID);
            User newFriend = await entitys.Users.FindAsync(friendId);

            if (me == null || newFriend == null)
            {
                await Response(sessionId, "Error: User not found");
                return;
            }

            if (entitys.Friends.Where(f => f.User1Id == UserID && f.User2Id == friendId).ToList().Count > 0
            || entitys.Friends.Where(f => f.User1Id == friendId && f.User2Id == UserID).ToList().Count > 0)
            {
                await Response(sessionId, "Error: You have already added this user as a friend");
                return;
            }

            friend.User1 = me;
            friend.User2 = newFriend;
            await entitys.Friends.AddAsync(friend);
            await entitys.SaveChangesAsync();

            await Response(sessionId, "Success");
        }

        async Task NewMessage(int sessionId, int messageId)
        {
            DBDesignEF entitys = new DBDesignEF();
            List<int> sessionList = entitys.Friends.Where(f => f.User1Id == UserID || f.User2Id == UserID).Select(f => f.SessionId).ToList();
            var messages = await entitys.UserMessages.Where(m => m.Id > messageId && sessionList.Contains(m.SessionID))
            .Include(m => m.Sender).Select(m => new { Id = m.Id, SessionId = m.SessionID, SenderName = m.Sender.Name, SenderId = m.Sender.Id, Content = m.Content, Time = m.SendTime }).ToListAsync();

            if (messages != null)
            {
                await Response(sessionId, JsonConvert.SerializeObject(messages));
            }
            else
            {
                await Response(sessionId, "");
            }
        }

        async Task FriendList(int SessionId)
        {
            DBDesignEF entitys = new DBDesignEF();
            var friends = await entitys.Friends.Where(f => f.User1Id == UserID).Select(f => new { f.User2.Id, f.User2.Name, f.User2.State, f.SessionId }).ToListAsync();
            friends.AddRange(await entitys.Friends.Where(f => f.User2Id == UserID).Select(f => new { f.User1.Id, f.User1.Name, f.User1.State, f.SessionId }).ToListAsync());
            friends.RemoveAll(f => f.Id == UserID);

            await Response(SessionId, JsonConvert.SerializeObject(friends));
        }
        async Task ChangeName(int SessionId, string newName)
        {
            DBDesignEF entitys = new DBDesignEF();
            User user = await entitys.Users.FindAsync(UserID);
            if (user == null)
            {
                await Response(SessionId, "Error: User not found");
                return;
            }
            user.Name = newName;
            await entitys.SaveChangesAsync();
            await Response(SessionId, user.Name);
        }

        async Task Register(int sessionId, string name, string password)
        {
            DBDesignEF entitys = new DBDesignEF();

            Console.WriteLine(password);

            User user = new User();
            user.Name = name;
            user.Password = password;
            user.State = 0;
            await entitys.Users.AddAsync(user);
            await entitys.SaveChangesAsync();

            await Response(sessionId, JsonConvert.SerializeObject(user.Id));
        }

        async Task<bool> Login(int Id, string Password)
        {
            DBDesignEF entitys = new DBDesignEF();
            User user = await entitys.Users.FindAsync(Id);
            if (user == null)
            {
                return false;
            }
            if (BCrypt.Net.BCrypt.Verify(Password, user.Password))
            {
                UserID = Id;
                user.State = 1;
                await entitys.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task Start()
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
                        try
                        {
                            JObject loginObj = JObject.Parse(request.Data);
                            int Id = loginObj.Value<int>("Id");
                            string Password = loginObj.Value<string>("Password");

                            if (await Login(Id, Password))
                            {
                                this.UserID = Id;
                                var entitys = new DBDesignEF();
                                await Response(request.SessionId, entitys.Users.Find(Id).Name);
                                Console.WriteLine(sClient.RemoteEndPoint + " Login Success as " + Id);
                            }
                            else
                            {
                                await Response(request.SessionId, "Login Failed");
                                Console.WriteLine(sClient.RemoteEndPoint + " Login Failed");
                            }
                        }
                        catch (JsonException)
                        {
                            Console.WriteLine("Invalid JSON");
                            await Response(0, "Error: Invalid JSON");
                            continue;
                        }
                        break;
                    case "FriendList":
                        await FriendList(request.SessionId);
                        break;
                    case "NewMessage":
                        try
                        {
                            int messageId = JsonConvert.DeserializeObject<int>(request.Data);
                            await NewMessage(request.SessionId, messageId);
                        }
                        catch (JsonException)
                        {
                            await Response(request.SessionId, "Error: Invalid JSON");
                        }
                        break;
                    case "SendMessage":
                        try
                        {
                            JObject sendMessageObj = JObject.Parse(request.Data);
                            int sessionId = sendMessageObj.Value<int>("SessionId");
                            string content = sendMessageObj.Value<string>("Content");
                            if (content == null || content == "")
                            {
                                await Response(request.SessionId, "Error: Empty Message");
                                break;
                            }
                            await SendMessage(request.SessionId, sessionId, content);
                        }
                        catch (JsonException)
                        {
                            await Response(request.SessionId, "Error: Invalid JSON");
                        }
                        break;
                    case "NewFriend":
                        try
                        {
                            int friendId = JsonConvert.DeserializeObject<int>(request.Data);
                            await NewFriend(request.SessionId, friendId);
                        }
                        catch (JsonException)
                        {
                            await Response(request.SessionId, "Error: Invalid JSON");
                        }
                        break;
                    case "DeleteFriend":
                        try
                        {
                            int friendId = JsonConvert.DeserializeObject<int>(request.Data);
                            await DeleteFriend(request.SessionId, friendId);
                        }
                        catch (JsonException)
                        {
                            await Response(request.SessionId, "Error: Invalid JSON");
                        }
                        break;
                    case "ChangeName":
                        try
                        {
                            string newName = request.Data;
                            await ChangeName(request.SessionId, newName);
                        }
                        catch (JsonException)
                        {
                            await Response(request.SessionId, "Error: Invalid JSON");
                        }
                        break;
                    case "Register":
                        try
                        {
                            JObject registerObj = JObject.Parse(request.Data);
                            string name = registerObj.Value<string>("Name");
                            string password = registerObj.Value<string>("Password");

                            await Register(request.SessionId, name, BCrypt.Net.BCrypt.HashPassword(password));
                        }
                        catch (JsonException)
                        {
                            await Response(request.SessionId, "Error: Invalid JSON");
                        }
                        break;
                    default:
                        break;
                }
            }
            if (UserID != 0)
            {
                DBDesignEF entitys = new DBDesignEF();
                User user = await entitys.Users.FindAsync(UserID);
                user.State = 0;
                await entitys.SaveChangesAsync();
            }
            Console.WriteLine(sClient.RemoteEndPoint + " disconnected.");
            sClient.Close();
        }

        public Session(Socket socket)
        {
            this.sClient = socket;
        }
    }
}
