﻿using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Sharpliner.AzureDevOps.ConditionedExpressions;

/// <summary>
/// Represents an ${{ if ... }} statement in the YAML.
/// When we build trees of definitions with conditions on them, we either start with a definition or a condition.
/// A condition then has to evolve into a conditioned definition (so that we have something inside the "if").
/// </summary>
public abstract class Condition : IYamlConvertible
{
    protected Condition()
    {
    }

    internal abstract string Serialize();

    internal const string ExpressionStart = "${{ ";
    protected const string IfTagStart = $"{ExpressionStart}if ";
    protected const string ElseTagStart = $"{ExpressionStart}else";
    internal const string ExpressionEnd = " }}";

    private const string VariableIndexAccessStart = "variables[";
    private const string VariablePropertyAccessStart = "variables.";
    private const string ParametersIndexAccessStart = "parameters[";
    private const string ParametersPropertyAccessStart = "parameters.";

    internal virtual string TagStart => IfTagStart;
    internal virtual string TagEnd => ExpressionEnd;

    internal Conditioned? Parent { get; set; }

    public static implicit operator string(Condition value) => value.Serialize();
    public override string ToString() => this;

    public static string Join(IEnumerable<string> expressions) => string.Join(", ", expressions);

    public static string WrapQuotes(string value)
    {
        if (value.StartsWith('\'')
            || bool.TryParse(value, out _)
            || long.TryParse(value, out _)
            || double.TryParse(value, out _)
            || value.StartsWith(VariableIndexAccessStart)
            || value.StartsWith(VariablePropertyAccessStart)
            || value.StartsWith(ParametersIndexAccessStart)
            || value.StartsWith(ParametersPropertyAccessStart))
        {
            return value;
        }

        return $"'{value}'";
    }

    public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer) => throw new NotImplementedException();

    public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer) =>
        emitter.Emit(new Scalar(Serialize()));
}

public class InlineCustomCondition : InlineCondition
{
    private readonly string _condition;

    public InlineCustomCondition(string condition)
    {
        _condition = condition;
    }

    internal override string Serialize() => _condition;
}

public class IfCustomCondition : IfCondition
{
    private readonly string _condition;

    public IfCustomCondition(string condition)
    {
        _condition = condition;
    }

    internal override string Serialize() => WrapTag(_condition);
}

public class InlineNotCondition : InlineCondition
{
    private readonly string _condition;

    internal InlineNotCondition(InlineCondition condition)
        : this(condition.Serialize())
    {
    }

    internal InlineNotCondition(string condition)
    {
        _condition = NotConditionHelper.NegateCondition(condition);
    }

    internal override string Serialize() => _condition;
}

public class IfNotCondition : IfCondition
{
    private readonly string _condition;

    internal IfNotCondition(IfCondition condition)
        : this(condition.WithoutTags())
    {
    }

    internal IfNotCondition(string condition)
    {
        _condition = NotConditionHelper.NegateCondition(WithoutTags(condition));
    }

    internal override string Serialize() => WrapTag(_condition);
}

public class IfEqualityCondition : IfStringCondition
{
    internal IfEqualityCondition(bool equal, Arguments.IfConditionOneOfStringValue expression1, Arguments.IfConditionOneOfStringValue expression2)
        : base(equal ? "eq" : "ne", expression1, expression2)
    {
    }
}

public class InlineEqualityCondition : InlineStringCondition
{
    internal InlineEqualityCondition(bool equal, Arguments.InlineConditionOneOfStringValue expression1, Arguments.InlineConditionOneOfStringValue expression2)
        : base(equal ? "eq" : "ne", expression1, expression2)
    {
    }
}

public class InlineAndCondition : InlineConjunctionCondition
{
    internal InlineAndCondition(params InlineCondition[] expressions) : base("and", expressions)
    {
    }

    internal InlineAndCondition(params string[] expressions) : base("and", expressions)
    {
    }
}

public class IfAndCondition : IfConjunctionCondition
{
    internal IfAndCondition(params IfCondition[] expressions) : base("and", expressions)
    {
    }

    internal IfAndCondition(params string[] expressions) : base("and", expressions)
    {
    }
}

public class InlineOrCondition : InlineConjunctionCondition
{
    internal InlineOrCondition(params InlineCondition[] expressions)
        : base("or", expressions)
    {
    }

    internal InlineOrCondition(params string[] expressions)
        : base("or", expressions)
    {
    }
}

public class IfOrCondition : IfConjunctionCondition
{
    internal IfOrCondition(params IfCondition[] expressions)
        : base("or", expressions)
    {
    }

