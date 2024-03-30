using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JUST.net.Selectables;

namespace JUST
{
    public class JsonTransformer : JsonTransformer<JsonPathSelectable>
    {
        public JsonTransformer(JUSTContext context = null) : base(context)
        {
        }
    }

    public class JsonTransformer<T> : Transformer<T> where T : ISelectableToken
    {
        public JsonTransformer(JUSTContext context = null) : base(context)
        {
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
            }
            return result;
        }

        private JToken TransformValue(JToken transformer, JToken input)
        {
            var tmp = new JObject
            {
                { "root", transformer }
            };
            Transform(tmp, input);
            return tmp["root"];
        }

        public JToken Transform(JObject transformer, string input)
        {
            return Transform(transformer, DeserializeWithoutDateParse<JToken>(input));
        }

        public JToken Transform(JObject transformer, JToken input)
        {
            var parentToken = (JToken)transformer;
            State state = new State(transformer, input, _levelCounter);
            RecursiveEvaluate(ref parentToken, state);
            return parentToken;
        }

        #region RecursiveEvaluate


        private void RecursiveEvaluate(ref JToken parentToken, State state)
        {
            if (parentToken == null)
            {
                return;
            }

            JEnumerable<JToken> tokens = parentToken.Children();

            TransformHelper helper = new TransformHelper();
            foreach (JToken childToken in tokens)
            {
                ParseToken(parentToken, state, helper, childToken);
            }

            if (helper.selectedTokens != null)
            {
                CopyPostOperationBuildUp(parentToken, helper.selectedTokens);
            }
            if (helper.tokensToReplace != null)
            {
                ReplacePostOperationBuildUp(parentToken, helper.tokensToReplace);
            }
            if (helper.tokensToDelete != null)
            {
                DeletePostOperationBuildUp(parentToken, helper.tokensToDelete);
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
                ScopePostOperationBuildUp(ref parentToken, helper.scopeProperties, helper.scopeToForm);
            }
        }

