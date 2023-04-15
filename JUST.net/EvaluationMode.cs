using System;

namespace JUST
{
    [Flags]
    public enum EvaluationMode : short
    {
        FallbackToDefault = 1,
        AddOrReplaceProperties = 2,
        Strict = 4,
        JoinArrays = 8,
        LookInTransformed = 16
    }
}