    internal IfOrCondition(params string[] expressions)
        : base("or", expressions)
    {
    }
}

public class InlineConjunctionCondition : InlineCondition
{
    private readonly string _keyword;
    private readonly string[] _expressions;

    internal InlineConjunctionCondition(string keyword, params InlineCondition[] expressions)
        : this(keyword, expressions.Select(e => e.Serialize()).ToArray())
    {
    }

    internal InlineConjunctionCondition(string keyword, params string[] expressions)
    {
        _keyword = keyword;
        _expressions = expressions;
    }

    internal override string Serialize() => $"{_keyword}({Join(_expressions)})";
}

public class InlineConjunctionCondition<T> : InlineCondition<T>
{
    private readonly string _keyword;
    private readonly string[] _expressions;

    internal InlineConjunctionCondition(string keyword, InlineCondition[] expressions, Conditioned<T>? parent = null)
        : this(keyword, expressions.Select(e => e.Serialize()).ToArray(), parent)
    {
    }

    internal InlineConjunctionCondition(string keyword, string[] expressions, Conditioned<T>? parent = null) : base(parent)
    {
        _keyword = keyword;
        _expressions = expressions;
    }

    internal override string Serialize() => $"{_keyword}({Join(_expressions)})";
}

public class IfConjunctionCondition : IfCondition
{
    private readonly string _keyword;
    private readonly string[] _expressions;

    internal IfConjunctionCondition(string keyword, params IfCondition[] expressions)
        : this(keyword, expressions.Select(WithoutTags).ToArray())
    {
    }

    internal IfConjunctionCondition(string keyword, params string[] expressions)
    {
        _keyword = keyword;
        _expressions = expressions;
    }

    internal override string Serialize() => WrapTag($"{_keyword}({Join(_expressions)})");
}

public class IfConjunctionCondition<T> : IfCondition<T>
{
    private readonly string _keyword;
    private readonly string[] _expressions;

    internal IfConjunctionCondition(string keyword, IfCondition[] expressions, Conditioned<T>? parent = null)
        : this(keyword, expressions.Select(e => e.Serialize()).ToArray(), parent)
    {
    }

    internal IfConjunctionCondition(string keyword, string[] expressions, Conditioned<T>? parent = null) : base(parent)
    {
        _keyword = keyword;
        _expressions = expressions.Select(WithoutTags).ToArray();
    }

    internal override string Serialize() => WrapTag($"{_keyword}({Join(_expressions)})");
}

public class InlineXorCondition : InlineConjunctionCondition
{
    internal InlineXorCondition(InlineCondition expression1, InlineCondition expression2)
        : base("xor", expression1, expression2)
    {
    }

    internal InlineXorCondition(string expression1, string expression2)
        : base("xor", expression1, expression2)
    {
    }
}

public class IfXorCondition : IfConjunctionCondition
{
    internal IfXorCondition(IfCondition expression1, IfCondition expression2)
        : base("xor", expression1, expression2)
    {
    }

    internal IfXorCondition(string expression1, string expression2)
        : base("xor", expression1, expression2)
    {
    }
}

public class InlineContainsCondition : InlineStringCondition
{
    internal InlineContainsCondition(Arguments.InlineConditionOneOfStringValue needle, Arguments.InlineConditionOneOfStringValue haystack)
        : base("contains", haystack, needle)
    {
    }
}

public class IfContainsCondition : IfStringCondition
{
    internal IfContainsCondition(Arguments.IfConditionOneOfStringValue needle, Arguments.IfConditionOneOfStringValue haystack)
        : base("contains", haystack, needle)
    {
    }
}

public class InlineStartsWithCondition : InlineStringCondition
{
    internal InlineStartsWithCondition(Arguments.InlineConditionOneOfStringValue needle, Arguments.InlineConditionOneOfStringValue haystack)
        : base("startsWith", haystack, needle)
    {
    }
}

public class IfStartsWithCondition : IfStringCondition
{
    internal IfStartsWithCondition(Arguments.IfConditionOneOfStringValue needle, Arguments.IfConditionOneOfStringValue haystack)
        : base("startsWith", haystack, needle)
    {
    }
}

public class InlineEndsWithCondition : InlineStringCondition
{
    internal InlineEndsWithCondition(Arguments.InlineConditionOneOfStringValue needle, Arguments.InlineConditionOneOfStringValue haystack)
        : base("endsWith", haystack, needle)
    {
    }
}

