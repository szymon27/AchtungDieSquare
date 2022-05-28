using Client.Utilities;
using System;
using System.Windows;

namespace Client.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private Client _client;
        private EventAggregator _eventAggregator;

        private BaseViewModel _connectToServerViewModel;
        private BaseViewModel _lobbyViewModel;
        private BaseViewModel _createNewRoomViewModel;
        private BaseViewModel _roomViewModel;

        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }

        public MainViewModel()
        {
            _eventAggregator = new EventAggregator();
            _client = new Client();

            _connectToServerViewModel = new ConnectToServerViewModel(_client);
            _lobbyViewModel = new LobbyViewModel(_client, _eventAggregator);
            _createNewRoomViewModel = new CreateRoomViewModel(_client, _eventAggregator);
            _roomViewModel = new RoomViewModel(_client, _eventAggregator);

            CurrentViewModel = _connectToServerViewModel;

            _client.ConnectionFailed += () => MessageBox.Show("connection failed");
            _client.ConnectionSuccess += () => CurrentViewModel = _lobbyViewModel;
            _client.Disconnect += () => CurrentViewModel = _connectToServerViewModel;
            _client.JoinToRoomFailed += (msg) => MessageBox.Show(msg);
            _client.JoinToRoomSuccess += () => CurrentViewModel = _roomViewModel;

            _eventAggregator.ChangeView += (view) => {
                if (view == "CreateNewRoom") 
                    CurrentViewModel = _createNewRoomViewModel;
                if (view == "Lobby")
                    CurrentViewModel = _lobbyViewModel;
                if (view == "Room")
                    CurrentViewModel = _roomViewModel;
            };            
        }

    }
}
