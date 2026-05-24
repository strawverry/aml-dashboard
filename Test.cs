using System;
using System.Collections.Generic;
using System.Linq;
using AML.Models;
using AML.Rules;
using AML.Services;
using AML.ML;

class Tests
{
    static int passed = 0;
    static int failed = 0;

    static void Assert(string testName, bool condition)
    {
        if (condition)
        {
            Console.WriteLine($"  ✓ PASS: {testName}");
            passed++;
        }
        else
        {
            Console.WriteLine($"  ✗ FAIL: {testName}");
            failed++;
        }
    }

    public static void RunAll()
    {
        Console.WriteLine("\n╔═══════════════════════════════════════╗");
        Console.WriteLine("║         AML UNIT TESTS                ║");
        Console.WriteLine("╚═══════════════════════════════════════╝");

        var engine = new RuleEngine();

        // ── Rule Engine Tests ──────────────────
        Console.WriteLine("\n--- Rule Engine Tests ---");

        var tx1 = new Transaction { Type="TRANSFER", Amount=300_000,
            OldbalanceOrg=300_000, NewbalanceOrig=0,
            OldbalanceDest=0, NewbalanceDest=0 };
        Assert("LARGE_TRANSFER fires for big transfer",
            engine.Evaluate(tx1).Contains("LARGE_TRANSFER"));

        var tx2 = new Transaction { Type="PAYMENT", Amount=500,
            OldbalanceOrg=10_000, NewbalanceOrig=9_500,
            OldbalanceDest=0, NewbalanceDest=500 };
        Assert("No rules fire for normal payment",
            engine.Evaluate(tx2).Count == 0);

        var tx3 = new Transaction { Type="TRANSFER", Amount=50_000,
            OldbalanceOrg=50_000, NewbalanceOrig=0,
            OldbalanceDest=0, NewbalanceDest=0 };
        Assert("ACCOUNT_DRAIN fires when account emptied",
            engine.Evaluate(tx3).Contains("ACCOUNT_DRAIN"));

        var tx4 = new Transaction { Type="PAYMENT", Amount=9_800,
            OldbalanceOrg=20_000, NewbalanceOrig=10_200,
            OldbalanceDest=0, NewbalanceDest=9_800 };
        Assert("STRUCTURING fires just below limit",
            engine.Evaluate(tx4).Contains("STRUCTURING"));

        var tx5 = new Transaction { Type="TRANSFER", Amount=600_000,
            OldbalanceOrg=600_000, NewbalanceOrig=0,
            OldbalanceDest=0, NewbalanceDest=0 };
        Assert("CRITICAL_AMOUNT fires above threshold",
            engine.Evaluate(tx5).Contains("CRITICAL_AMOUNT"));

        // ── Detection Service Tests ────────────
        Console.WriteLine("\n--- Detection Service Tests ---");

        var model = new FraudDetectionModel();
        var svc   = new AmlDetectionService(engine, model);

        var suspiciousTx = new Transaction { Type="TRANSFER", Amount=300_000,
            OldbalanceOrg=300_000, NewbalanceOrig=0,
            OldbalanceDest=0, NewbalanceDest=0, IsFraud=1 };
        Assert("Alert raised for suspicious transaction",
            svc.Analyse(suspiciousTx) != null);

        var normalTx = new Transaction { Type="PAYMENT", Amount=500,
            OldbalanceOrg=10_000, NewbalanceOrig=9_500,
            OldbalanceDest=0, NewbalanceDest=500, IsFraud=0 };
        Assert("No alert for normal transaction",
            svc.Analyse(normalTx) == null);

        var criticalTx = new Transaction { Type="TRANSFER", Amount=600_000,
            OldbalanceOrg=600_000, NewbalanceOrig=0,
            OldbalanceDest=50_000, NewbalanceDest=50_000, IsFraud=1 };
        var critAlert = svc.Analyse(criticalTx);
        Assert("Critical severity for many rules",
            critAlert != null && critAlert.Severity == AlertSeverity.Critical);

        Assert("True positive marked correctly",
            svc.Analyse(suspiciousTx)!.IsTruePositive == true);

        Assert("False positive marked correctly",
            svc.Analyse(new Transaction { Type="TRANSFER", Amount=300_000,
                OldbalanceOrg=300_000, NewbalanceOrig=0,
                IsFraud=0 })!.IsTruePositive == false);

        // ── Reporting Service Tests ────────────
        Console.WriteLine("\n--- Reporting Service Tests ---");

        var reporter = new ReportingService();
        var txList   = new List<Transaction>
        {
            new Transaction { IsFraud=1 },
            new Transaction { IsFraud=1 },
            new Transaction { IsFraud=0 },
            new Transaction { IsFraud=0 }
        };
        var alertList = new List<Alert>
        {
            new Alert { Transaction=txList[0], IsTruePositive=true,
                        Severity=AlertSeverity.Low, Source=AlertSource.RuleBased },
            new Alert { Transaction=txList[1], IsTruePositive=true,
                        Severity=AlertSeverity.Low, Source=AlertSource.RuleBased }
        };
        var report = reporter.GenerateReport(alertList, txList);
        Assert("Perfect detection: Precision = 1.0",
            Math.Abs(report.Precision - 1.0) < 0.001);
        Assert("Perfect detection: Recall = 1.0",
            Math.Abs(report.Recall - 1.0) < 0.001);
        Assert("Correct True Positives count",
            report.TruePositives == 2);
        Assert("Correct False Positives count",
            report.FalsePositives == 0);

        // ── Summary ───────────────────────────
        Console.WriteLine("\n─────────────────────────────────────────");
        Console.WriteLine($"  Results: {passed} passed, {failed} failed");
        if (failed == 0)
            Console.WriteLine("  ✓ ALL TESTS PASSED!");
        else
            Console.WriteLine("  ✗ SOME TESTS FAILED");
        Console.WriteLine("─────────────────────────────────────────");
    }
}