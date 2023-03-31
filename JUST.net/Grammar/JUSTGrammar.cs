using System;
using System.Collections.Generic;
using CSharpParserGenerator;
using JUST.net.Selectables;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections;

namespace JUST.Gramar
{
    public class Grammar<T>
    {
        private Parser<ELang> _parser;

        private string _arrayAlias;
        private IDictionary<string, JToken> _currentArrayToken;
        private IDictionary<string, JToken> _parentArrayToken;
        private JUSTContext _context;

        private Grammar()
        {
            if (this._parser == null)
            {
                this._parser = GetParser();
            }
        }

        public ParseResult<T> Parse(string expression, IDictionary<string, JToken> parentArrayToken, IDictionary<string, JToken> currentArrayToken, string arrayAlias, JUSTContext context)
        {
            this._parentArrayToken = parentArrayToken;
            this._currentArrayToken = currentArrayToken;
            this._arrayAlias = arrayAlias;
            this._context = context;
            return this._parser.Parse<T>(expression);
        }

        public static Grammar<T> Instance
        {
            get
            {
                return Nested.Instance;
            }
        }

        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly Grammar<T> Instance = new Grammar<T>();
        }

        protected enum ELang
        {
            Ignore,

            // Non-terminal
            EXPR,FUNC,ARGS,ARG,C_ARG,

            // Terminal
            Sharp,JsonPathEx,LParenthesis,RParenthesis,Comma,String,Number,

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

            ArrayLoop,Loop,Currentvalue,Currentindex,Currentproperty,Lastindex,Lastvalue,Currentvalueatpath,Lastvalueatpath,
            
            Exists,
            ExistsNotEmpty,
            IfGroup,
            Eval,
            Xconcat,
            Grouparrayby,
            Customfunction,
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
                [ELang.Exists] = "exists",
                [ELang.ExistsNotEmpty] = "existsandnotempty",
                [ELang.IfGroup] = "ifgroup",
                [ELang.Eval] = "eval",
                [ELang.Xconcat] = "xconcat",
                [ELang.Grouparrayby] = "grouparrayby",
                [ELang.Customfunction] = "customfunction",

                //[ELang.AggregateFn] = "concatall|sum|average|min|max",
                [ELang.Concatall] = "concatall",
                [ELang.Sum] = "sum",
                [ELang.Average] = "average",
                [ELang.Min] = "min",
                [ELang.Max] = "max",
                
