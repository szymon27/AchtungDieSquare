using Client.Utilities;
using Client.Windows;
using Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class LobbyViewModel : BaseViewModel
    {
        private Client _client;
        private EventAggregator _eventAggregator;

        public ObservableCollection<RoomInfo> Rooms => _client.Rooms;
        public ObservableCollection<ClientInfo> Clients => _client.Clients;

        public int ClientId => _client.CurrentClient.Id;
        public int? ClientRoomId => _client.CurrentClient.RoomId;
        public string ClientName => _client.CurrentClient.Name;

        public ICommand CreateRoomCommand { get; }
        public ICommand JoinCommand { get; }
        public ICommand DisconnectCommand { get; }

        public LobbyViewModel(Client client, EventAggregator eventAggregator)
        {
            _client = client;
            _eventAggregator = eventAggregator;

            CreateRoomCommand = new RelayCommand(() => _eventAggregator.OnChangeView("CreateNewRoom"));
            JoinCommand = new RelayCommand<RoomInfo>((room) => {
                string password = string.Empty;
                if(room.Private)
                {
                    PasswordWindow passwordWindow = new PasswordWindow();
                    passwordWindow.ShowDialog();
                    PasswordViewModel vm = passwordWindow.DataContext as PasswordViewModel;
                    if (vm.Join)
                    {
                        password = vm.Password;
                        _client.Join(room, password);
                    }
                }
                else
                    _client.Join(room, password);
            });

            _client.InvitationDialog += (invitation) =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    InvitationWindow invitationWindow = new InvitationWindow(invitation);
                    invitationWindow.ShowDialog();
                    InvitationViewModel vm = invitationWindow.DataContext as InvitationViewModel;
                    if (vm.Accept) _client.InviteAccept(vm.Invitation.RoomId);

                });
            };

            DisconnectCommand = new RelayCommand(() => _client.DSC());
        }
    }
}
