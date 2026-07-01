using FluentValidation;
using Jose;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordWise.Application.Common.Behaviours;
using WordWise.Application.Common.Settings;
using JwtSettings = WordWise.Application.Common.Settings.JwtSettings;

namespace WordWise.Application
{
    public static class AppServiceRegistration
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection services,IConfiguration _configuration)
        {

            services.Configure<JwtSettings>(
                _configuration.GetSection(JwtSettings.SectionName));

            services.Configure<YoutubeSettings>(
                _configuration.GetSection(YoutubeSettings.SectionName));

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(AppServiceRegistration).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            });

            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(AppServiceRegistration).Assembly));
            services.AddValidatorsFromAssembly(typeof(AppServiceRegistration).Assembly);



            return services;
        }
    }
}
 