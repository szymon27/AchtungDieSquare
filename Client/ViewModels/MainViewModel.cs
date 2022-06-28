using Client.Utilities;

namespace Client.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private Client _client;
        private EventAggregator _eventAggregator;

        private BaseViewModel _lobbyViewModel;

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

            _lobbyViewModel = new LobbyViewModel(_client, _eventAggregator);

            CurrentViewModel =  new ConnectToServerViewModel(_client);

            _client.ConnectionFailed += () => MsgBox.Error("connection failed");
            _client.ConnectionSuccess += () => CurrentViewModel = _lobbyViewModel;
            _client.Disconnect += () => CurrentViewModel = new ConnectToServerViewModel(_client);
            _client.JoinToRoomFailed += (msg) => MsgBox.Error(msg);
            _client.JoinToRoomSuccess += () => CurrentViewModel = new RoomViewModel(_client, _eventAggregator);
            _client.BackToLobby += () => CurrentViewModel = _lobbyViewModel;
            _client.Kicked += (str) => {
                MsgBox.Error(str);
                CurrentViewModel = _lobbyViewModel;
            };

            _client.StartGameEvent += () => CurrentViewModel = new GameViewModel(_client, _eventAggregator);

            _eventAggregator.ChangeView += (view) => {
                if (view == "CreateNewRoom") 
                    CurrentViewModel = new CreateRoomViewModel(_client, _eventAggregator);
                if (view == "Lobby")
                    CurrentViewModel = _lobbyViewModel;
                if (view == "Room")
                    CurrentViewModel = new RoomViewModel(_client, _eventAggregator);
            };            
        }

    }
}
