

using System.Collections.Generic;

namespace CSharpParserGenerator
{
    public class ParseResult
    {
        public string Text { get; }
        public bool Success { get; }
        public object Value { get; }

        public List<ErrorInfo> Errors { get; }

        public ParseResult(string text, bool success = false, object value = null, List<ErrorInfo> errors = null)
        {
            Text = text;
            Success = success;
            Errors = errors ?? new List<ErrorInfo>();
            Value = value;
        }
    }

    public class ErrorInfo
    {
        public string Type { get; set; }
        public string Description { get; set; }
    }
}