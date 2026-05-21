using System;
using System.Collections.Generic;
using AML.Models;

namespace AML.Services
{
    public class ReportingService
    {
        public DetectionReport GenerateReport(
            IReadOnlyList<Alert> alerts,
            IReadOnlyList<Transaction> allTransactions)
        {
            int totalFraud = 0, totalLegit = 0;
            foreach (var t in allTransactions)
            { if (t.IsFraud == 1) totalFraud++; else totalLegit++; }

            int tp = 0, fp = 0;
            foreach (var a in alerts)
            { if (a.IsTruePositive) tp++; else fp++; }

            int fn = totalFraud - tp;
            int tn = totalLegit - fp;

            double precision = tp + fp == 0 ? 0 : (double)tp / (tp + fp);
            double recall    = tp + fn == 0 ? 0 : (double)tp / (tp + fn);
            double f1        = precision + recall == 0 ? 0
                               : 2 * precision * recall / (precision + recall);
            double fpr       = fp + tn == 0 ? 0 : (double)fp / (fp + tn);
            double accuracy  = (double)(tp + tn) / allTransactions.Count;

            return new DetectionReport
            {
                TotalTransactions = allTransactions.Count,
                TotalFraud        = totalFraud,
                TotalAlerts       = alerts.Count,
                TruePositives     = tp,
                FalsePositives    = fp,
                FalseNegatives    = fn,
                TrueNegatives     = tn,
                Precision         = precision,
                Recall            = recall,
                F1Score           = f1,
                FalsePositiveRate = fpr,
                Accuracy          = accuracy
            };
        }

        public void PrintReport(DetectionReport r)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("        AML DETECTION PERFORMANCE REPORT       ");
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine($"  Total Transactions : {r.TotalTransactions,8:N0}");
            Console.WriteLine($"  Total Fraud Cases  : {r.TotalFraud,8:N0}");
            Console.WriteLine($"  Total Alerts Raised: {r.TotalAlerts,8:N0}");
            Console.WriteLine("───────────────────────────────────────────────");
            Console.WriteLine("  Confusion Matrix");
            Console.WriteLine($"    True  Positives : {r.TruePositives,6:N0}");
            Console.WriteLine($"    False Positives : {r.FalsePositives,6:N0}");
            Console.WriteLine($"    False Negatives : {r.FalseNegatives,6:N0}");
            Console.WriteLine($"    True  Negatives : {r.TrueNegatives,6:N0}");
            Console.WriteLine("───────────────────────────────────────────────");
            Console.WriteLine("  Performance Metrics");
            Console.WriteLine($"    Accuracy           : {r.Accuracy:P2}");
            Console.WriteLine($"    Precision          : {r.Precision:P2}");
            Console.WriteLine($"    Recall             : {r.Recall:P2}");
            Console.WriteLine($"    F1 Score           : {r.F1Score:F4}");
            Console.WriteLine($"    False Positive Rate: {r.FalsePositiveRate:P2}");
            Console.WriteLine("═══════════════════════════════════════════════");
        }
    }

    public class DetectionReport
    {
        public int    TotalTransactions  { get; set; }
        public int    TotalFraud         { get; set; }
        public int    TotalAlerts        { get; set; }
        public int    TruePositives      { get; set; }
        public int    FalsePositives     { get; set; }
        public int    FalseNegatives     { get; set; }
        public int    TrueNegatives      { get; set; }
        public double Precision          { get; set; }
        public double Recall             { get; set; }
        public double F1Score            { get; set; }
        public double FalsePositiveRate  { get; set; }
        public double Accuracy           { get; set; }
    }
}