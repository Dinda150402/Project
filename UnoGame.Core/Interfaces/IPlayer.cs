namespace UnoGame.Core.Interfaces;

public interface IPlayer
{
    string Name { get; }
    int Score { get; set; }
}