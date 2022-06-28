using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Models
{
    public class Room
    {
        private const int Board_W = 300;
        private const int Board_H = 300;

        public int Id { get; set; }
        public string Name { get; set; }
        public bool Private { get; set; }
        public string Password { get; set; }
        public int Games { get; set; }
        public int MaxPlayers { get; set; }
        public int Owner { get; set; }
        public ObservableCollection<Player> Players { get; set; }
        public bool GameIsRunning { get; set; }

        private int points = 0;

        public bool Collision()
        {
            List<Worm> wormList = new List<Worm>();
            foreach (Player p in Players)
            {
                wormList.Add(p.Worm);
            }
            foreach (Worm worm in wormList)
            {
                bool collided = false;
                WormPart head = worm.WormParts.Last();

                foreach (Worm w in wormList)
                {
                    foreach (WormPart wp in w.WormParts)
                    {
                        if (w.Id == worm.Id && wp.isHead) continue;
                        if ((head.Position.X == wp.Position.X) && (head.Position.Y == wp.Position.Y))
                            collided = true;
                    }
                }

                if(!collided)
                if ((head.Position.Y < 0) || (head.Position.Y >= Board_H) ||
                    (head.Position.X < 0) || (head.Position.X >= Board_W))
                {
                    collided = true;
                }

                if (collided)
                    foreach (Player p in Players)
                        if (p.Client.Id == worm.Id)
                        {
                            worm.isAlive = false;
                            p.Points += points++;
                        }
                
            }

            if (Players.Where(p => p.Worm.isAlive).Count() == 1)
            {
                Players.Where(p => p.Worm.isAlive).First().Points += points;
                points = 0;
                return false;
            }
            else return true;
        }
    }
}
