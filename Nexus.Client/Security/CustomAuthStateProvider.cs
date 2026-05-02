using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Nexus.Client.Security
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _http;

        public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
        {
            _localStorage = localStorage;
            _http = http;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = null;

            // Retry up to 3 times — localStorage isn't always ready on cold load
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    token = await _localStorage.GetItemAsStringAsync("authToken");
                    if (!string.IsNullOrWhiteSpace(token)) break;
                }
                catch
                {
                    // localStorage not ready yet — wait and retry
                }
                await Task.Delay(100);
            }

            if (string.IsNullOrWhiteSpace(token))
                return Anonymous();

            // Strip any extra quotes Blazored sometimes adds
            token = token.Trim('"');

            // Validate it's a real JWT (3 parts)
            if (token.Split('.').Length != 3)
                return Anonymous();

            // Check expiry before trusting it
            if (IsTokenExpired(token))
            {
                await _localStorage.RemoveItemAsync("authToken");
                return Anonymous();
            }

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        public void MarkUserAsAuthenticated(string token)
        {
            token = token.Trim('"');
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void MarkUserAsLoggedOut()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }

        // ── Helpers ──

        private static AuthenticationState Anonymous() =>
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        private bool IsTokenExpired(string jwt)
        {
            try
            {
                var claims = ParseClaimsFromJwt(jwt);
                var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
                if (expClaim == null) return false;

                var exp = long.Parse(expClaim.Value);
                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
                return expiry < DateTimeOffset.UtcNow;
            }
            catch
            {
                return true; // If we can't parse it, treat as expired
            }
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            return keyValuePairs!.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!));
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}