using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Behaviors;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.SearchRadius;
using PawTrack.Application.Sightings.Scoring;

namespace PawTrack.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<ISightingPriorityScorer, SightingPriorityScorer>();
        services.AddScoped<ILostPetSearchRadiusCalculator, LostPetSearchRadiusCalculator>();

        return services;
    }
}
