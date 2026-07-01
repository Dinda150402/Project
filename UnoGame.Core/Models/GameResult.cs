namespace UnoGame.Core.Models;


public class GameResult
{
public bool Success { get; }
public string? ErrorMessage { get; }
protected GameResult(bool success, string? errorMessage)
{
Success = success;

ErrorMessage = errorMessage;
}
public static GameResult Ok() => new GameResult(true, null);

public static GameResult Fail(string errorMessage) => new
GameResult(false, errorMessage);
}


public sealed class GameResult<T> : GameResult
{
public T Value { get; }
private GameResult(bool success, T value, string? errorMessage) : base(success, errorMessage) {Value = value; }
public static GameResult<T> Ok(T value) => new GameResult<T>(true, value, null);
public static new GameResult<T> Fail(string errorMessage) => new GameResult<T>(false, default!, errorMessage);
}