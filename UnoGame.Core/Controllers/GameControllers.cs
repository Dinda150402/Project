using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

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
    public event Action<IPlayer,int>? OnRoundEnded;
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
        if(_players.Count >= 2)
        {
            Shuffle();
            foreach (IPlayer player in _players)
            {
                _hands [player] = new List<ICard>();
                for(int i = 1; i <= 7; i++)
                {
                    if(_drawPile.Cards.Count > 0)
                    {
                        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count-1];
                        _hands[player].Add(drawnCard);
                        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count-1);
                    }
                }
            }

            ICard starterCard = _drawPile.Cards[_drawPile.Cards.Count -1];
            _drawPile.Cards.RemoveAt(_drawPile.Cards.Count -1);
            _discardPile = new DiscardPile(starterCard);
            _currentColor = starterCard.Color ?? CardColor.Red;

            IPlayer firstPlayer = _players[_currentPlayerIndex];
            OnTurnStarted?.Invoke(firstPlayer);

        } else {
            throw new InvalidOperationException("Jumlah Player Minimal 2 orang");}
    }

    public void PlayCard(IPlayer players, ICard card, CardColor? chosenColor)
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
        return _players[_currentPlayerIndex];
    }

    public CardColor GetCurrentColor()
    {
        return _currentColor ?? throw new InvalidOperationException("Current color is not set.");
    }

    public ICard GetTopDiscardCard()
    {
        return _discardPile!.Cards[_discardPile!.Cards.Count - 1];
    }

    public List<IPlayer> GetPlayers()
    {
        return _players.ToList();
    }

    public List<ICard> GetPlayerHand(IPlayer player)
    {
        if (_hands.TryGetValue(player, out var hand))
        {
            return hand.AsReadOnly().ToList();
        }
        
        throw new ArgumentException("Player not found in the game.");
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
        int n = _drawPile.Cards.Count;

        n--;
        while (n > 1)
        {
            int k = Random.Shared.Next(n);

            var value = _drawPile.Cards[k];
            _drawPile.Cards[k] = _drawPile.Cards[n];
            _drawPile.Cards[n] = value;
        }
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

