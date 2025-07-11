using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using JUST.net.Selectables;
using CSharpParserGenerator;

namespace JUST
{
    public class JsonTransformer : JsonTransformer<JsonPathSelectable>, IDisposable
    {
        public JsonTransformer(JUSTContext context = null) : base(context)
        {
        }

    }

    public class JsonTransformer<T> : Transformer<T>, IDisposable where T : ISelectableToken
    {
        private const string RootAlias = "root";
        protected Gramar.Grammar<T> Grammar;

        public JsonTransformer(JUSTContext context = null) : base(context)
        {
            this.Grammar = Gramar.Grammar<T>.GetInstance();
        }
        public string Transform(string transformerJson, string inputJson)
        {
            return Transform(transformerJson, DeserializeWithoutDateParse<JToken>(inputJson));
        }

        private static string SerializeWithoutDateParse<U>(U obj)
        {
            var settings = new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None };
            return JsonConvert.SerializeObject(obj, settings);
        }

        private static U DeserializeWithoutDateParse<U>(string inputJson)
        {
            var settings = new JsonSerializerSettings() { DateParseHandling = DateParseHandling.None };
            return JsonConvert.DeserializeObject<U>(inputJson, settings);
        }

        public string Transform(string transformerJson, JToken input)
        {
            JToken result;
            JToken transformerToken = DeserializeWithoutDateParse<JToken>(transformerJson);
            switch (transformerToken.Type)
            {
                case JTokenType.Object:
                    result = Transform(transformerToken as JObject, input);
                    break;
                case JTokenType.Array:
                    result = Transform(transformerToken as JArray, input);
                    break;
                default:
                    result = TransformValue(transformerToken, input);
                    break;
            }
            string output = SerializeWithoutDateParse(result);
            return output;
        }

        public JArray Transform(JArray transformerArray, string input)
        {
            return Transform(transformerArray, DeserializeWithoutDateParse<JToken>(input));
        }

        public JArray Transform(JArray transformerArray, JToken input)
        {
            var result = new JArray();
            var nr = transformerArray.Count;
            for (int i = 0; i < nr; i++)
            {
                var transformer = transformerArray[i];
                if (transformer.Type == JTokenType.Object)
                {
                    var t = Transform(transformer as JObject, input);
                    result.Add(t);
                }
                else
                {
                    var token = TransformValue(transformer, input);
                    foreach (var item in token)
                    {
                        result.Add(item);
                    }
                }

                if (Context.IsJoinArraysMode())
                {
                    JoinArrays(result);
                }
            }
            return result;
        }

        private static void JoinArrays(JArray result)
        {
            bool join = true;
            int child = 0;
            while (join && result.Children().Count() > child)
            {
                var item = result[child];
                if (item is JArray arr)
                {
                    foreach (var elem in arr)
                    {
                        result.Add(elem);
                    }
                    if (!result.Remove(arr))
                    {
                        join = false;
                    }
                }
                else child++;
            }
        }

        private JToken TransformValue(JToken transformer, JToken input)
        {
            var tmp = new JObject
            {
                { RootAlias, transformer }
            };
            Transform(tmp, input);
            return tmp[RootAlias];
        }

        public JToken Transform(JObject transformer, string input)
        {
            return Transform(transformer, DeserializeWithoutDateParse<JToken>(input));
        }

        public JToken Transform(JObject transformer, JToken input)
        {
            var parentToken = (JToken)transformer;
            State state = new State()
            {
                CurrentArrayToken = new Dictionary<LevelKey, JToken> { { new LevelKey { Level = _levelCounter, Key = "root"}, input } },
                CurrentScopeToken = new Dictionary<LevelKey, JToken> { { new LevelKey { Level = _levelCounter, Key = "root"}, input } }
            };
            RecursiveEvaluate(ref parentToken, state, input);
            return parentToken;
        }

        #region RecursiveEvaluate


        private void RecursiveEvaluate(ref JToken parentToken, State state, JToken input)
        {
            if (parentToken == null)
            {
                return;
            }

            JEnumerable<JToken> tokens = parentToken.Children();

            TransformHelper helper = new TransformHelper();
            for (int i = 0; i < tokens.Count(); i++)
            {
                var childToken = tokens.ElementAt(i);
                ParseToken(parentToken, state, helper, childToken, input);
            }

            if (helper.selectedTokens != null)
            {
                CopyPostOperationBuildUp(parentToken, helper.selectedTokens, this.Context);
            }
            if (helper.tokensToReplace != null)
            {
                ReplacePostOperationBuildUp(parentToken, helper.tokensToReplace, this.Context);
            }
            if (helper.tokensToDelete != null)
            {
                DeletePostOperationBuildUp(parentToken, helper.tokensToDelete, this.Context);
            }
            if (helper.tokensToAdd != null)
            {
                AddPostOperationBuildUp(parentToken, helper.tokensToAdd);
            }
            PostOperationsBuildUp(ref parentToken, helper.tokenToForm);
            if (helper.loopProperties != null || helper.condProps != null)
            {
                LoopPostOperationBuildUp(ref parentToken, helper);
            }
            if (helper.scopeToForm != null)
            {
                ScopePostOperationBuildUp(ref parentToken, helper);
            }
        }

