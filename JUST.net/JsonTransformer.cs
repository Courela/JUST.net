﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            this.Grammar = Gramar.Grammar<T>.Instance;
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
            Context.Input = input;
            Context.CurrentArrayElement = new Dictionary<string, JToken>() { { RootAlias, input }};
            var parentToken = (JToken)transformer;
            RecursiveEvaluate(ref parentToken);
            return parentToken;
        }

        #region RecursiveEvaluate

        private void RecursiveEvaluate(ref JToken parentToken)
        {
            if (parentToken == null)
            {
                return;
            }

            JEnumerable<JToken> tokens = parentToken.Children();

            List<JToken> selectedTokens = null;
            Dictionary<string, JToken> tokensToReplace = null;
            List<JToken> tokensToDelete = null;

            List<string> loopProperties = null;
            List<string> condProps = null;
            JArray arrayToForm = null;
            JObject dictToForm = null;
            List<JToken> tokenToForm = null;
            List<JToken> tokensToAdd = null;

            bool isLoop = false;
            bool isBulk = false;

            for (int i = 0; i < tokens.Count(); i++)
            {
                var childToken = tokens.ElementAt(i);
                ParseToken(parentToken, ref selectedTokens, ref tokensToReplace, ref tokensToDelete, ref loopProperties, ref condProps, ref arrayToForm, ref dictToForm, ref tokenToForm, ref tokensToAdd, ref isLoop, ref isBulk, childToken);
            }

            if (selectedTokens != null)
            {
                CopyPostOperationBuildUp(parentToken, selectedTokens, this.Context);
            }
            if (tokensToReplace != null)
            {
                ReplacePostOperationBuildUp(parentToken, tokensToReplace, this.Context);
            }
            if (tokensToDelete != null)
            {
                DeletePostOperationBuildUp(parentToken, tokensToDelete, this.Context);
            }
            if (tokensToAdd != null)
            {
                AddPostOperationBuildUp(parentToken, tokensToAdd);
            }
            PostOperationsBuildUp(ref parentToken, tokenToForm);
            if (loopProperties != null || condProps != null)
            {
                LoopPostOperationBuildUp(ref parentToken, condProps, loopProperties, arrayToForm, dictToForm);
            }
        }

        private void ParseToken(JToken parentToken, ref List<JToken> selectedTokens, ref Dictionary<string, JToken> tokensToReplace, ref List<JToken> tokensToDelete, ref List<string> loopProperties, ref List<string> condProps, ref JArray arrayToForm, ref JObject dictToForm, ref List<JToken> tokenToForm, ref List<JToken> tokensToAdd, ref bool isLoop, ref bool isBulk, JToken childToken)
        {
            if (childToken.Type == JTokenType.Array && (parentToken as JProperty)?.Name.Trim() != "#")
            {
                IEnumerable<object> itemsToAdd = TransformArray(childToken.Children());
                BuildArrayToken(childToken as JArray, itemsToAdd);
            }
            else if (childToken.Type == JTokenType.Property && childToken is JProperty property && property.Name != null)
            {
                /* For looping*/
                isLoop = false;

                if (property.Name == "#" && property.Value.Type == JTokenType.Array && property.Value is JArray values)
                {
                    BulkOperations(values.Children(), ref selectedTokens, ref tokensToReplace, ref tokensToDelete);
                    isBulk = true;
                }
                else
                {
                    isBulk = false;
                    if (ExpressionHelper.TryParseFunctionNameAndArguments(property.Name, out string functionName, out string arguments))
                    {
                        ParsePropertyFunction(ref loopProperties, ref condProps, ref arrayToForm, ref dictToForm, ref tokenToForm, ref tokensToAdd, ref isLoop, childToken, property, functionName, arguments);
                        
                    }
                    else if (property.Value.ToString().Trim().StartsWith("#"))
                    {
                        var propVal = property.Value.ToString().Trim();
                        var output = ParseFunction(propVal);
                        output = LookInTransformed(output, propVal, parentToken);
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
                && Context.ParentArray != null && Context.CurrentArrayElement != null)
            {
                object newValue = ParseFunction(childToken.Value<string>());
                childToken.Replace(GetToken(newValue));
            }

            if (!isLoop && !isBulk)
            {
                RecursiveEvaluate(ref childToken);
            }
        }

        private void ParsePropertyFunction(ref List<string> loopProperties, ref List<string> condProps, ref JArray arrayToForm, ref JObject dictToForm, ref List<JToken> tokenToForm, ref List<JToken> tokensToAdd, ref bool isLoop, JToken childToken, JProperty property, string functionName, string arguments)
        {
            switch (functionName)
            {
                case "ifgroup":
                    ConditionalGroupOperation(property.Name, arguments, ref condProps, ref tokenToForm, childToken);
                    break;
                case "loop":
                    LoopOperation(property.Name, arguments, ref loopProperties, ref arrayToForm, ref dictToForm, childToken);
                    isLoop = true;
                    break;
                case "eval":
                    EvalOperation(property, arguments, ref loopProperties, ref tokensToAdd);
                    break;
                case "transform":
                    TranformOperation(property, arguments);
                    break;
            }
        }

        private void LoopOperation(string propertyName, string arguments, ref List<string> loopProperties, ref JArray arrayToForm, ref JObject dictToForm, JToken childToken)
        {
            JToken input = Context.Input;
            object function = ParseFunction(propertyName);
            JArray arrayToken = function is JArray ? function as JArray : JArray.FromObject(function);
            
            string key = Context.ParentArray.Last().Key;
            using (IEnumerator<JToken> elements = arrayToken.GetEnumerator())
            {
                if (arrayToForm == null)
                {
                    arrayToForm = new JArray();
                }
                
                while (elements.MoveNext())
                {
                    JToken clonedToken = childToken.DeepClone();
                    
                    if (Context.CurrentArrayElement.ContainsKey(key))
                    {
                        Context.CurrentArrayElement.Remove(key);
                    }
                    Context.CurrentArrayElement.Add(key,elements.Current);
                    
                    RecursiveEvaluate(ref clonedToken);
                    foreach (JToken replacedProperty in clonedToken.Children())
                    {
                        arrayToForm.Add(replacedProperty.Type != JTokenType.Null ? replacedProperty : new JObject());
                    }
                }
            }

            if (loopProperties == null)
            {
                loopProperties = new List<string>();
            }

            loopProperties.Add(propertyName);

            Context.ParentArray.Remove(key);
            Context.CurrentArrayElement.Remove(key);
            Context.LoopCounter--;
            Context.Input = input;
        }

        private void TranformOperation(JProperty property, string arguments)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);

            object functionResult = ParseArgument(null, argumentArr[0]);
            if (!(functionResult is string jsonPath))
            {
                throw new ArgumentException($"Invalid path for #transform: '{argumentArr[0]}' resolved to null!");
            }

            JToken selectedToken = null;
            string alias = null;
            if (argumentArr.Length > 1)
            {
                alias = ParseArgument(null, argumentArr[1]) as string;
                if (!(Context.CurrentArrayElement?.ContainsKey(alias) ?? false))
                {
                    throw new ArgumentException($"Unknown loop alias: '{argumentArr[1]}'");
                }
                JToken input = alias != null ? Context.CurrentArrayElement?[alias] : Context.CurrentArrayElement?.Last().Value ?? Context.Input;
                var selectable = GetSelectableToken(Context.CurrentArrayElement[alias], Context);
                selectedToken = selectable.Select(argumentArr[0]);
            }
            else
            {
                var selectable = GetSelectableToken(Context.CurrentArrayElement?.Last().Value ?? Context.Input, Context);
                selectedToken = selectable.Select(argumentArr[0]);
            }
            
            if (property.Value.Type == JTokenType.Array)
            {
                JToken originalInput = Context.Input;
                Context.Input = selectedToken;
                for (int i = 0; i < property.Value.Count(); i++)
                {
                    JToken token = property.Value[i];
                    if (token.Type == JTokenType.String)
                    {
                        var obj = ParseFunction(token.Value<string>());
                        token.Replace(GetToken(obj));
                    }
                    else
                    {
                        RecursiveEvaluate(ref token);
                    }
                    Context.Input = token;
                }
                
                Context.Input = originalInput;
            }
            property.Parent.Replace(property.Value[property.Value.Count() - 1]);
        }

        private void PostOperationsBuildUp(ref JToken parentToken, List<JToken> tokenToForm)
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

        private static void CopyPostOperationBuildUp(JToken parentToken, List<JToken> selectedTokens, JUSTContext context)
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

        private static void AddPostOperationBuildUp(JToken parentToken, List<JToken> tokensToAdd)
        {
            if (tokensToAdd != null)
            {
                foreach (JToken token in tokensToAdd)
                {
                    (parentToken as JObject).Add((token as JProperty).Name, (token as JProperty).Value);
                }
            }
        }

        private static void DeletePostOperationBuildUp(JToken parentToken, List<JToken> tokensToDelete, JUSTContext context)
        {

            foreach (string selectedToken in tokensToDelete)
            {
                JToken tokenToRemove = GetSelectableToken(parentToken, context).Select(selectedToken);

                if (tokenToRemove != null)
                    tokenToRemove.Ancestors().First().Remove();
            }

        }

        private static void ReplacePostOperationBuildUp(JToken parentToken, Dictionary<string, JToken> tokensToReplace, JUSTContext context)
        {

            foreach (KeyValuePair<string, JToken> tokenToReplace in tokensToReplace)
            {
                JsonPathSelectable selectable = JsonTransformer.GetSelectableToken(parentToken, context);
                JToken selectedToken = selectable.Select(tokenToReplace.Key);
                selectedToken.Replace(tokenToReplace.Value);
            }
        }

        private static void LoopPostOperationBuildUp(ref JToken parentToken, List<string> condProps, List<string> loopProperties, JArray arrayToForm, JObject dictToForm)
        {
            if (loopProperties != null)
            {
                if (parentToken is JObject obj)
                {
                    foreach (string propertyToDelete in loopProperties)
                    {
                        if (dictToForm == null && arrayToForm == null && parentToken.Count() <= 1)
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

            if (condProps != null)
            {
                if (parentToken is JObject obj)
                {
                    foreach (string propertyToDelete in condProps)
                    {
                        obj.Remove(propertyToDelete);
                    }
                }
            }

            if (dictToForm != null)
            {
                parentToken.Replace(dictToForm);
            }
            else if (arrayToForm != null)
            {
                if (parentToken.Parent != null)
                {
                    if (parentToken.Parent is JArray arr)
                    {
                        foreach (var item in arrayToForm)
                        {
                            arr.Add(item);
                        }
                        if (!parentToken.HasValues)
                        {
                            parentToken = arrayToForm;
                        }
                    }
                    else
                    {
                        parentToken.Replace(arrayToForm);
                    }
                }
                else
                {
                    parentToken = arrayToForm;
                }
            }
        }

        private void ConditionalGroupOperation(string propertyName, string arguments, ref List<string> condProps, ref List<JToken> tokenToForm, JToken childToken)
        {
            object functionResult = ParseFunction(arguments);
            bool result;
            try
            {
                result = (bool)ReflectionHelper.GetTypedValue(typeof(bool), functionResult, Context.EvaluationMode);
            }
            catch
            {
                if (Context.IsStrictMode()) { throw; }
                result = false;
            }

            if (result)
            {
                if (condProps == null)
                    condProps = new List<string>();

                condProps.Add(propertyName);

                RecursiveEvaluate(ref childToken);

                if (tokenToForm == null)
                {
                    tokenToForm = new List<JToken>();
                }

                foreach (JToken grandChildToken in childToken.Children())
                {
                    tokenToForm.Add(grandChildToken.DeepClone());
                }
            }
            else
            {
                if (condProps == null)
                {
                    condProps = new List<string>();
                }

                condProps.Add(propertyName);
                childToken.First.Replace(JToken.Parse("{}"));
            }
        }

        private void EvalOperation(JProperty property, string arguments, ref List<string> loopProperties, ref List<JToken> tokensToAdd)
        {
            object functionResult = ParseFunction(arguments);

            object val;
            if (property.Value.Type == JTokenType.String)
            {
                val = ParseFunction(property.Value.Value<string>());
            }
            else
            {
                var propVal = property.Value;
                RecursiveEvaluate(ref propVal);
                val = property.Value;
            }
            JProperty clonedProperty = new JProperty(functionResult.ToString(), val);

            if (loopProperties == null)
                loopProperties = new List<string>();

            loopProperties.Add(property.Name);

            if (tokensToAdd == null)
            {
                tokensToAdd = new List<JToken>();
            }
            tokensToAdd.Add(clonedProperty);
        }

        private void BulkOperations(JEnumerable<JToken> arrayValues, ref List<JToken> selectedTokens, ref Dictionary<string, JToken> tokensToReplace, ref List<JToken> tokensToDelete)
        {
            foreach (JToken arrayValue in arrayValues)
            {
                if (arrayValue.Type == JTokenType.String &&
                    ExpressionHelper.TryParseFunctionNameAndArguments(
                        arrayValue.Value<string>().Trim(), out string functionName, out string arguments))
                {
                    if (functionName == "copy")
                    {
                        if (selectedTokens == null)
                            selectedTokens = new List<JToken>();
                        selectedTokens.Add(Copy(arguments));
                    }
                    else if (functionName == "replace")
                    {
                        if (tokensToReplace == null)
                            tokensToReplace = new Dictionary<string, JToken>();

                        var replaceResult = Replace(arguments);
                        tokensToReplace.Add(replaceResult.Key, replaceResult.Value);
                    }
                    else if (functionName == "delete")
                    {
                        if (tokensToDelete == null)
                            tokensToDelete = new List<JToken>();

                        tokensToDelete.Add(Delete(arguments));
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

        private IEnumerable<object> TransformArray(JEnumerable<JToken> children)
        {
            var result = new List<object>();

            foreach (JToken arrEl in children)
            {
                object itemToAdd = arrEl.Value<JToken>();
                if (arrEl.Type == JTokenType.String && arrEl.ToString().Trim().StartsWith("#"))
                {
                    itemToAdd = ParseFunction(arrEl.ToString());
                }
                result.Add(itemToAdd);
            }

            return result;
        }

        #region Copy
        private JToken Copy(string arguments)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            string path = argumentArr[0];
            if (!(ParseArgument(null, path) is string jsonPath))
            {
                throw new ArgumentException($"Invalid path for #copy: '{argumentArr[0]}' resolved to null!");
            }

            string alias = null;
            if (argumentArr.Length > 1)
            {
                alias = ParseArgument(null, argumentArr[1]) as string;
                if (!(Context.CurrentArrayElement?.ContainsKey(alias) ?? false))
                {
                    throw new ArgumentException($"Unknown loop alias: '{argumentArr[1]}'");
                }
            }
            JToken input = alias != null ? Context.CurrentArrayElement?[alias] : Context.CurrentArrayElement?.Last().Value ?? Context.Input;
            JToken selectedToken = GetSelectableToken(input, Context).Select(jsonPath);
            return selectedToken;
        }

        #endregion

        #region Replace
        private KeyValuePair<string, JToken> Replace(string arguments)
        {
            string[] argumentArr = ExpressionHelper.SplitArguments(arguments, Context.EscapeChar);
            if (argumentArr.Length < 2)
            {
                throw new Exception("Function #replace needs at least two arguments - 1. path to be replaced, 2. token to replace with.");
            }
            if (!(ParseArgument(null, argumentArr[0]) is string key))
            {
                throw new ArgumentException($"Invalid path for #replace: '{argumentArr[0]}' resolved to null!");
            }
            object str = ParseArgument(null, argumentArr[1]);
            JToken newToken = GetToken(str);
            return new KeyValuePair<string, JToken>(key, newToken);
        }

        #endregion

        #region Delete
        private string Delete(string argument)
        {
            if (!(ParseArgument(null, argument) is string result))
            {
                throw new ArgumentException($"Invalid path for #delete: '{argument}' resolved to null!");
            }
            return result;
        }
        #endregion

        #region ParseFunction

        private object ParseFunction(string functionString)
        {
            try
            {
                ParseResult parseResult = this.Grammar.Parse(
                    functionString,
                    this.Context);
                if (!parseResult.Success && this.Context.IsStrictMode())
                {
                    throw new Exception($"Error parsing '{functionString}': " + string.Join(Environment.NewLine, parseResult.Errors.Select(e => e.Description)));
                }
                return parseResult.Value;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while calling function : " + functionString + " - " + ex.Message, ex);
            }
        }

        private object ParseApplyOver(IList<object> listParameters)
        {
            object output;

            JToken tmpContext = Context.Input, contextInput = Context.Input;
            if (Context.ParentArray != null)
            {
                var alias = ParseLoopAlias(listParameters, 3, Context.ParentArray.Last().Key);
                contextInput = Context.CurrentArrayElement[alias];
            }

            var input = JToken.Parse(Transform(listParameters[0].ToString(), contextInput.ToString()));
            Context.Input = input;
            if (listParameters[1].ToString().Trim().Trim('\'').StartsWith("{"))
            {
                var jobj = JObject.Parse(listParameters[1].ToString().Trim().Trim('\''));
                output = new JsonTransformer(Context).Transform(jobj, input);
            }
            else if (listParameters[1].ToString().Trim().Trim('\'').StartsWith("["))
            {
                var jarr = JArray.Parse(listParameters[1].ToString().Trim().Trim('\''));
                output = new JsonTransformer(Context).Transform(jarr, input);
            }
            else
            {
                output = ParseFunction(listParameters[1].ToString().Trim().Trim('\''));
            }
            Context.Input = tmpContext;
            return output;
        }
        private string ParseLoopAlias(IList<object> listParameters, int index, string defaultValue)
        {
            string alias;
            if (listParameters != null && listParameters.Count > index)
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

        private object ParseArgument(JToken parentToken, string argument)
        {
            object output = argument;
            var trimmedArgument = argument.Trim();
            if (trimmedArgument.StartsWith("#"))
            {
                output = ParseFunction(trimmedArgument);
            }
            else if (trimmedArgument.StartsWith($"{Context.EscapeChar}#"))
            {
                output = ExpressionHelper.UnescapeSharp(argument, Context.EscapeChar);
            }
            return output;
        }

        private object GetConditionalOutput(JToken parentToken, string[] arguments)
        {
            var condition = ParseArgument(parentToken, arguments[0]);
            condition = LookInTransformed(condition, arguments[0], parentToken);
            var value = ParseArgument(parentToken, arguments[1]);
            value = LookInTransformed(value, arguments[1], parentToken);
            var equal = ComparisonHelper.Equals(condition, value, Context.EvaluationMode);
            var index = (equal) ? 2 : 3;

            return ParseArgument(parentToken, arguments[index]);
        }

        private object GetFunctionOutput(string functionName, IList<object> listParameters, bool convertParameters)
        {
            object output = null;
            if (new[] { "currentvalue", "currentindex", "lastindex", "lastvalue" }.Contains(functionName))
            {
                var alias = ParseLoopAlias(listParameters, 1, Context.ParentArray.Last().Key);
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName, new object[] { Context.ParentArray[alias], Context.CurrentArrayElement[alias] }, convertParameters, Context);
            }
            else if (new[] { "currentvalueatpath", "lastvalueatpath" }.Contains(functionName))
            {
                var alias = ParseLoopAlias(listParameters, 2, Context.ParentArray.Last().Key);
                output = ReflectionHelper.Caller<T>(
                    null,
                    "JUST.Transformer`1",
                    functionName,
                    new[] { Context.ParentArray[alias], Context.CurrentArrayElement[alias] }.Concat(listParameters.ToArray()).ToArray(),
                    convertParameters,
                    Context);
            }
            else if (functionName == "currentproperty")
            {
                var alias = ParseLoopAlias(listParameters, 1, Context.ParentArray.Last().Key);
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName,
                    new object[] { Context.ParentArray[alias], Context.CurrentArrayElement[alias], Context },
                    convertParameters, Context);
            }
            else if (functionName == "customfunction")
                output = CallCustomFunction(listParameters.ToArray());
            else if (Context?.IsRegisteredCustomFunction(functionName) ?? false)
            {
                var methodInfo = Context.GetCustomMethod(functionName);
                output = ReflectionHelper.InvokeCustomMethod<T>(methodInfo, listParameters.ToArray(), convertParameters, Context);
            }
            else if (Regex.IsMatch(functionName, ReflectionHelper.EXTERNAL_ASSEMBLY_REGEX))
            {
                output = ReflectionHelper.CallExternalAssembly<T>(functionName, listParameters.ToArray(), Context);
            }
            else if (new[] { "xconcat", "xadd",
                        "mathequals", "mathgreaterthan", "mathlessthan", "mathgreaterthanorequalto", "mathlessthanorequalto",
                        "stringcontains", "stringequals"}.Contains(functionName))
            {
                object[] oParams = new object[1];
                oParams[0] = listParameters.ToArray();
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName, oParams, convertParameters, Context);
            }
            else if (functionName == "applyover")
            {
                output = ParseApplyOver(listParameters);
            }
            else
            {
                var input = ((JUSTContext)listParameters.Last()).Input;
                if (Context.CurrentArrayElement != null && functionName != "valueof")
                {
                    ((JUSTContext)listParameters.Last()).Input = Context.CurrentArrayElement.Last().Value;
                }
                output = ReflectionHelper.Caller<T>(null, "JUST.Transformer`1", functionName, listParameters.ToArray(), convertParameters, Context);
                ((JUSTContext)listParameters.ToArray().Last()).Input = input;
            }
            return output;
        }

        private object LookInTransformed(object output, string propVal, JToken parentToken)
        {
            if (output == null && Context.IsLookInTransformed())
            {
                JToken tmpContext = Context.Input;
                Context.Input = parentToken;
                output = ParseFunction(propVal);
                Context.Input = tmpContext;
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

        public void Dispose()
        {
            this.Grammar.Dispose();
        }
    }
}
