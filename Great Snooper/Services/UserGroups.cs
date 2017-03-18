namespace GreatSnooper.Services
{
    using System;
    using System.Collections.Generic;
    using GreatSnooper.Helpers;
    using GreatSnooper.Model;

    public class UserGroups
    {
        #region Singleton
        private static readonly Lazy<UserGroups> lazy =
            new Lazy<UserGroups>(() => new UserGroups());

        public static UserGroups Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private UserGroups()
        {
            Groups = new Dictionary<string, UserGroup>(GlobalManager.CIStringComparer);
            Users = new Dictionary<string, UserGroup>(GlobalManager.CIStringComparer);
        }
        #endregion

        public const int SystemGroupID = int.MaxValue;

        public Dictionary<string, UserGroup> Groups { get; private set; }
        public Dictionary<string, UserGroup> Users { get; private set; }

        public void AddOrRemoveUser(User user, UserGroup newGroup)
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

        public void Initialize()
        {
            for (int i = 0; i < 7; i++)
            {
                var group = new UserGroup(i);
                Groups.Add(group.SettingName, group);

                foreach (string user in group.Users)
                {
                    if (!Users.ContainsKey(user))
                    {
                        Users.Add(user, group);
                    }
                }
            }
        }

        public void Reload()
        {
            foreach (var item in Groups)
                item.Value.ReloadData();
        }
    }
}