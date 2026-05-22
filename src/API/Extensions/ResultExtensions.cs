
using AfriPay.Application.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Extensions;

/// <summary>
/// Extensions pour convertir les <see cref="Result{T}"/> de la couche Application
/// en <see cref="IActionResult"/> HTTP appropriés.
///
/// Centralise le mapping code d'erreur → status HTTP au niveau API.
/// Les controllers n'ont jamais à écrire de switch sur les erreurs.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Convertit un <see cref="Result{T}"/> en <see cref="IActionResult"/>.
    /// </summary>
    /// <param name="result">Le résultat à convertir.</param>
    /// <param name="successStatusCode">
    ///   Code HTTP en cas de succès (200 par défaut, 201 pour les créations).
    /// </param>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        int            successStatusCode = 200)
        => result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = successStatusCode }
            : result.Error!.ToActionResult();

    /// <summary>
    /// Convertit un <see cref="AppError"/> en <see cref="IActionResult"/>
    /// en respectant le status code défini dans l'erreur.
    /// </summary>
    /// <summary>
    /// Convertit un <see cref="AppError"/> en <see cref="IActionResult"/>
    /// en respectant le status code défini dans l'erreur.
    /// </summary>
    public static IActionResult ToActionResult(this AppError error)
    {
        var body   = new ErrorResponse(error.Code, error.Message, error.StatusCode);
        var result = new ObjectResult(body);
        result.StatusCode = error.StatusCode;
        return result;
    }
}

/// <summary>
/// Corps JSON standardisé pour toutes les réponses d'erreur AfriPay.
///
/// Format :
/// <code>
/// {
///   "code":    "PAYMENT_NOT_FOUND",
///   "message": "Payment '...' not found.",
///   "status":  404,
///   "doc_url": "https://docs.afripay.io/errors#PAYMENT_NOT_FOUND"
/// }
/// </code>
/// </summary>
public sealed record ErrorResponse(
    string Code,
    string Message,
    int    Status)
{
    /// <summary>Lien vers la documentation de l'erreur (généré automatiquement).</summary>
    public string DocUrl =>
        $"https://docs.afripay.io/errors#{Code.ToLowerInvariant().Replace('_', '-')}";
}