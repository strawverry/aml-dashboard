using System;

namespace AML.Models
{
    public class Transaction
    {
        public int    Step           { get; set; }
        public string Type           { get; set; } = "";
        public double Amount         { get; set; }
        public string NameOrig       { get; set; } = "";
        public double OldbalanceOrg  { get; set; }
        public double NewbalanceOrig { get; set; }
        public string NameDest       { get; set; } = "";
        public double OldbalanceDest { get; set; }
        public double NewbalanceDest { get; set; }
        public int    IsFraud        { get; set; }
        public int    IsFlaggedFraud { get; set; }

        public double BalanceDiffOrig     => OldbalanceOrg - NewbalanceOrig;
        public double BalanceDiffDest     => NewbalanceDest - OldbalanceDest;
        public bool   OrigAccountDrained  => OldbalanceOrg > 0 && NewbalanceOrig == 0;
        public bool   DestAccountUnchanged => OldbalanceDest == NewbalanceDest && Amount > 0;
    }
}