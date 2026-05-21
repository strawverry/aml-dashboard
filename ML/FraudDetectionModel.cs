using System;
using System.Collections.Generic;
using System.Linq;
using AML.Models;

namespace AML.ML
{
    public class FraudDetectionModel
    {
        private List<DecisionTree> _forest = new List<DecisionTree>();
        private int _numTrees = 10;
        private int _maxDepth = 5;

        public bool IsTrained { get; private set; }

        public static double[] ExtractFeatures(Transaction tx)
        {
            return new double[]
            {
                tx.Amount,
                tx.OldbalanceOrg,
                tx.NewbalanceOrig,
                tx.OldbalanceDest,
                tx.NewbalanceDest,
                tx.BalanceDiffOrig,
                tx.BalanceDiffDest,
                tx.OrigAccountDrained   ? 1.0 : 0.0,
                tx.DestAccountUnchanged ? 1.0 : 0.0,
                tx.Type == "TRANSFER"   ? 1.0 : 0.0,
                tx.Type == "CASH_OUT"   ? 1.0 : 0.0,
                tx.Type == "PAYMENT"    ? 1.0 : 0.0,
                tx.Step
            };
        }

        public TrainingResult Train(IList<Transaction> transactions)
        {
            if (transactions.Count == 0)
                throw new InvalidOperationException("No training data.");

            var rng = new Random(42);
            _forest.Clear();

            for (int t = 0; t < _numTrees; t++)
            {
                var sample = BootstrapSample(transactions, rng);
                var tree   = new DecisionTree(_maxDepth);
                tree.Train(sample);
                _forest.Add(tree);
            }

            IsTrained = true;

            int tp = 0, fp = 0, fn = 0, tn = 0;
            foreach (var tx in transactions)
            {
                bool predicted = Predict(tx) >= 0.5;
                bool actual    = tx.IsFraud == 1;
                if ( predicted &&  actual) tp++;
                if ( predicted && !actual) fp++;
                if (!predicted &&  actual) fn++;
                if (!predicted && !actual) tn++;
            }

            double precision = tp + fp == 0 ? 0 : (double)tp / (tp + fp);
            double recall    = tp + fn == 0 ? 0 : (double)tp / (tp + fn);
            double f1        = precision + recall == 0 ? 0
                               : 2 * precision * recall / (precision + recall);
            double accuracy  = (double)(tp + tn) / transactions.Count;

            return new TrainingResult
            {
                Accuracy  = accuracy,
                Precision = precision,
                Recall    = recall,
                F1Score   = f1
            };
        }

        public double Predict(Transaction tx)
        {
            if (!IsTrained)
                throw new InvalidOperationException("Model not trained yet.");

            double votes = 0;
            foreach (var tree in _forest)
                votes += tree.Predict(ExtractFeatures(tx));
            return votes / _forest.Count;
        }

        private List<Transaction> BootstrapSample(
            IList<Transaction> data, Random rng)
        {
            var sample = new List<Transaction>();
            for (int i = 0; i < data.Count; i++)
                sample.Add(data[rng.Next(data.Count)]);
            return sample;
        }
    }

    internal class DecisionTree
    {
        private TreeNode? _root;
        private readonly int _maxDepth;
        private readonly Random _rng = new Random();

        public DecisionTree(int maxDepth)
        {
            _maxDepth = maxDepth;
        }

        public void Train(List<Transaction> data)
        {
            var features = data.Select(
                t => FraudDetectionModel.ExtractFeatures(t)).ToList();
            var labels = data.Select(t => t.IsFraud == 1).ToList();
            _root = BuildNode(features, labels, 0);
        }

        public double Predict(double[] features)
        {
            return _root?.Predict(features) ?? 0.5;
        }

        private TreeNode BuildNode(
            List<double[]> features, List<bool> labels, int depth)
        {
            if (depth >= _maxDepth || labels.Count < 5
                || labels.All(l => l == labels[0]))
            {
                double leafVal = labels.Count == 0 ? 0
                    : (double)labels.Count(l => l) / labels.Count;
                return new TreeNode { LeafValue = leafVal };
            }

            int numFeatures = features[0].Length;
            int sqrtF = (int)Math.Sqrt(numFeatures);
            var featureIdx = Enumerable.Range(0, numFeatures)
                .OrderBy(_ => _rng.Next()).Take(sqrtF).ToList();

            double bestGini      = double.MaxValue;
            int    bestFeat      = 0;
            double bestThreshold = 0;

            foreach (int fi in featureIdx)
            {
                var values = features.Select(f => f[fi])
                    .Distinct().OrderBy(v => v).ToList();

                for (int i = 0; i < values.Count - 1; i++)
                {
                    double threshold = (values[i] + values[i + 1]) / 2.0;
                    double gini = GiniSplit(features, labels, fi, threshold);

                    if (gini < bestGini)
                    {
                        bestGini      = gini;
                        bestFeat      = fi;
                        bestThreshold = threshold;
                    }
                }
            }

            var leftF  = new List<double[]>();
            var leftL  = new List<bool>();
            var rightF = new List<double[]>();
            var rightL = new List<bool>();

            for (int i = 0; i < features.Count; i++)
            {
                if (features[i][bestFeat] <= bestThreshold)
                {
                    leftF.Add(features[i]);
                    leftL.Add(labels[i]);
                }
                else
                {
                    rightF.Add(features[i]);
                    rightL.Add(labels[i]);
                }
            }

            if (leftF.Count == 0 || rightF.Count == 0)
            {
                double leafVal = (double)labels.Count(l => l) / labels.Count;
                return new TreeNode { LeafValue = leafVal };
            }

            return new TreeNode
            {
                FeatureIndex = bestFeat,
                Threshold    = bestThreshold,
                Left         = BuildNode(leftF, leftL, depth + 1),
                Right        = BuildNode(rightF, rightL, depth + 1)
            };
        }

        private double GiniSplit(
            List<double[]> features, List<bool> labels,
            int featureIdx, double threshold)
        {
            var leftL  = new List<bool>();
            var rightL = new List<bool>();

            for (int i = 0; i < features.Count; i++)
            {
                if (features[i][featureIdx] <= threshold)
                    leftL.Add(labels[i]);
                else
                    rightL.Add(labels[i]);
            }

            double total = features.Count;
            return (leftL.Count  / total) * Gini(leftL)
                 + (rightL.Count / total) * Gini(rightL);
        }

        private double Gini(List<bool> labels)
        {
            if (labels.Count == 0) return 0;
            double p = (double)labels.Count(l => l) / labels.Count;
            return 1 - (p * p) - ((1 - p) * (1 - p));
        }
    }

    internal class TreeNode
    {
        public int       FeatureIndex { get; set; }
        public double    Threshold    { get; set; }
        public TreeNode? Left         { get; set; }
        public TreeNode? Right        { get; set; }
        public double?   LeafValue    { get; set; }

        public bool IsLeaf => LeafValue.HasValue;

        public double Predict(double[] features)
        {
            if (IsLeaf) return LeafValue!.Value;
            return features[FeatureIndex] <= Threshold
                ? Left!.Predict(features)
                : Right!.Predict(features);
        }
    }

    public class TrainingResult
    {
        public double Accuracy  { get; set; }
        public double Precision { get; set; }
        public double Recall    { get; set; }
        public double F1Score   { get; set; }

        public override string ToString() =>
            string.Format(
                "Accuracy={0:P2}  Precision={1:F4}  Recall={2:F4}  F1={3:F4}",
                Accuracy, Precision, Recall, F1Score);
    }
}