using System.Reflection;
using JUST.net.Selectables;
using Newtonsoft.Json.Linq;

public interface IContext{
    char SplitGroupChar { get; }
    int DefaultDecimalPlaces { get; }
    bool IsStrictMode();
    bool IsRegisteredCustomFunction(string aliasOrName);
    MethodInfo GetCustomMethod(string key);
    T Resolve<T>(JToken token) where T: ISelectableToken;
}