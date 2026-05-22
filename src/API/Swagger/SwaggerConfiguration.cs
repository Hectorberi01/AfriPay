using AfriPay.API.Middleware;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AfriPay.API.Swagger;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddAfriPaySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            //Info
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AfriPay API",
                Version = "v1",
                Description = """
                              Passerelle de paiement multi-provider pour l'Afrique de l'Ouest.

                              ## Authentification

                              ### Paiements (API publique)
                              Toutes les requêtes de paiement nécessitent :
                              
                              - `X-API-Key: afp_test_sk_xxx` (sandbox)
                              - `X-API-Key: afp_live_sk_xxx` (production)
                              
                              ### Authentification utilisateur (dashboard)
                              
                              - `Authorization: Bearer <JWT>`
                              
                              ## Idempotence

                              Les endpoints de création (`POST /v1/payments/initiate`, `POST /v1/refunds`)
                              requièrent un header `Idempotency-Key: <uuid>`.

                              ## Erreurs

                              Toutes les erreurs suivent le format :
                              ```json
                              {
                                "code":    "PAYMENT_NOT_FOUND",
                                "message": "Payment 'xxx' not found.",
                                "status":  404,
                                "doc_url": "https://docs.afripay.io/errors#payment-not-found"
                              }
                              ```
                              """,
                Contact = new OpenApiContact
                {
                    Name = "AfriPay Support",
                    Email = "support@afripay.io",
                    Url = new Uri("https://docs.afripay.io"),
                },
                License = new OpenApiLicense
                {
                    Name = "Proprietary",
                    Url = new Uri("https://afripay.io/terms"),
                },
            });
            
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = """
                              Clé API AfriPay.

                              Sandbox : afp_test_sk_xxxxx
                              Production : afp_live_sk_xxxxx
                              """
            });

            c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "JWT utilisateur (dashboard uniquement)"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey",
                        },
                    },
                    Array.Empty<string>()
                },
            });

            //Header Idempotency-Key 
            c.OperationFilter<IdempotencyKeyHeaderFilter>();
            c.OperationFilter<SecurityByPathFilter>();

            //Tags ordonnés
            c.TagActionsBy(api =>
            {
                if (api.ActionDescriptor is ControllerActionDescriptor desc)
                    return [desc.ControllerName];
                return ["Other"];
            });

            c.OrderActionsBy(api => $"{api.GroupName}_{api.HttpMethod}_{api.RelativePath}");

            // Descriptions de chaque groupe d'endpoints
            c.DocumentFilter<TagDescriptionsDocumentFilter>();

            var xmlFile = $"{typeof(SwaggerConfiguration).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        });

        return services;
    }

    public static WebApplication UseAfriPayScalar(this WebApplication app)
    {
        // Swashbuckle génère le JSON OpenAPI à /swagger/v1/swagger.json
        app.UseSwagger();

        // Scalar UI — documentation professionnelle + testeur intégré
        app.MapScalarApiReference("/docs", options =>
        {
            options
                .WithTitle("AfriPay API — Documentation")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithOpenApiRoutePattern("/swagger/v1/swagger.json")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                .AddPreferredSecuritySchemes(["ApiKey"])
                .WithCustomCss(AfriPayCss);
        });

        return app;
    }

    private const string AfriPayCss = """
        /* ── AfriPay Brand Overrides on DeepSpace ── */
        :root {
            --scalar-color-accent:       #10B981;
            --scalar-color-green:        #10B981;
            --scalar-button-1:           #10B981;
            --scalar-button-1-hover:     #059669;
            --scalar-button-1-color:     #ffffff;
        }

        /* Logo / titre dans la sidebar */
        .sidebar-heading {
            font-size: 1.1rem;
            font-weight: 700;
            letter-spacing: .04em;
            color: #10B981 !important;
        }

        /* Badge "Send" (bouton test) */
        .send-request-button {
            background: linear-gradient(135deg, #10B981, #059669) !important;
            border: none !important;
        }

        /* En-tête de section */
        .section-header {
            border-left: 3px solid #10B981;
            padding-left: .5rem;
        }
        """;
}

// FILTRE DOCUMENT : descriptions des tags (rubriques)

public sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Payments"] =
            "Initier, suivre et annuler des paiements multi-fournisseurs (MTN MoMo, PayPal, Stripe, Wave). " +
            "Chaque opération d'écriture requiert un header `Idempotency-Key` pour éviter les doublons en cas de retry réseau.",

        ["Refunds"] =
            "Rembourser tout ou partie d'un paiement `Completed`. " +
            "Le total des remboursements ne peut pas dépasser le montant initial du paiement.",

        ["Merchants"] =
            "Gérer le compte marchand : consultation du profil, mise à jour de la configuration webhook " +
            "et régénération des clés API Live / Sandbox.",

        ["Auth"] =
            "Authentification des marchands par JWT. Endpoints : inscription, connexion, rafraîchissement " +
            "du token d'accès (15 min), déconnexion et changement de mot de passe.",

        ["Kyb"] =
            "Know Your Business — vérification réglementaire de l'identité de l'entreprise. " +
            "Workflow : initialisation → informations métier → upload documents → soumission → validation admin. " +
            "L'accès aux clés API Live est conditionné à l'approbation KYB.",

        ["Balance"] =
            "Consulter le solde disponible, en attente et réservé par devise, ainsi que l'historique " +
            "complet des entrées du grand livre (paiements crédités, frais débités, remboursements, virements).",

        ["Payouts"] =
            "Demander des virements de votre solde disponible vers un compte bancaire ou un portefeuille " +
            "mobile. Supporte les virements manuels, quotidiens, hebdomadaires et mensuels.",

        ["Webhooks"] =
            "Consulter l'historique des livraisons webhook et relancer manuellement les livraisons échouées. " +
            "Les webhooks sont signés HMAC-SHA256 et livrés avec un backoff exponentiel (7 tentatives sur 24h).",

        ["Currency"] =
            "Obtenir les taux de change en temps réel et convertir des montants entre devises africaines " +
            "et internationales (XOF, GHS, KES, USD, EUR, GBP…).",

        ["Disputes"] =
            "Gérer les contestations et chargebacks. Ouvrir un litige sur un paiement, soumettre des preuves " +
            "dans la fenêtre de 7 jours et suivre la résolution (Gagné / Perdu).",

        ["Subscriptions"] =
            "Créer des plans de facturation récurrente (journalier, hebdomadaire, mensuel) et gérer " +
            "les abonnements clients avec renouvellement automatique et gestion des échecs de paiement.",

        ["Team"] =
            "Gérer les membres de l'équipe marchande. Rôles disponibles : Owner (propriétaire), " +
            "Admin (accès complet) et Viewer (lecture seule). Suspension et réactivation incluses.",

        ["Admin"] =
            "Opérations réservées aux administrateurs internes AfriPay : supervision des marchands, " +
            "gestion des changements de plan tarifaire et validation / rejet des dossiers KYB.",

        ["Analytics"] =
            "Tableaux de bord analytiques : volume total des transactions, montant des frais collectés, " +
            "taux de succès des paiements et tendances sur une période sélectionnée.",

        ["Audit"] =
            "Journal d'audit immuable de toutes les modifications d'entités du compte marchand. " +
            "Chaque action (création, mise à jour, changement de statut) est tracée avec horodatage et acteur.",

        ["EmployeeAuth"] =
            "Authentification des employés marchands (membres de l'équipe). Retourne un JWT scopé " +
            "aux permissions du rôle de l'employé (Owner, Admin, Viewer).",

        ["StaffAuth"] =
            "Authentification du personnel interne AfriPay. Donne accès aux fonctionnalités d'administration " +
            "selon le rôle staff (SuperAdmin, Admin, Auditor, Support).",

        ["StaffManagement"] =
            "Gestion du personnel interne AfriPay : création de comptes staff, suspension et réactivation. " +
            "Réservé aux SuperAdmins.",
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags ??= [];
        foreach (var (name, description) in Descriptions)
        {
            var existing = swaggerDoc.Tags.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
                existing.Description = description;
            else
                swaggerDoc.Tags.Add(new OpenApiTag { Name = name, Description = description });
        }
    }
}

// FILTRE OPÉRATION : Idempotency-Key header

/// <summary>
/// Ajoute automatiquement le paramètre Idempotency-Key dans Swagger
/// pour les endpoints POST qui le requièrent.
/// </summary>
public sealed class IdempotencyKeyHeaderFilter : IOperationFilter
{
    private static readonly HashSet<string> IdempotentPaths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "/v1/payments/initiate",
            "/v1/refunds",
        };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        var method = context.ApiDescription.HttpMethod ?? "";

        if (!method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            return;
        


        if (!IdempotentPaths.Any(p =>
                ("/" + path).StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "UUID v4 unique par requête. Garantit qu'un retry réseau ne crée pas deux transactions.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid",
                Example = new Microsoft.OpenApi.Any.OpenApiString(Guid.NewGuid().ToString()),
            },
        });
    }
}