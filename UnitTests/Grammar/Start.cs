using System;
using System.Collections.Generic;
using CSharpParserGenerator;
using NUnit.Framework;

namespace JUST.UnitTests.Gramar
{
    [TestFixture]
    public class Start
    {
        private Parser<ELang> parser;

        private enum ELang
        {
            Ignore,

            // Non-terminal
            EXPR,
            FUNC,
            ARGS,
            ARG,

            // Terminal
            Sharp,
            JsonPathEx,
            LParenthesis,
            RParenthesis,
            Comma,
            String,
            Number,

            //JUST Functions
            ValueOf,
            IfCondition,
            LastIndexOf,
            FirstIndexOf,
            Substring,
            Concat,
            Length,
            Add,
            Subtract,
            Multiply,
            Divide,
            Round,

            StringEquals,
            StringContains,
            MathEquals,
            MathGreaterThan,
            MathLessThan,
            MathGreaterThanOrEqualTo,
            MathLessThanOrEqualTo,

            Concatall,
            Sum,
            Average,
            Min,
            Max,
            Concatallatpath,
            Sumatpath,
            Averageatpath,
            Minatpath,
            Maxatpath,
            Tointeger,
            Tostring,
            Toboolean,
            Todecimal,
            Isnumber,
            Isboolean,
            Isstring,
            Isarray,
            Copy,
            Replace,
            Delete,
            Loop,
            Currentvalue,
            Currentindex,
            Currentproperty,
            Lastindex,
            Lastvalue,
            Currentvalueatpath,
            Lastvalueatpath,
            Eval,
            Xconcat,
            Grouparrayby,
            Customfunction,
        }

        [OneTimeSetUp]
        public void Setup()
        {
            parser = GetParser();
        }

