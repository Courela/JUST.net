using System;
using System.IO;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using JUST;

namespace JSonTransformerBenchmark
{
    public class Benchmarks
    {
        private string _largeInput;
        private string _transformer;
        private string _input;
        private string _largeTransformer;
        
        [GlobalSetup]
        public void JSonTransformerBenchmark()
        {
            this._largeInput = File.ReadAllText("Inputs/large_input.json");
            this._transformer = "{ \"result\": { \"#loop($.list)\": { \"id\": \"#currentindex()\", \"name\": \"#concat(#currentvalueatpath($.title), #currentvalueatpath($.name))\", \"contact\": \"#currentvalueatpath($.contacts[?(@.is_default==true)])\", \"address\": \"#currentvalueatpath($.addresses[0])\" } }";
            
            this._input = "{ \" title\" : \" Mr.\" , \" name\" : \" Smith\" , \" addresses\" : [ { \" street\" : \" Some Street\" , \" number\" : 1, \" city\" : \" Some City\" , \" postal_code\" : 1234 }, { \" street\" : \" Some Other Street\" , \" number\" : 2, \" city\" : \" Some Other City\" , \" postal_code\" : 5678 } ], \" contacts\" : [ { \" type\" : \" home\" , \" number\" : 123546789, \" is_default\" : false }, { \" type\" : \" mobile\" , \" number\" : 987654321, \" is_default\" : true } ] }";
            this._largeTransformer = File.ReadAllText("Inputs/large_transformer.json");
        }

        [Benchmark]
        public string JsonTransformerLargeInput()
        {
            return new JsonTransformer().Transform(this._transformer, this._largeInput);
        }

        [Benchmark]
        public string JsonTransformerLargeTransformer()
        {
            return new JsonTransformer().Transform(this._largeTransformer, this._input);
        }
    }
}