        private void ParseToken(JToken parentToken, State state, TransformHelper helper, JToken childToken, JToken input)
        {
            if (childToken.Type == JTokenType.Array && (parentToken as JProperty)?.Name.Trim() != "#")
            {
                IEnumerable<object> itemsToAdd = TransformArray(childToken.Children(), state, input);
                BuildArrayToken(childToken as JArray, itemsToAdd);
            }
            else if (childToken.Type == JTokenType.Property && childToken is JProperty property && property.Name != null)
            {
                /* For looping*/
                helper.isLoop = false;

                if (property.Name == "#" && property.Value.Type == JTokenType.Array && property.Value is JArray values)
                {
                    BulkOperations(values.Children(), state, helper, input);
                    helper.isBulk = true;
                }
                else
                {
                    helper.isBulk = false;
                    if (ExpressionHelper.TryParseFunctionNameAndArguments(property.Name, out string functionName, out string arguments))
                    {
                        ParsePropertyFunction(state, helper, childToken, property, functionName, arguments, input);
                    }
                    else if (property.Value.ToString().Trim().StartsWith("#"))
                    {
                        var propVal = property.Value.ToString().Trim();
                        var output = ParseFunction(propVal, parentToken, state, input);
                        output = LookInTransformed(output, propVal, parentToken, state);
                        property.Value = GetToken(output);
                    }
                }

                if (property.Name != null && property.Value.ToString().StartsWith($"{Context.EscapeChar}#"))
                {
                    var clone = property.Value as JValue;
                    clone.Value = clone.Value.ToString().Substring(1);
                    property.Value.Replace(clone);
                }
                /*End looping */
            }
            else if (childToken.Type == JTokenType.String && childToken.Value<string>().Trim().StartsWith("#")
                && state.ParentArray != null && state.CurrentArrayToken != null)
            {
                object newValue = ParseFunction(childToken.Value<string>(), parentToken, state, input);
                childToken.Replace(GetToken(newValue));
            }

            if (!helper.isLoop && !helper.isBulk && !helper.isScope)
            {
                RecursiveEvaluate(ref childToken, state, input);
            }
        }

        private void ParsePropertyFunction(State state, TransformHelper helper, JToken childToken, JProperty property, string functionName, string arguments, JToken input)
        {
            switch (functionName)
            {
                case "ifgroup":
                    ConditionalGroupOperation(property.Name, arguments, state, helper, childToken, input);
                    break;
                case "loop":
                    LoopOperation(property.Name, arguments, state, helper, childToken, input);
                    helper.isLoop = true;
                    break;
                case "eval":
                    EvalOperation(property, arguments, state, helper, input);
                    break;
                case "transform":
                    TranformOperation(property, arguments, state, input);
                    break;
                case "scope":
                    ScopeOperation(property.Name, arguments, state, helper, childToken, input);
                    helper.isScope = true;
                    break;
            }
        }

        private void LoopOperation(string propertyName, State state, TransformHelper helper, JToken childToken, JToken input)
        {
            object function = ParseFunction(propertyName, null, state, input);
            JArray arrayToken = function is JArray ? function as JArray : GetPropertiesArray(function, Context.IsStrictMode());

            // bool isObject = loopContext?.IsObject ?? false;
            string alias = state.ParentArray.Keys.Last().Key;
            using (IEnumerator<JToken> elements = arrayToken.GetEnumerator())
            {
                while (elements.MoveNext())
                {
                    JToken clonedToken = childToken.DeepClone();

                    if (state.CurrentArrayToken.Keys.Any(l => l.Key == alias))
                    {
                        state.CurrentArrayToken.Remove(state.CurrentArrayToken.First(t => t.Key.Key == alias));
                    }
                    state.CurrentArrayToken.Add(new LevelKey() { Key = alias, Level = _levelCounter }, elements.Current);

                    RecursiveEvaluate(ref clonedToken, state, input);
                    if (isObject)
                    {
                        DictionaryToForm(helper, clonedToken);
                    }
                    else
                    {
                        ArrayToForm(helper, clonedToken);
                    }
                }
            }

            helper.loopProperties ??= new List<string>();
            helper.loopProperties.Add(propertyName);

            state.ParentArray.Remove(new LevelKey { Level = _levelCounter, Key = alias});
            state.CurrentArrayToken.Remove(new LevelKey { Level = _levelCounter, Key = alias});
            // state.ParentArray.Remove(state.ParentArray.First(t => t.Key.Key == key));
            // state.CurrentArrayToken.Remove(state.CurrentArrayToken.First(t => t.Key.Key == key));
            _levelCounter--;
        }
        