        [TestCase("#valueof($.test)")]
        [TestCase("#valueof(#valueof($.test))")]
        [TestCase("#valueof(#valueof(#valueof($.test)))")]
        [TestCase("#ifcondition(expr1,expr2,truevalue,falsevalue)")]
        [TestCase("#ifcondition(#valueof($.menu.id),github,#valueof($.menu.repository),fail)")]
        [TestCase("#lastindexof(#valueof($.stringref),and)")]
        [TestCase("#firstindexof(#valueof($.stringref),and)")]
        [TestCase("#substring(#valueof($.stringref),9,11)")]
        [TestCase("#concat(#valueof($.menu.id.file),#valueof($.menu.value.Window))")]
        [TestCase("#length(#valueof($.stringref))")]
        [TestCase("#length(#valueof($.numbers))")]
        [TestCase("#length($.numbers)")]
        [TestCase("#add(#valueof($.numbers[0]),3)")]
        [TestCase("#subtract(#valueof($.numbers[4]),#valueof($.numbers[0]))")]
        [TestCase("#multiply(2,#valueof($.numbers[2]))")]
        [TestCase("#divide(9,3)")]
        [TestCase("#round(10.005,2)")]
        [TestCase("#mathequals(#valueof($.numbers[2]),3)")]
        [TestCase("#mathgreaterthan(#valueof($.numbers[2]),2)")]
        [TestCase("#mathlessthan(#valueof($.numbers[2]),4)")]
        [TestCase("#mathgreaterthanorequalto(#valueof($.numbers[2]),4)")]
        [TestCase("#mathlessthanorequalto(#valueof($.numbers[2]),2)")]
        [TestCase("#stringequals(#valueof($.d[0]),one)")]
        [TestCase("#stringcontains(#valueof($.d[0]),n)")]
        [TestCase("#concatall(#valueof($.d))")]
        [TestCase("#sum($.numbers)")]
        [TestCase("#average(#valueof($.numbers))")]
        [TestCase("#min($.numbers)")]
        [TestCase("#max(#valueof($.numbers))")]
        [TestCase("#concatallatpath(#valueof($.x),$.v.a)")]
        [TestCase("#sumatpath(#valueof($.x),$.v.c)")]
        [TestCase("#averageatpath(#valueof($.x),$.v.c)")]
        [TestCase("#minatpath(#valueof($.x),$.v.b)")]
        [TestCase("#maxatpath(#valueof($.x),$.v.b)")]
        [TestCase("#toboolean(#valueof($.booleans.affirmative_string))")]
        [TestCase("#toboolean(#valueof($.booleans.negative_string))")]
        [TestCase("#toboolean(#valueof($.booleans.affirmative_int))")]
        [TestCase("#toboolean(#valueof($.booleans.negative_int))")]
        [TestCase("#tostring(#valueof($.strings.integer))")]
        [TestCase("#tostring(#valueof($.strings.decimal))")]
        [TestCase("#tostring(#valueof($.strings.affirmative_boolean))")]
        [TestCase("#tostring(#valueof($.strings.negative_boolean))")]
        [TestCase("#tointeger(#valueof($.integers.string))")]
        [TestCase("#tointeger(#valueof($.integers.decimal))")]
        [TestCase("#tointeger(#valueof($.integers.affirmative_boolean))")]
        [TestCase("#tointeger(#valueof($.integers.negative_boolean))")]
        [TestCase("#todecimal(#valueof($.decimals.integer))")]
        [TestCase("#todecimal(#valueof($.decimals.string))")]
        [TestCase("#isnumber(#valueof($.integer))")]
        [TestCase("#isnumber(#valueof($.decimal))")]
        [TestCase("#isnumber(#valueof($.boolean))")]
        [TestCase("#isboolean(#valueof($.boolean))")]
        [TestCase("#isboolean(#valueof($.integer))")]
        [TestCase("#isstring(#valueof($.string))")]
        [TestCase("#isstring(#valueof($.array))")]
        [TestCase("#isarray(#valueof($.array))")]
        [TestCase("#isarray(#valueof($.decimal))")]
        [TestCase("#copy($)")]
        [TestCase("#delete($.tree.branch.bird)")]
        [TestCase("#replace($.tree.branch.extra,#valueof($.tree.ladder))")]
        [TestCase("#loop($.numbers)")]
        [TestCase("#currentvalue()")]
        [TestCase("#currentindex()")]
        [TestCase("#lastindex()")]
        [TestCase("#lastvalue()")]
        [TestCase("#currentvalueatpath($.country.name)")]
        [TestCase("#currentindex()")]
        [TestCase("#lastvalueatpath($.country.language)")]
        [TestCase("#eval(#currentproperty())")]
        [TestCase("#xconcat(#valueof($.drugs), #valueof($.pa), #valueof($.sa))")]
        [TestCase("#grouparrayby($.Forest,type,all)")]
        [TestCase("#customfunction(JUST.NET.Test,JUST.NET.Test.Season.findseason,#valueof($.tree.branch.leaf),#valueof($.tree.branch.flower))")]
        [TestCase("#declared_function(dummy_arg)")]
        [Category("Grammar")]
        public void Grammar(string expression)
        {
            //string input = "{ \"test\": 1 }";
            ParseResult<string> result = this.parser.Parse<string>(expression);

            PrintResults(result);

            Assert.Multiple(() => {
                Assert.AreEqual(0, result.Errors.Count);
                Assert.True(result.Success);
            });
        }

