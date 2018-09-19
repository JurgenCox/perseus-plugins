using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
using PerseusPluginLib.Basic;

namespace PerseusPluginLib.Test.Basic
{
	[TestFixture]
	public class SummaryStatisticsRowsTest
	{
		[Test]
		public void TestSummaryStatisticsCanHandleRowWithOnlyNaNs()
		{
			var summaryStatistics = new SummaryStatisticsRows();
			var mdata = PerseusFactory.CreateMatrixData(new double[,] {{double.NaN, double.NaN}, {double.NaN, double.NaN}});
			var errString = string.Empty;
			var parameters = summaryStatistics.GetParameters(mdata, ref errString);
			IMatrixData[] supplData = null;
			IDocumentData[] supplDocs = null;
			summaryStatistics.ProcessData(mdata, parameters, ref supplData, ref supplDocs, new ProcessInfo(new Settings(),
				s => { }, i => { }, 1));
			Assert.IsTrue(mdata.IsConsistent(out var consistent), consistent);
		}
	}
}
