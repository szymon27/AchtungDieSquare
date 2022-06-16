using Client.Utilities;
using Models;
using System.Windows;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class InvitationViewModel : BaseViewModel
    {
        public string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public bool _accept;
        public bool Accept { get; set; }

        public ICommand AcceptCommand { get; } 
        public ICommand DeclineCommand { get; }

        public Invitation Invitation { get; private set; }

        public InvitationViewModel(Invitation invitation)
        {
            Invitation = invitation;
            Text = $"You have been invited to {invitation.RoomName}";
            Accept = false;

            AcceptCommand = new RelayCommand<Window>((window) =>
            {
                Accept = true;
                window.Close();
            });

            DeclineCommand = new RelayCommand<Window>((window) =>
            {
                Accept = false;
                window.Close();
            });
        }
    }
}
