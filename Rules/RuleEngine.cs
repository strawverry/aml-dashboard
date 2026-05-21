using System;
using System.Collections.Generic;
using AML.Models;

namespace AML.Rules
{
    public class RuleEngine
    {
        public double LargeTransactionThreshold { get; set; } = 200_000;
        public double StructuringThreshold      { get; set; } = 9_500;
        public double HighRiskAmountThreshold   { get; set; } = 500_000;

        private readonly List<AmlRule> _rules;

        public RuleEngine()
        {
            _rules = new List<AmlRule>
            {
                new AmlRule("LARGE_TRANSFER",
                    tx => (tx.Type == "TRANSFER" || tx.Type == "CASH_OUT")
                          && tx.Amount >= LargeTransactionThreshold),

                new AmlRule("ACCOUNT_DRAIN",
                    tx => tx.OrigAccountDrained 
                    && tx.Amount > 10_000
                    && (tx.Type == "TRANSFER" || tx.Type == "CASH_OUT")),

                    new AmlRule("DEST_BALANCE_UNCHANGED",
                    tx => tx.DestAccountUnchanged && tx.Amount > 5_000 && (tx.Type == "TRANSFER" || tx.Type == "CASH_OUT")),

                new AmlRule("STRUCTURING",
                    tx => tx.Amount >= StructuringThreshold && tx.Amount < 10_000),

                new AmlRule("ZERO_ORIGIN_LARGE_TX",
                    tx => tx.OldbalanceOrg == 0 
                    && tx.Amount >= 50_000
                    && (tx.Type == "TRANSFER" || tx.Type == "CASH_OUT")),

                new AmlRule("RAPID_CASH_OUT",
                    tx => tx.Type == "CASH_OUT" && tx.Step <= 5 && tx.Amount > 50_000),

                new AmlRule("CRITICAL_AMOUNT",
                    tx => tx.Amount >= HighRiskAmountThreshold),

                new AmlRule("BALANCE_MISMATCH",
                    tx => tx.Type == "TRANSFER"
                          && Math.Abs(tx.BalanceDiffOrig - tx.Amount) > 1.0
                          && tx.OldbalanceOrg > 0),
            };
        }

        public IReadOnlyList<string> Evaluate(Transaction tx)
        {
            var fired = new List<string>();
            foreach (var rule in _rules)
                if (rule.Condition(tx))
                    fired.Add(rule.Name);
            return fired;
        }
    }

    internal class AmlRule
    {
        public string Name { get; }
        public Func<Transaction, bool> Condition { get; }

        public AmlRule(string name, Func<Transaction, bool> condition)
        { Name = name; Condition = condition; }
    }
}