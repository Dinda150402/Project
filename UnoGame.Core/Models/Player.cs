using UnoGame.Core.Interfaces;

namespace UnoGame.Core.Models;

public class Player : IPlayer
{
    public string Name { get; }
    public int Score { get; set; }

    public Player (string name)
    {
        Name = name;
    }
}