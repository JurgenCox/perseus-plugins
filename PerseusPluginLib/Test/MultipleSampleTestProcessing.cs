using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Test;
using BaseLibS.Num.Test.Univariate;
using BaseLibS.Num.Test.Univariate.NSample;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Test{
	public class MultipleSampleTestProcessing : IMatrixProcessing{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("pn.png");
		public string Name => "Multiple-sample tests";
		public string Heading => "Tests";
		public bool IsActive => true;
		public float DisplayRank => 2;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url =>
			"http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Tests:MultipleSampleTestProcessing";

		public string Description =>
			"Multi-sample test for determining if any of the means of several groups are significantly different from each other.";

		public string HelpOutput =>
			"A numerical columns is added containing the p value. In addition there is a categorical column added in which it is " +
			"indicated by a '+' when the row is significant with respect to the specified criteria.";

		public int GetMaxThreads(Parameters parameters){
			return int.MaxValue;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			if (mdata.CategoryRowCount == 0){
				errorString = "No category row is loaded.";
				return null;
			}
			return new Parameters(new SingleChoiceParam("Grouping"){Values = mdata.CategoryRowNames}, GetTestParam(),
				TwoSampleTestUtil.GetTruncationParam(mdata, false, true),
				new BoolParam("-Log10", true){
					Help =
						"Indicate here whether the p value or -Log10 of the p value should be reported in the output matrix."
				},
				new StringParam("Suffix"){
					Help =
						"This suffix will be attached to newly generated columns. That way columns from multiple runs of the test can be " +
						"distinguished more easily."
				}, new BoolParam("Write residuals"));
		}

		// MultipleSampleTest test;
		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			bool residuals = param.GetParam<bool>("Write residuals").Value;
			ParameterWithSubParams<int> testParam = param.GetParamWithSubParams<int>("Test");
			int testInd = testParam.Value;
			MultipleSampleTest test;
			switch (testInd){
				case 0:
					test = new OneWayAnovaTest();
					break;
				case 1:
					test = new KruskalWallisTest();
					break;
				default:
					throw new Exception("Never get here.");
			}
			int groupColInd = param.GetParam<int>("Grouping").Value;
			string[][] groupCol = mdata.GetCategoryRowAt(groupColInd);
			string[] groupNames = ArrayUtils.UniqueValuesPreserveOrder(ArrayUtils.Concat(groupCol));
			int[][] colInds = PerseusPluginUtils.GetMainColIndices(groupCol, groupNames);
			ParameterWithSubParams<int> truncParam = param.GetParamWithSubParams<int>("Use for truncation");
			int truncIndex = truncParam.Value;
			TestTruncation truncation = truncIndex == 0
				? TestTruncation.Pvalue
				: (truncIndex == 1 ? TestTruncation.BenjaminiHochberg : TestTruncation.PermutationBased);
			Parameters truncSubParams = truncParam.GetSubParameters();
			bool qval = false;
			if (truncation != TestTruncation.Pvalue){
				qval = truncSubParams.GetParam<bool>("Report q-value").Value;
			}
			double threshold = truncation == TestTruncation.Pvalue
				? truncParam.GetSubParameters().GetParam<double>("Threshold p-value").Value
				: truncParam.GetSubParameters().GetParam<double>("FDR").Value;
			int nrand = -1;
			int[][][] colIndsPreserveX = null;
			if (truncation == TestTruncation.PermutationBased){
				Parameters q = truncParam.GetSubParameters();
				nrand = q.GetParam<int>("Number of randomizations").Value;
				int preserveGroupInd = q.GetParam<int>("Preserve grouping in randomizations").Value - 1;
				if (preserveGroupInd >= 0){
					string[][] preserveGroupCol = mdata.GetCategoryRowAt(preserveGroupInd);
					string[] allGroupsPreserve =
						ArrayUtils.UniqueValuesPreserveOrder(ArrayUtils.Concat(preserveGroupCol));
					int[][] colIndsPreserve = PerseusPluginUtils.GetMainColIndices(preserveGroupCol, allGroupsPreserve);
					int[] allInds = ArrayUtils.Concat(colIndsPreserve);
					int[] allIndsUnique = ArrayUtils.UniqueValues(allInds);
					if (allInds.Length != allIndsUnique.Length){
						processInfo.ErrString = "The grouping for randomizations is not unique";
						return;
					}
					if (allInds.Length != ArrayUtils.Concat(colInds).Length){
						processInfo.ErrString =
							"The grouping for randomizations is not valid because it does not cover all samples.";
						return;
					}
					List<int[]>[] colIndsPreserveX0 = new List<int[]>[groupNames.Length];
					colIndsPreserveX = new int[groupNames.Length][][];
					for (int i = 0; i < colIndsPreserveX0.Length; i++){
						colIndsPreserveX0[i] = new List<int[]>();
					}
					foreach (int[] inds in colIndsPreserve){
						int index = DetermineGroup(colInds, inds);
						if (index < 0){
							processInfo.ErrString =
								"The grouping for randomizations is not hierarchical with respect to the main grouping.";
							return;
						}
						colIndsPreserveX0[index].Add(inds);
					}
					for (int i = 0; i < groupNames.Length; i++){
						colIndsPreserveX[i] = colIndsPreserveX0[i].ToArray();
					}
				}
			}
			double s0;
			if (true){
				s0 = testParam.GetSubParameters().GetParam<double>("S0").Value;
			}
			bool log = param.GetParam<bool>("-Log10").Value;
			Testing(test, colInds, truncation, threshold, mdata, s0, log, colIndsPreserveX, nrand,
				processInfo.NumThreads, qval, residuals);
		}

		private static int DetermineGroup(IList<int[]> colInds, IEnumerable<int> inds){
			for (int i = 0; i < colInds.Count; i++){
				if (CompletelyContained(colInds[i], inds)){
					return i;
				}
			}
			return -1;
		}

		private static bool CompletelyContained(int[] colInds1, IEnumerable<int> inds){
			foreach (int ind in inds){
				if (Array.BinarySearch(colInds1, ind) < 0){
					return false;
				}
			}
			return true;
		}

		private static SingleChoiceWithSubParams GetTestParam(){
			Parameters[] subParams = new Parameters[MultipleSampleTests.allNames.Length];
			for (int i = 0; i < subParams.Length; i++){
				subParams[i] = GetTestSubParams(MultipleSampleTests.allTests[i]);
			}
			return new SingleChoiceWithSubParams("Test"){
				Values = MultipleSampleTests.allNames, Help = "Select here the kind of test.", SubParams = subParams
			};
		}

		internal static Parameters GetTestSubParams(UnivariateTest test){
			List<Parameter> p = new List<Parameter>();
			if (test.HasS0){
				p.Add(new DoubleParam("S0", 0){
					Help =
						"Artificial within groups variance. It controls the relative importance of t-test p value and difference between " +
						"means. At s0=0 only the p-value matters, while at nonzero s0 also the difference of means plays a role. See " +
						"Tusher, Tibshirani and Chu (2001) PNAS 98, pp5116-21 for details."
				});
			}
			if (test.HasSides){
				p.Add(new SingleChoiceParam("Side"){
					Values = new[]{"both", "right", "left"},
					Help =
						"'Both' stands for the two-sided test in which the the null hypothesis can be rejected regardless of the direction" +
						" of the effect. 'Left' and 'right' are the respective one sided tests."
				});
			}
			return new Parameters(p);
		}

		public void Testing(MultipleSampleTest test, int[][] colInds, TestTruncation truncation, double threshold,
			IMatrixData data, double s0, bool log, int[][][] colIndsPreserve, int nrand, int nthreads, bool qval,
			bool residuals){
			double[] pvals = new double[data.RowCount];
			double[] pvalsS0 = new double[data.RowCount];
			double[,] res = null;
			if (residuals){
				res = new double[data.RowCount, data.ColumnCount];
			}
			for (int i = 0; i < data.RowCount; i++){
				double[][] vals = new double[colInds.Length][];
				for (int j = 0; j < vals.Length; j++){
					vals[j] = GetValues(i, colInds[j], data);
				}
				pvals[i] = test.Test(vals, out double _, s0, out pvalsS0[i], out double[] gmeans);
				if (residuals){
					for (int j = 0; j < vals.Length; j++){
						for (int k = 0; k < vals[j].Length; k++){
							res[i, colInds[j][k]] = (float) (vals[j][k] - gmeans[j]);
						}
					}
				}
			}
			string[][] significant;
			double[] fdrs = null;
			switch (truncation){
				case TestTruncation.Pvalue:
					significant = CalcPvalueSignificance(pvals, threshold);
					break;
				case TestTruncation.BenjaminiHochberg:
					significant = PerseusPluginUtils.CalcBenjaminiHochbergFdr(pvals, threshold, out fdrs);
					break;
				case TestTruncation.PermutationBased:
					significant = CalcPermutationBasedFdr(pvalsS0, nrand, data, test, colInds, s0, threshold,
						colIndsPreserve, nthreads, out fdrs);
					break;
				default:
					throw new Exception("Never get here.");
			}
			string x = test.Name + " p value";
			if (log){
				x = "-Log " + x;
				for (int i = 0; i < pvals.Length; i++){
					pvals[i] = -Math.Log10(pvals[i]);
				}
			}
			data.AddNumericColumn(x, "", pvals);
			if (qval){
				data.AddNumericColumn(test.Name + " q-value", "", fdrs);
			}
			data.AddCategoryColumn(test.Name + " Significant", "", significant);
			if (residuals){
				data.Values.Set(res);
			}
		}

		private static string[][] CalcPvalueSignificance(IList<double> pvals, double threshold){
			string[][] result = new string[pvals.Count][];
			for (int i = 0; i < result.Length; i++){
				result[i] = pvals[i] <= threshold ? new[]{"+"} : new string[0];
			}
			return result;
		}

		private static void Calc1(int p, IList<List<double>> pq1, IMatrixData data, MultipleSampleTest test,
			int[][] colInds, double s0, int[][][] colIndsPreserve){
			Random2 r2 = new Random2(p);
			pq1[p] = new List<double>();
			int[][] colIndP;
			if (colIndsPreserve != null){
				PermBasedFdrUtil.BalancedPermutationsSubgroups(colIndsPreserve, out colIndP, r2);
			} else{
				PermBasedFdrUtil.BalancedPermutations(colInds, out colIndP, r2);
			}
			for (int i = 0; i < data.RowCount; i++){
				double[][] vals = new double[colIndP.Length][];
				for (int j = 0; j < vals.Length; j++){
					vals[j] = GetValues(i, colIndP[j], data);
				}
				test.Test(vals, out double _, s0, out double p1);
				pq1[p].Add(p1);
			}
		}

		private static string[][] CalcPermutationBasedFdr(IList<double> pvalsS0, int nperm, IMatrixData data,
			MultipleSampleTest test, int[][] colInds, double s0, double threshold, int[][][] colIndsPreserve,
			int nthreads, out double[] fdrs){
			List<double>[] pq1 = new List<double>[nperm];
			new ThreadDistributor(nthreads, nperm, p => Calc1(p, pq1, data, test, colInds, s0, colIndsPreserve))
				.Start();
			List<double> pq = new List<double>();
			for (int p = 0; p < nperm; p++){
				pq.AddRange(pq1[p]);
			}
			List<int> indices = new List<int>();
			for (int i = 0; i < pq.Count; i++){
				indices.Add(-1);
			}
			for (int i = 0; i < pvalsS0.Count; i++){
				pq.Add(pvalsS0[i]);
				indices.Add(i);
			}
			double[] pv = pq.ToArray();
			int[] inds = indices.ToArray();
			int[] o = pv.Order();
			double forw = 0;
			double rev = 0;
			int lastind = -1;
			fdrs = new double[pvalsS0.Count];
			foreach (int ind in o){
				if (inds[ind] == -1){
					rev++;
				} else{
					forw++;
					double fdr = Math.Min(1, rev / forw / nperm);
					fdrs[inds[ind]] = fdr;
					if (fdr <= threshold){
						lastind = (int) Math.Round(forw - 1);
					}
				}
			}
			string[][] result = new string[pvalsS0.Count][];
			for (int i = 0; i < result.Length; i++){
				result[i] = new string[0];
			}
			int[] o1 = pvalsS0.Order();
			for (int i = 0; i <= lastind; i++){
				result[o1[i]] = new[]{"+"};
			}
			return result;
		}

		private static double[] GetValues(int row, IEnumerable<int> cols, IMatrixData data){
			List<double> result = new List<double>();
			foreach (int col in cols){
				double val = data.Values.Get(row, col);
				if (!double.IsNaN(val) && !double.IsInfinity(val)){
					result.Add(val);
				}
			}
			return result.ToArray();
		}
	}
}