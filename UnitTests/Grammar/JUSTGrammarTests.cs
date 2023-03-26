using System;
using System.Collections.Generic;
using System.IO;
using CSharpParserGenerator;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace JUST.UnitTests.Gramar
{
    [TestFixture]
    [Category("Grammar")]
    public class JUSTGrammarTests
    {
        private JToken _input;

        [OneTimeSetUp]
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
          [TestCase("#eval(prop)", "prop")]
          //[TestCase("#grouparrayby($.Forest,type,all)")]
        // [TestCase("#customfunction(JUST.NET.Test,JUST.NET.Test.Season.findseason,#valueof($.tree.branch.leaf),#valueof($.tree.branch.flower))")]
        // [TestCase("#declared_function(dummy_arg)")]
        public void ValidateGrammar(string expression, object result)
        {
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input,
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult<object> parseResult = JUST.Gramar.Grammar<object>.Instance.Parse(expression, null, null, null, context);
            
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
            ParseResult<object[]> parseResult = JUST.Gramar.Grammar<object[]>.Instance.Parse(
                expression,
                null,
                null,
                null,
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

        // [TestCase("#delete($.tree.branch.bird)")]
        [Test]
        public void ValidateGrammarReplace()
        {
            const string expression = "#replace($.tree.branch.extra,#valueof($.tree.ladder))";
            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.bulk"),
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult<object[]> parseResult = JUST.Gramar.Grammar<object[]>.Instance.Parse(
                expression,
                null,
                null,
                null,
                context);
            
            PrintResults(parseResult);

            JToken result = JToken.Parse(
                "{ \"tree\": { \"branch\": { " +
                                    "\"leaf\": \"green\", \"flower\": \"red\", \"bird\": \"crow\", \"extra\": { " +
                                        "\"ladder\": { \"wood\": \"treehouse\" } } }, \"ladder\": { \"wood\": \"treehouse\" } }");
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
            };
            ParseResult<object[]> parseResult = JUST.Gramar.Grammar<object[]>.Instance.Parse(
                expression,
                null,
                null,
                null,
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
            JToken parentArray = this._input.SelectToken("$.arrayobjects");
            JToken loopElement = this._input.SelectToken("$.arrayobjects[1]");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.arrayobjects"),
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult<JObject> parseResult = JUST.Gramar.Grammar<JObject>.Instance.Parse(
                expression,
                new Dictionary<string, JToken>{ { arrayAlias,  parentArray }},
                new Dictionary<string, JToken>{ { arrayAlias,  loopElement }},
                arrayAlias,
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
            JToken parentArray = this._input.SelectToken("$.arrayobjects");
            JToken loopElement = this._input.SelectToken("$.arrayobjects[1]");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.arrayobjects"),
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult<object> parseResult = JUST.Gramar.Grammar<object>.Instance.Parse(
                expression,
                new Dictionary<string, JToken>{ { arrayAlias,  parentArray }},
                new Dictionary<string, JToken>{ { arrayAlias,  loopElement }},
                arrayAlias,
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
            JToken parentArray = this._input.SelectToken("$.arrayobjects");
            JToken loopElement = this._input.SelectToken("$.arrayobjects[1]");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.arrayobjects"),
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult<object> parseResult = JUST.Gramar.Grammar<object>.Instance.Parse(
                expression,
                new Dictionary<string, JToken>{ { arrayAlias,  parentArray }},
                new Dictionary<string, JToken>{ { arrayAlias,  loopElement }},
                arrayAlias,
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
            JToken parentArray = this._input.SelectToken("$.animals");
            JToken loopElement = this._input.SelectToken("$.animals");

            JUSTContext context = new JUSTContext(true)
            {
                Input = this._input.SelectToken("$.animals"),
                EvaluationMode = EvaluationMode.Strict,
            };
            ParseResult<object> parseResult = JUST.Gramar.Grammar<object>.Instance.Parse(
                expression,
                new Dictionary<string, JToken>{ { arrayAlias,  parentArray }},
                new Dictionary<string, JToken>{ { arrayAlias,  loopElement }},
                arrayAlias,
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
            ParseResult<string> parseResult = JUST.Gramar.Grammar<string>.Instance.Parse(
                expression,
                null, null, null,
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
            ParseResult<object[]> parseResult = JUST.Gramar.Grammar<object[]>.Instance.Parse(
                expression,
                null, null, null,
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

        private static void PrintResults<T>(ParseResult<T> result)
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