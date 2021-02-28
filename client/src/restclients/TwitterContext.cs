using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

public class TwitterContext {
    private readonly HttpClient Client = new HttpClient();
    private Authentication Context { get; }
    public AuthenticationHeaderValue AuthenticationHeaderValue { get; }
    private class Authentication {
        public string access_token { get; }
        public string token_type { get; }
        public Authentication(string access_token, string token_type) {
            this.access_token = access_token;
            this.token_type = token_type;
        }
    }
    public TwitterContext(string key, string secret) {
        var request = new HttpRequestMessage(
            HttpMethod.Post, new Uri("https://api.twitter.com/oauth2/token"))
        {
            Content = new StringContent(
                "grant_type=client_credentials",
                System.Text.Encoding.UTF8,
                "application/x-www-form-urlencoded"
            )
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(
                $"{key}:{secret}"))
        );
        this.Context = JsonSerializer.Deserialize<Authentication>(
            Client.SendAsync(request).Result.Content.ReadAsStringAsync().Result
        );
        this.AuthenticationHeaderValue = new AuthenticationHeaderValue(
            this.Context.token_type, this.Context.access_token
        );
    }
}