using System.ComponentModel;
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
    public event Action<IPlayer, ICard>? OnCardPlayed;
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
            
            ApplyCardEffect(starterCard);

            IPlayer firstPlayer = _players[_currentPlayerIndex];
            OnTurnStarted?.Invoke(firstPlayer);
            
        } else {
            throw new InvalidOperationException("Jumlah Player Minimal 2 orang");}
    }

    public void PlayCard(IPlayer player, ICard card, CardColor? chosenColor)
    {
        if(_players[_currentPlayerIndex] != player) throw new InvalidOperationException("It's not your turn yet");
        if(!_hands.ContainsKey(player) || !_hands[player].Contains(card)) throw new InvalidOperationException("You don't have card");
        if(!IsValidPlay(card)) throw new InvalidOperationException("There is no match in your cards");

        _hands[player].Remove(card);

        _discardPile!.Cards.Add(card);

        _currentColor = (card.Value == CardValue.Wild || card.Value == CardValue.WildDrawFour) ? 
        (chosenColor ?? _currentColor) : (card.Color ?? _currentColor);

        OnCardPlayed?.Invoke(player, card);

        ApplyCardEffect(card);

        if(_hands[player].Count == 0)
        {
            EndGame(player);
            Environment.Exit(0);
        }

        NextTurn();
    }

    public void DrawCard(IPlayer player)
    {
        if(_players[_currentPlayerIndex] != player) throw new InvalidOperationException ("It's not your turn yet");

        if(_drawnCardThisTurn != null) throw new InvalidOperationException("You already drawn a card");

        if(_drawPile.Cards.Count < 1)
        {
            RefillDrawPile();
        }
        
        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
        _hands[player].Add(drawnCard);
        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count-1);

        _drawnCardThisTurn = drawnCard;
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
        if(_players[_currentPlayerIndex] != player) throw new InvalidOperationException ("It's not your turn yet");

        if(_drawnCardThisTurn == null) throw new InvalidOperationException("You need to take a card first");

        NextTurn();
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
        List<ICard> validCard = new List<ICard>();

        if (_hands.TryGetValue(player, out var hand))
        {
            foreach (ICard card in hand)
            {
                if (IsValidPlay(card)){
                validCard.Add(card);
                }
            } 
            return validCard;
        } else {
            throw new ArgumentException("Player not found in the game.");
        }
    }

    private bool IsValidPlay(ICard card)
    {
        ICard topCards = GetTopDiscardCard();
        if(card.Value == CardValue.Wild || card.Value == CardValue.WildDrawFour)
        {
            return true;
        }
        else if(card.Color == _currentColor || card.Value == topCards.Value)
        {
            return true;
        }
        return false;
    }

    private void Shuffle()
    {
        int n = _drawPile.Cards.Count;

        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n+1);

            var value = _drawPile.Cards[k];
            _drawPile.Cards[k] = _drawPile.Cards[n];
            _drawPile.Cards[n] = value;
        }
    }

    private void RefillDrawPile()
    {
        if (_discardPile!.Cards.Count <= 1) throw new InvalidOperationException("There is Not Enough Card Here");

        ICard topCard = _discardPile.Cards[_discardPile.Cards.Count -1];

        var cardToMove = _discardPile.Cards.SkipLast(1).ToList();

        _drawPile.Cards.AddRange(cardToMove);

        _discardPile.Cards.Clear();
        _discardPile.Cards.Add(topCard);

        Shuffle();
    }

    private void ApplyCardEffect(ICard card)
    {
        int nextPlayerIndex = (_direction == GameDirection.ClockWise)? 
        (_currentPlayerIndex + 1) % _players.Count : (_currentPlayerIndex - 1 + _players.Count) % _players.Count;
        
        IPlayer nextPlayer = _players[nextPlayerIndex];
        
        switch (card.Value)
        {
            case CardValue.Skip: 
                _currentPlayerIndex = nextPlayerIndex;
                break;
            case CardValue.Reverse:
                _direction = (_direction == GameDirection.ClockWise)? 
                GameDirection.CounterClockWise : GameDirection.ClockWise ;
                break;
            case CardValue.DrawTwo:
                for(int i = 0; i < 2; i++)
                {
                    if(_drawPile.Cards.Count > 0)
                    {
                        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                        _hands[nextPlayer].Add(drawnCard);
                        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
                    }
                }
                _currentPlayerIndex = nextPlayerIndex;
                break;
            case CardValue.Wild:

                break;
            case CardValue.WildDrawFour:
                for(int i = 0; i < 4; i++)
                {
                    if(_drawPile.Cards.Count > 0)
                    {
                        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                        _hands[nextPlayer].Add(drawnCard);
                        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
                    }
                }
                _currentPlayerIndex = nextPlayerIndex;
                break;
            default: 
                break;
        }
    }

    private void NextTurn()
    {
        _drawnCardThisTurn = null;
        if (_direction == GameDirection.ClockWise)
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        } else {
            _currentPlayerIndex = (_currentPlayerIndex -1 + _players.Count) % _players.Count;}
        OnTurnStarted?.Invoke(_players[_currentPlayerIndex]);
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
        OnGameEnded?.Invoke(winner);
    }
}

