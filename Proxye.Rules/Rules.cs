using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Proxye.Rules.Collections;
using Proxye.Rules.Models;

namespace Proxye.Rules;

/// <summary>
///     Rules service for matching host to rule
/// </summary>
public interface IRules
{
    /// <summary>
    ///     Bind ip to rule matched by host
    /// </summary>
    void BindIp(string host, string ip, TimeSpan? timeout = null);

    /// <summary>
    ///     Get rule matched by host
    /// </summary>
    Rule? Match(string host);
}

internal sealed class Rules(IOptions<List<Rule>> options) : IRules
{
    private readonly BoundedDictionary<string, Rule?> _map = new(5000);
    private readonly ConcurrentDictionary<Rule, Regex> _regex = new();
    private DomainTree<Rule>? _hostTree;
    private Rule[]? _regexRules;

    public void BindIp(string host, string ip, TimeSpan? timeout = null)
    {
        var rule = Match(host);
        if (rule is null) return;
        rule.Domains = [..rule.Domains ?? [], ip];

        // TODO fix it
        _hostTree = null;
        _regexRules = null;
        _map.Clear();
        _regex.Clear();
    }

    public Rule? Match(string host)
    {
        if (_map.TryGetValue(host, out var result)) return result;

        EnsureHostTreeExist(options.Value);
        EnsureRegexRulesExist(options.Value);

        if (_hostTree!.TryGetValue(host, out result))
        {
            _map.Add(host, result);
            return result;
        }

        foreach (var rule in _regexRules!)
        {
            var regex = _regex.TryGetValue(rule, out var r) ? r : new Regex(rule.Pattern!);
            _regex.TryAdd(rule, regex);
            if (!regex.IsMatch(host)) continue;
            _map.Add(host, rule);
            return rule;
        }

        _map.Add(host, null);

        return null;
    }

    private void EnsureRegexRulesExist(List<Rule> rules)
    {
        if (_regexRules is not null) return;

        _regexRules = rules.Where(x => x.Pattern is not null).ToArray();
    }

    private void EnsureHostTreeExist(List<Rule> rules)
    {
        if (_hostTree is not null) return;

        _hostTree = new DomainTree<Rule>();
        foreach (var rule in rules)
        {
            if (rule.Domains is null) continue;
            foreach (var domain in rule.Domains) _hostTree.Add(domain, rule);
        }
    }
}