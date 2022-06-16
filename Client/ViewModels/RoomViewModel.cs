using Client.Utilities;
using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class RoomViewModel : BaseViewModel, IDataErrorInfo
    {
        private Client _client;
        private EventAggregator _eventAggregator;

        public ObservableCollection<PlayerDTO> Players => _client.CurrentRoom.Players;

        private string _roomName;
        private bool _roomPrivate;
        private string _roomPassword;
        private int _roomGames;
        private int _roomMaxPlayers;

        public string RoomName 
        {
            get => _roomName;
            set
            {
                _roomName = value;
                OnPropertyChanged(nameof(RoomName));
            }
        }
        public bool RoomPrivate
        {
            get => _roomPrivate;
            set
            {
                _roomPrivate = value;
                OnPropertyChanged(nameof(RoomPrivate));
            }
        }
        public string RoomPassword
        {
            get => _roomPassword;
            set
            {
                _roomPassword = value;
                OnPropertyChanged(nameof(RoomPassword));
            }
        }
        public int RoomGames
        {
            get => _roomGames;
            set
            {
                _roomGames = value;
                OnPropertyChanged(nameof(RoomGames));
            }
        }
        public int RoomMaxPlayers
        {
            get => _roomMaxPlayers;
            set
            {
                _roomMaxPlayers = value;
                OnPropertyChanged(nameof(RoomMaxPlayers));
            }
        }

        public ICommand LeaveCommand { get; }
        public ICommand KickCommand { get; }


        private Dictionary<string, List<string>> _errors;

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                ValidateName();
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(CanEditRoom));
            }
        }

        private bool _private;
        public bool Private
        {
            get => _private;
            set
            {
                _private = value;
                OnPropertyChanged(nameof(Private));
                Password = _password;
                OnPropertyChanged(nameof(CanEditRoom));
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                ValidatePassword();
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(CanEditRoom));
            }
        }

        private int _games;
        public int Games
        {
            get => _games;
            set
            {
                _games = value;
                ValidateGames();
                OnPropertyChanged(nameof(Games));
                OnPropertyChanged(nameof(CanEditRoom));
            }
        }

        private int _maxPlayers;
        public int MaxPlayers
        {
            get => _maxPlayers;
            set
            {
                _maxPlayers = value;
                ValidateMaxPlayers();
                OnPropertyChanged(nameof(MaxPlayers));
                OnPropertyChanged(nameof(CanEditRoom));
            }
        }

        public ICommand EditRoomCommand { get; }
        public ICommand CancelCommand { get; }

        public bool CanEditRoom => !this.HasErrors && !string.IsNullOrEmpty(_name);

        public int ClientId => _client.CurrentClient.Id;
        public int OwnerId => _client.CurrentRoom.Owner;

        private string _playerNameId;
        public string PlayerNameId
        {
            get => _playerNameId;
            set
            {
                _playerNameId = value;
                OnPropertyChanged(nameof(PlayerNameId));
                OnPropertyChanged(nameof(CanInvite));
            }
        }

        public ICommand InviteCommand { get; }
        public bool CanInvite => !string.IsNullOrWhiteSpace(_playerNameId) && new Regex(@"^\w{4,16}#\d+$").IsMatch(_playerNameId);

        public RoomViewModel(Client client, EventAggregator eventAggregator)
        {
            _client = client;
            _eventAggregator = eventAggregator;
            LeaveCommand = new RelayCommand(() => _client.Leave());

            _errors = new Dictionary<string, List<string>>();
            Name = _client.CurrentRoom.Name;
            Password = _client.CurrentRoom.Password;
            Private = _client.CurrentRoom.Private;
            Games = _client.CurrentRoom.Games;
            MaxPlayers = _client.CurrentRoom.MaxPlayers;

            RoomName = Name;
            RoomPassword = Password;
            RoomPrivate = Private;
            RoomGames = Games;
            RoomMaxPlayers = MaxPlayers;

            EditRoomCommand = new RelayCommand(() =>
            {
                EditRoom editRoom = new EditRoom
                {
                    Id = _client.CurrentRoom.Id,
                    Name = _name,
                    Private = _private,
                    Password = _password,
                    Games = _games,
                    MaxPlayers = _maxPlayers,
                };
                _client.EditRoom(editRoom);
            });

            _client.EditRoomFailed += (str) => MsgBox.Error(str);
            _client.RoomEdited += () =>
            {
                RoomName = _client.CurrentRoom.Name;
                RoomPassword = _client.CurrentRoom.Password;
                RoomPrivate = _client.CurrentRoom.Private;
                RoomGames = _client.CurrentRoom.Games;
                RoomMaxPlayers = _client.CurrentRoom.MaxPlayers;
            };

            KickCommand = new RelayCommand<PlayerDTO>((player) => _client.Kick(player));
            InviteCommand = new RelayCommand(() =>
            {
                string[] playerNameId = _playerNameId.Split('#');
                InvitePlayer invitePlayer = new InvitePlayer
                {
                    RoomId = _client.CurrentRoom.Id,
                    PlayerId = Convert.ToInt32(playerNameId[1]),
                    PlayerName = playerNameId[0]
                };
                _client.Invite(invitePlayer);
            });
            _client.InviteSuccess += (msg) => MsgBox.Info(msg);
            _client.InviteFailed += (msg) => MsgBox.Error(msg);
        }

        public void ValidateName()
        {
            string propertyName = nameof(Name);
            string error = "name cannot be empty";
            if (string.IsNullOrEmpty(_name)) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            bool hasOnlyLetters = _name.All(l => char.IsLetter(l));
            error = "name must have only letters";
            if (!hasOnlyLetters) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int minLength = 4;
            error = $"name must be at least {minLength} letters";
            if (_name.Length < minLength) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int maxLength = 16;
            error = $"name must be a maximum of {maxLength} letters";
            if (_name.Length > maxLength) AddError(propertyName, error);
            else RemoveError(propertyName, error);
        }

        public void ValidatePassword()
        {
            string propertyName = nameof(Password);
            string error = "password cannot be empty";
            if (_private && string.IsNullOrEmpty(_password)) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int minLength = 8;
            error = $"password must be at least {minLength} letters or digits";
            if (_private && _password.Length < minLength) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int maxLength = 32;
            error = $"password must be a maximum of {maxLength} letters or digits";
            if (_private && _password.Length > maxLength) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            bool hasDigit = _password.Any(l => char.IsDigit(l));
            error = $"password must be at least one digit";
            if (_private && !hasDigit) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            bool hasLowercaseLetter = _password.Any(l => char.IsLetter(l) && l.ToString() == l.ToString().ToLower());
            error = $"password must be at least one lowercase letter";
            if (_private && !hasLowercaseLetter) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            bool hasUppercaseLetter = _password.Any(l => char.IsLetter(l) && l.ToString() == l.ToString().ToUpper());
            error = $"password must be at least one uppercase letter";
            if (_private && !hasUppercaseLetter) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            bool hasSpecialCharacter = _password.Any(l => "!@#$%^&*()_+<>?{}[]".Contains(l));
            error = $"password must be at least one special character";
            if (_private && !hasSpecialCharacter) AddError(propertyName, error);
            else RemoveError(propertyName, error);
        }

        public void ValidateGames()
        {
            string propertyName = nameof(Games);
            string error = "name cannot be empty";
            if (string.IsNullOrEmpty(_games.ToString())) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int minGames = 5;
            error = $"games must be at least {minGames}";
            if (_games < minGames) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int maxGames = 30;
            error = $"games must be a maximum of {maxGames}";
            if (_games > maxGames) AddError(propertyName, error);
            else RemoveError(propertyName, error);
        }

        public void ValidateMaxPlayers()
        {
            string propertyName = nameof(MaxPlayers);
            string error = "name cannot be empty";
            if (string.IsNullOrEmpty(_maxPlayers.ToString())) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int minPlayers = 2;
            error = $"players must be at least {minPlayers}";
            if (_maxPlayers < minPlayers) AddError(propertyName, error);
            else RemoveError(propertyName, error);

            int maxPlayers = 6;
            error = $"players must be a maximum of {maxPlayers}";
            if (_maxPlayers > maxPlayers) AddError(propertyName, error);
            else RemoveError(propertyName, error);
        }

        public void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
                _errors[propertyName].Add(error);
        }

        public void RemoveError(string propertyName, string error)
        {
            if (_errors.ContainsKey(propertyName) && _errors[propertyName].Contains(error))
            {
                _errors[propertyName].Remove(error);

                if (_errors[propertyName].Count == 0)
                    _errors.Remove(propertyName);
            }
        }

        public bool HasErrors => _errors.Count > 0;

        public string Error => throw new NotImplementedException();

        public string this[string propertyName]
        {
            get
            {
                return !_errors.ContainsKey(propertyName) ? null :
                    String.Join(Environment.NewLine, _errors[propertyName]);
            }
        }
    }
}
