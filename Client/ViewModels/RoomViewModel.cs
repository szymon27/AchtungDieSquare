using Client.Utilities;
using Models;
using System.Collections.ObjectModel;

namespace Client.ViewModels
{
    public class RoomViewModel : BaseViewModel
    {
        private Client _client;
        private EventAggregator _eventAggregator;

        public ObservableCollection<PlayerDTO> Players => _client.CurrentRoom.Players;

        public RoomViewModel(Client client, EventAggregator eventAggregator)
        {
            _client = client;
            _eventAggregator = eventAggregator;
        }
    }
}
