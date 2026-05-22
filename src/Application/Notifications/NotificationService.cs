using AfriPay.Application.Notifications.Dtos;

namespace AfriPay.Application.Notifications;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public interface INotificationService
{
    Task SendPaymentReceivedAsync(PaymentNotificationDto dto, CancellationToken ct = default);
    Task SendPaymentFailedAsync(PaymentNotificationDto dto, CancellationToken ct = default);
    Task SendRefundIssuedAsync(RefundNotificationDto dto, CancellationToken ct = default);
    Task SendPayoutCompletedAsync(PayoutNotificationDto dto, CancellationToken ct = default);
    Task SendPayoutFailedAsync(PayoutNotificationDto dto, CancellationToken ct = default);
    Task SendKybApprovedAsync(KybNotificationDto dto, CancellationToken ct = default);
    Task SendKybRejectedAsync(KybNotificationDto dto, CancellationToken ct = default);
}

public sealed class NotificationService(IEmailSender email) : INotificationService
{
    public Task SendPaymentReceivedAsync(
        PaymentNotificationDto dto, CancellationToken ct = default)
        => email.SendAsync(new EmailMessage(
                To: dto.MerchantEmail,
                Subject: $"✅ Paiement reçu — {FormatAmount(dto.Amount, dto.Currency)}",
                HtmlBody: PaymentReceivedHtml(dto),
                TextBody:
                $"Paiement {dto.PaymentId} de {FormatAmount(dto.Amount, dto.Currency)} reçu via {dto.Provider}."),
            ct);

    public Task SendPaymentFailedAsync(
        PaymentNotificationDto dto, CancellationToken ct = default)
        => email.SendAsync(new EmailMessage(
                To: dto.MerchantEmail,
                Subject: $"❌ Paiement échoué — {FormatAmount(dto.Amount, dto.Currency)}",
                HtmlBody: PaymentFailedHtml(dto)),
            ct);

    public Task SendRefundIssuedAsync(
        RefundNotificationDto dto, CancellationToken ct = default)
        => email.SendAsync(new EmailMessage(
                To: dto.MerchantEmail,
                Subject: $"↩️ Remboursement émis — {FormatAmount(dto.Amount, dto.Currency)}",
                HtmlBody: RefundHtml(dto)),
            ct);

    public Task SendPayoutCompletedAsync(
        PayoutNotificationDto dto, CancellationToken ct = default)
        => email.SendAsync(new EmailMessage(
                To: dto.MerchantEmail,
                Subject: $"💸 Virement effectué — {FormatAmount(dto.Amount, dto.Currency)}",
                HtmlBody: PayoutCompletedHtml(dto)),
            ct);

    public Task SendPayoutFailedAsync(
        PayoutNotificationDto dto, CancellationToken ct = default)
        => email.SendAsync(new EmailMessage(
                To: dto.MerchantEmail,
                Subject: $"⚠️ Échec du virement — {FormatAmount(dto.Amount, dto.Currency)}",
                HtmlBody: PayoutFailedHtml(dto)),
            ct);

    public Task SendKybApprovedAsync(
        KybNotificationDto dto, CancellationToken ct = default)
        => email.SendAsync(new EmailMessage(
                To: dto.MerchantEmail,
                Subject: "🎉 Votre compte AfriPay est vérifié — Accès live activé",
                HtmlBody: KybApprovedHtml(dto)),
            ct);

    public Task SendKybRejectedAsync(
        KybNotificationDto dto, CancellationToken ct = default)
        => email.SendAsync(new EmailMessage(
                To: dto.MerchantEmail,
                Subject: "ℹ️ Vérification de compte — Action requise",
                HtmlBody: KybRejectedHtml(dto)),
            ct);

    // ── Templates HTML ─────────────────────────────────────────

    private static string FormatAmount(long amount, string currency)
        => currency.ToUpperInvariant() switch
        {
            "XOF" or "XAF" => $"{amount:N0} {currency}",
            _ => $"{amount / 100m:F2} {currency}",
        };

    private static string BaseLayout(string title, string body) => $$"""
                                                                     <!DOCTYPE html>
                                                                     <html lang="fr">
                                                                     <head>
                                                                       <meta charset="UTF-8">
                                                                       <style>
                                                                         body { font-family: 'Helvetica Neue', Arial, sans-serif; background: #F5F0E8; margin: 0; padding: 20px; }
                                                                         .container { max-width: 560px; margin: 0 auto; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,.08); }
                                                                         .header { background: #1A1510; padding: 24px; text-align: center; }
                                                                         .header h1 { color: #C94F1A; font-family: Georgia, serif; font-style: italic; margin: 0; font-size: 24px; }
                                                                         .content { padding: 28px 32px; }
                                                                         .amount { font-size: 32px; font-weight: 800; color: #1A1510; letter-spacing: -1px; }
                                                                         .detail { background: #F5F0E8; border-radius: 8px; padding: 14px 18px; margin: 16px 0; font-family: monospace; font-size: 13px; }
                                                                         .detail span { color: #6B5F52; }
                                                                         .cta { display: inline-block; background: #C94F1A; color: #fff; padding: 12px 24px; border-radius: 8px; text-decoration: none; font-weight: 600; margin: 16px 0; }
                                                                         .footer { padding: 16px 32px; background: #F5F0E8; font-size: 11px; color: #A89880; text-align: center; }
                                                                       </style>
                                                                     </head>
                                                                     <body>
                                                                       <div class="container">
                                                                         <div class="header"><h1>AfriPay</h1></div>
                                                                         <div class="content">
                                                                           <h2>{{title}}</h2>
                                                                           {{body}}
                                                                         </div>
                                                                         <div class="footer">AfriPay · support@afripay.io · <a href="https://afripay.io">afripay.io</a></div>
                                                                       </div>
                                                                     </body>
                                                                     </html>
                                                                     """;

