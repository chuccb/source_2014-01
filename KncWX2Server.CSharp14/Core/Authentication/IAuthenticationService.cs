namespace KncWX2Server.Core.Authentication;

/// <summary>
/// Represents authentication credentials.
/// </summary>
public sealed record AuthenticationCredentials(string Username, string Password);

/// <summary>
/// Result of authentication attempt.
/// </summary>
public sealed record AuthenticationResult(bool Success, string Message, long? UserId = null)
{
    public static AuthenticationResult Successful(long userId) 
        => new(true, "Authentication successful", userId);

    public static AuthenticationResult Failed(string reason) 
        => new(false, reason, null);
}

/// <summary>
/// Interface for authentication service.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with the given credentials.
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationCredentials credentials);

    /// <summary>
    /// Validates an authentication token.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Generates an authentication token for a user.
    /// </summary>
    Task<string> GenerateTokenAsync(long userId);
}
