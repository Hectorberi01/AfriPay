using System.Text;
using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Auth.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AfriPay.Application.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
 
        var issuer   = configuration["Jwt:Issuer"]   ?? "AfriPay";
        var audience = configuration["Jwt:Audience"] ?? "AfriPay";
 
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken            = false;
 
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secret)),
 
                    ValidateIssuer   = true,
                    ValidIssuer      = issuer,
 
                    ValidateAudience = true,
                    ValidAudience    = audience,
 
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.FromSeconds(30),
                    RoleClaimType            = System.Security.Claims.ClaimTypes.Role,
                };
 
                // Messages d'erreur clairs
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode  = 401;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsync(
                            """{"code":"UNAUTHORIZED","message":"Authentication required.","status":401}""");
                    },
                    OnForbidden = ctx =>
                    {
                        ctx.Response.StatusCode  = 403;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsync(
                            """{"code":"FORBIDDEN","message":"Insufficient permissions.","status":403}""");
                    },
                };
            });
 
        // Politiques d'autorisation
        services.AddAuthorization(options =>
        {
            // Accès admin AfriPay
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("admin"));
 
            // Accès marchand authentifié
            options.AddPolicy("MerchantOnly", policy =>
                policy.RequireRole("merchant", "admin"));
 
            // Tout utilisateur authentifié
            options.AddPolicy("Authenticated", policy =>
                policy.RequireAuthenticatedUser());
 
            // Staff AfriPay (tout rôle staff:*)
            options.AddPolicy("StaffOnly", policy =>
                policy.RequireClaim(
                    System.Security.Claims.ClaimTypes.Role,
                    "staff:superadmin", "staff:admin",
                    "staff:finance", "staff:support", "staff:developer"));
 
            // SuperAdmin uniquement
            options.AddPolicy("SuperAdminOnly", policy =>
                policy.RequireClaim(
                    System.Security.Claims.ClaimTypes.Role,
                    "staff:superadmin"));
 
            // Employé marchand (tout rôle employee:*)
            options.AddPolicy("EmployeeOrOwner", policy =>
                policy.RequireAuthenticatedUser());
        });
 
        // Services auth
        services.AddScoped<ITokenService,    JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<IAuthService, AuthService>();
 
        return services;
    }
}