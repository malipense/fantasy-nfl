using System;

namespace Game.Core
{
    internal static class PlayersList
    {
        public static List<Player> Players = new List<Player>()
        {
            new Player(1,"Jamal Murray"),
            new Player(2, "Charles Barkley"),
            new Player(3, "Mike Tyson"),
            new Player(4, "Big Joe")
        };
    }

    internal class Player
    {
        public Player(int id, string name)
        {
            Id = id;
            Name = name;
        }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
