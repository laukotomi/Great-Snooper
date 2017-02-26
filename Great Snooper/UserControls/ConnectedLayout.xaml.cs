namespace GreatSnooper.UserControls
{
    using System.Windows.Controls;

    using GreatSnooper.ViewModel;

    public partial class ConnectedLayout : Border
    {
        public ConnectedLayout(AbstractChannelViewModel chvm)
        {
            this.DataContext = chvm;
            InitializeComponent();
        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ((AbstractChannelViewModel)this.DataContext).MsgPreviewKeyDownCommand.Execute(e);
            if (e.Handled)
            {
                ((TextBox)sender).SelectAll();
            }
        }
    }
}