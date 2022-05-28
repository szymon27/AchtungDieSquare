﻿using Client.Utilities;
using Client.Windows;
using Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Client.ViewModels
{
    public class LobbyViewModel : BaseViewModel
    {
        private Client _client;
        private EventAggregator _eventAggregator;

        public ObservableCollection<RoomInfoDTO> Rooms => _client.Rooms;
        public ObservableCollection<ClientDTO> Clients => _client.Clients;

        public ICommand CreateRoomCommand { get; }
        public ICommand JoinCommand { get; }

        public LobbyViewModel(Client client, EventAggregator eventAggregator)
        {
            _client = client;
            _eventAggregator = eventAggregator;

            CreateRoomCommand = new RelayCommand(() => _eventAggregator.OnChangeView("CreateNewRoom"));
            JoinCommand = new RelayCommand<RoomInfoDTO>((room) => {
                string password = string.Empty;
                if(room.Private)
                {
                    PasswordWindow passwordWindow = new PasswordWindow();
                    passwordWindow.ShowDialog();
                    PasswordViewModel vm = passwordWindow.DataContext as PasswordViewModel;
                    password = vm.Password;
                }
                _client.Join(room, password);
            });
        }
    }
}
