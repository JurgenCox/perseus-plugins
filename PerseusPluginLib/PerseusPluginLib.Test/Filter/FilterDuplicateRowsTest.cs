using NUnit.Framework;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Filter;

namespace PerseusPluginLib.Test.Filter
{
	[TestFixture]
	public class FilterDuplicateRowsTest
    {
        private IMatrixData _mdata;
        private readonly FilterDuplicateRows _filter = new FilterDuplicateRows();

        [SetUp]
        public void Setup()
        {
            _mdata = PerseusFactory.CreateMatrixData(new[,] {{0.0, 1.0, 0.0}, {0.0, 0.0, 0.0}, {0.0, 1.0, 0.0}});
            _mdata.AddStringColumn("test", "", new []{"a", "b", "a"});
        }
        [Test]
        public void TestFilterOnSingleMainColumn()
        {
            var errString = string.Empty;
            var parameters = _filter.GetParameters(_mdata, ref errString);
            parameters.GetParam<int[]>("Main").Value = new[] {0};
            parameters.GetParam<int[]>("Text").Value = new int[0];
            Assert.IsTrue(string.IsNullOrEmpty(errString));
            _filter.ProcessData(_mdata, parameters);
            Assert.AreEqual(1, _mdata.RowCount);
        }
        [Test]
        public void TestFilterOnMainAndStringColumn()
        {
            var errString = string.Empty;
            var parameters = _filter.GetParameters(_mdata, ref errString);
            parameters.GetParam<int[]>("Main").Value = new[] {0};
            parameters.GetParam<int[]>("Text").Value = new[] {0};
            Assert.IsTrue(string.IsNullOrEmpty(errString));
            _filter.ProcessData(_mdata, parameters);
            Assert.AreEqual(2, _mdata.RowCount);
        }
	}
}