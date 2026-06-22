using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;

namespace UnoGame.Core.Controllers;

public class GameController
{
    //Private Field
    private CardColor? _currentColor;
    private Dictionary<IPlayer, List<ICard>> _hands;
    private List<IPlayer> _players;
    private GameDirection _direction;
    private IDrawPile _drawPile;
    private IDiscardPile? _discardPile;
    private int _currentPlayerIndex;
    private ICard? _drawnCardThisTurn;
    private List<IPlayer> _unoPendingPlayers;

    //Action Declaration
    public event Action<IPlayer>? OnTurnStarted;
    public event Action<IPlayer>? OnCardPlayed;
    public event Action<IPlayer>? OnUnoCalled;
    public event Action<IPlayer>? OnUnoPenaltyApplied;
    public event Action<IPlayer, int>? OnRoundEnded;
    public event Action<IPlayer>? OnGameEnded;

    //Constructor Declaration
    public GameController (List<IPlayer> players, IDrawPile drawPile){
        _currentColor = null;
        _hands = new Dictionary<IPlayer, List<ICard>>();
        _players = players;
        _direction = GameDirection.ClockWise;
        _drawPile = drawPile;
        _discardPile = null;
        _currentPlayerIndex = 0;
        _drawnCardThisTurn = null;
        _unoPendingPlayers = new List<IPlayer>();
    }

    //Method Declaration
    public void StartGame()
    {
        throw new NotImplementedException();
    }

    public void PlayCard(IPlayer player, ICard card, CardColor? chosenColor)
    {
        throw new NotImplementedException();
    }

    public void DrawCard(IPlayer player)
    {
        throw new NotImplementedException();
    }

    public void CallUno(IPlayer player)
    {
        throw new NotImplementedException();
    }

    public bool CatchUnoViolation(IPlayer player)
    {
        throw new NotImplementedException();
    }

    public void PassTurn(IPlayer player)
    {
        throw new NotImplementedException();
    }

    public IPlayer GetCurrentPlayer()
    {
        throw new NotImplementedException();
    }

    public CardColor GetCurrentColor()
    {
        throw new NotImplementedException();
    }

    public ICard GetTopDiscardCard()
    {
        throw new NotImplementedException();
    }

    public List<IPlayer> GetPlayers()
    {
        throw new NotImplementedException();
    }

    public List<ICard> GetPlayerHand(IPlayer player)
    {
        throw new NotImplementedException();
    }

    public List<ICard> GetValidCards(IPlayer player)
    {
        throw new NotImplementedException();
    }

    private bool IsValidPlay(ICard card)
    {
        throw new NotImplementedException();
    }

    private void Shuffle()
    {
        throw new NotImplementedException();
    }

    private void RefillDrawPile()
    {
        throw new NotImplementedException();
    }

    private void ApplyCardEffect(ICard card)
    {
        throw new NotImplementedException();
    }

    private void NextTurn()
    {
        throw new NotImplementedException();
    }

    private void ApplyUnoPenalty(IPlayer player)
    {
        throw new NotImplementedException();
    }

    private int CalculateRoundScore(IPlayer winner)
    {
        throw new NotImplementedException();
    }

    private void StartNextRound()
    {
        throw new NotImplementedException();
    }

    private void EndGame(IPlayer winner)
    {
        throw new NotImplementedException();
    }
}

