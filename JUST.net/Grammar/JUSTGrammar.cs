using System;
using System.Collections.Generic;
using CSharpParserGenerator;
using JUST.net.Selectables;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace JUST.Gramar
{
    public class Grammar<TSelectable> : IDisposable where TSelectable : ISelectableToken
    {
        private Parser<ELang> _parser;

        private string _arrayAlias;
        private JUSTContext _context;

        private Grammar()
        {
            if (this._parser == null)
            {
                this._parser = GetParser();
            }
        }

        public ParseResult Parse(string expression, JUSTContext context)
        {
            this._context = context;
            return this._parser.Parse(expression);
        }

        public static Grammar<TSelectable> Instance
        {
            get
            {
                return Nested.Instance;
            }
        }

        private class Nested : IDisposable
        {
            static Nested()
            {
            }
            internal static Grammar<TSelectable> Instance = new Grammar<TSelectable>();

            public void Dispose()
            {
                Instance = null;
            }
        }

        protected enum ELang
        {
            Ignore,

            // Non-terminal
            EXPR,FUNC,ARGS,ARG,C_ARG,STR_ESC,ESC,STR_REC,

            // Terminal
            Sharp,JsonPathEx,LParenthesis,RParenthesis,Comma,String,Number,EscapeChar,

            //JUST Functions
            ValueOf,
            IfCondition,
            StringAndMathFn,LastIndexOf,FirstIndexOf,Substring,Concat,Length,Add,Subtract,Multiply,Divide,Round,

            Operators,StringEquals,StringContains,MathEquals,MathGreaterThan,MathLessThan,MathGreaterThanOrEqualTo,MathLessThanOrEqualTo,

            Aggregate,Concatall,Sum,Average,Min,Max,

            AggregateArray,Concatallatpath,Sumatpath,Averageatpath,Minatpath,Maxatpath,

            TypeConversions,Tointeger,Tostring,Toboolean,Todecimal,

            TypeCheck,Isnumber,Isboolean,Isstring,Isarray,

            BulkFn,Copy,Replace,Delete,

            LoopDeclare,ArrayLoop,Loop,
            CurrentPropertyEval,Currentproperty,
            CurrentIndexEval,Currentindex,
            LastIndexEval,Lastindex,
            CurrentValueEval,Currentvalue,
            LastValueEval,Lastvalue,
            CurrentValueAtPathEval,Currentvalueatpath,
            LastValueAtPathEval,Lastvalueatpath,
            
            Exists,
            ExistsNotEmpty,
            IfGroupEval,IfGroup,
            Eval,
            Xconcat,Xadd,
            Grouparrayby,
            Customfunction,

            ConstantSharp,ConstantComma,StringEmpty,ArrayEmpty
        }
        
        private Parser<ELang> GetParser()
        {
            var tokens = new LexerDefinition<ELang>(new Dictionary<ELang, TokenRegex>
            {
                // JUST Functions
                [ELang.ValueOf] = "valueof",
                [ELang.IfCondition] = "ifcondition",
                [ELang.LastIndexOf] = "lastindexof",
                [ELang.FirstIndexOf] = "firstindexof",
                [ELang.Substring] = "substring",
                [ELang.Length] = "length",
                [ELang.Add] = "add",
                [ELang.Subtract] = "subtract",
                [ELang.Multiply] = "multiply",
                [ELang.Divide] = "divide",
                [ELang.Round] = "round",

                [ELang.StringEquals] = "stringequals",
                [ELang.StringContains] = "stringcontains",
                [ELang.MathEquals] = "mathequals",
                [ELang.MathGreaterThanOrEqualTo] = "mathgreaterthanorequalto",
                [ELang.MathLessThanOrEqualTo] = "mathlessthanorequalto",
                [ELang.MathGreaterThan] = "mathgreaterthan",
                [ELang.MathLessThan] = "mathlessthan",

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
                [ELang.Currentindex] = "currentindex",
                [ELang.Lastindex] = "lastindex",
                [ELang.Currentproperty] = "currentproperty",
                [ELang.Currentvalueatpath] = "currentvalueatpath",
                [ELang.Currentvalue] = "currentvalue",
                [ELang.Lastvalueatpath] = "lastvalueatpath",
                [ELang.Lastvalue] = "lastvalue",
                [ELang.ExistsNotEmpty] = "existsandnotempty",
                [ELang.Exists] = "exists",
                [ELang.IfGroup] = "ifgroup",
                [ELang.Eval] = "eval",
                [ELang.Xadd] = "xadd",
                [ELang.Xconcat] = "xconcat",
                [ELang.Grouparrayby] = "grouparrayby",
                [ELang.Customfunction] = "customfunction",

                [ELang.Concatall] = "concatall",
                [ELang.Concat] = "concat",
                [ELang.Sum] = "sum",
                [ELang.Average] = "average",
                [ELang.Min] = "min",
                [ELang.Max] = "max",

                [ELang.ConstantSharp] = "constant_hash",
                [ELang.ConstantComma] = "constant_comma",
                [ELang.StringEmpty] = "stringempty",
                [ELang.ArrayEmpty] = "arrayempty",
                
                [ELang.Ignore] = "[ \\n]+",
                [ELang.LParenthesis] = "(?<!\\/)\\(",
                [ELang.RParenthesis] = "(?<!\\/)\\)",
                [ELang.Comma] = "(?<!\\/),",
                [ELang.Sharp] = "(?<!\\/)#",
                [ELang.JsonPathEx] = "(?i)\\$[\\.a-z\\[\\]0-9_\\-\\?&\\*\\s:]*",
                [ELang.Number] = "\\d+\\.?\\d*",
                [ELang.String] = "(?i)[a-z0-9_\\-\\.@='\\[\\]&\\s]+",
                //[ELang.String] = "(?i)(?:[a-z0-9_\\-\\.]*(?:\\/\\(|\\/\\)|\\/,|\\/\\/)+?)|(?:(?:\\/\\(|\\/\\)|\\/,|\\/\\/)*?[a-z0-9_\\-\\.]+)",
                [ELang.EscapeChar] = "\\/",
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
                    new Token[] { ELang.Sharp, ELang.FUNC, new Op(o => o[0] = o[1] ) },
                },
                [ELang.FUNC] = new Token[][]
                {
                    new Token[] { ELang.ValueOf, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.IfCondition, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = AreEqual(o[2], o[4]) ? o[6] : o[8])},
                    new Token[] { ELang.StringAndMathFn },
                    new Token[] { ELang.Operators },
                    new Token[] { ELang.Aggregate },
                    new Token[] { ELang.AggregateArray },
                    new Token[] { ELang.TypeConversions },
                    new Token[] { ELang.TypeCheck },
                    new Token[] { ELang.BulkFn },
                    new Token[] { ELang.LoopDeclare },
                    new Token[] { ELang.ArrayLoop },
                    new Token[] { ELang.IfGroupEval },
                    
                    new Token[] { ELang.Exists, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = (Invoke("valueof", true, new object[] { o[2], this._context }) != null) )},
                    new Token[] { ELang.ExistsNotEmpty, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Eval, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = o[2] )},
                    new Token[] { ELang.Xconcat, ELang.LParenthesis, ELang.ARG, ELang.C_ARG, ELang.RParenthesis, new Op(o => o[0] = Xconcat(o[2], o[3])  )},
                    new Token[] { ELang.Xadd, ELang.LParenthesis, ELang.ARG, ELang.C_ARG, ELang.RParenthesis, new Op(o => o[0] = o[2] + o[3] )},
                    new Token[] { ELang.Grouparrayby, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], o[6], this._context }) )},
                    new Token[] { ELang.Customfunction, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = InvokeCustomFunction(o[2], o[4], o[6], this._context)) },
                    new Token[] { ELang.String, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = InvokeRegisteredFunction(o[0], o[2], this._context)) },

                    new Token[] { ELang.ConstantSharp, ELang.LParenthesis, ELang.RParenthesis, new Op(o => o[0] = "#" ) },
                    new Token[] { ELang.ConstantComma, ELang.LParenthesis, ELang.RParenthesis, new Op(o => o[0] = "," ) },
                    new Token[] { ELang.StringEmpty, ELang.LParenthesis, ELang.RParenthesis, new Op(o => o[0] = "" ) },
                    new Token[] { ELang.ArrayEmpty, ELang.LParenthesis, ELang.RParenthesis, new Op(o => o[0] = Array.Empty<object>() ) },
                },

                [ELang.StringAndMathFn] = new Token[][]
                {
                    new Token[] { ELang.LastIndexOf, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2]?.ToString().LastIndexOf(o[4]) ?? -1 )},
                    new Token[] { ELang.FirstIndexOf, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2]?.ToString().IndexOf(o[4]) ?? -1 )},
                    new Token[] { ELang.Substring, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2].ToString().Substring(Convert.ToInt32(o[4]), Convert.ToInt32(o[6])) )},
                    new Token[] { ELang.Concat, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], this._context}) )},
                    new Token[] { ELang.Length, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Add, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[0] = o[2] + o[4] )},
                    new Token[] { ELang.Subtract, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] - o[4] )},
                    new Token[] { ELang.Multiply, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] * o[4] )},
                    new Token[] { ELang.Divide, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[0] = o[2] / o[4] )},
                    new Token[] { ELang.Round, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = decimal.Round(Convert.ToDecimal(o[2]), Convert.ToInt32(o[4]), MidpointRounding.AwayFromZero) )},
                },
                [ELang.Operators] = new Token[][]
                {
                    new Token[] { ELang.MathEquals, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] == o[4] )},
                    new Token[] { ELang.MathGreaterThan, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] > o[4] )},
                    new Token[] { ELang.MathLessThan, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] < o[4] )},
                    new Token[] { ELang.MathGreaterThanOrEqualTo, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] >= o[4] )},
                    new Token[] { ELang.MathLessThanOrEqualTo, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] <= o[4] )},
                    new Token[] { ELang.StringEquals, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2]?.ToString() == o[4]?.ToString() )},
                    new Token[] { ELang.StringContains, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2]?.ToString().Contains(o[4]?.ToString()) ?? false )},
                },
                [ELang.Aggregate] = new Token[][]
                {
                    //new Token[] { ELang.AggregateFn, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Concatall, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Sum, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Average, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Min, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Max, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                },
                [ELang.AggregateArray] = new Token[][]
                {
                    new Token[] { ELang.Concatallatpath, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], this._context}) )},
                    new Token[] { ELang.Sumatpath, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], this._context}) )},
                    new Token[] { ELang.Averageatpath, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], this._context}) )},
                    new Token[] { ELang.Minatpath, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], this._context}) )},
                    new Token[] { ELang.Maxatpath, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], this._context}) )},
                },
                [ELang.TypeConversions] = new Token[][]
                {
                    new Token[] { ELang.Tointeger, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = ReflectionHelper.GetTypedValue(typeof(int), o[2], this._context.EvaluationMode) )},
                    new Token[] { ELang.Tostring, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = ReflectionHelper.GetTypedValue(typeof(string), o[2], this._context.EvaluationMode) )},
                    new Token[] { ELang.Toboolean, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = ReflectionHelper.GetTypedValue(typeof(bool), o[2], this._context.EvaluationMode)) },
                    new Token[] { ELang.Todecimal, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = ReflectionHelper.GetTypedValue(typeof(decimal), o[2], this._context.EvaluationMode)) },
                },
                [ELang.TypeCheck] = new Token[][]
                {
                    new Token[] { ELang.Isnumber, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Isboolean, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = o[2].GetType() == typeof(bool) )},
                    new Token[] { ELang.Isstring, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = o[2].GetType() == typeof(string) )},
                    new Token[] { ELang.Isarray, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = o[2].GetType().IsArray )},
                },
                [ELang.BulkFn] = new Token[][]
                {
                    new Token[] { ELang.Copy, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke("valueof", true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Replace, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Replace(o[2], o[4]) )},
                    new Token[] { ELang.Delete, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Delete(o[2]) )},
                },
                [ELang.LoopDeclare] = new Token[][]
                {
                    new Token[] { ELang.Loop, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = LoopOverAlias(o[2], null, null, this._context) ) },
                    new Token[] { ELang.Loop, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = LoopOverAlias(o[2], o[4], null, this._context) ) },
                    new Token[] { ELang.Loop, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = LoopOverAlias(o[2], o[4], o[6], this._context) ) },
                },
                [ELang.ArrayLoop] = new Token[][]
                {
                    //new Token[] { ELang.Loop, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke("valueof", true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.CurrentValueEval },
                    new Token[] { ELang.CurrentIndexEval },
                    new Token[] { ELang.CurrentPropertyEval },
                    new Token[] { ELang.LastIndexEval },
                    new Token[] { ELang.LastValueEval },
                    new Token[] { ELang.CurrentValueAtPathEval },
                    new Token[] { ELang.LastValueAtPathEval },
                },
                [ELang.CurrentValueEval] = new Token[][]
                {
                    new Token[] { ELang.Currentvalue, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], o[4]) )},
                    new Token[] { ELang.Currentvalue, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], null) )},
                },
                [ELang.CurrentIndexEval] = new Token[][]
                {
                    new Token[] { ELang.Currentindex, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], o[4]) )},
                    new Token[] { ELang.Currentindex, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], null) )},
                },
                [ELang.CurrentPropertyEval] = new Token[][]
                {
                    new Token[] { ELang.Currentproperty, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], o[4]) )},
                    new Token[] { ELang.Currentproperty, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], null) )},
                },
                [ELang.LastIndexEval] = new Token[][]
                {
                    new Token[] { ELang.Lastindex, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], o[4]) )},
                    new Token[] { ELang.Lastindex, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], null) )},
                },
                [ELang.LastValueEval] = new Token[][]
                {
                    new Token[] { ELang.Lastvalue, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], o[4]) )},
                    new Token[] { ELang.Lastvalue, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], null) )},
                },
                [ELang.CurrentValueAtPathEval] = new Token[][]
                {
                    new Token[] { ELang.Currentvalueatpath, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], o[4]) )},
                    new Token[] { ELang.Currentvalueatpath, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], null) )},
                },
                [ELang.LastValueAtPathEval] = new Token[][]
                {
                    new Token[] { ELang.Lastvalueatpath, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], o[4]) )},
                    new Token[] { ELang.Lastvalueatpath, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = InvokeLoopFunction(o[0], o[2], null) )},
                },
                [ELang.IfGroupEval] = new Token[][]
                {
                    new Token[] { ELang.IfGroup, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = IfGroupEval(o[2], o[4]) )},
                    new Token[] { ELang.IfGroup, ELang.LParenthesis, ELang.ARG, ELang.RParenthesis, new Op(o => o[0] = o[2] )},
                },

                [ELang.C_ARG] = new Token[][]
                {
                    new Token[] { ELang.Comma, ELang.ARG, ELang.C_ARG, new Op(o => o[0] = Xconcat(o[1], o[2])) },
                    new Token[] { ELang.Comma, ELang.ARG, new Op(o => o[0] = o[1] ) },
                },

                [ELang.ARGS] = new Token[][]
                {
                    new Token[] { ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = JoinArgs(o[0], o[2]) ) },
                    new Token[] { ELang.ARG, ELang.RParenthesis },
                },
                [ELang.ARG] = new Token[][]
                {
                    new Token[] { ELang.EXPR },
                    new Token[] { ELang.JsonPathEx, ELang.STR_ESC, new Op(o => o[0] = o[0] + o[1]) },
                    new Token[] { ELang.Number, new Op(o => o[0] = Convert.ToDecimal(o[0]) )},
                    new Token[] { ELang.STR_ESC },
                },
                [ELang.STR_ESC] = new Token[][]
                {
                    new Token[] { ELang.String, ELang.ESC, ELang.STR_ESC, new Op(o => o[0] = o[0] + o[1] + o[2]) },
                    new Token[] { ELang.ESC, ELang.STR_ESC, new Op(o => o[0] = o[0] + o[1]) },
                    //new Token[] { ELang.String, ELang.STR_REC, new Op(o => o[0] = o[0] + o[1]) },
                    new Token[] { ELang.String, ELang.String, ELang.STR_ESC, new Op(o => o[0] = o[0] + o[1] + o[2]) },
                    new Token[] { ELang.String },
                    new Token[] { },
                },
                [ELang.STR_REC] = new Token[][]
                {
                    new Token[] { ELang.String, ELang.STR_REC, new Op(o => o[0] = o[0] + o[1]) },
                    new Token[] { },
                },
                [ELang.ESC] = new Token[][]
                {
                    new Token[] { ELang.EscapeChar, ELang.Sharp, new Op(o => o[0] = o[1]) },
                    new Token[] { ELang.EscapeChar, ELang.LParenthesis, new Op(o => o[0] = o[1]) },
                    new Token[] { ELang.EscapeChar, ELang.RParenthesis, new Op(o => o[0] = o[1]) },
                    new Token[] { ELang.EscapeChar, ELang.Comma, new Op(o => o[0] = o[1]) },
                    new Token[] { ELang.EscapeChar, ELang.EscapeChar, new Op(o => o[0] = o[1]) },
                }
            });
            return new ParserGenerator<ELang>(new Lexer<ELang>(tokens, ELang.Ignore), rules).CompileParser();
        }

        private dynamic InvokeLoopFunction(string fn, string path, string alias)
        {
            this._arrayAlias = !string.IsNullOrEmpty(alias) ? alias : GetAlias(this._context);
            object[] parameters = !string.IsNullOrEmpty(path) ? 
                new object[] { null, this._context.CurrentArrayElement[this._arrayAlias], path, this._context } :
                new object[] { null, this._context.CurrentArrayElement[this._arrayAlias], this._context };
            return Invoke(fn, true, parameters);
        }

        private bool AreEqual(dynamic arg1, dynamic arg2)
        {
            bool.TryParse(arg1?.ToString() ?? string.Empty, out bool arg1Bool);
            bool.TryParse(arg2?.ToString() ?? string.Empty, out bool arg2Bool);

            return 
                arg1?.Equals(arg2) ||
                arg1?.Equals(arg2Bool) || 
                arg2?.Equals(arg1) ||
                arg2?.Equals(arg1Bool);
        }

        private dynamic IfGroupEval(string isIncluded, dynamic arg2)
        {
            bool.TryParse(isIncluded, out bool result);
            if (result)
            {
                return arg2;
            }
            return null;
        }

        private string GetAlias(JUSTContext context)
        {
            return this._arrayAlias ?? context.CurrentArrayElement.Last().Key;
        }

        private JArray LoopOverAlias(string loopPath, string loopAlias, string previousAlias, JUSTContext context)
        {
            previousAlias = previousAlias != null ? previousAlias : context.CurrentArrayElement.Last().Key;

            context.Input = context.CurrentArrayElement[previousAlias];
            
            object loopToken = Invoke("valueof", true, new object[] { loopPath, context });
            int loopCounter = ++context.LoopCounter;

            JArray loopArray = GetLoopArray(loopToken);
            
            KeyValuePair<string, JArray> k = new KeyValuePair<string, JArray>(loopAlias ?? $"loop{loopCounter}", loopArray);

            if (context.ParentArray == null)
            {
                context.ParentArray = new Dictionary<string, JArray>();
            }
            context.ParentArray.Add(k);

            return loopArray;
        }

        private JArray GetLoopArray(object loopToken)
        {
            JArray result;
            if (loopToken is Array)
            {
                result = JArray.FromObject(loopToken);
            }
            else if (loopToken is JArray)
            {
                result = loopToken as JArray;
            }
            else
            {
                result = new JArray();
                result.Add(loopToken);
            }
            return result;
        }

        private object InvokeCustomFunction(string assemblyName, string method, object[] args, JUSTContext context)
        {
            List<object> l = new List<object>();
            l.Add(assemblyName);
            l.Add(method);
            l.AddRange(args);
            return CallCustomFunction(l.ToArray(), context);
        }

        private object CallCustomFunction(object[] parameters, JUSTContext context)
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

            return ReflectionHelper.Caller<TSelectable>(null, className, functionName, customParameters, false, context);
        }

        private object[] JoinArgs(dynamic arg1, dynamic arg2)
        {
            List<object> result = new List<object>();
            if (arg1 is Array arr1)
            {
                result.AddRange(arr1 as IEnumerable<object>);
                if (arg2 is Array arr2)
                {
                    result.AddRange(arr1 as IEnumerable<object>);
                }
                else {
                    result.Add(arg2);
                }
            }
            else
            {
                result.Add(arg1);
                if (arg2 is Array arr2)
                {
                    result.AddRange(arr2 as IEnumerable<object>);
                }
                else {
                    result.Add(arg2);
                }
            }
            return result.ToArray();
        }

        private object InvokeRegisteredFunction(dynamic functionName, dynamic args, JUSTContext context)
        {
            if (context?.IsRegisteredCustomFunction(functionName) ?? false)
            {
                var methodInfo = context.GetCustomMethod(functionName);
                List<object> parameters = new List<object>();
                if (args is Array arr)
                {
                    parameters.AddRange(args);
                }
                else
                {
                    parameters.Add(args);
                }
                return ReflectionHelper.InvokeCustomMethod<TSelectable>(methodInfo, parameters.ToArray(), false, context);
            }
            else
            {
                if ( (context.EvaluationMode & EvaluationMode.Strict) == EvaluationMode.Strict)
                {
                    throw new Exception($"Function not registered: {functionName}");
                }
            }
            return null;
        }

        private object Invoke(string fn, bool convertParameters, object[] parameters)
        {
            return ReflectionHelper.Caller<TSelectable>(null, "JUST.Transformer`1", fn, parameters, convertParameters, this._context);
        }

        private dynamic Replace(dynamic arg1, dynamic arg2)
        {
            object arg1Val = Invoke("valueof", true, new object[] { arg1, this._context });
            (arg1Val as JToken).Replace(arg2 as JToken);
            return this._context.Input;
        }

        private dynamic Delete(dynamic arg1)
        {
            JToken toRemove = this._context.Input.SelectToken(arg1);
            toRemove.Ancestors().First().Remove();
            return this._context.Input; 
        }

        private object Xconcat(dynamic arg1, dynamic arg2)
        {
            if (arg1 == null)
            {
                return arg2;
            }
            if (arg1 is Array arr1)
            {
                List<object> result = new List<object>();
                foreach (var o in arr1)
                {
                    result.Add(o);
                }
                if(arg2 is Array arr)
                {
                    foreach (var o in arr)
                    {
                        result.Add(o);
                    }
                }
                else 
                {
                    if (arg2 != null)
                    {
                        result.Add(arg2);
                    }
                }
                return result.ToArray();
            }
            else 
            {
                return arg1 + arg2;
            }

        }

        public void Dispose()
        {
            this._parser = null;
            //Instance = null;
        }
    }
}