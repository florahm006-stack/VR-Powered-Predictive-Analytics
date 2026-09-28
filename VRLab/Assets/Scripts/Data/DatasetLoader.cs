using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace VRLab.Data
{
    /// <summary>
    /// Loads CSV files from StreamingAssets/Data into Dataset objects.
    /// Expected format: header row, optional "Date" column, then numeric columns.
    /// Guards against empty/malformed files and sanitizes NaN/Infinity values.
    /// </summary>
    public static class DatasetLoader
    {
        /// <param name="fileName">Relative to StreamingAssets, e.g. "Data/stocks.csv".</param>
        public static Dataset Load(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Dataset not found: {path}");

            return ParseText(fileName, File.ReadAllText(path));
        }

        /// <summary>Parses CSV text. Exposed for EditMode tests.</summary>
        public static Dataset ParseText(string datasetName, string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                throw new InvalidDataException($"Dataset '{datasetName}' is empty.");

            var dataset = new Dataset { Name = datasetName };
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                throw new InvalidDataException($"Dataset '{datasetName}' needs a header and at least one data row.");

            string[] headers = SplitLine(lines[0]);
            bool hasDate = headers.Length > 0 && string.Equals(headers[0], "Date", StringComparison.OrdinalIgnoreCase);
            int seriesCount = hasDate ? headers.Length - 1 : headers.Length;
            if (seriesCount <= 0)
                throw new InvalidDataException($"Dataset '{datasetName}' has no numeric columns.");

            for (int i = 0; i < seriesCount; i++)
                dataset.Series.Add(new DataSeries { Name = headers[hasDate ? i + 1 : i] });

            for (int row = 1; row < lines.Length; row++)
            {
                string[] cells = SplitLine(lines[row]);
                if (cells.Length != headers.Length)
                    continue; // skip malformed rows rather than crashing

                DateTime date = default;
                if (hasDate && !DateTime.TryParse(cells[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    continue;

                for (int i = 0; i < seriesCount; i++)
                {
                    string cell = cells[hasDate ? i + 1 : i];
                    float v = float.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                        ? DataSeries.Sanitize(parsed)
                        : 0f; // malformed numeric cell → 0 (column min recalculated after)
                    var s = dataset.Series[i];
                    s.Values.Add(v);
                    if (hasDate) s.Timestamps.Add(date);
                }
            }

            foreach (var s in dataset.Series)
                s.RecalculateBounds();

            if (dataset.RowCount == 0)
                throw new InvalidDataException($"Dataset '{datasetName}' parsed to zero valid rows.");

            return dataset;
        }

        /// <summary>CSV line splitter with basic quote handling.</summary>
        internal static string[] SplitLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"') inQuotes = !inQuotes;
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(line.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            result.Add(line.Substring(start).Trim());
            return result.ToArray();
        }
    }
}
