using System;
using System.Collections.Generic;
using System.Linq;
using AML.Models;
using AML.Rules;
using AML.ML;
using AML.Services;

class Program
{
    static void Main(string[] args)
    {
        Tests.RunAll();
        Console.WriteLine("╔═══════════════════════════════════════════════╗");
        Console.WriteLine("║     AI-Powered AML Detection Platform        ║");
        Console.WriteLine("╚═══════════════════════════════════════════════╝");

        // Step 1: Load Data
        Console.WriteLine("\n▶ Step 1: Loading transactions...");
        var loader       = new TransactionDataLoader();
        var transactions = loader.LoadFromCsv("aml_sample.csv");
        Console.WriteLine($"  Loaded {transactions.Count:N0} transactions");

        // Step 2: Train ML Model
        Console.WriteLine("\n▶ Step 2: Training ML model...");
        var mlModel = new FraudDetectionModel();
        var result  = mlModel.Train(transactions);
        Console.WriteLine("  " + result);

        // Step 3: Run Detection
        Console.WriteLine("\n▶ Step 3: Running detection...");
        var ruleEngine = new RuleEngine();
        var detector   = new AmlDetectionService(ruleEngine, mlModel);
        var alerts     = new List<Alert>(detector.AnalyseBatch(transactions));
        Console.WriteLine($"  {alerts.Count} alerts raised");

        // Step 4: Show Sample Alerts
        Console.WriteLine("\n▶ Step 4: Sample alerts:");
        int shown = 0;
        foreach (var alert in alerts)
        {
            if (shown >= 5) break;
            Console.WriteLine($"  {alert.Summary}");
            shown++;
        }

        // Step 5: Report
        Console.WriteLine("\n▶ Step 5: Performance Report:");
        var reporter = new ReportingService();
        var report   = reporter.GenerateReport(alerts, transactions);
        reporter.PrintReport(report);

       
        // Step 6: Generate HTML Report
        Console.WriteLine("\n▶ Step 6: Generating HTML report...");
        var generator = new ReportGenerator();
        generator.GenerateHtmlReport(report, alerts);
        Console.WriteLine("  Open AML_Report.html in your browser!");
        Console.WriteLine("\n✔ Done! Press any key to exit.");
        Console.ReadKey();
    }
}