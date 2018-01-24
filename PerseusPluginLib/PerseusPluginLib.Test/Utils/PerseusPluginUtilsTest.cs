using NUnit.Framework;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Test.Utils
{
    [TestFixture]
    public class PerseusPluginUtilsTest
    {
        [Test]
        public void TestBenjaminiHochbergFdrCorrectionWithoutValues()
        {
            PerseusPluginUtils.CalcBenjaminiHochbergFdr(new double[0], 0.05, out var fdrs);
            Assert.AreEqual(0, fdrs.Length);
        }
        [Test]
        public void TestBenjaminiHochbergFdrCorrectionWithSinglePvalue()
        {
            var pValues = new[]
            {
                0.55418364,
            };
            var expectedFdrs = new[]
            {
                0.55418364,
            };
            PerseusPluginUtils.CalcBenjaminiHochbergFdr(pValues, 0.05, out var fdrs);
            for (int i = 0; i < expectedFdrs.Length; i++)
            {
                Assert.AreEqual(expectedFdrs[i], fdrs[i], 0.00001);
            }
        }

        [Test]
        public void TestBenjaminiHochberFdrCorrectionKinactExample()
        {
            var pValues = new[]
            {
                0.5, 0.26996402, 0.17923912, 0.29354353, double.NaN
            };
            var expectedFdrs = new[]
            {
                0.5, 0.39139137, 0.39139137, 0.39139137, double.NaN
            };
            PerseusPluginUtils.CalcBenjaminiHochbergFdr(pValues, 0.05, out var fdrs);
            for (int i = 0; i < expectedFdrs.Length; i++)
            {
                Assert.AreEqual(expectedFdrs[i], fdrs[i], 0.00001);
            }
        }
        [Test]
        public void TestBenjaminiHochbergFdrCorrectionAgainstR()
        {
            var pValues = new[]
            {
                0.55418364, 0.33169014, 0.61117003, 0.79263279, 0.74714936,
                0.93567141, 0.41151512, 0.99690655, 0.57863046, 0.35048756,
                0.17302064, 0.58728787, 0.45285588, 0.67122903, 0.99010006,
                0.32346151, 0.02248119, 0.5575581, 0.54179022, 0.30518608
            };
            var expectedFdrs = new[]
            {
                0.87310004, 0.87310004, 0.87310004, 0.93250916, 0.93250916,
                0.99690655, 0.87310004, 0.99690655, 0.87310004, 0.87310004,
                0.87310004, 0.87310004, 0.87310004, 0.89497204, 0.99690655,
                0.87310004, 0.4496238, 0.87310004, 0.87310004, 0.87310004
            };
            PerseusPluginUtils.CalcBenjaminiHochbergFdr(pValues, 0.05, out var fdrs);
            for (int i = 0; i < expectedFdrs.Length; i++)
            {
                Assert.AreEqual(expectedFdrs[i], fdrs[i], 0.00001);
            }
        }
        [Test]
        public void TestBenjaminiHochbergFdrCorrectionAgainstRWithNaNs()
        {
            var pValues = new[]
            {
                double.NaN,
                0.55418364, 0.33169014, 0.61117003, 0.79263279, 0.74714936,
                0.93567141, 0.41151512, 0.99690655, 0.57863046, 0.35048756, double.NaN,
                0.17302064, 0.58728787, 0.45285588, 0.67122903, 0.99010006,
                0.32346151, 0.02248119, 0.5575581, 0.54179022, 0.30518608
            };
            var expectedFdrs = new[]
            {
                double.NaN,
                0.87310004, 0.87310004, 0.87310004, 0.93250916, 0.93250916,
                0.99690655, 0.87310004, 0.99690655, 0.87310004, 0.87310004, double.NaN,
                0.87310004, 0.87310004, 0.87310004, 0.89497204, 0.99690655,
                0.87310004, 0.4496238, 0.87310004, 0.87310004, 0.87310004
            };
            PerseusPluginUtils.CalcBenjaminiHochbergFdr(pValues, 0.05, out var fdrs);
            for (int i = 0; i < expectedFdrs.Length; i++)
            {
                var expected = expectedFdrs[i];
                var actual = fdrs[i];
                if (double.IsNaN(expected) && double.IsNaN(actual))
                {
                    continue;
                }
                Assert.AreEqual(expected, actual, 0.00001);
            }
        }
    }
}
