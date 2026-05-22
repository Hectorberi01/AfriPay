using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AfriPay.API.Contracts.Responses.PayPal;
using Microsoft.Extensions.Options;

namespace AfriPay.Infrastructure.Providers.Paypal;

public sealed class PayPalClient(HttpClient http,IOptions<PayPalConfig> options, ILogger<PayPalClient> logger)
{
    private readonly PayPalConfig _config = options.Value;
 
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
    };
 
    // OAuth2 token
 
    public async Task<string> GetAccessTokenAsync(bool isLive, CancellationToken ct)
    {
        var clientId     = _config.GetClientId(isLive);
        var clientSecret = _config.GetClientSecret(isLive);
        
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException(
            $"PayPal credentials not configured for environment '{(isLive ? "live" : "sandbox")}'. " +
            $"Set Providers:PayPal:{(isLive ? "Live" : "Sandbox")}ClientId and " +
            $"Providers:PayPal:{(isLive ? "Live" : "Sandbox")}ClientSecret in appsettings.");
 
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
      
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_config.GetBaseUrl(isLive).TrimEnd('/')}/v1/oauth2/token");
 
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
        });
 
        var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PayPal token request failed. StatusCode: {(int)response.StatusCode}, Body: {body}");
        
        var token = JsonSerializer.Deserialize<PayPalTokenResponse>(
            body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("PayPal token response was invalid.");
        
        logger.LogDebug("PayPal token obtained, expires in {Seconds}s", token.ExpiresIn);
        return token.AccessToken;
    }
 
    // Créer un Order 
 
    public async Task<PayPalOrderResponse> CreateOrderAsync(
        PayPalOrderRequest body,
        string             accessToken,
        bool               isLive,
        string             idempotencyKey,
        CancellationToken  ct)
    {
         
        logger.LogInformation("PayPal payload: {Payload}", JsonSerializer.Serialize(body, JsonOpts));
        
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.GetBaseUrl(isLive)}v2/checkout/orders");
 
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
 
        // Idempotence PayPal — clé unique par tentative
        request.Headers.Add("PayPal-Request-Id", idempotencyKey);
        request.Headers.Add("Prefer", "return=representation");
 
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOpts), 
            Encoding.UTF8, 
            "application/json");
 
        var response = await http.SendAsync(request, ct);
 
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "PayPal CreateOrder failed [{Status}]: {Body}",
                response.StatusCode, raw);
            response.EnsureSuccessStatusCode();
        }
 
        return await response.Content.ReadFromJsonAsync<PayPalOrderResponse>(ct)
            ?? throw new InvalidOperationException("PayPal order response was null.");
    }
    
    // ── Capturer un Order approuvé ────────────────────────────

    public async Task<PayPalOrderResponse> CaptureOrderAsync(string orderId, string accessToken, bool isLive, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{_config.GetBaseUrl(isLive)}v2/checkout/orders/{orderId}/capture"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "return=representation");

        // Body vide requis par PayPal pour la capture
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "PayPal CaptureOrder failed [{Status}] orderId={OrderId}: {Body}",
                response.StatusCode, orderId, raw);
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<PayPalOrderResponse>(ct)
               ?? throw new InvalidOperationException("PayPal capture response was null.");
    }


    // Statut d'un Order 
    public async Task<PayPalOrderResponse> GetOrderAsync(string orderId, string accessToken, bool isLive, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.GetBaseUrl(isLive)}v2/checkout/orders/{orderId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
 
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
 
        return await response.Content.ReadFromJsonAsync<PayPalOrderResponse>(ct)
            ?? throw new InvalidOperationException("PayPal order status response was null.");
    }
}
