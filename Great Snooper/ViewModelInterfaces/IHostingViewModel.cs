using System.Windows.Input;
using GreatSnooper.Services;
using GreatSnooper.ViewModel;

namespace GreatSnooper.ViewModelInterfaces
{
    interface IHostingViewModel
    {
        void Init(IMetroDialogService DialogService, ChannelViewModel channel);
        ICommand CloseCommand { get; }
        ICommand CreateGameCommand { get; }
        bool? ExitSnooper { get; set; }
        string GameName { get; set; }
        string GamePassword { get; set; }
        bool? InfoToChannel { get; set; }
        bool Loading { get; }
        int SelectedWaExe { get; set; }
        bool? UsingWormNat2 { get; set; }
    }
}
