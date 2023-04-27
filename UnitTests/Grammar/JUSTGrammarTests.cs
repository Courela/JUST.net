using System;
using System.Collections.Generic;
using System.IO;
using CSharpParserGenerator;
using JUST.net.Selectables;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace JUST.UnitTests.Gramar
{
    [TestFixture]
    [Category("Grammar")]
    public class JUSTGrammarTests
    {
        private JToken _input;

        [SetUp]
        public void Setup()
        {
            string jsonContent = File.ReadAllText("Inputs/grammar_input.json");
            this._input = JToken.Parse(jsonContent);
        }

        [TestCase("#valueof($.test)", 2)]
        [TestCase("#valueof(#valueof($.path))", "OpenDoc()")]
        [TestCase("#ifcondition(expr1,expr2,truevalue,falsevalue)", "falsevalue")]
        [TestCase("#ifcondition(#valueof($.menu.host),github,#valueof($.menu.repository),fail)", "JUST")]
        [TestCase("#lastindexof(thisisandveryunuasualandlongstring,and)", 21)]
        [TestCase("#lastindexof(#valueof($.stringref),#valueof($.stringref_result))", 21)]
        [TestCase("#firstindexof(thisisandveryunuasualandlongstring,and)", 6)]
        [TestCase("#firstindexof(#valueof($.stringref),#valueof($.stringref_result))", 6)]
        [TestCase("#substring(thisisandveryunuasualandlongstring,9,11)", "veryunuasua")]
        [TestCase("#substring(#valueof($.stringref),9,11)", "veryunuasua")]
        [TestCase("#concat(string1_,_string2)", "string1__string2")]
        [TestCase("#concat(#valueof($.menu.id.file),#valueof($.menu.value.window))", "csvpopup")]
        [TestCase("#concat(#valueof($.missing),#valueof($.menu.missing))", null)]
        [TestCase("#length(thisisandveryunuasualandlongstring)", 34)]
        [TestCase("#length(#valueof($.stringref))", 34)]
        [TestCase("#length($.numbers)", 5)]
        [TestCase("#length(#valueof($.numbers))", 5)]
        [TestCase("#add(1,3)", 4)]
        [TestCase("#add(#valueof($.numbers[0]),3)", 4)]
        [TestCase("#subtract(5,1)", 4)]
        [TestCase("#subtract(#valueof($.numbers[4]),#valueof($.numbers[0]))", 4)]
        [TestCase("#multiply(2,3)", 6)]
        [TestCase("#multiply(#valueof($.numbers[1]),#valueof($.numbers[2]))", 6)]
        [TestCase("#divide(4,2)", 2)]
        [TestCase("#divide(#valueof($.numbers[3]),#valueof($.numbers[1]))", 2)]
        [TestCase("#round(10.005,2)", 10.01)]
        [TestCase("#round(#valueof($.decimal_nr),2)", 123.46)]
        [TestCase("#mathequals(2,3)", false)]
        [TestCase("#mathequals(#valueof($.numbers[2]),3)", true)]
        [TestCase("#mathgreaterthan(1,2)", false)]
        [TestCase("#mathgreaterthan(#valueof($.numbers[2]),#valueof($.numbers[1]))", true)]
        [TestCase("#mathlessthan(5,4)", false)]
        [TestCase("#mathlessthan(#valueof($.numbers[2]),#valueof($.numbers[4]))", true)]
        [TestCase("#mathgreaterthanorequalto(3,4)", false)]
        [TestCase("#mathgreaterthanorequalto(#valueof($.numbers[2]),#valueof($.numbers[2]))", true)]
        [TestCase("#mathlessthanorequalto(3,2)", false)]
        [TestCase("#mathlessthanorequalto(#valueof($.numbers[2]),#valueof($.numbers[2]))", true)]
        [TestCase("#stringequals(two,one)", false)]
        [TestCase("#stringequals(#valueof($.d[0]),#valueof($.string_one))", true)]
        [TestCase("#stringcontains(and,z)", false)]
        [TestCase("#stringcontains(#valueof($.stringref),#valueof($.stringref_result))", true)]
        [TestCase("#concatall($.d)", "onetwothree")]
        [TestCase("#concatall(#valueof($.d))", "onetwothree")]
        [TestCase("#sum($.numbers)", 15)]
        [TestCase("#sum(#valueof($.numbers))", 15)]
        [TestCase("#average($.numbers)", 3)]
        [TestCase("#average(#valueof($.numbers))", 3)]
        [TestCase("#min($.numbers)", 1)]
        [TestCase("#min(#valueof($.numbers))", 1)]
        [TestCase("#max($.numbers)", 5)]
        [TestCase("#max(#valueof($.numbers))", 5)]
        [TestCase("#concatallatpath(#valueof($.x),$.v.a)", "a1,a2,a3b1,b2c1,c2,c3")]
        [TestCase("#sumatpath(#valueof($.x),$.v.c)", 60)]
        [TestCase("#averageatpath(#valueof($.x),$.v.c)", 20)]
        [TestCase("#minatpath(#valueof($.x),$.v.b)", 1)]
        [TestCase("#maxatpath(#valueof($.x),$.v.b)", 3)]
        [TestCase("#toboolean(0)", false)]
        [TestCase("#toboolean(1)", true)]
        [TestCase("#toboolean(#valueof($.booleans.affirmative_string))", true)]
        [TestCase("#toboolean(#valueof($.booleans.negative_string))", false)]
        [TestCase("#toboolean(#valueof($.booleans.affirmative_int))", true)]
        [TestCase("#toboolean(#valueof($.booleans.negative_int))", false)]
        [TestCase("#tostring(#valueof($.strings.integer))", "123")]
        [TestCase("#tostring(#valueof($.strings.decimal))", "12.34")]
        [TestCase("#tostring(#valueof($.strings.affirmative_boolean))", "True")]
        [TestCase("#tostring(#valueof($.strings.negative_boolean))", "False")]
        [TestCase("#tointeger(#valueof($.integers.string))", 123)]
        [TestCase("#tointeger(#valueof($.integers.decimal))", 1)]
        [TestCase("#tointeger(#valueof($.integers.affirmative_boolean))", 1)]
        [TestCase("#tointeger(#valueof($.integers.negative_boolean))", 0)]
        [TestCase("#todecimal(#valueof($.decimals.integer))", 123.0)]
        [TestCase("#todecimal(#valueof($.decimals.string))", 1.23)]
        [TestCase("#isnumber(#valueof($.integer))", true)]
        [TestCase("#isnumber(#valueof($.decimal))", true)]
        [TestCase("#isnumber(#valueof($.boolean))", false)]
        [TestCase("#isboolean(#valueof($.boolean))", true)]
        [TestCase("#isboolean(#valueof($.integer))", false)]
        [TestCase("#isstring(#valueof($.string))", true)]
        [TestCase("#isstring(#valueof($.array))", false)]
        [TestCase("#isarray(#valueof($.array))", true)]
        [TestCase("#isarray(#valueof($.decimal))", false)]
        [TestCase("#exists($.bulk.tree.branch)", true)]
        [TestCase("#exists($.dummy.not.exists)", false)]
        [TestCase("#existsandnotempty($.bulk.tree.branch.leaf)", true)]
        [TestCase("#existsandnotempty($.empty)", false)]
        [TestCase("#ifgroup(#exists($.bulk.tree.branch))", true)]
        [TestCase("#ifgroup(#exists($.dummy.not.exists))", false)]
        [TestCase("#eval(prop)", "prop")]
        [TestCase("#eval(#valueof($.string_one))", "one")]
        public void ValidateGrammar(string expression, object result)
        {
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(expression, context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarCopy()
        {
            const string expression = "#copy($.drugs)";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            JToken result = JToken.Parse(
                "[{ \"code\": \"001\", \"display\": \"Drug1\" }," +
                 "{ \"code\": \"002\", \"display\": \"Drug2\" }]");
            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarReplace()
        {
            const string expression = "#replace($.tree.branch.extra,#valueof($.tree.ladder))";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.bulk"),
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            JToken result = JToken.Parse(
                "{ \"tree\": {" +
                    "\"branch\": { " +
                        "\"leaf\": \"green\"," +
                        "\"flower\": \"red\"," +
                        "\"bird\": \"crow\"," +
                        "\"extra\": { " +
                            "\"wood\": \"treehouse\" } }," +
                    "\"ladder\": { \"wood\": \"treehouse\" } } }");
            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarDelete()
        {
            const string expression = "#delete($.tree.branch.bird)";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.bulk"),
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            JToken result = JToken.Parse(
                "{ \"tree\": {" +
                    "\"branch\": { " +
                        "\"leaf\": \"green\"," +
                        "\"flower\": \"red\"," +
                        "\"extra\": { " +
                            "\"twig\": \"birdnest\" } }," +
                    "\"ladder\": { \"wood\": \"treehouse\" } } }");
            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarLoop()
        {
            const string expression = "#loop($.numbers)";

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
                CurrentArrayElement = new Dictionary<string, JToken>() {{ "root", this._input }}
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            JToken result = JToken.Parse("[ 1, 2, 3, 4, 5 ]");
            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [TestCase("#currentvalue()", "{ \"country\": { \"name\": \"UK\", \"language\": \"english\" } }")]
        [TestCase("#lastvalue()", "{ \"country\": { \"name\": \"Sweden\", \"language\": \"swedish\" } }")]
        public void ValidateGrammarLoopJsonValues(string expression, string result)
        {
            const string arrayAlias = "loop1";
            JArray parentArray = this._input.SelectToken("$.arrayobjects") as JArray;
            JToken loopElement = this._input.SelectToken("$.arrayobjects[1]");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.arrayobjects"),
                EvaluationMode = EvaluationMode.Strict,
                ParentArray = new Dictionary<string, JArray>{ { arrayAlias, parentArray }},
                CurrentArrayElement = new Dictionary<string, JToken>{ { arrayAlias, loopElement }},
                LoopCounter = 1,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(JToken.Parse(result), parseResult.Value);
            });
        }

        [TestCase("#currentindex()", 1)]
        [TestCase("#lastindex()", 2)]
        public void ValidateGrammarLoopIndexes(string expression, int result)
        {
            const string arrayAlias = "loop1";
            JArray parentArray = this._input.SelectToken("$.arrayobjects") as JArray;
            JToken loopElement = this._input.SelectToken("$.arrayobjects[1]");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.arrayobjects"),
                EvaluationMode = EvaluationMode.Strict,
                ParentArray = new Dictionary<string, JArray>{ { arrayAlias, parentArray }},
                CurrentArrayElement = new Dictionary<string, JToken>{ { arrayAlias, loopElement }},
                LoopCounter = 1,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [TestCase("#currentvalueatpath($.country.name)", "UK")]
        [TestCase("#lastvalueatpath($.country.language)", "swedish")]
        public void ValidateGrammarLoopPrimiteValues(string expression, string result)
        {
            const string arrayAlias = "loop1";
            JArray parentArray = this._input.SelectToken("$.arrayobjects") as JArray;
            JToken loopElement = this._input.SelectToken("$.arrayobjects[1]");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.arrayobjects"),
                EvaluationMode = EvaluationMode.Strict,
                ParentArray = new Dictionary<string, JArray>{ { arrayAlias, parentArray }},
                CurrentArrayElement = new Dictionary<string, JToken>{ { arrayAlias, loopElement }},
                LoopCounter = 1,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [TestCase("#eval(#currentproperty())", "dog")]
        public void ValidateGrammarLoopObject(string expression, string result)
        {
            const string arrayAlias = "dog";
            JArray parentArray = this._input.SelectToken("$.animals") as JArray;
            JToken loopElement = this._input.SelectToken("$.animals");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.animals"),
                EvaluationMode = EvaluationMode.Strict,
                ParentArray = new Dictionary<string, JArray>{ { arrayAlias, parentArray }},
                CurrentArrayElement = new Dictionary<string, JToken>{ { arrayAlias, loopElement }},
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarXconcatString()
        {
            const string expression = "#xconcat(abc, def, ghi)";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual("abcdefghi", parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarXconcatArrays()
        {
            const string expression = "#xconcat(#valueof($.drugs), #valueof($.pa), #valueof($.sa))";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            JToken result = JToken.Parse(
                "[{ \"code\": \"001\", \"display\": \"Drug1\" }," +
                 "{ \"code\": \"002\", \"display\": \"Drug2\" }," +
                 "{ \"code\": \"pa1\", \"display\": \"PA1\" }," +
                 "{ \"code\": \"pa2\", \"display\": \"PA2\" }," +
                 "{ \"code\": \"sa1\", \"display\": \"SA1\" }," +
                 "{ \"code\": \"sa2\", \"display\": \"SA2\" }]");
            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarGroupArrayBy()
        {
            const string expression = "#grouparrayby($.Forest,type,all)";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            JToken result = JToken.Parse(
                "[{" +
                    "\"type\": \"Mammal\", " +
                    "\"all\": [{" +
                        "\"qty\": 1," +
                        "\"name\": \"Hippo\"" +
                    "}, {" +
                        "\"qty\": 1,"+
                        "\"name\": \"Elephant\""+
                    "}, {" +
                        "\"qty\": 10," +
                        "\"name\": \"Dog\""+
                    "}]"+
                "}, {" +
                    "\"type\": \"Bird\","+
                    "\"all\": [{" +
                        "\"qty\": 2," +
                        "\"name\": \"Sparrow\""+
                    "}, {" +
                        "\"qty\": 3," +
                        "\"name\": \"Parrot\"" +
                    "}]" +
                "}, {" +
                    "\"type\": \"Amphibian\"," +
                    "\"all\": [{" +
                        "\"qty\": 300," +
                        "\"name\": \"Lizard\"" +
                    "}]" +
                "}]");
            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [TestCase("#declared_function(string_arg)", "StringMethod", "success string_arg")]
        [TestCase("#declared_function(#valueof($.menu))", "JTokenMethod", "success JUST")]
        [TestCase("#declared_function(#valueof($.menu), #valueof($.string))", "MixMethod", "success JUST abc")]
        public void ValidateGrammarRegisteredFunction(string expression, string method, string result)
        {
            const string func = "declared_function";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            context.RegisterCustomFunction(null, "JUST.UnitTests.Gramar.JUSTGrammarTests", method, func);

            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(result, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarCustomFunction()
        {
            const string expression = "#customfunction(ExternalMethods,SeasonsHelper.Season.findseasontemperaturetable,#valueof($.data))";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            
            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(0, parseResult.Errors.Count);
                Assert.True(parseResult.Success);
                Assert.AreEqual(true, parseResult.Value);
            });
        }

        [Test]
        public void ValidateGrammarNotRegisteredFunctionStrict()
        {
            const string func = "declared_function";
            const string expression = $"#{func}(dummy_arg)";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };

            ParseResult parseResult = JUST.Gramar.Grammar<JsonPathSelectable>.Instance.Parse(
                expression,
                context);
            
            PrintResults(parseResult);

            Assert.Multiple(() => {
                Assert.AreEqual(1, parseResult.Errors.Count);
                Assert.AreEqual("Function not registered: declared_function", parseResult.Errors[0].Description);
                Assert.False(parseResult.Success);
            });
        }

        public string StringMethod(string arg1)
        {
            return $"success {arg1}";
        }

        public string JTokenMethod(JToken arg1)
        {
            return $"success {arg1.SelectToken("$.repository")}";
        }

        public string MixMethod(JToken arg1, string arg2)
        {
            return $"success {arg1.SelectToken("$.repository")} {arg2}";
        }

        private static void PrintResults(ParseResult result)
        {
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                {
                    Console.WriteLine($"{err.Type} - {err.Description}");
                }
            }
            else
            {
                Console.WriteLine(result.Text);
                if (result.Value is Array arr)
                {
                    foreach (var item in arr)
                    {
                        Console.WriteLine(item);
                    }
                }
                else
                {
                    Console.WriteLine(result.Value);
                }
            }
        }
    }
}