public class IfEndsWithCondition : IfStringCondition
{
    internal IfEndsWithCondition(Arguments.IfConditionOneOfStringValue needle, Arguments.IfConditionOneOfStringValue haystack)
        : base("endsWith", haystack, needle)
    {
    }
}

public class InlineInCondition : InlineStringCondition
{
    internal InlineInCondition(Arguments.InlineConditionOneOfStringValue needle, params Arguments.InlineConditionOneOfStringValue[] haystack)
        : base("in", needle, haystack)
    {
    }
}

public class IfInCondition : IfStringCondition
{
    internal IfInCondition(Arguments.IfConditionOneOfStringValue needle, params Arguments.IfConditionOneOfStringValue[] haystack)
        : base("in", needle, haystack)
    {
    }
}

public class InlineNotInCondition : InlineStringCondition
{
    internal InlineNotInCondition(Arguments.InlineConditionOneOfStringValue needle, params Arguments.InlineConditionOneOfStringValue[] haystack)
        : base("notIn", needle, haystack)
    {
    }
}

public class IfNotInCondition : IfStringCondition
{
    internal IfNotInCondition(Arguments.IfConditionOneOfStringValue needle, params Arguments.IfConditionOneOfStringValue[] haystack)
        : base("notIn", needle, haystack)
    {
    }
}

public class InlineContainsValueCondition : InlineStringCondition
{
    internal InlineContainsValueCondition(Arguments.InlineConditionOneOfStringValue needle, params Arguments.InlineConditionOneOfStringValue[] haystack)
        : base("containsValue", haystack, needle)
    {
    }
}

public class IfContainsValueCondition : IfStringCondition
{
    internal IfContainsValueCondition(Arguments.IfConditionOneOfStringValue needle, params Arguments.IfConditionOneOfStringValue[] haystack)
        : base("containsValue", haystack, needle)
    {
    }
}

public class InlineGreaterCondition : InlineStringCondition
{
    internal InlineGreaterCondition(Arguments.InlineConditionOneOfStringValue first, Arguments.InlineConditionOneOfStringValue second)
        : base("gt", first, second)
    {
    }
}

public class IfGreaterCondition : IfStringCondition
{
    internal IfGreaterCondition(Arguments.IfConditionOneOfStringValue first, Arguments.IfConditionOneOfStringValue second)
        : base("gt", first, second)
    {
    }
}

public class InlineLessCondition : InlineStringCondition
{
    internal InlineLessCondition(Arguments.InlineConditionOneOfStringValue first, Arguments.InlineConditionOneOfStringValue second)
        : base("lt", first, second)
    {
    }
}

public class IfLessCondition : IfStringCondition
{
    internal IfLessCondition(Arguments.IfConditionOneOfStringValue first, Arguments.IfConditionOneOfStringValue second)
        : base("lt", first, second)
    {
    }
}

public class InlineCustomCondition<T> : InlineCondition<T>
{
    private readonly string _condition;

    public InlineCustomCondition(string condition)
    {
        _condition = condition;
    }

    internal override string Serialize() => _condition;
}

public class IfCustomCondition<T> : IfCondition<T>
{
    private readonly string _condition;

    public IfCustomCondition(string condition)
    {
        _condition = condition;
    }

    internal override string Serialize() => WrapTag(_condition);
}

public class InlineNotCondition<T> : InlineCondition<T>
{
    private readonly string _condition;

    internal InlineNotCondition(Condition condition)
        : this(condition.Serialize())
    {
    }

    internal InlineNotCondition(string condition)
    {
        _condition = NotConditionHelper.NegateCondition(condition);
    }

    internal override string Serialize() => _condition;
}

public class IfNotCondition<T> : IfCondition<T>
{
    private readonly string _condition;

    internal IfNotCondition(IfCondition condition)
        : this(condition.WithoutTags())
    {
    }

    internal IfNotCondition(string condition)
    {
        _condition = NotConditionHelper.NegateCondition(WithoutTags(condition));
    }

    internal override string Serialize() => _condition;
}

public class ElseCondition<T> : IfCondition<T>
{
    internal override string Serialize() => ElseTagStart + ExpressionEnd;

    internal override string TagStart => ElseTagStart;

    internal ElseCondition()
    {
    }

    public static implicit operator string(ElseCondition<T> value) => ElseTagStart + ExpressionEnd;

    public override string ToString() => ElseTagStart + ExpressionEnd;
}

public class InlineEqualityCondition<T> : InlineStringCondition<T>
{
    internal InlineEqualityCondition(bool equal, Arguments.InlineConditionOneOfStringValue expression1, Arguments.InlineConditionOneOfStringValue expression2)
        : base(equal ? "eq" : "ne", expression1, expression2)
    {
    }
}

