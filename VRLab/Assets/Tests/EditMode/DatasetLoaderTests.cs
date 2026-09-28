using NUnit.Framework;
using VRLab.Data;

namespace VRLab.Tests.EditMode
{
    public class DatasetLoaderTests
    {
        [Test]
        public void ParseText_ValidCsv_LoadsSeriesAndRows()
        {
            const string csv = "Date,Open,Close,Volume\n" +
                               "2025-01-01,100,105,900000\n" +
                               "2025-01-02,105,103,850000\n";
            var ds = DatasetLoader.ParseText("test", csv);

            Assert.AreEqual(3, ds.Series.Count);
            Assert.AreEqual(2, ds.RowCount);
            Assert.AreEqual(105f, ds.GetSeries("Close").Values[0]);
            Assert.AreEqual(2, ds.GetSeries("Close").Timestamps.Count);
        }

        [Test]
        public void ParseText_SkipsMalformedRows()
        {
            const string csv = "Date,Close\n2025-01-01,10\nBROKEN\n2025-01-02,12\n";
            var ds = DatasetLoader.ParseText("test", csv);
            Assert.AreEqual(2, ds.RowCount, "Short/malformed row must be skipped.");
        }

        [Test]
        public void ParseText_SanitizesNaN()
        {
            const string csv = "Date,Close\n2025-01-01,NaN\n2025-01-02,5\n";
            var ds = DatasetLoader.ParseText("test", csv);
            var close = ds.GetSeries("Close");
            Assert.IsFalse(float.IsNaN(close.Values[0]));
        }

        [Test]
        public void ParseText_EmptyFile_Throws()
        {
            Assert.Throws<System.IO.InvalidDataException>(() => DatasetLoader.ParseText("empty", "  "));
        }

        [Test]
        public void NormalizeBounds_AreCorrect()
        {
            const string csv = "Date,Close\n2025-01-01,10\n2025-01-02,30\n2025-01-03,20\n";
            var s = DatasetLoader.ParseText("test", csv).GetSeries("Close");
            Assert.AreEqual(10f, s.Min);
            Assert.AreEqual(30f, s.Max);
        }
    }
}