        /* private void LoopOperation(string propertyName, string arguments, State state, TransformHelper helper, JToken childToken, JToken input)
        {
            var args = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            var previousAlias = "root";
            args[0] = (string)ParseFunction(args[0], null, state, input);
            _levelCounter++;
            string alias = args.Length > 1 ? (string)ParseFunction(args[1].Trim(), null, state, input) : $"loop{_levelCounter}";

            if (state.CurrentArrayToken?.Any() ?? false)
            {
                previousAlias = (string)ParseFunction(args[2].Trim(), null, state, input);
                state.CurrentArrayToken = new Dictionary<LevelKey, JToken> { { new LevelKey { Level =_levelCounter, Key = previousAlias }, input } };
            }
            else
            {
                previousAlias = state.GetHigherAlias();
            }
            
            var strArrayToken = ParseArgument(null, state, args[0], input) as string;

            bool isDictionary = false;
            JToken arrayToken;
            var selectable = GetSelectableToken(state.GetAliasToken(previousAlias), Context);
            arrayToken = selectable.Select(strArrayToken);

            if (arrayToken != null)
            {
                //workaround: result should be an array if path ends up with array filter
                if (IsArray(arrayToken, strArrayToken, state, alias))
                {
                    arrayToken = new JArray(arrayToken);
                }

                if (arrayToken is IDictionary<string, JToken> dict) //JObject is a dictionary
                {
                    isDictionary = true;
                    JArray arr = new JArray();
                    foreach (var item in dict)
                    {
                        arr.Add(new JObject { { item.Key, item.Value } });
                    }

                    arrayToken = arr;
                }

                if (arrayToken is JArray array)
                {
                    using (IEnumerator<JToken> elements = array.GetEnumerator())
                    {
                        if (state.ParentArray?.Any() ?? false)
                        {
                            state.ParentArray.Add(new LevelKey { Level = _levelCounter, Key = alias}, array);
                        }
                        else
                        {
                            state.ParentArray = new Dictionary<LevelKey, JArray> { { new LevelKey { Level = _levelCounter, Key = alias}, array } };
                        }

                        if (helper.arrayToForm == null)
                        {
                            helper.arrayToForm = new JArray();
                        }
                        if (!isDictionary)
                        {
                            while (elements.MoveNext())
                            {
                                JToken clonedToken = childToken.DeepClone();
                                if (state.CurrentArrayToken.Any(a => a.Key.Key == alias))
                                {
                                    state.CurrentArrayToken.Remove(new LevelKey { Level = _levelCounter, Key = alias});
                                }
                                state.CurrentArrayToken.Add(new LevelKey { Level = _levelCounter, Key = alias}, elements.Current);
                                RecursiveEvaluate(ref clonedToken, state, input);
                                foreach (JToken replacedProperty in clonedToken.Children())
                                {
                                    helper.arrayToForm.Add(replacedProperty.Type != JTokenType.Null ? replacedProperty : new JObject());
                                }
                            }
                        }
                        else
                        {
                            helper.dictToForm = new JObject();
                            while (elements.MoveNext())
                            {
                                JToken clonedToken = childToken.DeepClone();
                                if (state.CurrentArrayToken.Any(a => a.Key.Key == alias))
                                {
                                    state.CurrentArrayToken.Remove(new LevelKey { Level = _levelCounter, Key = alias});
                                }
                                state.CurrentArrayToken.Add(new LevelKey { Level = _levelCounter, Key = alias}, elements.Current);
                                RecursiveEvaluate(ref clonedToken, state, input);
                                foreach (JToken replacedProperty in clonedToken.Children().Select(t => t.First))
                                {
                                    helper.dictToForm.Add(replacedProperty);
                                }
                            }
                        }

                        state.ParentArray.Remove(new LevelKey { Level = _levelCounter, Key = alias});
                        state.CurrentArrayToken.Remove(new LevelKey { Level = _levelCounter, Key = alias});
                    }
                }
            }

            if (helper.loopProperties == null)
                helper.loopProperties = new List<string>();

            helper.loopProperties.Add(propertyName);
            _levelCounter--;
        }
        */

        private static void DictionaryToForm(TransformHelper helper, JToken clonedToken)
        {
            helper.dictToForm ??= new JObject();
            foreach (JObject replacedProperty in clonedToken.Children())
            {
                foreach (var item in replacedProperty.Children())
                {
                    JProperty p = item as JProperty;
                    string name = p.Name;
                    helper.dictToForm.Add(name, p.Value);
                }
            }
        }

        private static void ArrayToForm(TransformHelper helper, JToken clonedToken)
        {
            helper.arrayToForm ??= new JArray();
            foreach (JToken replacedProperty in clonedToken.Children())
            {
                JToken tokenToAdd = replacedProperty.Type != JTokenType.Null ? replacedProperty : new JObject();
                helper.arrayToForm.Add(tokenToAdd);
            }
        }

        private static JArray GetPropertiesArray(object arrayToken, bool isStrictMode)
        {
            JArray arr = new JArray();
            if (arrayToken is IDictionary<string, JToken> dict) //JObject is a dictionary
            {
                foreach (var item in dict)
                {
                    arr.Add(new JObject { { item.Key, item.Value } });
                }
            }
            else if (isStrictMode)
            {
                throw new Exception("Not an JObject");
            }
            return arr;
        }

