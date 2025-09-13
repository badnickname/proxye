using Microsoft.Extensions.DependencyInjection;
using Proxye.Rules.Models;

namespace Proxye.Rules;

public static class RulesExtensions
{
    /// <summary>
    ///     Add rules service
    /// </summary>
    public static IServiceCollection AddRules(this IServiceCollection services)
        => services
            .AddSingleton<IRules, Rules>()
            .AddOptions<List<Rule>>().Services;
}