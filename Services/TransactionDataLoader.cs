using System;
using System.Collections.Generic;
using System.IO;
using AML.Models;

namespace AML.Services
{
    public class TransactionDataLoader
    {
        public List<Transaction> LoadFromCsv(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dataset not found: " + filePath);

            var result    = new List<Transaction>();
            bool firstLine = true;

            foreach (var line in File.ReadLines(filePath))
            {
                if (firstLine) { firstLine = false; continue; }
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(',');
                if (cols.Length < 11) continue;

                try
                {
                    result.Add(new Transaction
                    {
                        Step           = int.Parse(cols[0].Trim()),
                        Type           = cols[1].Trim(),
                        Amount         = double.Parse(cols[2].Trim()),
                        NameOrig       = cols[3].Trim(),
                        OldbalanceOrg  = double.Parse(cols[4].Trim()),
                        NewbalanceOrig = double.Parse(cols[5].Trim()),
                        NameDest       = cols[6].Trim(),
                        OldbalanceDest = double.Parse(cols[7].Trim()),
                        NewbalanceDest = double.Parse(cols[8].Trim()),
                        IsFraud        = int.Parse(cols[9].Trim()),
                        IsFlaggedFraud = int.Parse(cols[10].Trim())
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Skipping row: " + ex.Message);
                }
            }
            return result;
        }
    }
}