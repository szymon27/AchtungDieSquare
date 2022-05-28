using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Private { get; set; }
        public string Password { get; set; }
        public int Games { get; set; }
        public int MaxPlayers { get; set; }
        public int Owner { get; set; }
        public ObservableCollection<Player> Players { get; set; }
    }
}
