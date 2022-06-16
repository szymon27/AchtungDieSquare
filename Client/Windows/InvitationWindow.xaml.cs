using Client.ViewModels;
using Models;
using System.Windows;

namespace Client.Windows
{
    public partial class InvitationWindow : Window
    {
        public InvitationWindow(Invitation invitation)
        {
            InitializeComponent();
            this.DataContext = new InvitationViewModel(invitation);
        }
    }
}
