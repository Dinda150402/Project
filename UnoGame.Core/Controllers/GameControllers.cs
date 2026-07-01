using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;

namespace UnoGame.Core.Controllers;

public class GameController
{
    // Private Fields
    private CardColor? _currentColor;
    private Dictionary<IPlayer, List<ICard>> _hands;
    private List<IPlayer> _players;
    private GameDirection _direction;
    private IDrawPile _drawPile;
    private IDiscardPile _discardPile;
    private int _currentPlayerIndex;
    private ICard? _drawnCardThisTurn;
    private List<IPlayer> _unoPendingPlayers;

    // Event Declarations
    public event Action<IPlayer>? OnTurnStarted;
    public event Action<IPlayer, ICard>? OnCardPlayed;
    public event Action<IPlayer>? OnUnoCalled;
    public event Action<IPlayer>? OnUnoPenaltyApplied;
    public event Action<IPlayer, int>? OnRoundEnded;
    public event Action<IPlayer>? OnGameEnded;

    // Constructor
    public GameController(List<IPlayer> players, IDrawPile drawPile, IDiscardPile discardPile)
    {
        _currentColor = null;
        _hands = new Dictionary<IPlayer, List<ICard>>();
        _players = players;
        _direction = GameDirection.ClockWise;
        _drawPile = drawPile;
        _discardPile = discardPile;
        _currentPlayerIndex = 0;
        _drawnCardThisTurn = null;
        _unoPendingPlayers = new List<IPlayer>();
    }

    // Public Methods
    public GameResult StartGame()
    {
        if (_players.Count < 2)
        {
            return GameResult.Fail("Jumlah Player Minimal 2 orang");
        }

        Shuffle();

        foreach (IPlayer player in _players)
        {
            if (!_hands.ContainsKey(player))
            {
                _hands[player] = new List<ICard>();
            }
            else
            {
                _hands[player].Clear();
            }

            for (int i = 1; i <= 7; i++)
            {
                if (_drawPile.Cards.Count > 0)
                {
                    ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                    _hands[player].Add(drawnCard);
                    _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
                }
            }
        }

        ICard starterCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
        _discardPile.Cards.Add(starterCard);
        _currentColor = starterCard.Color ?? CardColor.Red;

        if (starterCard.Value == CardValue.WildDrawFour)
        {
            ICard newStarterCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
            _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
            _discardPile.Cards.Add(newStarterCard);
            _currentColor = newStarterCard.Color ?? CardColor.Red;
            ApplyCardEffect(newStarterCard);
        }
        else if (starterCard.Value == CardValue.DrawTwo)
        {
            IPlayer firstTurn = _players[_currentPlayerIndex];
            for (int i = 0; i < 2; i++)
            {
                ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                _hands[firstTurn].Add(drawnCard);
                _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
            }
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        }
        else
        {
            ApplyCardEffect(starterCard);
        }

        IPlayer firstPlayer = _players[_currentPlayerIndex];
        OnTurnStarted?.Invoke(firstPlayer);

        return GameResult.Ok();
    }

    public GameResult PlayCard(IPlayer player, ICard card, CardColor? chosenColor)
    {
        if (_players[_currentPlayerIndex] != player)
        {
            return GameResult.Fail("It's not your turn yet");
        }

        if (!_hands.ContainsKey(player) || !_hands[player].Contains(card))
        {
            return GameResult.Fail("You don't have that card");
        }

        if (!IsValidPlay(card))
        {
            return GameResult.Fail("There is no match in your cards");
        }

        _hands[player].Remove(card);
        _discardPile.Cards.Add(card);

        bool isWild = card.Value == CardValue.Wild || card.Value == CardValue.WildDrawFour;
        _currentColor = isWild
            ? (chosenColor ?? _currentColor)
            : (card.Color ?? _currentColor);

        if (_hands[player].Count == 1)
        {
            _unoPendingPlayers.Add(player);
        }

        OnCardPlayed?.Invoke(player, card);
        ApplyCardEffect(card);

        if (_hands[player].Count == 0)
        {
            int roundScore = CalculateRoundScore(player);
            player.Score += roundScore;
            OnRoundEnded?.Invoke(player, roundScore);

            if (player.Score >= 500)
            {
                EndGame(player);
            }

            return GameResult.Ok();
        }

        NextTurn();
        return GameResult.Ok();
    }

