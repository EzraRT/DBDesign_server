using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DBDesign
{
    using Model;
    class Test
    {
        public static void TestQuery()
        {
            DBDesignEF ef = new DBDesignEF();

            foreach (User user in ef.Users)
            {
                Console.WriteLine(user.Id + " " + user.Name + " " + user.Password + " " + user.State);
            }

            foreach (Friend friend in ef.Friends)
            {
                Console.WriteLine(friend.User1.Name + " " + friend.User2.Name + " " + friend.SessionId);
            }

            foreach (UserMessage message in ef.UserMessages.Where(m => m.SessionID == 0 && m.Id >= 1))
            {
                Console.WriteLine(message.Id + " " + message.Sender.Name + " " + message.SendTime + " " + message.Content);
            }


            foreach (Group group in ef.Groups)
            {
                Console.WriteLine(group.Id + " " + group.Name);
            }

            foreach (UserGroup userGroup in ef.UserGroups.Include(ug => ug.User).ThenInclude(u => u.UserGroups).ThenInclude(ug => ug.Group))
            {
                Console.WriteLine(userGroup.User.Name + " " + userGroup.Group.Name + " " + userGroup.Authority);
            }

            foreach (Group group in ef.Groups.Include(g => g.UserGroups).ThenInclude(ug => ug.User))
            {
                Console.WriteLine(group.Name);
                foreach (UserGroup userGroup in group.UserGroups)
                {
                    Console.WriteLine(userGroup.User.Name);
                }
            }
        }
    }
}
