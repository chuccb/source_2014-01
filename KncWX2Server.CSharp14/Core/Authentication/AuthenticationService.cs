using System.Security.Cryptography;
using System.Text;
using KncWX2Server.Core.Logging;
using Serilog;

namespace KncWX2Server.Core.Authentication;

/// <summary>
/// Default authentication service implementation.
/// Uses token-based authentication with expiration.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, (long UserId, DateTime ExpiresAt)> _tokenStore;
    private readonly ReaderWriterLockSlim _tokenLock;
    private readonly TimeSpan _tokenExpiration;
    private const string TokenSecret = "KncWX2Server-Secret-Key-2024"; // Should be loaded from config

    public AuthenticationService(TimeSpan? tokenExpiration = null, ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
        _tokenExpiration = tokenExpiration ?? TimeSpan.FromHours(24);
        _tokenStore = [];
        _tokenLock = new();
    }

    /// <summary>
    /// Authenticates a user with credentials.
    /// In production, this would verify against a database.
    /// </summary>
    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationCredentials credentials)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(credentials);
            ArgumentException.ThrowIfNullOrEmpty(credentials.Username);
            ArgumentException.ThrowIfNullOrEmpty(credentials.Password);

            // TODO: Verify credentials against database
            // For now, accepting all authenticated users
            var userId = HashUsername(credentials.Username);

            _logger.LogInfoWithContext($"User {credentials.Username} authenticated successfully");
            return AuthenticationResult.Successful(userId);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(ex, "Authentication failed");
            return AuthenticationResult.Failed($"Authentication error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates an authentication token.
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        _tokenLock.EnterReadLock();
        try
        {
            if (_tokenStore.TryGetValue(token, out var tokenData))
            {
                if (tokenData.ExpiresAt > DateTime.UtcNow)
                {
                    return true;
                }

                // Token expired, remove it
                _tokenLock.ExitReadLock();
                _tokenLock.EnterWriteLock();
                try
                {
                    _tokenStore.Remove(token);
                }
                finally
                {
                    _tokenLock.ExitWriteLock();
                }
                return false;
            }

            return false;
        }
        finally
        {
            if (_tokenLock.IsReadLockHeld)
            {
                _tokenLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Generates an authentication token for a user.
    /// </summary>
    public async Task<string> GenerateTokenAsync(long userId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.Add(_tokenExpiration);

        _tokenLock.EnterWriteLock();
        try
        {
            _tokenStore[token] = (userId, expiresAt);
            _logger.LogInfoWithContext($"Generated token for user {userId}, expires at {expiresAt}");
            return token;
        }
        finally
        {
            _tokenLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the user ID associated with a token.
    /// </summary>
    public long? GetUserIdFromToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        _tokenLock.EnterReadLock();
        try
        {
            if (_tokenStore.TryGetValue(token, out var tokenData))
            {
                if (tokenData.ExpiresAt > DateTime.UtcNow)
                {
                    return tokenData.UserId;
                }
            }
            return null;
        }
        finally
        {
            _tokenLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Revokes a token.
    /// </summary>
    public void RevokeToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        _tokenLock.EnterWriteLock();
        try
        {
            _tokenStore.Remove(token);
        }
        finally
        {
            _tokenLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Cleans up expired tokens.
    /// </summary>
    public void CleanupExpiredTokens()
    {
        _tokenLock.EnterWriteLock();
        try
        {
            var now = DateTime.UtcNow;
            var expiredTokens = _tokenStore
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _tokenStore.Remove(token);
            }

            if (expiredTokens.Count > 0)
            {
                _logger.LogInfoWithContext($"Cleaned up {expiredTokens.Count} expired tokens");
            }
        }
        finally
        {
            _tokenLock.ExitWriteLock();
        }
    }

    private static long HashUsername(string username)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(username));
        return BitConverter.ToInt64(hash, 0);
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenData = new byte[32];
        rng.GetBytes(tokenData);
        return Convert.ToBase64String(tokenData);
    }
}