public class IfEqualityCondition<T> : IfStringCondition<T>
{
    internal IfEqualityCondition(bool equal, Arguments.IfConditionOneOfStringValue expression1, Arguments.IfConditionOneOfStringValue expression2)
        : base(equal ? "eq" : "ne", expression1, expression2)
    {
    }
}


public class InlineAndCondition<T> : InlineConjunctionCondition<T>
{
    internal InlineAndCondition(params InlineCondition[] expressions)
        : base("and", expressions)
    {
    }

    internal InlineAndCondition(params string[] expressions)
        : base("and", expressions)
    {
    }
}

public class IfAndCondition<T> : IfConjunctionCondition<T>
{
    internal IfAndCondition(params IfCondition[] expressions)
        : base("and", expressions)
    {
    }

    internal IfAndCondition(params string[] expressions)
        : base("and", expressions)
    {
    }
}

public class InlineOrCondition<T> : InlineConjunctionCondition<T>
{
    internal InlineOrCondition(params InlineCondition[] expressions)
        : base("or", expressions)
    {
    }

    internal InlineOrCondition(params string[] expressions)
        : base("or", expressions)
    {
    }
}

public class IfOrCondition<T> : IfConjunctionCondition<T>
{
    internal IfOrCondition(params IfCondition[] expressions)
        : base("or", expressions)
    {
    }

    internal IfOrCondition(params string[] expressions)
        : base("or", expressions)
    {
    }
}

public class InlineXorCondition<T> : InlineConjunctionCondition<T>
{
    internal InlineXorCondition(InlineCondition expression1, InlineCondition expression2)
        : base("xor", new[] { expression1, expression2 })
    {
    }

    internal InlineXorCondition(string expression1, string expression2)
        : base("xor", new[] { expression1, expression2 })
    {
    }
}

public class IfXorCondition<T> : IfConjunctionCondition<T>
{
    internal IfXorCondition(IfCondition expression1, IfCondition expression2)
        : base("xor", new[] { expression1, expression2 })
    {
    }

    internal IfXorCondition(string expression1, string expression2)
        : base("xor", new[] { expression1, expression2 })
    {
    }
}

public class InlineContainsCondition<T> : InlineStringCondition<T>
{
    internal InlineContainsCondition(Arguments.InlineConditionOneOfStringValue haystack, Arguments.InlineConditionOneOfStringValue needle)
        : base("contains", haystack, needle)
    {
    }
}

public class IfContainsCondition<T> : IfStringCondition<T>
{
    internal IfContainsCondition(Arguments.IfConditionOneOfStringValue haystack, Arguments.IfConditionOneOfStringValue needle)
        : base("contains", haystack, needle)
    {
    }
}

public class InlineStartsWithCondition<T> : InlineStringCondition<T>
{
    internal InlineStartsWithCondition(Arguments.InlineConditionOneOfStringValue needle, Arguments.InlineConditionOneOfStringValue haystack)
        : base("startsWith", haystack, needle)
    {
    }
}

public class IfStartsWithCondition<T> : IfStringCondition<T>
{
    internal IfStartsWithCondition(Arguments.IfConditionOneOfStringValue needle, Arguments.IfConditionOneOfStringValue haystack)
        : base("startsWith", haystack, needle)
    {
    }
}

public class InlineEndsWithCondition<T> : InlineStringCondition<T>
{
    internal InlineEndsWithCondition(Arguments.InlineConditionOneOfStringValue needle, Arguments.InlineConditionOneOfStringValue haystack)
        : base("endsWith", haystack, needle)
    {
    }
}

public class IfEndsWithCondition<T> : IfStringCondition<T>
{
    internal IfEndsWithCondition(Arguments.IfConditionOneOfStringValue needle, Arguments.IfConditionOneOfStringValue haystack)
        : base("endsWith", haystack, needle)
    {
    }
}

public class InlineContainsValueCondition<T> : InlineStringCondition<T>
{
    internal InlineContainsValueCondition(Arguments.InlineConditionOneOfStringValue needle, params Arguments.InlineConditionOneOfStringValue[] haystack)
        : base("containsValue", haystack, needle)
    {
    }
}

public class IfContainsValueCondition<T> : IfStringCondition<T>
{
    internal IfContainsValueCondition(Arguments.IfConditionOneOfStringValue needle, params Arguments.IfConditionOneOfStringValue[] haystack)
        : base("containsValue", haystack, needle)
    {
    }
}

