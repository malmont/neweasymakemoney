using System.Diagnostics;
using Easymakemoney.Models.Session;
using Newtonsoft.Json;

namespace Easymakemoney.Services.Session;

public class SessionService : ISessionService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _cachedToken;
    private string? _cachedRefreshToken;
    private UserInfo? _cachedUser;
    private bool _tokenLoaded;
    private bool _refreshTokenLoaded;
    private bool _userLoaded;

    public UserInfo? CurrentUser => _cachedUser;

    public async Task SaveSessionAsync(string token, string refreshToken, UserInfo user)
    {
        await _semaphore.WaitAsync();
        try
        {
            _cachedToken = token;
            _cachedRefreshToken = refreshToken;
            _cachedUser = user;
            _tokenLoaded = true;
            _refreshTokenLoaded = true;
            _userLoaded = true;

            await SecureStorage.Default.SetAsync(SessionKeys.TokenKey, token);
            await SecureStorage.Default.SetAsync(SessionKeys.RefreshTokenKey, refreshToken);
            var userJson = JsonConvert.SerializeObject(user);
            await SecureStorage.Default.SetAsync(SessionKeys.UserKey, userJson);

            // Règle N6 : retenir la dernière adresse e-mail dans SecureStorage après connexion réussie
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await SecureStorage.Default.SetAsync(SessionKeys.LastLoginEmailKey, user.Email);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur sauvegarde session : {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_tokenLoaded)
        {
            return _cachedToken;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_tokenLoaded)
            {
                return _cachedToken;
            }

            _cachedToken = await SecureStorage.Default.GetAsync(SessionKeys.TokenKey);
            _tokenLoaded = true;
            return _cachedToken;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur lecture token : {ex.Message}");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        if (_refreshTokenLoaded)
        {
            return _cachedRefreshToken;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_refreshTokenLoaded)
            {
                return _cachedRefreshToken;
            }

            _cachedRefreshToken = await SecureStorage.Default.GetAsync(SessionKeys.RefreshTokenKey);
            _refreshTokenLoaded = true;
            return _cachedRefreshToken;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur lecture refresh token : {ex.Message}");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateTokenAsync(string newToken)
    {
        await _semaphore.WaitAsync();
        try
        {
            _cachedToken = newToken;
            _tokenLoaded = true;
            await SecureStorage.Default.SetAsync(SessionKeys.TokenKey, newToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur mise à jour token : {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<UserInfo?> GetUserAsync()
    {
        if (_userLoaded)
        {
            return _cachedUser;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (_userLoaded)
            {
                return _cachedUser;
            }

            var userJson = await SecureStorage.Default.GetAsync(SessionKeys.UserKey);
            if (!string.IsNullOrWhiteSpace(userJson))
            {
                _cachedUser = JsonConvert.DeserializeObject<UserInfo>(userJson);
            }
            else
            {
                _cachedUser = null;
            }
            _userLoaded = true;

            return _cachedUser;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur lecture user : {ex.Message}");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string?> GetLastLoginEmailAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(SessionKeys.LastLoginEmailKey);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur lecture last login email : {ex.Message}");
            return null;
        }
    }

    public async Task SaveLastLoginEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        try
        {
            await SecureStorage.Default.SetAsync(SessionKeys.LastLoginEmailKey, email.Trim());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur sauvegarde last login email : {ex.Message}");
        }
    }

    public async Task ClearSessionAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _cachedToken = null;
            _cachedRefreshToken = null;
            _cachedUser = null;
            _tokenLoaded = false;
            _refreshTokenLoaded = false;
            _userLoaded = false;

            // Règle N6 : ClearSessionAsync ne supprime PAS SessionKeys.LastLoginEmailKey
            SecureStorage.Default.Remove(SessionKeys.TokenKey);
            SecureStorage.Default.Remove(SessionKeys.RefreshTokenKey);
            SecureStorage.Default.Remove(SessionKeys.UserKey);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SessionService] Erreur effacement session : {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
