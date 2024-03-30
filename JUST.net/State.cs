using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

internal struct LevelKey 
{
    internal int Level;
    internal string Key;
}

internal sealed class State
{
    internal State(JToken transformer, JToken input, int levelCounter)
    {
        Transformer = transformer;
        ParentArray = new Dictionary<LevelKey, JArray>();
        CurrentArrayToken = new Dictionary<LevelKey, JToken> { { new LevelKey { Level = levelCounter, Key = "root"}, input } };
        CurrentScopeToken = new Dictionary<LevelKey, JToken> { { new LevelKey { Level = levelCounter, Key = "root"}, input } };
    }
    internal JToken Transformer { get; private set; }
    internal IDictionary<LevelKey, JArray> ParentArray { get; private set; }
    internal IDictionary<LevelKey, JToken> CurrentArrayToken { get; private set; }
    internal IDictionary<LevelKey, JToken> CurrentScopeToken { get; private set; }

    internal string GetHigherAlias()
    {
        if (IsArrayHigherThanScope())
        {
            return CurrentArrayToken.Last().Key.Key;
        }
        return CurrentScopeToken.Last().Key.Key;
    }

    internal JToken GetAliasToken(string alias)
    {
        IEnumerable<KeyValuePair<LevelKey, JToken>> arr = CurrentArrayToken.Where(a => a.Key.Key == alias);
        IEnumerable<KeyValuePair<LevelKey, JToken>> scope = CurrentScopeToken.Where(a => a.Key.Key == alias);

        int maxArr = arr.Any() ? arr.Max(a => a.Key.Level) : -1;
        int maxScope = scope.Any() ? scope.Max(a => a.Key.Level) : -1;

        if (maxArr > maxScope)
        {
            return arr.Single(a => a.Key.Level == maxArr).Value;
        }
        return scope.Single(a => a.Key.Level == maxScope).Value;
    }

    private bool IsArrayHigherThanScope()
    {
        return (CurrentArrayToken?.Max(k => k.Key.Level) ?? 0) > (CurrentScopeToken?.Max(k => k.Key.Level) ?? 0);
    }

    internal JToken GetInput()
    {
        return CurrentScopeToken.Single(s => s.Key.Key == "root").Value;
    }
}