        private static Parser<ELang> GetParser()
        {
            var tokens = new LexerDefinition<ELang>(new Dictionary<ELang, TokenRegex>
            {
                // JUST Functions
                [ELang.ValueOf] = "valueof",
                [ELang.IfCondition] = "ifcondition",
                [ELang.LastIndexOf] = "lastindexof",
                [ELang.FirstIndexOf] = "firstindexof",
                [ELang.Substring] = "substring",
                [ELang.Concat] = "concat",
                [ELang.Length] = "length",
                [ELang.Add] = "add",
                [ELang.Subtract] = "subtract",
                [ELang.Multiply] = "multiply",
                [ELang.Divide] = "divide",
                [ELang.Round] = "round",

                [ELang.StringEquals] = "stringequals",
                [ELang.StringContains] = "stringcontains",
                [ELang.MathEquals] = "mathequals",
                [ELang.MathGreaterThan] = "mathgreaterthan",
                [ELang.MathLessThan] = "mathlessthan",
                [ELang.MathGreaterThanOrEqualTo] = "mathgreaterthanorequalto",
                [ELang.MathLessThanOrEqualTo] = "mathlessthanorequalto",

                [ELang.Concatall] = "concatall",
                [ELang.Sum] = "sum",
                [ELang.Average] = "average",
                [ELang.Min] = "min",
                [ELang.Max] = "max",
                [ELang.Concatallatpath] = "concatallatpath",
                [ELang.Sumatpath] = "sumatpath",
                [ELang.Averageatpath] = "averageatpath",
                [ELang.Minatpath] = "minatpath",
                [ELang.Maxatpath] = "maxatpath",
                [ELang.Tointeger] = "tointeger",
                [ELang.Tostring] = "tostring",
                [ELang.Toboolean] = "toboolean",
                [ELang.Todecimal] = "todecimal",
                [ELang.Isnumber] = "isnumber",
                [ELang.Isboolean] = "isboolean",
                [ELang.Isstring] = "isstring",
                [ELang.Isarray] = "isarray",
                [ELang.Copy] = "copy",
                [ELang.Replace] = "replace",
                [ELang.Delete] = "delete",
                [ELang.Loop] = "loop",
                [ELang.Currentvalue] = "currentvalue",
                [ELang.Currentindex] = "currentindex",
                [ELang.Currentproperty] = "currentproperty",
                [ELang.Lastindex] = "lastindex",
                [ELang.Lastvalue] = "lastvalue",
                [ELang.Currentvalueatpath] = "currentvalueatpath",
                [ELang.Lastvalueatpath] = "lastvalueatpath",
                [ELang.Concat] = "concat",
                [ELang.Eval] = "eval",
                [ELang.Xconcat] = "xconcat",
                [ELang.Grouparrayby] = "grouparrayby",
                [ELang.Customfunction] = "customfunction",
                
                [ELang.Ignore] = "[ \\n]+",
                [ELang.LParenthesis] = "\\(",
                [ELang.RParenthesis] = "\\)",
                [ELang.Comma] = ",",
                [ELang.Sharp] = "#",
                [ELang.JsonPathEx] = "(?i)\\$[\\.a-z\\[\\]0-9_\\-]*",
                [ELang.String] = "(?i)[a-z0-9_\\-\\.]+",
                [ELang.Number] = "[\\d\\.]+",
            });
        
            // EXPR -> Sharp FUNC
            // FUNC -> <JUST Function> LParenthesis ARGS 
            // ARGS -> ARG Comma ARGS
            // ARGS -> ARG RParenthesis
            // ARGS ->
            // ARG -> EXPR
            // ARG -> JsonPathEx
            // ARG -> String
            // ARG -> Number
            
            // Sharp -> #
            // Comms -> ,
            // LParenthesis -> (
            // RParenthesis -> )
            // String -> (?i)[a-z0-9_-]+
            // Number -> [\\d\\.]+
            var rules = new GrammarRules<ELang>(new Dictionary<ELang, Token[][]>()
            {
                [ELang.EXPR] = new Token[][]
                {
                    new Token[] { ELang.Sharp, ELang.FUNC, new Op(o => o[0] = o[1]) },
                },
                [ELang.FUNC] = new Token[][]
                {
                    new Token[] { ELang.ValueOf, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.IfCondition, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.LastIndexOf, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.FirstIndexOf, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Substring, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Concat, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Length, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Add, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Subtract, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Multiply, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Divide, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Round, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.StringEquals, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.StringContains, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.MathEquals, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.MathGreaterThan, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.MathLessThan, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.MathGreaterThanOrEqualTo, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.MathLessThanOrEqualTo, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Concatall, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Sum, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Average, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Min, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Max, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Concatallatpath, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Sumatpath, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Averageatpath, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Minatpath, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Maxatpath, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Tointeger, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Tostring, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Toboolean, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Todecimal, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Isnumber, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Isboolean, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Isstring, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Isarray, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Copy, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Replace, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Delete, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Loop, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Currentvalue, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Currentindex, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Currentproperty, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Lastindex, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Lastvalue, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Currentvalueatpath, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Lastvalueatpath, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Concat, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Eval, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Xconcat, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Grouparrayby, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.Customfunction, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.String, ELang.LParenthesis, ELang.ARGS },
                },
                
                [ELang.ARGS] = new Token[][]
                {
                    new Token[] { ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] += 1) },
                    new Token[] { ELang.ARG, ELang.RParenthesis, new Op(o => o[0] += 1) },
                },
                [ELang.ARG] = new Token[][]
                {
                    new Token[] { ELang.EXPR, new Op(o => o[0] = 0) },
                    new Token[] { ELang.JsonPathEx, new Op(o => o[0] = 0) },
                    new Token[] { ELang.String, new Op(o => o[0] = 0) },
                    new Token[] { ELang.Number, new Op(o => o[0] = 0) },
                    new Token[] {},
                },
            });
            return new ParserGenerator<ELang>(new Lexer<ELang>(tokens, ELang.Ignore), rules).CompileParser();
        }

        private static void PrintResults(ParseResult<string> result)
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
                Console.WriteLine(result.Value);
            }
        }
    }
}