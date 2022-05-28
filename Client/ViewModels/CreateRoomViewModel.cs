using Client.Utilities;
using Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class CreateRoomViewModel : BaseViewModel, IDataErrorInfo
    {
        private Dictionary<string, List<string>> _errors;

        private Client _client;
        private EventAggregator _eventAggregator;

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                ValidateName();
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(CanCreateRoom));
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
                OnPropertyChanged(nameof(CanCreateRoom));
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
                OnPropertyChanged(nameof(CanCreateRoom));
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
                OnPropertyChanged(nameof(CanCreateRoom));
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
                OnPropertyChanged(nameof(CanCreateRoom));
            }
        }

        public ICommand CreateRoomCommand { get; }
        public ICommand CancelCommand { get; }

        public bool CanCreateRoom => !this.HasErrors && !string.IsNullOrEmpty(_name);

        public CreateRoomViewModel(Client client, EventAggregator eventAggregator)
        {
            _client = client;
            _eventAggregator = eventAggregator;

            _errors = new Dictionary<string, List<string>>();
            Name = string.Empty;
            Password = string.Empty;
            Private = false;
            Games = 5;
            MaxPlayers = 2;

            CreateRoomCommand = new RelayCommand(() =>
            {
                NewRoom newRoom = new NewRoom
                {
                    Name = _name,
                    Private = _private,
                    Password = _password,
                    Games = _games,
                    MaxPlayers = _maxPlayers,
                    Owner = _client.CurrentClient.Id
                };
                _client.CreateRoom(newRoom);
            });
            _client.RoomCreated += () => _eventAggregator.OnChangeView("Room");
            CancelCommand = new RelayCommand(() => _eventAggregator.OnChangeView("Lobby"));
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
            else  RemoveError(propertyName, error);

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
