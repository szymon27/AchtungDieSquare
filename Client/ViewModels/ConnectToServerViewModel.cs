using Client.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class ConnectToServerViewModel : BaseViewModel, IDataErrorInfo
    {
        private Dictionary<string, List<string>> _errors;
        private Client _client;

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                ValidateName();
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(CanJoin));
            }
        }

        public ICommand JoinCommand { get; }
        public bool CanJoin { get => !HasErrors && !string.IsNullOrEmpty(_name); }

        public ConnectToServerViewModel(Client client)
        {
            _client = client;
            _errors = new Dictionary<string, List<string>>();
            JoinCommand = new RelayCommand(() => _client.Connect(_name));
        }

        private void ValidateName()
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
