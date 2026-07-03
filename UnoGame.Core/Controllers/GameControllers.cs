using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UnoGame.Core.Enums;
using UnoGame.Core.Interfaces;
using UnoGame.Core.Models;
using UnoGame.Core.Services;

namespace UnoGame.Core.Controllers;

public class GameController
{
    private CardColor? _currentColor;
    private Dictionary<IPlayer, List<ICard>> _hands;
    private List<IPlayer> _players;
    private GameDirection _direction;
    private IDrawPile _drawPile;
    private IDiscardPile _discardPile;
    private int _currentPlayerIndex;
    private ICard? _drawnCardThisTurn;
    private List<IPlayer> _unoPendingPlayers;
    private readonly ICardShuffler _shuffler;
    private readonly ILogger<GameController> _logger;

    public event Action<IPlayer>? OnTurnStarted;
    public event Action<IPlayer, ICard>? OnCardPlayed;
    public event Action<IPlayer>? OnUnoCalled;
    public event Action<IPlayer>? OnUnoPenaltyApplied;
    public event Action<IPlayer, int>? OnRoundEnded;
    public event Action<IPlayer>? OnGameEnded;

    public GameController(
        List<IPlayer> players,
        IDrawPile drawPile,
        IDiscardPile discardPile,
        ICardShuffler? shuffler = null,
        ILogger<GameController>? logger = null)
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
        _shuffler = shuffler ?? new RandomCardShuffler();
        _logger = logger ?? NullLogger<GameController>.Instance;
    }

    public GameResult StartGame()
    {
        _logger.LogInformation("Memulai game dengan {PlayerCount} pemain", _players.Count);

        int playerCount = _players.Count;
        bool notEnoughPlayers = playerCount < 2;
        if (notEnoughPlayers)
        {
            _logger.LogWarning("Game gagal dimulai. Alasan: {Reason}", "Jumlah pemain kurang dari 2");
            return GameResult.Fail("Jumlah Player Minimal 2 orang");
        }

        Shuffle();

        foreach (IPlayer player in _players)
        {
            bool handAlreadyExists = _hands.ContainsKey(player);
            if (!handAlreadyExists)
            {
                _hands[player] = new List<ICard>();
            }
            else
            {
                _hands[player].Clear();
            }

            for (int i = 1; i <= 7; i++)
            {
                int drawPileCount = _drawPile.Cards.Count;
                if (drawPileCount > 0)
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

        bool starterIsWildDrawFour = starterCard.Value == CardValue.WildDrawFour;
        bool starterIsDrawTwo = starterCard.Value == CardValue.DrawTwo;

        if (starterIsWildDrawFour)
        {
            ICard newStarterCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
            _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
            _discardPile.Cards.Add(newStarterCard);
            _currentColor = newStarterCard.Color ?? CardColor.Red;
            ApplyCardEffect(newStarterCard);
        }
        else if (starterIsDrawTwo)
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
        _logger.LogInformation(
            "Game dimulai. CurrentColor: {CurrentColor}, FirstPlayer: {PlayerName}",
            _currentColor, firstPlayer.Name);
        OnTurnStarted?.Invoke(firstPlayer);

        return GameResult.Ok();
    }

    public GameResult PlayCard(IPlayer player, ICard card, CardColor? chosenColor)
    {
        IPlayer currentTurnPlayer = _players[_currentPlayerIndex];
        bool isPlayersTurn = currentTurnPlayer == player;
        if (!isPlayersTurn)
        {
            _logger.LogWarning("PlayCard ditolak: bukan giliran {PlayerName}", player.Name);
            return GameResult.Fail("It's not your turn yet");
        }

        bool playerHandExists = _hands.ContainsKey(player);
        bool playerHasThisCard = playerHandExists && _hands[player].Contains(card);
        if (!playerHandExists || !playerHasThisCard)
        {
            _logger.LogWarning(
                "PlayCard ditolak: {PlayerName} tidak punya kartu {CardColor} {CardValue}",
                player.Name, card.Color, card.Value);
            return GameResult.Fail("You don't have that card");
        }

        bool isValidPlay = IsValidPlay(card);
        if (!isValidPlay)
        {
            _logger.LogWarning(
                "PlayCard ditolak: kartu {CardColor} {CardValue} tidak cocok dengan CurrentColor {CurrentColor}",
                card.Color, card.Value, _currentColor);
            return GameResult.Fail("There is no match in your cards");
        }

        _hands[player].Remove(card);
        _discardPile.Cards.Add(card);

        bool isWild = card.Value == CardValue.Wild || card.Value == CardValue.WildDrawFour;
        _currentColor = isWild
            ? (chosenColor ?? _currentColor)
            : (card.Color ?? _currentColor);

        _logger.LogInformation(
            "{PlayerName} memainkan {CardColor} {CardValue}. CurrentColor sekarang {CurrentColor}",
            player.Name, card.Color, card.Value, _currentColor);

        int remainingCardsAfterPlay = _hands[player].Count;
        bool playerHasOneCardLeft = remainingCardsAfterPlay == 1;
        if (playerHasOneCardLeft)
        {
            _unoPendingPlayers.Add(player);
            _logger.LogInformation("{PlayerName} tersisa 1 kartu (UNO pending)", player.Name);
        }

        OnCardPlayed?.Invoke(player, card);
        ApplyCardEffect(card);

        bool playerHandIsEmpty = _hands[player].Count == 0;
        if (playerHandIsEmpty)
        {
            int roundScore = CalculateRoundScore(player);
            player.Score += roundScore;
            _logger.LogInformation(
                "Ronde selesai. Winner: {PlayerName}, RoundScore: {RoundScore}, TotalScore: {TotalScore}",
                player.Name, roundScore, player.Score);
            OnRoundEnded?.Invoke(player, roundScore);

            int totalScore = player.Score;
            bool reachedWinningScore = totalScore >= 500;
            if (reachedWinningScore)
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
        IPlayer currentTurnPlayer = _players[_currentPlayerIndex];
        bool isPlayersTurn = currentTurnPlayer == player;
        if (!isPlayersTurn)
        {
            _logger.LogWarning("DrawCard ditolak: bukan giliran {PlayerName}", player.Name);
            return GameResult<ICard>.Fail("It's not your turn yet");
        }

        bool alreadyDrawnThisTurn = _drawnCardThisTurn != null;
        if (alreadyDrawnThisTurn)
        {
            _logger.LogWarning("DrawCard ditolak: {PlayerName} sudah mengambil kartu di giliran ini", player.Name);
            return GameResult<ICard>.Fail("You already drawn a card");
        }

        int drawPileCount = _drawPile.Cards.Count;
        bool drawPileIsEmpty = drawPileCount < 1;
        if (drawPileIsEmpty)
        {
            bool refilled = RefillDrawPile();
            if (!refilled)
            {
                _logger.LogWarning("DrawCard ditolak: draw pile dan discard pile sama-sama habis");
                return GameResult<ICard>.Fail("There is not enough cards to draw");
            }
        }

        ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
        _hands[player].Add(drawnCard);
        _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);

        bool wasUnoPending = _unoPendingPlayers.Contains(player);
        if (wasUnoPending)
        {
            _unoPendingPlayers.Remove(player);
        }

        _drawnCardThisTurn = drawnCard;
        _logger.LogInformation(
            "{PlayerName} mengambil kartu {CardColor} {CardValue}",
            player.Name, drawnCard.Color, drawnCard.Value);
        return GameResult<ICard>.Ok(drawnCard);
    }

    public GameResult CallUno(IPlayer player)
    {
        bool isUnoPending = _unoPendingPlayers.Contains(player);
        if (!isUnoPending)
        {
            _logger.LogWarning("CallUno ditolak: {PlayerName} belum eligible memanggil UNO", player.Name);
            return GameResult.Fail("You cannot call UNO now");
        }

        _unoPendingPlayers.Remove(player);
        _logger.LogInformation("{PlayerName} berhasil memanggil UNO", player.Name);
        OnUnoCalled?.Invoke(player);
        return GameResult.Ok();
    }

    public bool CatchUnoViolation(IPlayer player)
    {
        bool isUnoPending = _unoPendingPlayers.Contains(player);
        if (!isUnoPending)
        {
            return false;
        }

        int handCount = _hands[player].Count;
        bool handCountIsNotOne = handCount != 1;
        if (handCountIsNotOne)
        {
            _unoPendingPlayers.Remove(player);
            return false;
        }

        _logger.LogInformation("{PlayerName} tertangkap melanggar aturan UNO", player.Name);
        ApplyUnoPenalty(player);
        _unoPendingPlayers.Remove(player);
        return true;
    }

    public GameResult PassTurn(IPlayer player)
    {
        IPlayer currentTurnPlayer = _players[_currentPlayerIndex];
        bool isPlayersTurn = currentTurnPlayer == player;
        if (!isPlayersTurn)
        {
            _logger.LogWarning("PassTurn ditolak: bukan giliran {PlayerName}", player.Name);
            return GameResult.Fail("It's not your turn yet");
        }

        bool hasNotDrawnThisTurn = _drawnCardThisTurn == null;
        if (hasNotDrawnThisTurn)
        {
            _logger.LogWarning("PassTurn ditolak: {PlayerName} belum mengambil kartu", player.Name);
            return GameResult.Fail("You need to draw a card first");
        }

        _logger.LogInformation("{PlayerName} melewati giliran setelah mengambil kartu", player.Name);
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
            bool isValid = IsValidPlay(card);
            if (isValid)
            {
                validCards.Add(card);
            }
        }

        return GameResult<List<ICard>>.Ok(validCards);
    }

    public GameResult StartNextRound(IPlayer startingPlayer)
    {
        _logger.LogInformation("Memulai ronde berikutnya. StartingPlayer: {PlayerName}", startingPlayer.Name);
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

    private bool IsValidPlay(ICard card)
    {
        ICard topCard = GetTopDiscardCard();

        bool isWildCard = card.Value == CardValue.Wild || card.Value == CardValue.WildDrawFour;
        if (isWildCard)
        {
            return true;
        }

        bool colorMatches = card.Color == _currentColor;
        bool valueMatches = card.Value == topCard.Value;
        if (colorMatches || valueMatches)
        {
            return true;
        }

        return false;
    }

    private void Shuffle()
    {
        _shuffler.Shuffle(_drawPile.Cards);
        _logger.LogDebug("Draw pile diacak. CardCount: {CardCount}", _drawPile.Cards.Count);
    }

    private bool RefillDrawPile()
    {
        int discardCount = _discardPile.Cards.Count;
        bool notEnoughToRefill = discardCount <= 1;
        if (notEnoughToRefill)
        {
            return false;
        }

        ICard topCard = _discardPile.Cards[_discardPile.Cards.Count - 1];
        List<ICard> cardsToMove = _discardPile.Cards.SkipLast(1).ToList();

        _drawPile.Cards.AddRange(cardsToMove);
        _discardPile.Cards.Clear();
        _discardPile.Cards.Add(topCard);

        _logger.LogInformation(
            "Draw pile habis, refill dari discard pile. CardsMoved: {CardsMoved}", cardsToMove.Count);

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
                int playerCount = _players.Count;
                bool onlyTwoPlayers = playerCount == 2;
                if (onlyTwoPlayers)
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
                    int drawPileCount = _drawPile.Cards.Count;
                    if (drawPileCount > 0)
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
                    int drawPileCount = _drawPile.Cards.Count;
                    if (drawPileCount > 0)
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

        bool isClockwise = _direction == GameDirection.ClockWise;
        if (isClockwise)
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        }
        else
        {
            _currentPlayerIndex = (_currentPlayerIndex - 1 + _players.Count) % _players.Count;
        }

        _logger.LogDebug(
            "Giliran berpindah ke {PlayerName}. Direction: {Direction}",
            _players[_currentPlayerIndex].Name, _direction);
        OnTurnStarted?.Invoke(_players[_currentPlayerIndex]);
    }

    private void ApplyUnoPenalty(IPlayer player)
    {
        for (int i = 0; i < 2; i++)
        {
            int drawPileCount = _drawPile.Cards.Count;
            if (drawPileCount > 0)
            {
                ICard drawnCard = _drawPile.Cards[_drawPile.Cards.Count - 1];
                _hands[player].Add(drawnCard);
                _drawPile.Cards.RemoveAt(_drawPile.Cards.Count - 1);
            }
        }

        _logger.LogInformation("Penalty UNO diterapkan ke {PlayerName}. CardsDrawn: 2", player.Name);
        OnUnoPenaltyApplied?.Invoke(player);
    }

    private int CalculateRoundScore(IPlayer winner)
    {
        int score = 0;

        foreach (IPlayer player in _players)
        {
            bool isWinner = player == winner;
            if (isWinner)
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
        _logger.LogInformation(
            "Game berakhir. Winner: {PlayerName}, FinalScore: {FinalScore}", winner.Name, winner.Score);
        OnGameEnded?.Invoke(winner);
    }
}