                [ELang.Ignore] = "[ \\n]+",
                [ELang.LParenthesis] = "\\(",
                [ELang.RParenthesis] = "\\)",
                [ELang.Comma] = ",",
                [ELang.Sharp] = "#",
                [ELang.JsonPathEx] = "(?i)\\$[\\.a-z\\[\\]0-9_\\-]*",
                [ELang.Number] = "\\d+\\.?\\d*",
                [ELang.String] = "(?i)[a-z0-9_\\-\\.]+",
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
                    new Token[] { ELang.ValueOf, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.IfCondition, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] == o[4] ? o[6] : o[8] )},
                    new Token[] { ELang.StringAndMathFn },
                    new Token[] { ELang.Operators },
                    new Token[] { ELang.Aggregate },
                    new Token[] { ELang.AggregateArray },
                    new Token[] { ELang.TypeConversions },
                    new Token[] { ELang.TypeCheck },
                    new Token[] { ELang.BulkFn },
                    new Token[] { ELang.ArrayLoop },
                    
                    new Token[] { ELang.Exists, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = (Invoke("valueof", true, new object[] { o[2], this._context }) != null) )},
                    new Token[] { ELang.ExistsNotEmpty, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.IfGroup, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = o[2] )},
                    new Token[] { ELang.Eval, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = o[2] )},
                    new Token[] { ELang.Xconcat, ELang.LParenthesis, ELang.ARG, ELang.C_ARG, ELang.RParenthesis, new Op(o => o[0] = Xconcat(o[2], o[3])  )},
                    new Token[] { ELang.Grouparrayby, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], o[6], this._context }) )},
                    new Token[] { ELang.Customfunction, ELang.LParenthesis, ELang.ARGS },
                    new Token[] { ELang.String, ELang.LParenthesis, ELang.ARGS },
                },

                [ELang.StringAndMathFn] = new Token[][]
                {
                    new Token[] { ELang.LastIndexOf, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2].LastIndexOf(o[4]) )},
                    new Token[] { ELang.FirstIndexOf, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2].IndexOf(o[4]) )},
                    new Token[] { ELang.Substring, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2].Substring(Convert.ToInt32(o[4]), Convert.ToInt32(o[6])) )},
                    new Token[] { ELang.Concat, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], o[4], this._context}) )},
                    new Token[] { ELang.Length, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Add, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] + o[4] )},
                    new Token[] { ELang.Subtract, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] - o[4] )},
                    new Token[] { ELang.Multiply, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] * o[4] )},
                    new Token[] { ELang.Divide, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] / o[4] )},
                    new Token[] { ELang.Round, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = decimal.Round(Convert.ToDecimal(o[2]), Convert.ToInt32(o[4]), MidpointRounding.AwayFromZero) )},
                },
                [ELang.Operators] = new Token[][]
                {
                    new Token[] { ELang.MathEquals, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] == o[4] )},
                    new Token[] { ELang.MathGreaterThan, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] > o[4] )},
                    new Token[] { ELang.MathLessThan, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] < o[4] )},
                    new Token[] { ELang.MathGreaterThanOrEqualTo, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] >= o[4] )},
                    new Token[] { ELang.MathLessThanOrEqualTo, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2] <= o[4] )},
                    new Token[] { ELang.StringEquals, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2].ToString() == o[4].ToString() )},
                    new Token[] { ELang.StringContains, ELang.LParenthesis, ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = o[2].ToString().Contains(o[4].ToString()) )},
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
                [ELang.ArrayLoop] = new Token[][]
                {
                    new Token[] { ELang.Loop, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke("valueof", true, new object[] { o[2], this._context }) )},
                    new Token[] { ELang.Currentvalue, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { null, this._currentArrayToken[this._arrayAlias] }) )},
                    new Token[] { ELang.Currentindex, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { this._parentArrayToken[this._arrayAlias], this._currentArrayToken[this._arrayAlias] }) )},
                    new Token[] { ELang.Currentproperty, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { null, this._currentArrayToken[this._arrayAlias], this._context }) )},
                    new Token[] { ELang.Lastindex, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { this._parentArrayToken[this._arrayAlias], this._currentArrayToken[this._arrayAlias] }) )},
                    new Token[] { ELang.Lastvalue, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { this._parentArrayToken[this._arrayAlias], this._currentArrayToken[this._arrayAlias] }) )},
                    new Token[] { ELang.Currentvalueatpath, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { null, this._currentArrayToken[this._arrayAlias], o[2], this._context }) )},
                    new Token[] { ELang.Lastvalueatpath, ELang.LParenthesis, ELang.ARGS, new Op(o => o[0] = Invoke(o[0], true, new object[] { this._parentArrayToken[this._arrayAlias], this._currentArrayToken[this._arrayAlias], o[2], this._context }) )},
                },

                [ELang.C_ARG] = new Token[][]
                {
                    new Token[] { ELang.Comma, ELang.ARG, ELang.C_ARG, new Op(o => o[0] = Xconcat(o[1], o[2])) },
                    new Token[] { ELang.Comma, ELang.ARG, new Op(o => o[0] = o[1] ) },
                },

                [ELang.ARGS] = new Token[][]
                {
                    new Token[] { ELang.ARG, ELang.Comma, ELang.ARGS, new Op(o => o[0] = Xconcat(o[0], o[2]) ) },
                    new Token[] { ELang.ARG, ELang.RParenthesis },
                },
                [ELang.ARG] = new Token[][]
                {
                    new Token[] { ELang.EXPR },
                    new Token[] { ELang.JsonPathEx },
                    new Token[] { ELang.Number, new Op(o => o[0] = Convert.ToDecimal(o[0]) )},
                    new Token[] { ELang.String },
                    new Token[] {},
                },
            });
            return new ParserGenerator<ELang>(new Lexer<ELang>(tokens, ELang.Ignore), rules).CompileParser();
        }

        private object Invoke(string fn, bool convertParameters, object[] parameters)
        {
            return ReflectionHelper.Caller<JsonPathSelectable>(null, "JUST.Transformer`1", fn, parameters, convertParameters, this._context);
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
                    result.Add(arg2);
                }
                return result.ToArray();
            }
            else 
            {
                return arg1 + arg2;
            }

        }
    }
}