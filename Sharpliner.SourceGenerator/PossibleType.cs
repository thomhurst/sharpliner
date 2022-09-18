﻿namespace Sharpliner.SourceGenerator;

internal record PossibleType
{
    internal PossibleType(string className, string shortName)
    {
        ClassName = className;
        ShortName = shortName;
    }

    public string ClassName { get; }
    public string ShortName { get; }

    internal static PossibleType Parameter = new("ParameterReference", "Parameter");
    internal static PossibleType Variable = new("VariableReference", "Variable");
    internal static PossibleType StaticVariable = new("StaticVariableReference", "Variable");
    internal static PossibleType String = new("string", "String");
    internal static PossibleType Array = new("IEnumerable<object>", "Array");

    internal static PossibleType[] InlinePossibleTypes = {Parameter, Variable, String, Array};
    internal static PossibleType[] IfPossibleTypes = {Parameter, StaticVariable, String, Array};
}
