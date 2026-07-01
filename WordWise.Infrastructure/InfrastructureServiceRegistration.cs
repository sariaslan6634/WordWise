using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WordWise.Application.Common.Interfaces;
using WordWise.Application.Common.Interfaces.Repositories;
using WordWise.Application.Common.Settings;
using WordWise.Infrastructure.Persistence;
using WordWise.Infrastructure.Persistence.Context;
using WordWise.Infrastructure.Services;
using WordWise.Infrastructure.Services.Caching;
using WordWise.Infrastructure.Services.Email;

namespace WordWise.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(
      this IServiceCollection services,
      IConfiguration configuration)
        {
            services.AddDbContext<WordWiseDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("SqlConnection")
                , sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)
                ));

            services.AddScoped<IWordWiseDbContext, WordWiseDbContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtService, JwtService>();

            var jwtsettings = configuration
                .GetSection(JwtSettings.SectionName)
                .Get<JwtSettings>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtsettings.Issuer,
                    ValidAudience = jwtsettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtsettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.Configure<EmailSettings>(
                configuration.GetSection(EmailSettings.SectionName)
                );
            services.AddHttpClient<IEmailSender, ResendEmailSender>();
            services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();


            services.AddMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();

            return services;
        }
    }
}
