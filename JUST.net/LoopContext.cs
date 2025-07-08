using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class LoopContext
{
    public IDictionary<string, JArray> ParentArray { get; private set; }
    public IDictionary<string, JToken> CurrentArrayElement { get; private set; }
    public bool IsObject { get; set; }

    public LoopContext(IDictionary<string, JArray> parentArray, IDictionary<string, JToken> currentArrayElement)
    {
        this.ParentArray = parentArray ?? new Dictionary<string, JArray>();
        this.CurrentArrayElement = currentArrayElement ?? new Dictionary<string, JToken>();
    }
}