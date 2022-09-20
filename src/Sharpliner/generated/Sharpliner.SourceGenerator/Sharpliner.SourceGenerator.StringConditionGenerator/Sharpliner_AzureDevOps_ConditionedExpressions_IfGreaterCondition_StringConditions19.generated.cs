﻿using System;
using Sharpliner.SourceGenerator;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using Sharpliner.AzureDevOps.ConditionedExpressions;

namespace Sharpliner.AzureDevOps.ConditionedExpressions
{
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(ParameterReference[] first, ParameterReference[] second) : base("gt",first,second)
        {
        }
    }
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(ParameterReference[] first, StaticVariableReference[] second) : base("gt",first,second)
        {
        }
    }
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(ParameterReference[] first, string[] second) : base("gt",first,second)
        {
        }
    }
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(StaticVariableReference[] first, ParameterReference[] second) : base("gt",first,second)
        {
        }
    }
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(StaticVariableReference[] first, StaticVariableReference[] second) : base("gt",first,second)
        {
        }
    }
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(StaticVariableReference[] first, string[] second) : base("gt",first,second)
        {
        }
    }
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(string[] first, ParameterReference[] second) : base("gt",first,second)
        {
        }
    }
    public partial class IfGreaterCondition : IfStringCondition
    {
        internal IfGreaterCondition(string[] first, StaticVariableReference[] second) : base("gt",first,second)
        {
        }
    }
}