    private static string PaymentReceivedHtml(PaymentNotificationDto d) => BaseLayout(
        "Nouveau paiement reçu",
        $"""
         <p>Bonjour <strong>{d.MerchantName}</strong>,</p>
         <p>Un paiement a été reçu sur votre compte.</p>
         <div class="amount">{FormatAmount(d.Amount, d.Currency)}</div>
         <div class="detail">
           <span>ID Paiement :</span> {d.PaymentId}<br>
           <span>Provider :</span> {d.Provider}<br>
           <span>Date :</span> {d.OccurredAt:dd/MM/yyyy HH:mm} UTC
         </div>
         <a href="https://dashboard.afripay.io/payments/{d.PaymentId}" class="cta">Voir le paiement →</a>
         """);

    private static string PaymentFailedHtml(PaymentNotificationDto d) => BaseLayout(
        "Paiement échoué",
        $"""
         <p>Bonjour <strong>{d.MerchantName}</strong>,</p>
         <p>Un paiement de <strong>{FormatAmount(d.Amount, d.Currency)}</strong> via {d.Provider} a échoué.</p>
         <div class="detail"><span>ID :</span> {d.PaymentId}</div>
         <p>Le client peut réessayer. Aucun montant n'a été débité.</p>
         """);

    private static string RefundHtml(RefundNotificationDto d) => BaseLayout(
        "Remboursement émis",
        $"""
         <p>Bonjour <strong>{d.MerchantName}</strong>,</p>
         <p>Un remboursement de <strong>{FormatAmount(d.Amount, d.Currency)}</strong> a été émis.</p>
         <div class="detail">
           <span>ID Remboursement :</span> {d.RefundId}<br>
           <span>Paiement original :</span> {d.PaymentId}<br>
           <span>Motif :</span> {d.Reason}
         </div>
         """);

    private static string PayoutCompletedHtml(PayoutNotificationDto d) => BaseLayout(
        "Virement effectué ✅",
        $"""
         <p>Bonjour <strong>{d.MerchantName}</strong>,</p>
         <p>Votre virement de <strong>{FormatAmount(d.Amount, d.Currency)}</strong> a été effectué avec succès.</p>
         <div class="detail">
           <span>ID Virement :</span> {d.PayoutId}<br>
           <span>Méthode :</span> {d.Method}<br>
           <span>Référence :</span> {d.ProviderReference ?? "N/A"}
         </div>
         """);

    private static string PayoutFailedHtml(PayoutNotificationDto d) => BaseLayout(
        "Échec du virement ⚠️",
        $"""
         <p>Bonjour <strong>{d.MerchantName}</strong>,</p>
         <p>Votre virement de <strong>{FormatAmount(d.Amount, d.Currency)}</strong> a échoué.</p>
         <div class="detail">
           <span>Raison :</span> {d.FailureReason ?? "Erreur inconnue"}
         </div>
         <p>Votre solde a été récrédité. Veuillez réessayer ou contacter le support.</p>
         <a href="mailto:support@afripay.io" class="cta">Contacter le support →</a>
         """);

    private static string KybApprovedHtml(KybNotificationDto d) => BaseLayout(
        "Compte vérifié — Accès live activé 🎉",
        $"""
         <p>Bonjour <strong>{d.MerchantName}</strong>,</p>
         <p>Votre dossier KYB a été <strong>approuvé</strong>. Vous pouvez maintenant accepter des paiements en production avec votre clé <code>afp_live_sk_...</code>.</p>
         <a href="https://dashboard.afripay.io/api-keys" class="cta">Accéder à mes clés live →</a>
         """);

    private static string KybRejectedHtml(KybNotificationDto d) => BaseLayout(
        "Vérification de compte — Action requise",
        $"""
         <p>Bonjour <strong>{d.MerchantName}</strong>,</p>
         <p>Votre dossier KYB n'a pas pu être validé pour la raison suivante :</p>
         <div class="detail">{d.Note ?? "Documents non conformes."}</div>
         <p>Vous pouvez soumettre un nouveau dossier après avoir corrigé les points mentionnés.</p>
         <a href="https://dashboard.afripay.io/kyb" class="cta">Mettre à jour mon dossier →</a>
         """);
}