using Client.Utilities;
using System.Windows;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class PasswordViewModel : BaseViewModel
    {
        public string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(CanJoin));
            }
        }

        public ICommand JoinCommand { get; }
        public bool CanJoin => !string.IsNullOrWhiteSpace(_password);

        private bool _join;
        public bool Join
        {
            get => _join;
            set => _join = value;
        }

        public PasswordViewModel()
        {
            Join = false;
            JoinCommand = new RelayCommand<Window>((window) =>
            {
                Join = true;
                window.Close();
            });
        }
    }
}
