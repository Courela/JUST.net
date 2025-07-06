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
            char escapeChar = context != null ? context.EscapeChar : '/';
            this.Grammar = Gramar.Grammar<T>.GetInstance(escapeChar);
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
            RecursiveEvaluate(ref parentToken, null, input);
            return parentToken;
        }

        #region RecursiveEvaluate

        private void RecursiveEvaluate(ref JToken parentToken, LoopContext loopContext, JToken input)
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
                ParseToken(parentToken, loopContext, helper, childToken, input);
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
        }

        private void ParseToken(JToken parentToken, LoopContext loopContext, TransformHelper helper, JToken childToken, JToken input)
        {
            if (childToken.Type == JTokenType.Array && (parentToken as JProperty)?.Name.Trim() != "#")
            {
                IEnumerable<object> itemsToAdd = TransformArray(childToken.Children(), loopContext, input);
                BuildArrayToken(childToken as JArray, itemsToAdd);
            }
            else if (childToken.Type == JTokenType.Property && childToken is JProperty property && property.Name != null)
            {
                /* For looping*/
                helper.isLoop = false;

                if (property.Name == "#" && property.Value.Type == JTokenType.Array && property.Value is JArray values)
                {
                    BulkOperations(values.Children(), loopContext, helper, input);
                    helper.isBulk = true;
                }
                else
                {
                    helper.isBulk = false;
                    if (ExpressionHelper.TryParseFunctionNameAndArguments(property.Name, out string functionName, out string arguments))
                    {
                        ParsePropertyFunction(loopContext, helper, childToken, property, functionName, arguments, input);
                    }
                    else if (property.Value.ToString().Trim().StartsWith("#"))
                    {
                        var propVal = property.Value.ToString().Trim();
                        var output = ParseFunction(propVal, ref loopContext, input);
                        output = LookInTransformed(output, propVal, parentToken, loopContext);
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
                && loopContext != null)
            {
                object newValue = ParseFunction(childToken.Value<string>(), ref loopContext, input);
                childToken.Replace(GetToken(newValue));
            }

            if (!helper.isLoop && !helper.isBulk)
            {
                RecursiveEvaluate(ref childToken, loopContext, input);
            }
        }

        private void ParsePropertyFunction(LoopContext loopContext, TransformHelper helper, JToken childToken, JProperty property, string functionName, string arguments, JToken input)
        {
            switch (functionName)
            {
                case "ifgroup":
                    ConditionalGroupOperation(property.Name, arguments, loopContext, helper, childToken, input);
                    break;
                case "loop":
                    LoopOperation(property.Name, loopContext, helper, childToken, input);
                    helper.isLoop = true;
                    break;
                case "eval":
                    EvalOperation(property, arguments, loopContext, helper, input);
                    break;
                case "transform":
                    TranformOperation(property, arguments, loopContext, input);
                    break;
            }
        }

        private void LoopOperation(string propertyName, LoopContext loopContext, TransformHelper helper, JToken childToken, JToken input)
        {
            object function = ParseFunction(propertyName, ref loopContext, input);
            JArray arrayToken = function is JArray ? function as JArray : GetPropertiesArray(function, Context.IsStrictMode());
            
            string key = loopContext?.ParentArray.Last().Key ?? RootAlias;
            using (IEnumerator<JToken> elements = arrayToken.GetEnumerator())
            {
                while (elements.MoveNext())
                {
                    JToken clonedToken = childToken.DeepClone();

                    if (loopContext.CurrentArrayElement.ContainsKey(key))
                    {
                        loopContext.CurrentArrayElement.Remove(key);
                    }
                    loopContext.CurrentArrayElement.Add(key, elements.Current);

                    RecursiveEvaluate(ref clonedToken, loopContext, input);
                    if (function == arrayToken)
                    {
                        helper.arrayToForm ??= new JArray();
                        foreach (JToken replacedProperty in clonedToken.Children())
                        {
                            JToken tokenToAdd = replacedProperty.Type != JTokenType.Null ? replacedProperty : new JObject();
                            helper.arrayToForm.Add(tokenToAdd);
                        }
                    }
                    else
                    {
                        helper.dictToForm ??= new JObject();
                        foreach (JToken replacedProperty in clonedToken.Children())
                        {
                            helper.dictToForm.Add(replacedProperty);
                        }
                    }
                }
            }

            if (helper.loopProperties == null)
            {
                helper.loopProperties = new List<string>();
            }

            helper.loopProperties.Add(propertyName);

            loopContext.ParentArray.Remove(key);
            loopContext.CurrentArrayElement.Remove(key);
            _loopCounter--;
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

        private void TranformOperation(JProperty property, string arguments, LoopContext loopContext, JToken input)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);

            object functionResult = ParseArgument(null, loopContext, argumentArr[0], input);
            if (!(functionResult is string))
            {
                throw new ArgumentException($"Invalid path for #transform: '{argumentArr[0]}' resolved to null!");
            }

            JToken selectedToken;
            string alias;
            if (argumentArr.Length > 1)
            {
                alias = ParseArgument(null, loopContext, argumentArr[1], input) as string;
                if (!(loopContext?.CurrentArrayElement.ContainsKey(alias) ?? false))
                {
                    throw new ArgumentException($"Unknown loop alias: '{argumentArr[1]}'");
                }
                var selectable = GetSelectableToken(loopContext.CurrentArrayElement[alias], Context);
                selectedToken = selectable.Select(argumentArr[0]);
            }
            else
            {
                var selectable = GetSelectableToken(loopContext?.CurrentArrayElement.Last().Value ?? input, Context);
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
                        var obj = ParseFunction(token.Value<string>(), ref loopContext, transformInput);
                        token.Replace(GetToken(obj));
                    }
                    else
                    {
                        RecursiveEvaluate(ref token, i == 0 ? loopContext : null, transformInput);
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

        private void ConditionalGroupOperation(string propertyName, string arguments, LoopContext loopContext, TransformHelper helper, JToken childToken, JToken input)
        {
            object functionResult = ParseFunction(arguments, ref loopContext, input);
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

                RecursiveEvaluate(ref childToken, loopContext, input);

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

        private void EvalOperation(JProperty property, string arguments, LoopContext loopContext, TransformHelper helper, JToken input)
        {
            object functionResult = ParseFunction(arguments, ref loopContext, input);

            object val;
            if (property.Value.Type == JTokenType.String)
            {
                val = ParseFunction(property.Value.Value<string>(), ref loopContext, input);
            }
            else
            {
                var propVal = property.Value;
                RecursiveEvaluate(ref propVal, loopContext, input);
                val = property.Value;
            }
            JProperty clonedProperty = new JProperty(functionResult.ToString(), val);

            helper.loopProperties ??= new List<string>();
            helper.loopProperties.Add(property.Name);

            helper.tokensToAdd ??= new List<JToken>();
            helper.tokensToAdd.Add(clonedProperty);
        }

        private void BulkOperations(JEnumerable<JToken> arrayValues, LoopContext loopContext, TransformHelper helper, JToken input)
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
                        helper.selectedTokens.Add(Copy(arguments, loopContext, input));
                    }
                    else if (functionName == "replace")
                    {
                        if (helper.tokensToReplace == null)
                            helper.tokensToReplace = new Dictionary<string, JToken>();

                        var replaceResult = Replace(arguments, loopContext, input);
                        helper.tokensToReplace.Add(replaceResult.Key, replaceResult.Value);
                    }
                    else if (functionName == "delete")
                    {
                        if (helper.tokensToDelete == null)
                            helper.tokensToDelete = new List<JToken>();

                        helper.tokensToDelete.Add(Delete(arguments, loopContext, input));
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

        private IEnumerable<object> TransformArray(JEnumerable<JToken> children, LoopContext loopContext, JToken input)
        {
            var result = new List<object>();

            foreach (JToken arrEl in children)
            {
                object itemToAdd = arrEl.Value<JToken>();
                if (arrEl.Type == JTokenType.String && arrEl.ToString().Trim().StartsWith("#"))
                {
                    itemToAdd = ParseFunction(arrEl.ToString(), ref loopContext, input);
                }
                result.Add(itemToAdd);
            }

            return result;
        }

        #region Copy
        private JToken Copy(string arguments, LoopContext loopContext, JToken input)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            string path = argumentArr[0];
            if (!(ParseArgument(null, loopContext, path, input) is string jsonPath))
            {
                throw new ArgumentException($"Invalid path for #copy: '{argumentArr[0]}' resolved to null!");
            }

            string alias = null;
            if (argumentArr.Length > 1)
            {
                alias = ParseArgument(null, loopContext, argumentArr[1], input) as string;
                if (!(loopContext.CurrentArrayElement?.ContainsKey(alias) ?? false))
                {
                    throw new ArgumentException($"Unknown loop alias: '{argumentArr[1]}'");
                }
            }
            JToken localInput = alias != null ? loopContext.CurrentArrayElement[alias] : loopContext.CurrentArrayElement?.Last().Value ?? input;
            JToken selectedToken = GetSelectableToken(localInput, Context).Select(jsonPath);
            return selectedToken;
        }

        #endregion

        #region Replace
        private KeyValuePair<string, JToken> Replace(string arguments, LoopContext loopContext, JToken input)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            if (argumentArr.Length < 2)
            {
                throw new Exception("Function #replace needs at least two arguments - 1. path to be replaced, 2. token to replace with.");
            }
            if (!(ParseArgument(null, loopContext, argumentArr[0], input) is string key))
            {
                throw new ArgumentException($"Invalid path for #replace: '{argumentArr[0]}' resolved to null!");
            }
            object str = ParseArgument(null, loopContext, argumentArr[1], input);
            JToken newToken = GetToken(str);
            return new KeyValuePair<string, JToken>(key, newToken);
        }

        #endregion

        #region Delete
        private string Delete(string argument, LoopContext loopContext, JToken input)
        {
            if (!(ParseArgument(null, loopContext, argument, input) is string result))
            {
                throw new ArgumentException($"Invalid path for #delete: '{argument}' resolved to null!");
            }
            return result;
        }
        #endregion

        #region ParseFunction

        private object ParseFunction(string functionString, ref LoopContext loopContext, JToken input)
        {
            LoopContext localLoopContext = loopContext;

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
                JArray loopArray = JsonTransformer.GetLoopArray(loopToken, context.IsStrictMode());
                KeyValuePair<string, JArray> k = new KeyValuePair<string, JArray>(loopAlias ?? $"loop{++this._loopCounter}", loopArray);

                if (localLoopContext == null)
                {
                    localLoopContext = new LoopContext(null, null);
                }
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
            }
            return parseResult.Value;
        }

        public static string GetAlias(string alias, IDictionary<string, JToken> currentArrayElement)
        {
            return !string.IsNullOrEmpty(alias) ? alias : currentArrayElement.Last().Key;
        }

        public static JArray GetLoopArray(object loopToken, bool isStrictMode)
        {
            JArray result = new JArray();
            if (loopToken is Array)
            {
                result = JArray.FromObject(loopToken);
            }
            else if (loopToken is JArray)
            {
                result = loopToken as JArray;
            }
            else if (loopToken is JObject)
            {
                result = GetPropertiesArray(loopToken, isStrictMode);
            }
            return result;
        }

        private object ParseApplyOver(LoopContext loopContext, object[] parameters, JToken input)
        {
            object output;

            JToken contextInput = input;
            if (loopContext.ParentArray != null)
            {
                var alias = ParseLoopAlias(parameters, 3, loopContext.ParentArray.Last().Key);
                contextInput = loopContext.CurrentArrayElement[alias];
            }
            
            string localInput = Transform(parameters[0].ToString(), contextInput.ToString());
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
                output = ParseFunction(parameters[1].ToString().Trim().Trim('\''), ref loopContext, JToken.Parse(localInput));
            }
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

        private object ParseArgument(JToken parentToken, LoopContext loopContext, string argument, JToken input)
        {
            object output = argument;
            var trimmedArgument = argument.Trim();
            if (trimmedArgument.StartsWith("#"))
            {
                return ParseFunction(trimmedArgument, ref loopContext, input);
            }
            else if (trimmedArgument.StartsWith($"{Context.EscapeChar}#"))
            {
                output = ExpressionHelper.UnescapeSharp(argument, Context.EscapeChar);
            }
            return output;
        }

        private object GetConditionalOutput(JToken parentToken, string[] arguments, LoopContext loopContext, JToken input)
        {
            var condition = ParseArgument(parentToken, loopContext, arguments[0], input);
            condition = LookInTransformed(condition, arguments[0], parentToken, loopContext);
            var value = ParseArgument(parentToken, loopContext, arguments[1], input);
            value = LookInTransformed(value, arguments[1], parentToken, loopContext);
            var equal = ComparisonHelper.Equals(condition, value, Context.EvaluationMode);
            var index = (equal) ? 2 : 3;

            return ParseArgument(parentToken, loopContext, arguments[index], input);
        }

        private object LookInTransformed(object output, string propVal, JToken parentToken, LoopContext loopContext)
        {
            if (output == null && Context.IsLookInTransformed())
            {
                output = ParseFunction(propVal, ref loopContext, parentToken);
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
