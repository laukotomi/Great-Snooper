using System.Collections.Generic;

namespace MySnooper
{
    public static class UserGroups
    {
        public static Dictionary<string, UserGroup> Users = new Dictionary<string, UserGroup>();
        public static Dictionary<string, UserGroup> Groups = new Dictionary<string, UserGroup>();
        public const int BuddiesGroupID = 0;

        public static void Initialize()
        {
            for (int i = 0; i < 7; i++)
            {
                var ug = new UserGroup(i);
                Groups.Add(ug.SettingName, ug);

                foreach (var item in ug.Users)
                {
                    if (!Users.ContainsKey(item.Key))
                        Users.Add(item.Key, ug);
                }
            }
        }

        public static void AddOrRemoveUser(Client c, UserGroup group)
        {
            if (Users.ContainsKey(c.LowerName))
            {
                var oldGroup = Users[c.LowerName];
                oldGroup.Users.Remove(c.LowerName);
                oldGroup.SaveUsers();

                if (group == null)
                {
                    Users.Remove(c.LowerName);
                    c.Group = null;
                }
                else
                {
                    Users[c.LowerName] = group;
                    group.Users.Add(c.LowerName, c.Name);
                    group.SaveUsers();
                    c.Group = group;
                }
            }
            else if (group != null)
            {
                Users.Add(c.LowerName, group);
                group.Users.Add(c.LowerName, c.Name);
                group.SaveUsers();
                c.Group = group;
            }
        }
    }
}
