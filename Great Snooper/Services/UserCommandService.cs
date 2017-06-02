using System;
using System.Collections.Generic;
using GreatSnooper.Helpers;
using GreatSnooper.UserCommands;
using GreatSnooper.ViewModel;

namespace GreatSnooper.Services
{
    class UserCommandService
    {
        private static UserCommandService instance;
        private Dictionary<string, UserCommand> _commandList;

        private UserCommandService()
        {
            _commandList = new Dictionary<string, UserCommand>(StringComparer.OrdinalIgnoreCase);
        }

        public static UserCommandService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UserCommandService();
                }
                return instance;
            }
        }

        public void Initialize()
        {
            foreach (Type type in ReflectionHelper.GetTypesThatInheritsFrom(typeof(UserCommand)))
            {
                UserCommand userCommand = (UserCommand)Activator.CreateInstance(type);
                foreach (string command in userCommand.Commands)
                {
                    _commandList.Add(command, userCommand);
                }
            }
        }

        public void Run(AbstractChannelViewModel chvm, string command, string text)
        {
            UserCommand userCommand;
            if (_commandList.TryGetValue(command, out userCommand))
            {
                userCommand.Run(chvm, command, text);
            }
        }
    }
}
