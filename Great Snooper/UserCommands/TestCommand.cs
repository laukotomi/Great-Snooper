using System;
using Bogus;
using GreatSnooper.Helpers;
using GreatSnooper.IRCTasks;

namespace GreatSnooper.UserCommands
{
    class TestCommand : UserCommand
    {
        private static Random rnd = new Random();

        public TestCommand()
            : base("test")
        {

        }

        public override void Run(ViewModel.AbstractChannelViewModel sender, string command, string text)
        {
            int number;
            if (int.TryParse(text, out number))
            {
                var faker = new Faker<MessageTask>()
                    .CustomInstantiator(f =>
                    {
                        int index = rnd.Next(sender.Users.Count);
                        return new MessageTask(sender.Server, sender.Users[index].Name, sender.Name, f.Lorem.Sentence(), MessageSettings.ChannelMessage);
                    });

                foreach (MessageTask task in faker.Generate(number))
                {
                    sender.MainViewModel.HandleTask(task);
                }
            }
            else
            {
                if (sender.MainViewModel.IsEnergySaveMode)
                {
                    sender.MainViewModel.LeaveEnergySaveMode();
                }
                else
                {
                    sender.MainViewModel.EnterEnergySaveMode();
                }
            }
        }
    }
}
