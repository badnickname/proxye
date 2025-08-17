using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Proxye.Collections;
using Proxye.Models;

namespace Proxye.Rules;

public interface IProxyeRules
{
    void BindIp(string host, string ip, TimeSpan? timeout = null);

    ProxyeRule? Match(string host);
}

internal sealed class ProxyeRules(IOptions<ProxyeOptions> options) : IProxyeRules
{
    private static readonly BoundedDictionary<string, ProxyeRule?> Map = new(5000);
    private static readonly ConcurrentDictionary<ProxyeRule, Regex> Regex = new();
    private static DomainTree<ProxyeRule>? _hostTree;
    private static ProxyeRule[]? _regexRules;

    public void BindIp(string host, string ip, TimeSpan? timeout = null)
    {
        var rule = Match(host);
        if (rule is null) return;
        rule.Domains = [..rule.Domains ?? [], ip];

        // todo fix it
        _hostTree = null;
        _regexRules = null;
        Map.Clear();
        Regex.Clear();
    }

    public ProxyeRule? Match(string host)
    {
        if (Map.TryGetValue(host, out var result)) return result;

        EnsureHostTreeExist(options.Value);
        EnsureRegexRulesExist(options.Value);

        if (_hostTree!.TryGetValue(host, out result))
        {
            Map.Add(host, result);
            return result;
        }

        foreach (var rule in _regexRules!)
        {
            var regex = Regex.TryGetValue(rule, out var r) ? r : new Regex(rule.Pattern!);
            Regex.TryAdd(rule, regex);
            if (!regex.IsMatch(host)) continue;
            Map.Add(host, rule);
            return rule;
        }

        Map.Add(host, null);

        return null;
    }

    private static void EnsureRegexRulesExist(ProxyeOptions options)
    {
        if (_regexRules is not null) return;

        _regexRules = options.Rules.Where(x => x.Pattern is not null).ToArray();
    }

    private static void EnsureHostTreeExist(ProxyeOptions options)
    {
        if (_hostTree is not null) return;

        _hostTree = new DomainTree<ProxyeRule>();
        foreach (var rule in options.Rules)
        {
            if (rule.Domains is null) continue;
            foreach (var domain in rule.Domains) _hostTree.Add(domain, rule);
        }
    }
}