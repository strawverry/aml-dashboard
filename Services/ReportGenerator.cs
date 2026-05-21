using System;
using System.Collections.Generic;
using System.IO;
using AML.Models;

namespace AML.Services
{
    public class ReportGenerator
    {
        public void GenerateHtmlReport(
            DetectionReport report,
            List<Alert> alerts,
            string outputPath = "AML_Report.html")
        {
            // Count alerts by severity
            int critical = 0, high = 0, medium = 0, low = 0;
            foreach (var a in alerts)
            {
                if (a.Severity == AlertSeverity.Critical) critical++;
                else if (a.Severity == AlertSeverity.High) high++;
                else if (a.Severity == AlertSeverity.Medium) medium++;
                else low++;
            }

            // Count alerts by type
            var typeCounts = new Dictionary<string, int>();
            foreach (var a in alerts)
            {
                string t = a.Transaction.Type;
                if (!typeCounts.ContainsKey(t)) typeCounts[t] = 0;
                typeCounts[t]++;
            }

            string html = $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>AML Detection Dashboard</title>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.4.0/chart.umd.min.js'></script>
    <style>
        * {{ margin:0; padding:0; box-sizing:border-box; }}
        body {{
            font-family: 'Segoe UI', sans-serif;
            background: #0f172a;
            color: #e2e8f0;
            padding: 20px;
        }}

        /* ── Header ── */
        .header {{
            text-align: center;
            padding: 25px;
            background: linear-gradient(135deg, #1e3a5f, #0f2744);
            border-radius: 12px;
            margin-bottom: 20px;
            border: 1px solid #2d4a6e;
        }}
        .header h1 {{ font-size:1.8em; color:#60a5fa; margin-bottom:6px; }}
        .header p  {{ color:#94a3b8; font-size:0.9em; }}

        /* ── KPI Cards ── */
        .kpi-row {{
            display: grid;
            grid-template-columns: repeat(5, 1fr);
            gap: 15px;
            margin-bottom: 20px;
        }}
        .kpi {{
            background: #1e293b;
            border-radius: 10px;
            padding: 18px;
            text-align: center;
            border: 1px solid #2d3f55;
        }}
        .kpi .val  {{ font-size:2em; font-weight:700; margin-bottom:4px; }}
        .kpi .lbl  {{ color:#94a3b8; font-size:0.78em; text-transform:uppercase; }}
        .blue   {{ color:#60a5fa; }}
        .red    {{ color:#f87171; }}
        .green  {{ color:#34d399; }}
        .yellow {{ color:#fbbf24; }}
        .orange {{ color:#fb923c; }}
        .purple {{ color:#a78bfa; }}

        /* ── Chart Grid ── */
        .chart-row {{
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 15px;
            margin-bottom: 20px;
        }}
        .chart-row-2 {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
            margin-bottom: 20px;
        }}
        .card {{
            background: #1e293b;
            border-radius: 10px;
            padding: 20px;
            border: 1px solid #2d3f55;
        }}
        .card h3 {{
            color: #60a5fa;
            font-size: 0.9em;
            margin-bottom: 15px;
            text-transform: uppercase;
            letter-spacing: 0.05em;
            border-bottom: 1px solid #2d3f55;
            padding-bottom: 8px;
        }}
        .chart-wrap {{
            position: relative;
            height: 220px;
        }}

        /* ── Gauge ── */
        .gauge-wrap {{
            position: relative;
            height: 160px;
            display:flex;
            flex-direction:column;
            align-items:center;
            justify-content:center;
        }}
        .gauge-label {{
            font-size:2em;
            font-weight:700;
            margin-top:10px;
        }}
        .gauge-sub {{
            color:#94a3b8;
            font-size:0.8em;
            margin-top:4px;
        }}

        /* ── Metric Bars ── */
        .metric-item {{
            margin-bottom:14px;
        }}
        .metric-header {{
            display:flex;
            justify-content:space-between;
            margin-bottom:5px;
            font-size:0.85em;
        }}
        .metric-name {{ color:#94a3b8; }}
        .metric-val  {{ font-weight:700; }}
        .bar-bg {{
            background:#0f172a;
            border-radius:999px;
            height:10px;
            overflow:hidden;
        }}
        .bar-fill {{
            height:100%;
            border-radius:999px;
            transition: width 1s ease;
        }}

        /* ── Confusion Matrix ── */
        .confusion {{
            display:grid;
            grid-template-columns:1fr 1fr;
            gap:12px;
            margin-top:5px;
        }}
        .cf-cell {{
            padding:18px;
            border-radius:8px;
            text-align:center;
        }}
        .cf-cell .n {{ font-size:2.2em; font-weight:700; }}
        .cf-cell .l {{ font-size:0.75em; opacity:0.8; margin-top:4px; }}
        .tp {{ background:#14532d; }}
        .fp {{ background:#7f1d1d; }}
        .fn {{ background:#7c2d12; }}
        .tn {{ background:#1e3a5f; }}

        /* ── Alerts Table ── */
        table {{
            width:100%;
            border-collapse:collapse;
            font-size:0.82em;
        }}
        th {{
            background:#0f172a;
            padding:10px 12px;
            text-align:left;
            color:#60a5fa;
            font-size:0.8em;
            text-transform:uppercase;
        }}
        td {{ padding:9px 12px; border-bottom:1px solid #2d3f55; }}
        tr:hover td {{ background:#263248; }}
        .badge {{
            padding:2px 8px;
            border-radius:999px;
            font-size:0.78em;
            font-weight:700;
        }}
        .b-critical {{ background:#7f1d1d; color:#fca5a5; }}
        .b-high     {{ background:#7c2d12; color:#fdba74; }}
        .b-medium   {{ background:#713f12; color:#fde68a; }}
        .b-low      {{ background:#14532d; color:#86efac; }}
        .b-tp       {{ background:#14532d; color:#86efac; }}
        .b-fp       {{ background:#7f1d1d; color:#fca5a5; }}

        /* ── Footer ── */
        .footer {{
            text-align:center;
            color:#475569;
            font-size:0.78em;
            margin-top:20px;
            padding-top:15px;
            border-top:1px solid #2d3f55;
        }}
    </style>
</head>
<body>

<!-- Header -->
<div class='header'>
    <h1>🛡️ AI-Powered AML Detection Platform</h1>
    <p>Anti-Money Laundering Dashboard — {DateTime.Now:dd MMM yyyy HH:mm} &nbsp;|&nbsp; PaySim Dataset &nbsp;|&nbsp; Random Forest + FATF Rules</p>
</div>

<!-- KPI Row -->
<div class='kpi-row'>
    <div class='kpi'>
        <div class='val blue'>{report.TotalTransactions:N0}</div>
        <div class='lbl'>Transactions Analysed</div>
    </div>
    <div class='kpi'>
        <div class='val red'>{report.TotalFraud:N0}</div>
        <div class='lbl'>Fraud Cases</div>
    </div>
    <div class='kpi'>
        <div class='val yellow'>{report.TotalAlerts:N0}</div>
        <div class='lbl'>Alerts Raised</div>
    </div>
    <div class='kpi'>
        <div class='val green'>{report.TruePositives:N0}</div>
        <div class='lbl'>True Positives</div>
    </div>
    <div class='kpi'>
        <div class='val orange'>{report.FalsePositives:N0}</div>
        <div class='lbl'>False Positives</div>
    </div>
</div>

<!-- Row 1: 3 Charts -->
<div class='chart-row'>

    <!-- Donut: Alerts by Severity -->
    <div class='card'>
        <h3>Alerts by Severity</h3>
        <div class='chart-wrap'>
            <canvas id='severityChart'></canvas>
        </div>
    </div>

    <!-- Bar: Alerts by Transaction Type -->
    <div class='card'>
        <h3>Alerts by Transaction Type</h3>
        <div class='chart-wrap'>
            <canvas id='typeChart'></canvas>
        </div>
    </div>

    <!-- Donut: Detection Outcome -->
    <div class='card'>
        <h3>Detection Outcome</h3>
        <div class='chart-wrap'>
            <canvas id='outcomeChart'></canvas>
        </div>
    </div>

</div>

<!-- Row 2: Metrics + Confusion + Gauge -->
<div class='chart-row'>

    <!-- Performance Metrics Bars -->
    <div class='card'>
        <h3>Performance Metrics</h3>

        <div class='metric-item'>
            <div class='metric-header'>
                <span class='metric-name'>Accuracy</span>
                <span class='metric-val green'>{report.Accuracy:P1}</span>
            </div>
            <div class='bar-bg'>
                <div class='bar-fill'
                     style='width:{report.Accuracy*100:F1}%;
                            background:linear-gradient(90deg,#059669,#34d399)'>
                </div>
            </div>
        </div>

        <div class='metric-item'>
            <div class='metric-header'>
                <span class='metric-name'>Precision</span>
                <span class='metric-val blue'>{report.Precision:P1}</span>
            </div>
            <div class='bar-bg'>
                <div class='bar-fill'
                     style='width:{report.Precision*100:F1}%;
                            background:linear-gradient(90deg,#1d4ed8,#60a5fa)'>
                </div>
            </div>
        </div>

        <div class='metric-item'>
            <div class='metric-header'>
                <span class='metric-name'>Recall</span>
                <span class='metric-val yellow'>{report.Recall:P1}</span>
            </div>
            <div class='bar-bg'>
                <div class='bar-fill'
                     style='width:{report.Recall*100:F1}%;
                            background:linear-gradient(90deg,#b45309,#fbbf24)'>
                </div>
            </div>
        </div>

        <div class='metric-item'>
            <div class='metric-header'>
                <span class='metric-name'>F1 Score</span>
                <span class='metric-val orange'>{report.F1Score:P1}</span>
            </div>
            <div class='bar-bg'>
                <div class='bar-fill'
                     style='width:{report.F1Score*100:F1}%;
                            background:linear-gradient(90deg,#c2410c,#fb923c)'>
                </div>
            </div>
        </div>

        <div class='metric-item'>
            <div class='metric-header'>
                <span class='metric-name'>False Positive Rate</span>
                <span class='metric-val red'>{report.FalsePositiveRate:P1}</span>
            </div>
            <div class='bar-bg'>
                <div class='bar-fill'
                     style='width:{report.FalsePositiveRate*100:F1}%;
                            background:linear-gradient(90deg,#991b1b,#f87171)'>
                </div>
            </div>
        </div>

    </div>

    <!-- Confusion Matrix -->
    <div class='card'>
        <h3>Confusion Matrix</h3>
        <div class='confusion'>
            <div class='cf-cell tp'>
                <div class='n'>{report.TruePositives}</div>
                <div class='l'>True Positives<br/>Fraud caught ✓</div>
            </div>
            <div class='cf-cell fp'>
                <div class='n'>{report.FalsePositives}</div>
                <div class='l'>False Positives<br/>Legitimate flagged ✗</div>
            </div>
            <div class='cf-cell fn'>
                <div class='n'>{report.FalseNegatives}</div>
                <div class='l'>False Negatives<br/>Fraud missed ✗</div>
            </div>
            <div class='cf-cell tn'>
                <div class='n'>{report.TrueNegatives}</div>
                <div class='l'>True Negatives<br/>Legitimate cleared ✓</div>
            </div>
        </div>
    </div>

    <!-- Recall Gauge -->
    <div class='card'>
        <h3>Fraud Detection Rate</h3>
        <div class='chart-wrap'>
            <canvas id='gaugeChart'></canvas>
        </div>
    </div>

</div>

<!-- Alerts Table -->
<div class='card' style='margin-bottom:20px'>
    <h3>Top 20 Alerts</h3>
    <table>
        <thead>
            <tr>
                <th>#</th>
                <th>Type</th>
                <th>Amount</th>
                <th>Severity</th>
                <th>ML Score</th>
                <th>Rules Triggered</th>
                <th>Result</th>
            </tr>
        </thead>
        <tbody>
            {GenerateAlertRows(alerts)}
        </tbody>
    </table>
</div>

<div class='footer'>
    <p>AI-Powered AML Detection Platform &nbsp;|&nbsp; TheGirls Team &nbsp;|&nbsp;
       Built with C# .NET 7 &nbsp;|&nbsp; Random Forest Classifier + FATF Rule Engine</p>
</div>

<script>
// ── Chart defaults ────────────────────────────────────────────
Chart.defaults.color = '#94a3b8';
Chart.defaults.borderColor = '#2d3f55';

// ── Severity Donut ────────────────────────────────────────────
new Chart(document.getElementById('severityChart'), {{
    type: 'doughnut',
    data: {{
        labels: ['Critical','High','Medium','Low'],
        datasets: [{{
            data: [{critical},{high},{medium},{low}],
            backgroundColor: ['#ef4444','#f97316','#eab308','#22c55e'],
            borderWidth: 0,
            hoverOffset: 8
        }}]
    }},
    options: {{
        responsive: true,
        maintainAspectRatio: false,
        plugins: {{
            legend: {{ position:'bottom', labels:{{ padding:12, font:{{size:11}} }} }}
        }}
    }}
}});

// ── Type Bar Chart ────────────────────────────────────────────
new Chart(document.getElementById('typeChart'), {{
    type: 'bar',
    data: {{
        labels: {GetTypeLabels(typeCounts)},
        datasets: [{{
            label: 'Alerts',
            data: {GetTypeValues(typeCounts)},
            backgroundColor: ['#3b82f6','#f97316','#22c55e','#a855f7','#ef4444'],
            borderRadius: 6,
            borderWidth: 0
        }}]
    }},
    options: {{
        responsive: true,
        maintainAspectRatio: false,
        plugins: {{ legend: {{ display:false }} }},
        scales: {{
            y: {{ grid:{{ color:'#2d3f55' }}, ticks:{{ color:'#94a3b8' }} }},
            x: {{ grid:{{ display:false }}, ticks:{{ color:'#94a3b8' }} }}
        }}
    }}
}});

// ── Outcome Donut ─────────────────────────────────────────────
new Chart(document.getElementById('outcomeChart'), {{
    type: 'doughnut',
    data: {{
        labels: ['True Positives','False Positives','False Negatives','True Negatives'],
        datasets: [{{
            data: [{report.TruePositives},{report.FalsePositives},
                   {report.FalseNegatives},{report.TrueNegatives}],
            backgroundColor: ['#22c55e','#ef4444','#f97316','#3b82f6'],
            borderWidth: 0,
            hoverOffset: 8
        }}]
    }},
    options: {{
        responsive: true,
        maintainAspectRatio: false,
        plugins: {{
            legend: {{ position:'bottom', labels:{{ padding:10, font:{{size:10}} }} }}
        }}
    }}
}});

// ── Recall Gauge ──────────────────────────────────────────────
new Chart(document.getElementById('gaugeChart'), {{
    type: 'doughnut',
    data: {{
        datasets: [{{
            data: [{report.Recall*100:F1}, {(1-report.Recall)*100:F1}],
            backgroundColor: ['#22c55e','#1e293b'],
            borderWidth: 0,
            circumference: 180,
            rotation: 270
        }}]
    }},
    options: {{
        responsive: true,
        maintainAspectRatio: false,
        cutout: '75%',
        plugins: {{
            legend: {{ display:false }},
            tooltip: {{ enabled:false }}
        }}
    }},
    plugins: [{{
        id: 'gaugeText',
        afterDraw(chart) {{
            const {{ ctx, chartArea:{{left,right,top,bottom}} }} = chart;
            const cx = (left+right)/2, cy = (top+bottom)/2 + 30;
            ctx.save();
            ctx.textAlign = 'center';
            ctx.fillStyle = '#34d399';
            ctx.font = 'bold 28px Segoe UI';
            ctx.fillText('{report.Recall:P1}', cx, cy);
            ctx.fillStyle = '#94a3b8';
            ctx.font = '13px Segoe UI';
            ctx.fillText('Recall Rate', cx, cy+22);
            ctx.restore();
        }}
    }}]
}});
</script>
</body>
</html>";

            File.WriteAllText(outputPath, html);
            Console.WriteLine($"  HTML report saved: {outputPath}");
        }

        private string GetTypeLabels(Dictionary<string, int> counts)
        {
            var labels = new List<string>();
            foreach (var k in counts.Keys)
                labels.Add($"'{k}'");
            return "[" + string.Join(",", labels) + "]";
        }

        private string GetTypeValues(Dictionary<string, int> counts)
        {
            var vals = new List<string>();
            foreach (var v in counts.Values)
                vals.Add(v.ToString());
            return "[" + string.Join(",", vals) + "]";
        }

        private string GenerateAlertRows(List<Alert> alerts)
        {
            var rows = "";
            int count = 0;
            foreach (var a in alerts)
            {
                if (count >= 20) break;
                count++;
                string sev   = a.Severity.ToString().ToLower();
                string res   = a.IsTruePositive ? "tp" : "fp";
                string resT  = a.IsTruePositive ? "✓ Fraud" : "✗ False Alarm";
                string rules = a.TriggeredRules.Count > 0
                    ? string.Join(", ", a.TriggeredRules) : "ML Only";

                rows += $@"<tr>
                <td style='color:#475569'>{count}</td>
                <td>{a.Transaction.Type}</td>
                <td style='color:#fbbf24'>${a.Transaction.Amount:N0}</td>
                <td><span class='badge b-{sev}'>{a.Severity}</span></td>
                <td>{a.MlScore:P1}</td>
                <td style='font-size:0.8em;color:#94a3b8'>{rules}</td>
                <td><span class='badge b-{res}'>{resT}</span></td>
            </tr>";
            }
            return rows;
        }
    }
}