        private void TranformOperation(JProperty property, string arguments, State state, JToken input)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);

            object functionResult = ParseArgument(null, state, argumentArr[0], input);
            if (!(functionResult is string jsonPath))
            {
                throw new ArgumentException($"Invalid path for #transform: '{argumentArr[0]}' resolved to null!");
            }

            JToken selectedToken = null;
            string alias = "root";
            if (argumentArr.Length > 1)
            {
                alias = ParseArgument(null, state, argumentArr[1], input) as string;
                if (!state.CurrentArrayToken.Any(a => a.Key.Key == alias))
                {
                    throw new ArgumentException($"Unknown loop alias: '{argumentArr[1]}'");
                }
                JToken localInput = alias != null ? state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value : state.CurrentArrayToken.Last().Value;
                var selectable = GetSelectableToken(state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value, Context);
                selectedToken = selectable.Select(argumentArr[0]);
            }
            else
            {
                var selectable = GetSelectableToken(state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value, Context);
                selectedToken = selectable.Select(argumentArr[0]);
            }

            if (property.Value.Type == JTokenType.Array)
            {
                JToken transformInput = selectedToken;
                for (int i = 0; i < property.Value.Count(); i++)
                {
                    JToken token = property.Value[i];
                    if (token.Type == JTokenType.String)
                    {
                        var obj = ParseFunction(token.Value<string>(), null, state, transformInput);
                        token.Replace(GetToken(obj));
                    }
                    else
                    {
                        RecursiveEvaluate(ref token, state /*i == 0 ? parentArray : null, i == 0 ? currentArrayToken : null*/, transformInput);
                    }
                    transformInput = token;
                }
            }
            property.Parent.Replace(property.Value[property.Value.Count() - 1]);
        }

        private void PostOperationsBuildUp(ref JToken parentToken, IList<JToken> tokenToForm)
        {
            if (tokenToForm != null)
            {
                foreach (JToken token in tokenToForm)
                {
                    foreach (JToken childToken in token.Children())
                    {
                        if (childToken is JProperty child)
                        {
                            (parentToken as JObject).Add(child.Name, child.Value);
                        }
                        else if (token is JArray arr && parentToken.Parent != null)
                        {
                            switch (parentToken.Parent.Type)    
                            {
                                case JTokenType.Array:
                                    parentToken.Replace(arr);
                                    break;
                                case JTokenType.Property:
                                    (parentToken.Parent as JProperty).Value = arr;
                                    break;
                                default:
                                    if (Context.IsStrictMode())
                                    {
                                        throw new Exception($"don't know what to do with {token} and {parentToken.Type} parent!");
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            if (Context.IsStrictMode())
                            {
                                throw new Exception($"found {parentToken.Type} without parent!");
                            }
                        }
                    }
                }
            }
            if (parentToken is JObject jObject)
            {
                jObject.Remove("#");
            }
        }

        private static void CopyPostOperationBuildUp(JToken parentToken, IList<JToken> selectedTokens, JUSTContext context)
        {
            foreach (JToken selectedToken in selectedTokens)
            {
                if (selectedToken != null)
                {
                    JObject parent = parentToken as JObject;
                    JEnumerable<JToken> copyChildren = selectedToken.Children();
                    if (context.IsAddOrReplacePropertiesMode())
                    {
                        CopyDescendants(parent, copyChildren);
                    }
                    else
                    {
                        foreach (JProperty property in copyChildren)
                        {
                            parent.Add(property.Name, property.Value);
                        }
                    }
                }
            }
        }

        private static void CopyDescendants(JObject parent, JEnumerable<JToken> children)
        {
            if (parent == null)
            {
                return;
            }

            int i = 0;
            while (i < children.Count())
            {
                JToken token = children.ElementAt(i);
                if (token is JProperty property)
                {
                    if (parent.ContainsKey(property.Name))
                    {
                        CopyDescendants(parent[property.Name] as JObject, property.Children());
                        property.Remove();
                    }
                    else
                    {
                        parent.Add(property.Name, property.Value);
                        i++;
                    }
                }
                else if (token is JObject obj)
                {
                    CopyDescendants(parent, obj.Children());
                    i++;
                }
                else
                {
                    i++;
                }
            }
        }

        private static void AddPostOperationBuildUp(JToken parentToken, IList<JToken> tokensToAdd)
        {
            if (tokensToAdd != null)
            {
                foreach (JToken token in tokensToAdd)
                {
                    (parentToken as JObject).Add((token as JProperty).Name, (token as JProperty).Value);
                }
            }
        }

        private static void DeletePostOperationBuildUp(JToken parentToken, IList<JToken> tokensToDelete, JUSTContext context)
        {
            foreach (string selectedToken in tokensToDelete)
            {
                JToken tokenToRemove = GetSelectableToken(parentToken, context).Select(selectedToken);

                if (tokenToRemove != null)
                    tokenToRemove.Ancestors().First().Remove();
            }

        }

        private static void ReplacePostOperationBuildUp(JToken parentToken, IDictionary<string, JToken> tokensToReplace, JUSTContext context)
        {

            foreach (KeyValuePair<string, JToken> tokenToReplace in tokensToReplace)
            {
                JsonPathSelectable selectable = JsonTransformer.GetSelectableToken(parentToken, context);
                JToken selectedToken = selectable.Select(tokenToReplace.Key);
                selectedToken.Replace(tokenToReplace.Value);
            }
        }

        private static void LoopPostOperationBuildUp(ref JToken parentToken, TransformHelper helper)
        {
            if (helper.loopProperties != null)
            {
                if (parentToken is JObject obj)
                {
                    foreach (string propertyToDelete in helper.loopProperties)
                    {
                        if (helper.dictToForm == null && helper.arrayToForm == null && parentToken.Count() <= 1)
                        {
                            obj.Replace(JValue.CreateNull());
                        }
                        else
                        {
                            obj.Remove(propertyToDelete);
                        }
                    }
                }
            }

            if (helper.condProps != null)
            {
                if (parentToken is JObject obj)
                {
                    foreach (string propertyToDelete in helper.condProps)
                    {
                        obj.Remove(propertyToDelete);
                    }
                }
            }

            if (helper.dictToForm != null)
            {
                parentToken.Replace(helper.dictToForm);
            }
            else if (helper.arrayToForm != null)
            {
                if (parentToken.Parent != null)
                {
                    if (parentToken.Parent is JArray arr)
                    {
                        foreach (var item in helper.arrayToForm)
                        {
                            arr.Add(item);
                        }
                        if (!parentToken.HasValues)
                        {
                            parentToken = helper.arrayToForm;
                        }
                    }
                    else
                    {
                        parentToken.Replace(helper.arrayToForm);
                    }
                }
                else
                {
                    parentToken = helper.arrayToForm;
                }
            }
        }

        private static void ScopePostOperationBuildUp(ref JToken parentToken, TransformHelper helper)
        {
            if (parentToken is JObject obj)
            {
                foreach (string propertyToDelete in helper.scopeProperties)
                {
                    if (helper.scopeToForm == null && parentToken.Count() <= 1)
                    {
                        obj.Replace(JValue.CreateNull());
                    }
                    else
                    {
                        obj.Remove(propertyToDelete);
                    }
                }
            }

            if (helper.scopeToForm != null)
            {
                parentToken.Replace(helper.scopeToForm);
            }
        }

        private bool IsArray(JToken arrayToken, string strArrayToken, State state, string alias)
        {
            return typeof(T) == typeof(JsonPathSelectable) && arrayToken.Type != JTokenType.Array && (Regex.IsMatch(strArrayToken ?? string.Empty, "\\[.+\\]$") || (state.CurrentArrayToken != null && state.CurrentArrayToken.Any(a => a.Key.Key == alias) && state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value != null && Regex.IsMatch(state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value.Value<string>(), "\\[.+\\]$")));
        }

        private void ScopeOperation(string propertyName, string arguments, State state, TransformHelper helper, JToken childToken, JToken input)
        {
            var args = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            var previousAlias = "root";
            args[0] = (string)ParseFunction(args[0], null, state, input);
            _levelCounter++;
            string alias = args.Length > 1 ? (string)ParseFunction(args[1].Trim(), null, state, input) : $"scope{_levelCounter}";

            if (args.Length > 2)
            {
                previousAlias = (string)ParseFunction(args[2].Trim(), null, state, input);
            }
            else
            {
                previousAlias = state.GetHigherAlias();
            }

            var strScopeToken = ParseArgument(null, state, args[0], input) as string;

            JToken scopeToken;
            var selectable = GetSelectableToken(state.GetAliasToken(previousAlias), Context);
            scopeToken = selectable.Select(strScopeToken);

            JToken clonedToken = childToken.DeepClone();
            if (state.CurrentScopeToken.Any(s => s.Key.Key == alias))
            {
                state.CurrentScopeToken.Remove(new LevelKey {Level = _levelCounter, Key = alias});
            }
            state.CurrentScopeToken.Add(new LevelKey {Level = _levelCounter, Key = alias}, scopeToken);
            RecursiveEvaluate(ref clonedToken, state, input);
            helper.scopeToForm = clonedToken.Children().First().Value<JObject>();
            
            state.CurrentScopeToken.Remove(new LevelKey {Level = _levelCounter, Key = alias});

            if (helper.scopeProperties == null)
                helper.scopeProperties = new List<string>();

            helper.scopeProperties.Add(propertyName);
            _levelCounter--;
        }

        private void ConditionalGroupOperation(string propertyName, string arguments, State state, TransformHelper helper, JToken childToken, JToken input)
        {
            object functionResult = ParseFunction(arguments, null, state, input);
            bool result;
            try
            {
                result = (bool)ReflectionHelper.GetTypedValue(typeof(bool), functionResult, Context.IsStrictMode());
            }
            catch
            {
                if (Context.IsStrictMode()) { throw; }
                result = false;
            }

            if (result)
            {
                if (helper.condProps == null)
                    helper.condProps = new List<string>();

                helper.condProps.Add(propertyName);

                RecursiveEvaluate(ref childToken, state, input);

                if (helper.tokenToForm == null)
                {
                    helper.tokenToForm = new List<JToken>();
                }

                foreach (JToken grandChildToken in childToken.Children())
                {
                    helper.tokenToForm.Add(grandChildToken.DeepClone());
                }
            }
            else
            {
                if (helper.condProps == null)
                {
                    helper.condProps = new List<string>();
                }

                helper.condProps.Add(propertyName);
                childToken.First.Replace(JToken.Parse("{}"));
            }
        }

        private void EvalOperation(JProperty property, string arguments, State state, TransformHelper helper, JToken input)
        {
            object functionResult = ParseFunction(arguments, null, state, input);

            object val;
            if (property.Value.Type == JTokenType.String)
            {
                val = ParseFunction(property.Value.Value<string>(), null, state, input);
            }
            else
            {
                var propVal = property.Value;
                RecursiveEvaluate(ref propVal, state, input);
                val = property.Value;
            }
            JProperty clonedProperty = new JProperty(functionResult.ToString(), val);

            helper.loopProperties ??= new List<string>();
            helper.loopProperties.Add(property.Name);

            helper.tokensToAdd ??= new List<JToken>();
            helper.tokensToAdd.Add(clonedProperty);
        }

        private void BulkOperations(JEnumerable<JToken> arrayValues, State state, TransformHelper helper, JToken input)
        {
            foreach (JToken arrayValue in arrayValues)
            {
                if (arrayValue.Type == JTokenType.String &&
                    ExpressionHelper.TryParseFunctionNameAndArguments(
                        arrayValue.Value<string>().Trim(), out string functionName, out string arguments))
                {
                    if (functionName == "copy")
                    {
                        if (helper.selectedTokens == null)
                            helper.selectedTokens = new List<JToken>();
                        helper.selectedTokens.Add(Copy(arguments, state, input));
                    }
                    else if (functionName == "replace")
                    {
                        if (helper.tokensToReplace == null)
                            helper.tokensToReplace = new Dictionary<string, JToken>();

                        var replaceResult = Replace(arguments, state, input);
                        helper.tokensToReplace.Add(replaceResult.Key, replaceResult.Value);
                    }
                    else if (functionName == "delete")
                    {
                        if (helper.tokensToDelete == null)
                            helper.tokensToDelete = new List<JToken>();

                        helper.tokensToDelete.Add(Delete(arguments, state, input));
                    }
                }
            }
        }

        private static void BuildArrayToken(JArray arrayToken, IEnumerable<object> itemsToAdd)
        {
            arrayToken.RemoveAll();
            foreach (object itemToAdd in itemsToAdd)
            {
                if (itemToAdd is Array)
                {
                    foreach (var item in itemToAdd as Array)
                    {
                        arrayToken.Add(Utilities.GetNestedData(item));
                    }
                }
                else
                {
                    if (itemToAdd != null)
                    {
                        arrayToken.Add(JToken.FromObject(itemToAdd));
                    }
                }
            }
        }
        #endregion

        private JToken GetToken(object newValue)
        {
            JToken result = null;
            if (newValue != null)
            {
                if (newValue is JToken token)
                {
                    result = token;
                }
                else
                {
                    try
                    {
                        if (newValue is IEnumerable<object> newArray)
                        {
                            result = new JArray(newArray);
                        }
                        else
                        {
                            result = new JValue(newValue);
                        }
                    }
                    catch
                    {
                        if (Context.IsStrictMode())
                        {
                            throw;
                        }

                        if (Context.IsFallbackToDefault())
                        {
                            result = JValue.CreateNull();
                        }
                    }
                }
            }
            else
            {
                result = JValue.CreateNull();
            }

            return result;
        }

        private IEnumerable<object> TransformArray(JEnumerable<JToken> children, State state, JToken input)
        {
            var result = new List<object>();

            foreach (JToken arrEl in children)
            {
                object itemToAdd = arrEl.Value<JToken>();
                if (arrEl.Type == JTokenType.String && arrEl.ToString().Trim().StartsWith("#"))
                {
                    itemToAdd = ParseFunction(null, arrEl.ToString(), state, input);
                }
                result.Add(itemToAdd);
            }

            return result;
        }

        #region Copy
        private JToken Copy(string arguments, State state, JToken input)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            string path = argumentArr[0];
            if (!(ParseArgument(null, state, path, input) is string jsonPath))
            {
                throw new ArgumentException($"Invalid path for #copy: '{argumentArr[0]}' resolved to null!");
            }

            string alias = null;
            if (argumentArr.Length > 1)
            {
                alias = ParseArgument(null, state, argumentArr[1], input) as string;
                if (!(state.CurrentArrayToken?.Any(a => a.Key.Key == alias) ?? false))
                {
                    throw new ArgumentException($"Unknown loop alias: '{argumentArr[1]}'");
                }
            }
            JToken localInput = alias != null ? state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value : state.CurrentArrayToken?.Last().Value ?? input;
            JToken selectedToken = GetSelectableToken(localInput, Context).Select(jsonPath);
            return selectedToken;
        }

        #endregion

        #region Replace
        private KeyValuePair<string, JToken> Replace(string arguments, State state, JToken input)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            if (argumentArr.Length < 2)
            {
                throw new Exception("Function #replace needs at least two arguments - 1. path to be replaced, 2. token to replace with.");
            }
            if (!(ParseArgument(null, state, argumentArr[0], input) is string key))
            {
                throw new ArgumentException($"Invalid path for #replace: '{argumentArr[0]}' resolved to null!");
            }
            object str = ParseArgument(null, state, argumentArr[1], input);
            JToken newToken = GetToken(str);
            return new KeyValuePair<string, JToken>(key, newToken);
        }

        #endregion

        #region Delete
        private string Delete(string argument, State state, JToken input)
        {
            if (!(ParseArgument(null, state, argument, input) is string result))
            {
                throw new ArgumentException($"Invalid path for #delete: '{argument}' resolved to null!");
            }
            return result;
        }
        #endregion

        #region ParseFunction

        private object ParseFunction(string functionString, JToken parentToken, State state, JToken input)
        {
            LoopContext localLoopContext = loopContext;
            bool isObject = false;

            Func<string, bool, object[], IContext, object> invokeFunc = (fn, convertParameters, parameters, context) =>
            {
                return Invoke(fn, convertParameters, parameters.Concat(new object[] { input, context }).ToArray());
            };

            Func<string, string, IContext, object> invokeCheckLoopFunc = (fn, path, context) =>
            {
                object result;
                JToken loopInput = localLoopContext?.CurrentArrayElement.Last().Value != null ?
                    localLoopContext.CurrentArrayElement.Last().Value :
                    input;
                result = Invoke(fn, true, new object[] { path, loopInput, context });
                return result;
            };

            Func<string, string, string, IContext, object> invokeLoopFunctionFunc = (fn, path, alias, context) =>
            {
                string arrayAlias = GetAlias(alias, localLoopContext.CurrentArrayElement);
                object[] parameters = !string.IsNullOrEmpty(path) ? 
                    new object[] { localLoopContext.ParentArray[arrayAlias], localLoopContext.CurrentArrayElement[arrayAlias], path, context } :
                    new object[] { localLoopContext.ParentArray[arrayAlias], localLoopContext.CurrentArrayElement[arrayAlias], context };
                return Invoke(fn, true, parameters);
            };

            Func<string, string, string, IContext, JArray> loopOverAliasFunc = (loopPath, loopAlias, previousAlias, context) =>
            {
                previousAlias = previousAlias ?? localLoopContext?.CurrentArrayElement.Last().Key ?? RootAlias;
                JToken loopInput = localLoopContext?.CurrentArrayElement?[previousAlias] ?? input;
                object loopToken = Invoke("valueof", true, new object[] { loopPath, loopInput, context });
                JArray loopArray = JsonTransformer.GetLoopArray(loopToken, context.IsStrictMode(), out isObject);
                KeyValuePair<string, JArray> k = new KeyValuePair<string, JArray>(loopAlias ?? $"loop{++this._loopCounter}", loopArray);

                localLoopContext ??= new LoopContext(null, null);
                localLoopContext.IsObject = isObject;
                localLoopContext.ParentArray.Add(k);

                return loopArray;
            };

            Func<dynamic, dynamic, IContext, dynamic> replaceFunc = (arg1, arg2, context) =>
            {
                object arg1Val = Invoke("valueof", true, new object[] { arg1, input, context });
                (arg1Val as JToken).Replace(arg2 as JToken);
                return input;
            };

            Func<dynamic, IContext, dynamic> deleteFunc = (arg1, context) =>
            {
                JToken toRemove = input.SelectToken(arg1);
                toRemove.Ancestors().First().Remove();
                return input;
            };

            ParseResult parseResult = this.Grammar.Parse(
                functionString,
                invokeFunc,
                invokeCheckLoopFunc,
                invokeLoopFunctionFunc,
                loopOverAliasFunc,
                replaceFunc,
                deleteFunc,
                this.Context);
            if (!parseResult.Success && this.Context.IsStrictMode())
            {
                throw new Exception($"Error parsing '{functionString}': " + string.Join(Environment.NewLine, parseResult.Errors.Select(e => e.Description)));
            }

            if (loopContext is null)
            {
                loopContext = localLoopContext;
            }
            else
            {
                foreach (KeyValuePair<string, JArray> item in localLoopContext.ParentArray)
                {
                    if (!loopContext.ParentArray.ContainsKey(item.Key))
                    {
                        loopContext.ParentArray.Add(item);
                    }
                }
                loopContext.IsObject = isObject;
            }
            return parseResult.Value;
        }

        private object ParseApplyOver(State state, object[] parameters, JToken input)
        {
            object output;
            var contextInput = input;
            if (state.ParentArray != null)
            {
                var alias = ParseLoopAlias(parameters, 3, state.ParentArray.Last().Key.Key);
                contextInput = state.CurrentArrayToken.Single(t => t.Key.Key == alias).Value;
            }

            var localInput = Transform(parameters[0].ToString(), contextInput.ToString());
            
            IDictionary<LevelKey, JToken> tmpArray = state.CurrentArrayToken;
            IDictionary<LevelKey, JToken> tmpScope = state.CurrentScopeToken;

            state.CurrentArrayToken = new Dictionary<LevelKey, JToken>() { { new LevelKey { Key = "root", Level = 0 }, input } };
            state.CurrentScopeToken = new Dictionary<LevelKey, JToken>() { { new LevelKey { Key = "root", Level = 0 }, input } };

            if (parameters[1].ToString().Trim().Trim('\'').StartsWith("{"))
            {
                var jobj = JObject.Parse(parameters[1].ToString().Trim().Trim('\''));
                output = new JsonTransformer(Context).Transform(jobj, localInput);
            }
            else if (parameters[1].ToString().Trim().Trim('\'').StartsWith("["))
            {
                var jarr = JArray.Parse(parameters[1].ToString().Trim().Trim('\''));
                output = new JsonTransformer(Context).Transform(jarr, localInput);
            }
            else
            {
                output = ParseFunction(parameters[1].ToString().Trim().Trim('\''), null, state, JToken.Parse(localInput));
            }
            
            state.CurrentArrayToken = tmpArray;
            state.CurrentScopeToken = tmpScope;

            return output;
        }

        private string ParseLoopAlias(IList<object> listParameters, int index, string defaultValue)
        {
            string alias;
            if (listParameters != null && listParameters.Count >= index)
            {
                alias = (listParameters[index - 1] as string).Trim();
            }
            else
            {
                alias = defaultValue;
            }
            return alias;
        }

        private object ParseArgument(JToken parentToken, State state, string argument, JToken input)
        {
            object output = argument;
            var trimmedArgument = argument.Trim();
            if (trimmedArgument.StartsWith("#"))
            {
                return ParseFunction(trimmedArgument, parentToken, state, input);
            }
            else if (trimmedArgument.StartsWith($"{Context.EscapeChar}#"))
            {
                output = ExpressionHelper.UnescapeSharp(argument, Context.EscapeChar);
            }
            return output;
        }

        private object GetConditionalOutput(JToken parentToken, string[] arguments, State state, JToken input)
        {
            var condition = ParseArgument(parentToken, state, arguments[0], input);
            condition = LookInTransformed(condition, arguments[0], parentToken, state);
            var value = ParseArgument(parentToken, state, arguments[1], input);
            value = LookInTransformed(value, arguments[1], parentToken, state);
            var equal = ComparisonHelper.Equals(condition, value, Context.EvaluationMode);
            var index = (equal) ? 2 : 3;

            return ParseArgument(parentToken, state, arguments[index], input);
        }
        
        private object LookInTransformed(object output, string propVal, JToken parentToken, State state)
        {
            if (output == null && Context.IsLookInTransformed())
            {
                output = ParseFunction(propVal, parentToken, state, parentToken);
            }
            return output;
        }
        #endregion

        #region Split
        public static IEnumerable<string> SplitJson(string input, string arrayPath, JUSTContext context)
        {
            JObject inputJObject = DeserializeWithoutDateParse<JObject>(input);

            List<JObject> jObjects = SplitJson(inputJObject, arrayPath, context).ToList();

            List<string> output = null;

            foreach (JObject jObject in jObjects)
            {
                if (output == null)
                    output = new List<string>();

                output.Add(SerializeWithoutDateParse(jObject));
            }

            return output;
        }

        public static IEnumerable<JObject> SplitJson(JObject input, string arrayPath, JUSTContext context)
        {
            List<JObject> jsonObjects = null;

            JToken tokenArr = GetSelectableToken(input, context).Select(arrayPath);

            string pathToReplace = tokenArr.Path;

            if (tokenArr != null && tokenArr is JArray)
            {
                JArray array = tokenArr as JArray;

                foreach (JToken tokenInd in array)
                {

                    string path = tokenInd.Path;

                    JToken clonedToken = input.DeepClone();

                    var selectable = GetSelectableToken(clonedToken, context);
                    JToken foundToken = selectable.Select(selectable.RootReference + path);
                    JToken tokenToReplce = selectable.Select(selectable.RootReference + pathToReplace);

                    tokenToReplce.Replace(foundToken);

                    if (jsonObjects == null)
                        jsonObjects = new List<JObject>();

                    jsonObjects.Add(clonedToken as JObject);


                }
            }
            else
                throw new Exception("ArrayPath must be a valid JSON path to a JSON array.");

            return jsonObjects;
        }
        #endregion

        private static T GetSelectableToken(JToken token, JUSTContext context)
        {
            return context.Resolve<T>(token);
        }

        public void Dispose()
        {
            this.Grammar.Dispose();
        }

        private object InvokeCheckLoop(string fn, string path, IDictionary<string, JToken> currentArrayElement, JToken input)
        {
            object result;
            if (currentArrayElement?.Last().Value != null)
            {
                result = Invoke(fn, true, new object[] { path, currentArrayElement.Last().Value, this.Context });
            }
            else
            {
                result = Invoke(fn, true, new object[] { path, input, this.Context });
            }
            return result;
        }

        private object Invoke(string fn, bool convertParameters, object[] parameters)
        {
            return ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", fn, parameters, convertParameters, this.Context);
        }
    }
}