public class InlineInCondition<T> : InlineStringCondition<T>
{
    internal InlineInCondition(Arguments.InlineConditionOneOfStringValue needle, params Arguments.InlineConditionOneOfStringValue[] haystack)
        : base("in", needle, haystack)
    {
    }
}

public class IfInCondition<T> : IfStringCondition<T>
{
    internal IfInCondition(Arguments.IfConditionOneOfStringValue needle, params Arguments.IfConditionOneOfStringValue[] haystack)
        : base("in", needle, haystack)
    {
    }
}

public class InlineNotInCondition<T> : InlineStringCondition<T>
{
    internal InlineNotInCondition(Arguments.InlineConditionOneOfStringValue needle, params Arguments.InlineConditionOneOfStringValue[] haystack)
        : base("notin", needle, haystack)
    {
    }
}

public class IfNotInCondition<T> : IfStringCondition<T>
{
    internal IfNotInCondition(Arguments.IfConditionOneOfStringValue needle, params Arguments.IfConditionOneOfStringValue[] haystack)
        : base("notin", needle, haystack)
    {
    }
}

public class InlineGreaterCondition<T> : InlineStringCondition<T>
{
    internal InlineGreaterCondition(Arguments.InlineConditionOneOfStringValue first, Arguments.InlineConditionOneOfStringValue second)
        : base("gt", first, second)
    {
    }
}

public class IfGreaterCondition<T> : IfStringCondition<T>
{
    internal IfGreaterCondition(Arguments.IfConditionOneOfStringValue first, Arguments.IfConditionOneOfStringValue second)
        : base("gt", first, second)
    {
    }
}

public class InlineLessCondition<T> : InlineStringCondition<T>
{
    internal InlineLessCondition(Arguments.InlineConditionOneOfStringValue first, Arguments.InlineConditionOneOfStringValue second)
        : base("lt", first, second)
    {
    }
}

public class IfLessCondition<T> : IfStringCondition<T>
{
    internal IfLessCondition(Arguments.IfConditionOneOfStringValue first, Arguments.IfConditionOneOfStringValue second)
        : base("lt", first, second)
    {
    }
}

public class InlineBranchCondition : InlineEqualityCondition
{
    internal InlineBranchCondition(Arguments.InlineConditionOneOfStringValue branchName, bool equal)
        : base(equal, new StaticVariableReference("Build.SourceBranch"), BranchNameHelper.FormatBranchName(Serialize(branchName)))
    {
    }
}

public class IfBranchCondition : IfEqualityCondition
{
    internal IfBranchCondition(Arguments.IfConditionOneOfStringValue branchName, bool equal)
        : base(equal, new StaticVariableReference("Build.SourceBranch"), BranchNameHelper.FormatBranchName(Serialize(branchName)))
    {
    }
}

public class InlineBranchCondition<T> : InlineEqualityCondition<T>
{
    internal InlineBranchCondition(Arguments.InlineConditionOneOfStringValue branchName, bool equal)
        : base(equal, new StaticVariableReference("Build.SourceBranch"), BranchNameHelper.FormatBranchName(Serialize(branchName)))
    {
    }
}

public class IfBranchCondition<T> : IfEqualityCondition<T>
{
    internal IfBranchCondition(Arguments.IfConditionOneOfStringValue branchName, bool equal)
        : base(equal, new StaticVariableReference("Build.SourceBranch"), BranchNameHelper.FormatBranchName(Serialize(branchName)))
    {
    }
}

public class InlineBuildReasonCondition : InlineEqualityCondition
{
    internal InlineBuildReasonCondition(Arguments.InlineConditionOneOfStringValue reason, bool equal)
        : base(equal, new StaticVariableReference("Build.Reason"), reason)
    {
    }
}

public class IfBuildReasonCondition : IfEqualityCondition
{
    internal IfBuildReasonCondition(Arguments.IfConditionOneOfStringValue reason, bool equal)
        : base(equal, new StaticVariableReference("Build.Reason"), reason)
    {
    }
}

public class InlineBuildReasonCondition<T> : InlineEqualityCondition<T>
{
    internal InlineBuildReasonCondition(Arguments.InlineConditionOneOfStringValue reason, bool equal)
        : base(equal, new StaticVariableReference("Build.Reason"), reason)
    {
    }
}

public class IfBuildReasonCondition<T> : IfEqualityCondition<T>
{
    internal IfBuildReasonCondition(Arguments.IfConditionOneOfStringValue reason, bool equal)
        : base(equal, new StaticVariableReference("Build.Reason"), reason)
    {
    }
}
