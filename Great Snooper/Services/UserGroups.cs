namespace GreatSnooper.Services
{
    using System.Collections.Generic;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;

    public static class UserGroups
    {
        public const int BuddiesGroupID = 0;
        public const int SystemGroupID = int.MaxValue;

        public static readonly Dictionary<string, UserGroup> Groups = new Dictionary<string, UserGroup>(GlobalManager.CIStringComparer);
        public static readonly Dictionary<string, UserGroup> Users = new Dictionary<string, UserGroup>(GlobalManager.CIStringComparer);

        public static void AddOrRemoveUser(User user, UserGroup newGroup)
        {
            UserGroup oldGroup;
            if (Users.TryGetValue(user.Name, out oldGroup))
            {
                oldGroup.Users.Remove(user.Name);
                oldGroup.SaveUsers();

                if (newGroup == null)
                {
                    Users.Remove(user.Name);
                    user.Group = null;
                }
                else
                {
                    Users[user.Name] = newGroup;
                    newGroup.Users.Add(user.Name);
                    newGroup.SaveUsers();
                    user.Group = newGroup;
                }
            }
            else if (newGroup != null)
            {
                Users.Add(user.Name, newGroup);
                newGroup.Users.Add(user.Name);
                newGroup.SaveUsers();
                user.Group = newGroup;
            }

            UserHelper.UpdateMessageStyle(user);
        }

        public static void Initialize()
        {
            for (int i = 0; i < 7; i++)
            {
                var ug = new UserGroup(i);
                Groups.Add(ug.SettingName, ug);

                foreach (var user in ug.Users)
                {
                    if (!Users.ContainsKey(user))
                    {
                        Users.Add(user, ug);
                    }
                }
            }
        }
    }
}