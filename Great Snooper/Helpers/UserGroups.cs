using GreatSnooper.Model;
using System;
using System.Collections.Generic;

namespace GreatSnooper.Helpers
{
    public static class UserGroups
    {
        public const int BuddiesGroupID = 0;
        public const int SystemGroupID = int.MaxValue;

        public static readonly Dictionary<string, UserGroup> Users = new Dictionary<string, UserGroup>(StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<string, UserGroup> Groups = new Dictionary<string, UserGroup>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            for (int i = 0; i < 7; i++)
            {
                var ug = new UserGroup(i);
                Groups.Add(ug.SettingName, ug);

                foreach (var user in ug.Users)
                {
                    if (!Users.ContainsKey(user))
                        Users.Add(user, ug);
                }
            }
        }


        public static void AddOrRemoveUser(User u, UserGroup newGroup)
        {
            UserGroup oldGroup;
            if (Users.TryGetValue(u.Name, out oldGroup))
            {
                oldGroup.Users.Remove(u.Name);
                oldGroup.SaveUsers();

                if (newGroup == null)
                {
                    Users.Remove(u.Name);
                    u.Group = null;
                }
                else
                {
                    Users[u.Name] = newGroup;
                    newGroup.Users.Add(u.Name);
                    newGroup.SaveUsers();
                    u.Group = newGroup;
                }
            }
            else if (newGroup != null)
            {
                Users.Add(u.Name, newGroup);
                newGroup.Users.Add(u.Name);
                newGroup.SaveUsers();
                u.Group = newGroup;
            }
        }
    }
}