        private void ParseToken(JToken parentToken, State state, TransformHelper helper, JToken childToken)
        {
            if (childToken.Type == JTokenType.Array && (parentToken as JProperty)?.Name.Trim() != "#")
            {
                IEnumerable<object> itemsToAdd = TransformArray(childToken.Children(), state);
                BuildArrayToken(childToken as JArray, itemsToAdd);
            }
            else if (childToken.Type == JTokenType.Property && childToken is JProperty property && property.Name != null)
            {
                /* For looping*/
                helper.isLoop = false;

                if (property.Name == "#" && property.Value.Type == JTokenType.Array && property.Value is JArray values)
                {
                    BulkOperations(values.Children(), state, helper);
                    helper.isBulk = true;
                }
                else
                {
                    helper.isBulk = false;
                    if (ExpressionHelper.TryParseFunctionNameAndArguments(property.Name, out string functionName, out string arguments))
                    {
                        ParsePropertyFunction(state, helper, childToken, property, functionName, arguments);
                    }
                    else if (property.Value.ToString().Trim().StartsWith("#"))
                    {
                        var propVal = property.Value.ToString().Trim();
                        var output = ParseFunction(propVal, state);
                        output = LookInTransformed(output, propVal, state);
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
                object newValue = ParseFunction(childToken.Value<string>(), state);
                childToken.Replace(GetToken(newValue));
            }

            if (!helper.isLoop && !helper.isBulk && !helper.isScope)
            {
                RecursiveEvaluate(ref childToken, state);
            }
        }

        private void ParsePropertyFunction(State state, TransformHelper helper, JToken childToken, JProperty property, string functionName, string arguments)
        {
            switch (functionName)
            {
                case "ifgroup":
                    ConditionalGroupOperation(property.Name, arguments, state, helper, childToken);
                    break;
                case "loop":
                    LoopOperation(property.Name, arguments, state, helper, childToken);
                    helper.isLoop = true;
                    break;
                case "eval":
                    EvalOperation(property, arguments, state, helper);
                    break;
                case "scope":
                    ScopeOperation(property.Name, arguments, state, helper, childToken);
                    helper.isScope = true;
                    break;
            }
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
                            (parentToken.Parent as JProperty).Value = arr;
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

        private void CopyPostOperationBuildUp(JToken parentToken, IList<JToken> selectedTokens)
        {
            foreach (JToken selectedToken in selectedTokens)
            {
                if (selectedToken != null)
                {
                    JObject parent = parentToken as JObject;
                    JEnumerable<JToken> copyChildren = selectedToken.Children();
                    if (Context.IsAddOrReplacePropertiesMode())
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

        private void DeletePostOperationBuildUp(JToken parentToken, IList<JToken> tokensToDelete)
        {

            foreach (string selectedToken in tokensToDelete)
            {
                JToken tokenToRemove = GetSelectableToken(parentToken, Context).Select(selectedToken);

                if (tokenToRemove != null)
                    tokenToRemove.Ancestors().First().Remove();
            }
        }

        private void ReplacePostOperationBuildUp(JToken parentToken, IDictionary<string, JToken> tokensToReplace)
        {

            foreach (KeyValuePair<string, JToken> tokenToReplace in tokensToReplace)
            {
                JsonPathSelectable selectable = JsonTransformer.GetSelectableToken(parentToken, Context);
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
                            var tmp = parentToken;
                            parentToken = arr;
                            tmp.Remove();
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

        private static void ScopePostOperationBuildUp(ref JToken parentToken, IList<string> scopeProperties, JObject scopeToForm)
        {
            if (parentToken is JObject obj)
            {
                foreach (string propertyToDelete in scopeProperties)
                {
                    if (scopeToForm == null && parentToken.Count() <= 1)
                    {
                        obj.Replace(JValue.CreateNull());
                    }
                    else
                    {
                        obj.Remove(propertyToDelete);
                    }
                }
            }

            if (scopeToForm != null)
            {
                parentToken.Replace(scopeToForm);
            }
        }

        private void LoopOperation(string propertyName, string arguments, State state, TransformHelper helper, JToken childToken)
        {
            var args = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            var previousAlias = "root";
            args[0] = (string)ParseFunction(args[0], state);
            _levelCounter++;
            string alias = args.Length > 1 ? (string)ParseFunction(args[1].Trim(), state) : $"loop{_levelCounter}";

            if (args.Length > 2)
            {
                previousAlias = (string)ParseFunction(args[2].Trim(), state);
                //state.CurrentArrayToken.Add(new LevelKey() { Key = previousAlias, Level = _levelCounter }, state.GetInput());
            }
            else if (state.CurrentArrayToken.Any(t => t.Key.Key == alias))
            {
                previousAlias = state.CurrentArrayToken.Single(t => t.Key.Key == alias).Key.Key;

            }
            else if (state.CurrentScopeToken.Any(t => t.Key.Key == alias))
            {
                previousAlias = state.CurrentScopeToken.Single(t => t.Key.Key == alias).Key.Key;
            }
            else
            {
                previousAlias = state.GetHigherAlias();
            }
            
            var strArrayToken = ParseArgument(state, args[0]) as string;

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
                        state.ParentArray.Add(new LevelKey { Level = _levelCounter, Key = alias}, array);

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
                                RecursiveEvaluate(ref clonedToken, state);
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
                                RecursiveEvaluate(ref clonedToken, state);
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

        private bool IsArray(JToken arrayToken, string strArrayToken, State state, string alias)
        {
            return typeof(T) == typeof(JsonPathSelectable) && arrayToken.Type != JTokenType.Array && (Regex.IsMatch(strArrayToken ?? string.Empty, "\\[.+\\]$") || (state.CurrentArrayToken != null && state.CurrentArrayToken.Any(a => a.Key.Key == alias) && state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value != null && Regex.IsMatch(state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value.Value<string>(), "\\[.+\\]$")));
        }

        private void ScopeOperation(string propertyName, string arguments, State state, TransformHelper helper, JToken childToken)
        {
            var args = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            var previousAlias = "root";
            args[0] = (string)ParseFunction(args[0], state);
            _levelCounter++;
            string alias = args.Length > 1 ? (string)ParseFunction(args[1].Trim(), state) : $"scope{_levelCounter}";

            if (args.Length > 2)
            {
                previousAlias = (string)ParseFunction(args[2].Trim(), state);
            }
            else
            {
                previousAlias = state.GetHigherAlias();
            }

            var strScopeToken = ParseArgument(state, args[0]) as string;

            JToken scopeToken;
            var selectable = GetSelectableToken(state.GetAliasToken(previousAlias), Context);
            scopeToken = selectable.Select(strScopeToken);

            JToken clonedToken = childToken.DeepClone();
            if (state.CurrentScopeToken.Any(s => s.Key.Key == alias))
            {
                state.CurrentScopeToken.Remove(new LevelKey {Level = _levelCounter, Key = alias});
            }
            state.CurrentScopeToken.Add(new LevelKey {Level = _levelCounter, Key = alias}, scopeToken);
            RecursiveEvaluate(ref clonedToken, state);
            helper.scopeToForm = clonedToken.Children().First().Value<JObject>();
            
            state.CurrentScopeToken.Remove(new LevelKey {Level = _levelCounter, Key = alias});

            if (helper.scopeProperties == null)
                helper.scopeProperties = new List<string>();

            helper.scopeProperties.Add(propertyName);
            _levelCounter--;
        }

        private void ConditionalGroupOperation(string propertyName, string arguments, State state, TransformHelper helper, JToken childToken)
        {
            object functionResult = ParseFunction(arguments, state);
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

                RecursiveEvaluate(ref childToken, state);

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

        private void EvalOperation(JProperty property, string arguments, State state, TransformHelper helper)
        {
            object functionResult = ParseFunction(arguments, state);

            object val;
            if (property.Value.Type == JTokenType.String)
            {
                val = ParseFunction(property.Value.Value<string>(), state);
            }
            else
            {
                var propVal = property.Value;
                RecursiveEvaluate(ref propVal, state);
                val = property.Value;
            }

            JProperty clonedProperty = new JProperty(functionResult.ToString(), val);

            if (helper.loopProperties == null)
                helper.loopProperties = new List<string>();

            helper.loopProperties.Add(property.Name);

            if (helper.tokensToAdd == null)
            {
                helper.tokensToAdd = new List<JToken>();
            }
            helper.tokensToAdd.Add(clonedProperty);
        }

        private void BulkOperations(JEnumerable<JToken> arrayValues, State state, TransformHelper helper)
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
                        helper.selectedTokens.Add(Copy(arguments, state));
                    }
                    else if (functionName == "replace")
                    {
                        if (helper.tokensToReplace == null)
                            helper.tokensToReplace = new Dictionary<string, JToken>();

                        var replaceResult = Replace(arguments, state);
                        helper.tokensToReplace.Add(replaceResult.Key, replaceResult.Value);
                    }
                    else if (functionName == "delete")
                    {
                        if (helper.tokensToDelete == null)
                            helper.tokensToDelete = new List<JToken>();

                        helper.tokensToDelete.Add(Delete(arguments, state));
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

        private IEnumerable<object> TransformArray(JEnumerable<JToken> children, State state)
        {
            var result = new List<object>();

            foreach (JToken arrEl in children)
            {
                object itemToAdd = arrEl.Value<JToken>();
                if (arrEl.Type == JTokenType.String && arrEl.ToString().Trim().StartsWith("#"))
                {
                    itemToAdd = ParseFunction(arrEl.ToString(), state);
                }
                result.Add(itemToAdd);
            }

            return result;
        }

        #region Copy
        private JToken Copy(string arguments, State state)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            string path = argumentArr[0];
            if (!(ParseArgument(state, path) is string jsonPath))
            {
                throw new ArgumentException($"Invalid path for #copy: '{argumentArr[0]}' resolved to null!");
            }

            string alias = null;
            if (argumentArr.Length > 1)
            {
                alias = ParseArgument(state, argumentArr[1]) as string;
                if (!state.CurrentArrayToken.Any(a => a.Key.Key == alias))
                {
                    throw new ArgumentException($"Unknown loop alias: '{argumentArr[1]}'");
                }
            }
            JToken input = alias != null ? state.CurrentArrayToken.Single(a => a.Key.Key == alias).Value : state.CurrentArrayToken.Last().Value ?? state.GetInput();
            JToken selectedToken = GetSelectableToken(input, Context).Select(jsonPath);
            return selectedToken;
        }

        #endregion

        #region Replace
        private KeyValuePair<string, JToken> Replace(string arguments, State state)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            if (argumentArr.Length < 2)
            {
                throw new Exception("Function #replace needs at least two arguments - 1. path to be replaced, 2. token to replace with.");
            }
            if (!(ParseArgument(state, argumentArr[0]) is string key))
            {
                throw new ArgumentException($"Invalid path for #replace: '{argumentArr[0]}' resolved to null!");
            }
            object str = ParseArgument(state, argumentArr[1]);
            JToken newToken = GetToken(str);
            return new KeyValuePair<string, JToken>(key, newToken);
        }

        #endregion

        #region Delete
        private string Delete(string argument, State state)
        {
            if (!(ParseArgument(state, argument) is string result))
            {
                throw new ArgumentException($"Invalid path for #delete: '{argument}' resolved to null!");
            }
            return result;
        }
        #endregion

        #region ParseFunction

        private object ParseFunction(string functionString, State state)
        {
            try
            {
                object output = null;
                if (!ExpressionHelper.TryParseFunctionNameAndArguments(functionString, out string functionName, out string argumentString))
                {
                    return functionName;
                }

                string[] arguments = ExpressionHelper.SplitArguments(argumentString, Context.EscapeChar);
                var listParameters = new List<object>();

                if (functionName == "ifcondition")
                {
                    output = GetConditionalOutput(arguments, state);
                }
                else
                {
                    int i = 0;
                    for (; i < (arguments?.Length ?? 0); i++)
                    {
                        output = ParseArgument(state, arguments[i]);
                        output = LookInTransformed(output, arguments[i], state);
                        listParameters.Add(output);
                    }
                    
                    var convertParameters = true;
                    if (new[] { "concat", "xconcat", "currentproperty" }.Contains(functionName))
                    {
                        convertParameters = false;
                    }

                    output = GetFunctionOutput(functionName, listParameters, convertParameters, state);
                }

                return output;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while calling function : " + functionString + " - " + ex.Message, ex);
                // throw;
            }
        }

        private object ParseApplyOver(IList<object> listParameters, State state)
        {
            object output;
            JToken tmpContext = state.GetInput(), contextInput = state.GetInput();
            if (state.ParentArray.Any())
            {
                var alias = ParseLoopAlias(listParameters, 3, state.ParentArray.Last().Key.Key);
                contextInput = state.GetAliasToken(alias);
            }

            var input = JToken.Parse(Transform(listParameters.ElementAt(0).ToString(), contextInput.ToString()));
            //Context.Input = input;
            
            IDictionary<LevelKey, JToken> tmpArray = new Dictionary<LevelKey, JToken>(state.CurrentArrayToken);
            IDictionary<LevelKey, JToken> tmpScope = new Dictionary<LevelKey, JToken>(state.CurrentScopeToken);

            state.CurrentArrayToken.Clear();
            state.CurrentArrayToken.Add(new LevelKey { Key = "root", Level = 0 }, input);

            state.CurrentScopeToken.Clear();
            state.CurrentScopeToken.Add(new LevelKey { Key = "root", Level = 0 }, input);

            if (listParameters.ElementAt(1).ToString().Trim().Trim('\'').StartsWith("{"))
            {
                var jobj = JObject.Parse(listParameters.ElementAt(1).ToString().Trim().Trim('\''));
                output = new JsonTransformer(Context).Transform(jobj, input);
            }
            else if (listParameters.ElementAt(1).ToString().Trim().Trim('\'').StartsWith("["))
            {
                var jarr = JArray.Parse(listParameters.ElementAt(1).ToString().Trim().Trim('\''));
                output = new JsonTransformer(Context).Transform(jarr, input);
            }
            else
            {
                output = ParseFunction(listParameters.ElementAt(1).ToString().Trim().Trim('\''), state);
            }
            // Context.Input = tmpContext;
            
            state.CurrentArrayToken.Clear();
            foreach (KeyValuePair<LevelKey, JToken> item in tmpArray)
            {
                state.CurrentArrayToken.Add(item);
            }
            
            state.CurrentScopeToken.Clear();
            foreach (KeyValuePair<LevelKey, JToken> item in tmpScope)
            {
                state.CurrentScopeToken.Add(item);
            }
            return output;
        }

        private string ParseLoopAlias(IList<object> listParameters, int index, string defaultValue)
        {
            string alias;
            if (listParameters != null && listParameters.Count >= index)
            {
                alias = (listParameters[index - 1] as string).Trim();
                listParameters.RemoveAt(index - 1);
            }
            else
            {
                alias = defaultValue;
            }
            return alias;
        }

        private object ParseArgument(State state, string argument)
        {
            object output = argument;
            var trimmedArgument = argument.Trim();
            if (trimmedArgument.StartsWith("#"))
            {
                return ParseFunction(trimmedArgument, state);
            }
            else if (trimmedArgument.StartsWith($"{Context.EscapeChar}#"))
            {
                output = ExpressionHelper.UnescapeSharp(argument, Context.EscapeChar);
            }
            return output;
        }

        private object GetConditionalOutput(string[] arguments, State state)
        {
            var condition = ParseArgument(state, arguments[0]);
            condition = LookInTransformed(condition, arguments[0], state);
            var value = ParseArgument(state, arguments[1]);
            value = LookInTransformed(value, arguments[1], state);
            
            var equal = ComparisonHelper.Equals(condition, value, Context.EvaluationMode);
            var index = equal ? 2 : 3;

            return ParseArgument(state, arguments[index]);
        }

        private object GetFunctionOutput(string functionName, IList<object> listParameters, bool convertParameters, State state)
        {
            object output;
            if (new[] { "currentvalue", "currentindex", "lastindex", "lastvalue" }.Contains(functionName))
            {
                var alias = ParseLoopAlias(listParameters, 1, state.ParentArray.Last().Key.Key);
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName, new object[] { state.ParentArray.Single(p => p.Key.Key == alias).Value, state.CurrentArrayToken.Single(p => p.Key.Key == alias).Value }, convertParameters, Context);
            }
            else if (new[] { "currentvalueatpath", "lastvalueatpath" }.Contains(functionName))
            {
                var alias = ParseLoopAlias(listParameters, 2, state.ParentArray.Last().Key.Key);
                output = ReflectionHelper.Caller<T>(
                    null,
                    "JUST.Transformer`1",
                    functionName,
                    new[] { state.ParentArray.Single(p => p.Key.Key == alias).Value, state.CurrentArrayToken.Single(p => p.Key.Key == alias).Value }
                        .Concat(listParameters.ToArray()
                        .Concat(new object[] { Context })).ToArray(),
                    convertParameters,
                    Context);
            }
            else if (functionName == "currentproperty")
            {
                var alias = ParseLoopAlias(listParameters, 1, state.ParentArray.Last().Key.Key);
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName,
                    new object[] { state.ParentArray.Single(p => p.Key.Key == alias).Value, state.CurrentArrayToken.Single(p => p.Key.Key == alias).Value, Context },
                    convertParameters, Context);
            }
            else if (functionName == "customfunction")
            {
                listParameters.Add(state.CurrentScopeToken.Last().Value);
                output = CallCustomFunction(listParameters.ToArray());
            }
            else if (Context.IsRegisteredCustomFunction(functionName))
            {
                var methodInfo = Context.GetCustomMethod(functionName);
                listParameters.Add(state.CurrentScopeToken.Last().Value);
                output = ReflectionHelper.InvokeCustomMethod<T>(methodInfo, listParameters.ToArray(), convertParameters, Context);
            }
            else if (Regex.IsMatch(functionName, ReflectionHelper.EXTERNAL_ASSEMBLY_REGEX))
            {
                listParameters.Add(state.CurrentScopeToken.Last().Value);
                output = ReflectionHelper.CallExternalAssembly<T>(functionName, listParameters.ToArray(), Context);
            }
            else if (new[] { "xconcat", "xadd",
                "mathequals", "mathgreaterthan", "mathlessthan", "mathgreaterthanorequalto", "mathlessthanorequalto",
                "stringcontains", "stringequals"}.Contains(functionName))
            {
                object[] oParams = new object[1];
                oParams[0] = listParameters.Concat(new object[] { Context }).ToArray();
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName, oParams, convertParameters, Context);
            }
            else if (functionName == "applyover")
            {
                output = ParseApplyOver(listParameters, state);
            }
            else
            {
                var input = state.GetAliasToken(state.GetHigherAlias()); //((JUSTContext)listParameters.Last()).Input;
                if (input != state.Transformer)
                {
                    if (functionName != "valueof")
                    {
                        //((JUSTContext)listParameters.Last()).Input = state.CurrentArrayToken.Last().Value;
                        listParameters.Add(state.CurrentArrayToken.Last().Value);
                    }
                    else if (listParameters.Count >= 2)
                    {
                        //((JUSTContext)listParameters.Last()).Input = state.CurrentScopeToken.Single(p => p.Key.Key == listParameters[1].ToString()).Value;
                        listParameters.Add(state.CurrentScopeToken.Single(p => p.Key.Key == listParameters[1].ToString()).Value);
                        listParameters.Remove(listParameters.ElementAt(listParameters.Count - 2));
                    }
                    else
                    {
                        //((JUSTContext)listParameters.Last()).Input = state.CurrentScopeToken.Last().Value;
                        listParameters.Add(state.CurrentScopeToken.Last().Value);
                    }
                }
                else
                {
                    //((JUSTContext)listParameters.Last()).Input = state.CurrentScopeToken.Last().Value;
                    listParameters.Add(state.CurrentScopeToken.Last().Value);
                }

                //listParameters.Add(state.GetInput());
                listParameters.Add(Context);
                
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName, listParameters.ToArray(), convertParameters, Context);
            }
            return output;
        }

        private object LookInTransformed(object output, string propVal, State state)
        {
            if (output == null && Context.IsLookInTransformed())
            {
                state.CurrentScopeToken.Add(new LevelKey { Key = "transformed", Level = ++_levelCounter }, state.Transformer.Root);
                //JToken tmpContext = state.GetInput(); //Context.Input;
                //Context.Input = state.Transformer.Root;
                output = ParseFunction(propVal, state);
                //Context.Input = tmpContext;
                state.CurrentScopeToken.Remove(state.CurrentScopeToken.Single(s => s.Key.Key == "transformed"));
                _levelCounter--;
            }
            return output;
        }

        private object CallCustomFunction(object[] parameters)
        {
            object[] customParameters = new object[parameters.Length - 3];
            string functionString = string.Empty;
            string dllName = string.Empty;
            int i = 0;
            foreach (object parameter in parameters)
            {
                if (i == 0)
                    dllName = parameter.ToString();
                else if (i == 1)
                    functionString = parameter.ToString();
                else
                if (i != (parameters.Length - 1))
                    customParameters[i - 2] = parameter;

                i++;
            }

            int index = functionString.LastIndexOf(".");

            string className = functionString.Substring(0, index);
            string functionName = functionString.Substring(index + 1, functionString.Length - index - 1);

            className = className + "," + dllName;

            return ReflectionHelper.Caller<T>(null, className, functionName, customParameters, true, Context);

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
    }
}
