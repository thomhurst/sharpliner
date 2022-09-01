using Sharpliner.AzureDevOps.ConditionedExpressions.Interfaces;

namespace Sharpliner.AzureDevOps.ConditionedExpressions;

public class ParameterValue : IRuntimeExpression, ICompileTimeExpression
{
    public string ParameterName { get; }

    internal ParameterValue(string parameterName)
    {
        ParameterName = parameterName;
    }

    public string RuntimeExpression => $"parameters.{ParameterName}";

    public string CompileTimeExpression => Condition.ExpressionStart + $"parameters.{ParameterName}" + Condition.ExpressionEnd;

    public override string ToString() => CompileTimeExpression;

    public static implicit operator string(ParameterValue value) => value.ToString();
}
