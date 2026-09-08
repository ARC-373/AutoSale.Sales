namespace AutoSale.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    string? Subject { get; }
}
