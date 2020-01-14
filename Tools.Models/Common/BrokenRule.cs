using System;
using System.Collections.Generic;
using System.Text;

namespace Tools.Models.Common
{
    public class BrokenRule
    {
        public string Rule { get; set; }
        public object Instance { get; set; }

        public BrokenRule()
        {
            Rule = "";
        }

        public BrokenRule(string rule)
        {
            Rule = rule;
        }

        public BrokenRule(string rule, object instance)
        {
            Rule = rule;
            Instance = instance;
        }
    }

    public class BrokenRules : List<BrokenRule>
    {
    }
}
