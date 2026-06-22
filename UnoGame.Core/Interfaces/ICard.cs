using UnoGame.Core.Enums;
namespace UnoGame.Core.Interfaces;

public interface ICard
{
    CardValue Value { get; init; }
    int Points { get; }
    CardColor? Color { get; }
}