    public GameResult<ICard> DrawCard(IPlayer player)
    {
        if (_players[_currentPlayerIndex] != player)
        {
            return GameResult<ICard>.Fail("It's not your turn yet");
        }

        if (_drawnCardThisTurn != null)
        {
            return GameResult<ICard>.Fail("You already drawn a card");
        }

        if (_drawPile.Cards.Count < 1)
        {
            bool refilled = RefillDrawPile();
            if (!refilled)
            {
                return GameResult<ICard>.Fail("There is not enough cards to draw");
            }
        }

        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
        _hands[player].Add(drawnCard);
        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);

        if (_unoPendingPlayers.Contains(player))
        {
            _unoPendingPlayers.Remove(player);
        }

        _drawnCardThisTurn = drawnCard;
        return GameResult<ICard>.Ok(drawnCard);
    }

    public GameResult CallUno(IPlayer player)
    {
        if (!_unoPendingPlayers.Contains(player))
        {
            return GameResult.Fail("You cannot call UNO now");
        }

        _unoPendingPlayers.Remove(player);
        OnUnoCalled?.Invoke(player);
        return GameResult.Ok();
    }

    public bool CatchUnoViolation(IPlayer player)
    {
        if (!_unoPendingPlayers.Contains(player))
        {
            return false;
        }

        if (_hands[player].Count != 1)
        {
            _unoPendingPlayers.Remove(player);
            return false;
        }

        ApplyUnoPenalty(player);
        _unoPendingPlayers.Remove(player);
        return true;
    }

    public GameResult PassTurn(IPlayer player)
    {
        if (_players[_currentPlayerIndex] != player)
        {
            return GameResult.Fail("It's not your turn yet");
        }

        if (_drawnCardThisTurn == null)
        {
            return GameResult.Fail("You need to draw a card first");
        }

        NextTurn();
        return GameResult.Ok();
    }

    public IPlayer GetCurrentPlayer()
    {
        IPlayer currentPlayer = _players[_currentPlayerIndex];
        return currentPlayer;
    }

    public GameResult<CardColor> GetCurrentColor()
    {
        if (_currentColor == null)
        {
            return GameResult<CardColor>.Fail("Current color is not set");
        }

        return GameResult<CardColor>.Ok(_currentColor.Value);
    }

    public ICard GetTopDiscardCard()
    {
        ICard topCard = _discardPile.Cards[_discardPile.Cards.Count - 1];
        return topCard;
    }

    public List<IPlayer> GetPlayers()
    {
        List<IPlayer> players = _players.ToList();
        return players;
    }

    public GameResult<List<ICard>> GetPlayerHand(IPlayer player)
    {
        if (!_hands.TryGetValue(player, out List<ICard>? hand))
        {
            return GameResult<List<ICard>>.Fail("Player not found in the game");
        }

        List<ICard> handCopy = hand.AsReadOnly().ToList();
        return GameResult<List<ICard>>.Ok(handCopy);
    }

    public List<IPlayer> GetUnoPendingPlayers()
    {
        List<IPlayer> pendingPlayers = _unoPendingPlayers.ToList();
        return pendingPlayers;
    }

    public GameResult<List<ICard>> GetValidCards(IPlayer player)
    {
        if (!_hands.TryGetValue(player, out List<ICard>? hand))
        {
            return GameResult<List<ICard>>.Fail("Player not found in the game");
        }

        List<ICard> validCards = new List<ICard>();
        foreach (ICard card in hand)
        {
            if (IsValidPlay(card))
            {
                validCards.Add(card);
            }
        }

        return GameResult<List<ICard>>.Ok(validCards);
    }

    public GameResult StartNextRound(IPlayer startingPlayer)
    {
        foreach (IPlayer player in _players)
        {
            _drawPile.Cards.AddRange(_hands[player]);
            _hands[player].Clear();
        }

        _drawPile.Cards.AddRange(_discardPile.Cards);
        _discardPile.Cards.Clear();

        _currentColor = null;
        _direction = GameDirection.ClockWise;
        _currentPlayerIndex = _players.IndexOf(startingPlayer);
        _drawnCardThisTurn = null;
        _unoPendingPlayers.Clear();

        GameResult startResult = StartGame();
        return startResult;
    }

    // Private Methods
    private bool IsValidPlay(ICard card)
    {
        ICard topCard = GetTopDiscardCard();

        if (card.Value == CardValue.Wild || card.Value == CardValue.WildDrawFour)
        {
            return true;
        }

        if (card.Color == _currentColor || card.Value == topCard.Value)
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
            int k = Random.Shared.Next(n + 1);

            ICard temp = _drawPile.Cards[k];
            _drawPile.Cards[k] = _drawPile.Cards[n];
            _drawPile.Cards[n] = temp;
        }
    }
    
    private bool RefillDrawPile()
    {
        if (_discardPile.Cards.Count <= 1)
        {
            return false;
        }

        ICard topCard = _discardPile.Cards[_discardPile.Cards.Count - 1];
        List<ICard> cardsToMove = _discardPile.Cards.SkipLast(1).ToList();

        _drawPile.Cards.AddRange(cardsToMove);
        _discardPile.Cards.Clear();
        _discardPile.Cards.Add(topCard);

        Shuffle();
        return true;
    }

    private void ApplyCardEffect(ICard card)
    {
        int nextPlayerIndex =
            (_direction == GameDirection.ClockWise)
                ? (_currentPlayerIndex + 1) % _players.Count
                : (_currentPlayerIndex - 1 + _players.Count) % _players.Count;

        IPlayer nextPlayer = _players[nextPlayerIndex];

        switch (card.Value)
        {
            case CardValue.Skip:
                _currentPlayerIndex = nextPlayerIndex;
                break;
            case CardValue.Reverse:
                if (_players.Count == 2)
                {
                    goto case CardValue.Skip;
                }
                _direction = (_direction == GameDirection.ClockWise)
                    ? GameDirection.CounterClockWise
                    : GameDirection.ClockWise;
                break;
            case CardValue.DrawTwo:
                _currentPlayerIndex = nextPlayerIndex;
                for (int i = 0; i < 2; i++)
                {
                    if (_drawPile.Cards.Count > 0)
                    {
                        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                        _hands[nextPlayer].Add(drawnCard);
                        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
                    }
                }
                goto case CardValue.Skip;
            case CardValue.Wild:
                break;
            case CardValue.WildDrawFour:
                _currentPlayerIndex = nextPlayerIndex;
                for (int i = 0; i < 4; i++)
                {
                    if (_drawPile.Cards.Count > 0)
                    {
                        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                        _hands[nextPlayer].Add(drawnCard);
                        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
                    }
                }
                goto case CardValue.Skip;
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
        }
        else
        {
            _currentPlayerIndex = (_currentPlayerIndex - 1 + _players.Count) % _players.Count;
        }

        OnTurnStarted?.Invoke(_players[_currentPlayerIndex]);
    }

    private void ApplyUnoPenalty(IPlayer player)
    {
        for (int i = 0; i < 2; i++)
        {
            if (_drawPile.Cards.Count > 0)
            {
                ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                _hands[player].Add(drawnCard);
                _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
            }
        }

        OnUnoPenaltyApplied?.Invoke(player);
    }

    private int CalculateRoundScore(IPlayer winner)
    {
        int score = 0;

        foreach (IPlayer player in _players)
        {
            if (player == winner)
            {
                continue;
            }

            foreach (ICard card in _hands[player])
            {
                score += card.Points;
            }
        }

        return score;
    }

    private void EndGame(IPlayer winner)
    {
        OnGameEnded?.Invoke(winner);
    }
}