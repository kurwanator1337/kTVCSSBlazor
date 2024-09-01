using Dapper;
using kTVCSSBlazor.Db;
using kTVCSSBlazor.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace kTVCSSBlazor.Data
{
    public class kTVCSSAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
    {
        private readonly kTVCSSUserService _kTVCSSUserService;
        public User CurrentUser { get; private set; } = new();

        public kTVCSSAuthenticationStateProvider(kTVCSSUserService kTVCSSUserService)
        {
            _kTVCSSUserService = kTVCSSUserService;
            AuthenticationStateChanged += OnAuthenticationStateChangedAsync;
        }

        private async void OnAuthenticationStateChangedAsync(Task<AuthenticationState> task)
        {
            var authenticationState = await task;

            if (authenticationState is not null)
            {
                CurrentUser = User.FromClaimsPrincipal(authenticationState.User);
            }
        }

        public async Task<User?> GetUserFromDataBase()
        {
            var localStorageData = await _kTVCSSUserService.FetchUserFromBrowserAsync();
            if (localStorageData != null)
            {
                return await _kTVCSSUserService.LookupUserInDatabaseAsync(localStorageData.Username, localStorageData.Password);
            }
            else return null;
        }

        public async Task<User?> GetUserFromLocalStorage()
        {
            return await _kTVCSSUserService.FetchUserFromBrowserAsync();
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = new ClaimsPrincipal();
            var user = await _kTVCSSUserService.FetchUserFromBrowserAsync();

            if (user is not null)
            {
                var userInDatabase = await _kTVCSSUserService.LookupUserInDatabaseAsync(user.Username, user.Password);

                if (userInDatabase is not null)
                {
                    principal = userInDatabase.ToClaimsPrincipal();
                    CurrentUser = userInDatabase;
                }
            }

            return new(principal);
        }

        public async Task<string> LoginAsync(string username, string password)
        {
            string error = string.Empty;
            var principal = new ClaimsPrincipal();
            var user = await _kTVCSSUserService.LookupUserInDatabaseAsync(username, password);

            if (user is not null)
            {
                await _kTVCSSUserService.PersistUserToBrowserAsync(user);
                principal = user.ToClaimsPrincipal();
            }
            else
            {
                error = "Неверный логин или пароль!";
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));

            return error;
        }

        public async Task<string> SignupAsync(string username, string password)
        {
            string error = string.Empty;

            error = await _kTVCSSUserService.Register(username, password);

            return error;
        }

        public async Task LogoutAsync()
        {
            await _kTVCSSUserService.ClearBrowserUserDataAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new())));
        }

        public void Dispose() => AuthenticationStateChanged -= OnAuthenticationStateChangedAsync;
    }
}