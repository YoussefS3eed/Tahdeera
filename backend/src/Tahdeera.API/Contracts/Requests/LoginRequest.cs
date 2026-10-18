namespace Tahdeera.API.Contracts.Requests;

public class LoginRequest
{
    public string UsernameOrEmailOrPhone { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
