using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AfriPay.Infrastructure.Providers.MtnMomo;

public sealed record MtnAccessTokenResponse(string access_token, string token_type, int    expires_in);
 
public sealed record MtnRequestToPayBody(
    long         amount,
    string       currency,
    string       externalId,
    MtnParty     payer,
    string       payerMessage,
    string       payeeNote);
 
public sealed record MtnParty(string partyIdType, string partyId);
 
public sealed record MtnPaymentStatusResponse(
    string?  financialTransactionId,
    string   externalId,
    long     amount,
    string   currency,
    string   status, 
    string?  reason);
public sealed class MtnMomoClient(HttpClient http, IOptions<MtnMomoConfig> options, ILogger<MtnMomoClient> logger)
{
    private readonly MtnMomoConfig _config = options.Value;
 
    public async Task<string> GetAccessTokenAsync(bool isLive,CancellationToken ct)
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                $"{_config.GetApiUserId(isLive)}:{_config.GetApiKey(isLive)}"));
 
        using var request = new HttpRequestMessage(HttpMethod.Post, "collection/token/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _config.GetSubscriptionKey(isLive));
 
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
 
        var token = await response.Content
            .ReadFromJsonAsync<MtnAccessTokenResponse>(ct)
            ?? throw new InvalidOperationException("MTN token response was null.");
 
        logger.LogDebug("MTN token obtained, expires in {Seconds}s", token.expires_in);
        return token.access_token;
    }
 
    public async Task RequestToPayAsync(string referenceId, MtnRequestToPayBody body, string  accessToken, bool isLive, CancellationToken   ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "collection/v1_0/requesttopay");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Ocp-Apim-Subscription-Key",  _config.GetSubscriptionKey(isLive));
        request.Headers.Add("X-Reference-Id",      referenceId);
        request.Headers.Add("X-Target-Environment",  _config.GetTargetEnvironment(isLive));
 
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
 
        var response = await http.SendAsync(request, ct);
 
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "MTN RequestToPay failed [{Status}]: {Body}",
                response.StatusCode, raw);
            response.EnsureSuccessStatusCode();
        }
    }
 
    public async Task<MtnPaymentStatusResponse> GetTransactionStatusAsync(string referenceId, string accessToken,bool isLive, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"collection/v1_0/requesttopay/{referenceId}");
 
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _config.GetSubscriptionKey(isLive));
        request.Headers.Add("X-Target-Environment", _config.GetTargetEnvironment(isLive));
 
        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
 
        return await response.Content
            .ReadFromJsonAsync<MtnPaymentStatusResponse>(ct)
            ?? throw new InvalidOperationException("MTN status response was null.");
    }
}