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
            }
        }
    }
}
