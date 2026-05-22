namespace AfriPay.Application.Notifications;

public sealed class SendGridEmailSender(
    IConfiguration                configuration,
    ILogger<SendGridEmailSender>  logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        var from   = configuration["SendGrid:FromEmail"] ?? "noreply@afripay.io";
        var name   = configuration["SendGrid:FromName"]  ?? "AfriPay";
 
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning(
                "SendGrid API key not configured — email not sent to {To}: {Subject}",
                message.To, message.Subject);
            return;
        }
 
        using var http    = new System.Net.Http.HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
 
        var payload = new
        {
            personalizations = new[]
            {
                new { to = new[] { new { email = message.To } } },
            },
            from    = new { email = from, name },
            subject = message.Subject,
            content = new[]
            {
                new { type = "text/html",  value = message.HtmlBody },
                new { type = "text/plain", value = message.TextBody ?? message.Subject },
            },
        };
 
        var response = await http.PostAsJsonAsync(
            "https://api.sendgrid.com/v3/mail/send", payload, ct);
 
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "SendGrid failed [{Status}]: {Body}", response.StatusCode, body);
        }
        else
        {
            logger.LogInformation("Email sent to {To}: {Subject}", message.To, message.Subject);
        }
    }
}