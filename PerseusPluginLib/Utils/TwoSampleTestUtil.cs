using System;
using System.Collections.Generic;
using BaseLibS.Num;
using BaseLibS.Num.Test;
using BaseLibS.Num.Test.Univariate;
using BaseLibS.Num.Test.Univariate.OneSample;
using BaseLibS.Num.Test.Univariate.TwoSample;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Utils {
	public static class TwoSampleTestUtil {
		public const int totWidth = 800;

		public static Parameters GetParametersImpl(IMatrixData mdata, ref string errorString, bool isVolcano,
			TwoSampleTest[] allTests, string[] allNames) {
			if (mdata.CategoryRowCount == 0) {
				errorString = "No category row is loaded.";
				return null;
			}
			return new Parameters(GetGroupingParam(mdata), GetTestParam(allTests, allNames), GetFilterValidValuesParam(),
				GetTruncationParam(mdata, isVolcano, true), GetCombinedScoreParam(),
				new BoolParam("-Log10 p-value") {
					Value = true,
					Help = "Indicate here whether the p value or -Log10 of the p-value should be reported in the output matrix."
				},
				new StringParam("Suffix") {
					Help = "This suffix will be attached to newly generated columns. That way columns from multiple runs of the " +
					       "test can be distinguished more easily."
				});
		}

		private static SingleChoiceWithSubParams GetGroupingParam(IDataWithAnnotationRows mdata) {
			return new SingleChoiceWithSubParams("Grouping") {
				Values = mdata.CategoryRowNames,
				SubParams = GetFirstGroupParameters(mdata),
				Help = "The grouping(s) of columns to be used in the test. Each test takes two groups as input. " +
				       "Multiple tests can be performed simultaneously by specifying more than one pair of groups.",
				ParamNameWidth = 120,
				TotalWidth = totWidth
			};
		}

		private static BoolWithSubParams GetCombinedScoreParam() {
			return new BoolWithSubParams("Calculate combined score") {
				SubParamsTrue =
					new Parameters(
						new SingleChoiceParam("Mode") {
							Values = new[] {"Product", "Best"},
							Help = "Here the user can define the combined score which is either the p-value from " +
							       "the best test or the product over all tests."
						},
						new BoolParam("Combined q-value", true) {
							Help = "In case this is checked, a combined " +
							       "q-value based on the combined score and permutations of the whole matrix is calculated."
						}),
				Help = "In case multiple two sample tests are performed, the combined score helps to define a global set of " +
				       "significant items over all the tests combined. A global q-value can be calculated based " +
				       "on permutations of the whole matrix.",
				ParamNameWidth = 195,
				TotalWidth = totWidth
			};
		}

		private static Parameters[] GetFirstGroupParameters(IDataWithAnnotationRows mdata) {
			Parameters[] q = new Parameters[mdata.CategoryRowCount];
			for (int i = 0; i < q.Length; i++) {
				string[] vals = ArrayUtils.UniqueValuesPreserveOrder(ArrayUtils.Concat(mdata.GetCategoryRowAt(i)));
				q[i] = new Parameters(
					new MultiChoiceParam("First group (right)") {
						Values = vals,
						Value = new[] {0},
						Repeats = true,
						Help = "All 'right' groups of the two sample tests are defined here. The number " +
						       "of groups selected here equals the number of different tests performed."
					}, GetSecondGroupParameter(vals));
			}
			return q;
		}

		private static Parameter GetSecondGroupParameter(IList<string> vals) {
			return new SingleChoiceWithSubParams("Second groups mode") {
				Values = new[] {"Specify individual groups", "Single control group", "Complement"},
				SubParams =
					new[] {
						new Parameters(
							new MultiChoiceParam("Second group (left)") {
								Values = vals,
								Value = new[] {vals.Count > 1 ? 1 : 0},
								Repeats = true,
								Help = "The 'left' groups for the two sample tests. The number of selected items must match the number of " +
								       "selected groups in the 'First group (right)' input field."
							}, new BoolParam("Paired")),
						new Parameters(
							new SingleChoiceParam("Second group (left)") {
								Values = vals,
								Value = vals.Count > 1 ? 1 : 0,
								Help = "This group is taken as the 'left' group in all tests."
							}, new BoolParam("Paired")),
						new Parameters()
					},
				Help = "Specify here how the 'left' groups of the two sample tests are specified. Possible ways are to " +
				       "specify for each individual 'right' group the corresponding 'left' group, to use one single control group, " +
				       "or to always use the complement of each individual 'right' group as the 'left' group.",
				ParamNameWidth = 120,
				TotalWidth = 675
			};
		}

		private static BoolWithSubParams GetFilterValidValuesParam() {
			return new BoolWithSubParams("Valid value filter", true) {
				SubParamsTrue =
					new Parameters(
						new IntParam("Min. number of valid values", 1) {
							Help = "Here the required number of valid values is specified." +
							       "How this threshold is applied (in total, per group, etc.) is specified in the next field."
						},
						new SingleChoiceParam("Min. number mode", 0) {
							Values = new[]
								{"In total", "In both groups", "In at least one group", "In the first group", "In the second group"},
							Help = "Specify here how the above threshold is applied."
						},
						new IntParam("Min. percentage of valid values", 0) {
							Help = "Here the required percentage of valid values is specified." +
							       "How this threshold is applied (in total, per group, etc) is " +
							       "specified in the next field. Values can range from 0 to 100."
						},
						new SingleChoiceParam("Min. percentage mode", 0) {
							Values = new[]
								{"In total", "In both groups", "In at least one group", "In the first group", "In the second group"},
							Help = "Specify here how the above threshold is applied."
						}),
				Help = "Specify here how rows are filtered regarding the number and percentage of valid values. " +
				       "This criterion will be applied to each test individually, not just once to the whole matrix." +
				       "The absolute number and relative percentage filters are both applied together.",
				ParamNameWidth = 195,
				TotalWidth = totWidth,
			};
		}

		private static SingleChoiceWithSubParams GetTestParam(TwoSampleTest[] allTests, string[] allNames) {
			Parameters[] subParams = new Parameters[allNames.Length];
			for (int i = 0; i < subParams.Length; i++) {
				subParams[i] = GetTestSubParams(allTests[i]);
			}
			return new SingleChoiceWithSubParams("Test", 0) {
				Values = allNames,
				Help = "Select here the kind of test.",
				SubParams = subParams,
				ParamNameWidth = 195,
				TotalWidth = totWidth,
			};
		}

		private static Parameters GetTestSubParams(UnivariateTest test) {
			List<Parameter> p = new List<Parameter>();
			if (test.HasS0) {
				p.Add(new DoubleParam("S0", 0) {
					Help =
						"Artificial within groups variance. It controls the relative importance of t-test p value and difference between " +
						"means. At s0=0 only the p-value matters, while at nonzero s0 also the difference of means plays a role. See " +
						"Tusher, Tibshirani and Chu (2001) PNAS 98, pp5116-21 for details."
				});
			}
			if (test.HasSides) {
				p.Add(new SingleChoiceParam("Side") {
					Values = new[] {"Both", "Right", "Left"},
					Help =
						"'Both' stands for the two-sided test in which the the null hypothesis can be rejected regardless of the direction" +
						" of the effect. 'Left' and 'Right' are the respective one sided tests."
				});
			}
			return new Parameters(p);
		}

		public static Parameter GetTruncationParam(IMatrixData mdata, bool isVolcano, bool hasPreserveGrouping) {
			Parameters p0 = new Parameters(new Parameter[] {
				new DoubleParam("Threshold p-value", 0.05) {
					Help =
						"Rows with a test result below this value are reported as significant. Depending on the choice made above this " +
						"threshold value is applied to the p value or to the Benjamini Hochberg or permutation-based FDR."
				}
			});
			Parameters p1 =
				new Parameters(
					new DoubleParam("FDR", 0.05) {
						Help =
							"Rows with a test result below this value are reported as significant. Depending on the choice made above this " +
							"threshold value is applied to the p value or to the Benjamini Hochberg or permutation-based FDR."
					},
					new BoolParam("Report q-value", true) {
						Help = "If true, for each test a column will be reported containing q-values. " +
						       "Filtering the table on these q-values is equivalent to applying the correponding FDR threshhold."
					});
			List<Parameter> p2 = new List<Parameter>(new Parameter[] {
				new DoubleParam("FDR", 0.05) {
					Help =
						"Rows with a test result below this value are reported as significant. Depending on the choice made above this " +
						"threshold value is applied to the p value or to the Benjamini Hochberg or permutation-based FDR."
				},
				new BoolParam("Report q-value", true) {
					Help = "If true, for each test a column will be reported containing q-values. " +
					       "Filtering the table on these q-values is equivalent to applying the correponding FDR threshhold."
				},
				new IntParam("Number of randomizations", 250) {
					Help = "This is the number of randomizations used to generate a null distribution " +
					       "for calculating the permutation based FDR."
				}
			});
			if (hasPreserveGrouping) {
				p2.Add(new SingleChoiceParam("Preserve grouping in randomizations") {
					Values = ArrayUtils.Concat(new[] {"<None>"}, mdata.CategoryRowNames),
					Help = "Specify here if a subgroup structure should be preserved during randomizations."
				});
			}
			if (isVolcano) {
				return new SingleChoiceWithSubParams("Use for truncation") {
					Value = 0,
					Values = new[] {"Permutation-based FDR"},
					SubParams = new[] {new Parameters(p2)},
					Help = "The truncation is based on a permutation-based FDR.",
					ParamNameWidth = 195,
					TotalWidth = totWidth
				};
			}
			return new SingleChoiceWithSubParams("Use for truncation") {
				Value = 2,
				Values = new[] {"p-value", "Benjamini-Hochberg FDR", "Permutation-based FDR"},
				SubParams = new[] {p0, p1, new Parameters(p2)},
				Help =
					"Choose here whether the truncation should be based on the p values, if the Benjamini Hochberg correction for " +
					"multiple hypothesis testing should be applied, or if a permutation-based FDR is calculated.",
				ParamNameWidth = 195,
				TotalWidth = totWidth
			};
		}

		public static void ReadParams(bool isVolcano, IMatrixData mdata, Parameters param, ProcessInfo processInfo,
			out int groupRowInd, out int[] firstGroupInds, out int[] secondGroupInds, out SecondGroupMode secondGroupMode,
			out string[] groupNames, out bool logPval, out double threshold, out TestTruncation truncation,
			out int preserveGroupInd, out int nrand, out TwoSampleTest test, out TestSide side, out double s0,
			out bool filterValidValues, out int minNumValidValues, out int minNumValidValuesMode, out int minPercValidValues,
			out int minPercValidValuesMode, out bool calcCombinedScore, out CombinedScoreMode combinedScoreMode,
			out bool combinedScoreQvalue, out bool qval, out string suffix, out bool paired, out OneSampleTest test1,
			TwoSampleTest[] allTests) {
			ParameterWithSubParams<int> groupingParam = param.GetParamWithSubParams<int>("Grouping");
			groupRowInd = groupingParam.Value;
			Parameters subparam = groupingParam.GetSubParameters();
			firstGroupInds = subparam.GetParam<int[]>("First group (right)").Value;
			ParameterWithSubParams<int> p = subparam.GetParamWithSubParams<int>("Second groups mode");
			int modeInd = p.Value;
			paired = false;
			switch (modeInd) {
				case 0:
					secondGroupMode = SecondGroupMode.SpecifiyAll;
					secondGroupInds = p.GetSubParameters().GetParam<int[]>("Second group (left)").Value;
					paired = p.GetSubParameters().GetParam<bool>("Paired").Value;
					break;
				case 1:
					secondGroupMode = SecondGroupMode.SingleControl;
					int ind = p.GetSubParameters().GetParam<int>("Second group (left)").Value;
					secondGroupInds = new int[firstGroupInds.Length];
					for (int i = 0; i < secondGroupInds.Length; i++) {
						secondGroupInds[i] = ind;
					}
					paired = p.GetSubParameters().GetParam<bool>("Paired").Value;
					break;
				case 2:
					secondGroupMode = SecondGroupMode.Complement;
					secondGroupInds = new int[0];
					break;
				default: throw new Exception("Never get here.");
			}
			groupNames = null;
			logPval = false;
			suffix = "";
			threshold = 0;
			truncation = TestTruncation.PermutationBased;
			preserveGroupInd = 0;
			nrand = 0;
			test = null;
			side = TestSide.Both;
			s0 = 0;
			filterValidValues = false;
			minNumValidValues = 0;
			minNumValidValuesMode = 0;
			minPercValidValues = 0;
			minPercValidValuesMode = 0;
			calcCombinedScore = false;
			combinedScoreMode = CombinedScoreMode.Product;
			combinedScoreQvalue = false;
			test1 = null;
			qval = false;
			if (secondGroupMode != SecondGroupMode.Complement && firstGroupInds.Length != secondGroupInds.Length) {
				processInfo.ErrString = "Please specify the same number of groups in the 'First group' and 'Second group' boxes.";
				return;
			}
			if (firstGroupInds.Length == 0) {
				processInfo.ErrString = "Please specify some groups.";
				return;
			}
			if (secondGroupMode != SecondGroupMode.Complement) {
				for (int i = 0; i < firstGroupInds.Length; i++) {
					if (firstGroupInds[i] == secondGroupInds[i]) {
						processInfo.ErrString = "Groups to be compared in the test cannot be equal.";
						return;
					}
				}
			}
			groupNames = ArrayUtils.UniqueValuesPreserveOrder(ArrayUtils.Concat(mdata.GetCategoryRowAt(groupRowInd)));
			logPval = param.GetParam<bool>("-Log10 p-value").Value;
			suffix = param.GetParam<string>("Suffix").Value;
			ParameterWithSubParams<int> truncParam = param.GetParamWithSubParams<int>("Use for truncation");
			if (!isVolcano) {
				int truncIndex = truncParam.Value;
				truncation = truncIndex == 0
					? TestTruncation.Pvalue
					: (truncIndex == 1 ? TestTruncation.BenjaminiHochberg : TestTruncation.PermutationBased);
			}
			Parameters truncSubParams = truncParam.GetSubParameters();
			threshold = truncation == TestTruncation.Pvalue
				? truncParam.GetSubParameters().GetParam<double>("Threshold p-value").Value
				: truncParam.GetSubParameters().GetParam<double>("FDR").Value;
			ParameterWithSubParams<int> testParam = param.GetParamWithSubParams<int>("Test");
			int testInd = testParam.Value;
			ParameterWithSubParams<bool> validValParam = param.GetParamWithSubParams<bool>("Valid value filter");
			filterValidValues = validValParam.Value;
			minNumValidValues = 0;
			minNumValidValuesMode = 0;
			minPercValidValues = 0;
			minPercValidValuesMode = 0;
			if (filterValidValues) {
				Parameters px = validValParam.GetSubParameters();
				minNumValidValues = px.GetParam<int>("Min. number of valid values").Value;
				minNumValidValuesMode = px.GetParam<int>("Min. number mode").Value;
				minPercValidValues = px.GetParam<int>("Min. percentage of valid values").Value;
				minPercValidValuesMode = px.GetParam<int>("Min. percentage mode").Value;
			}
			int sideInd = 0;
			test = allTests[testInd];
			Parameters testSubParams = testParam.GetSubParameters();
			if (test.HasSides) {
				sideInd = testSubParams.GetParam<int>("Side").Value;
			}
			switch (sideInd) {
				case 0:
					side = TestSide.Both;
					break;
				case 1:
					side = TestSide.Left;
					break;
				case 2:
					side = TestSide.Right;
					break;
				default: throw new Exception("Never get here.");
			}
			s0 = 0;
			if (test.HasS0) {
				s0 = testSubParams.GetParam<double>("S0").Value;
			}
			if (paired) {
				test1 = test.GetOneSampleTest();
			}
			ParameterWithSubParams<bool> combinedScoreParam = param.GetParamWithSubParams<bool>("Calculate combined score");
			calcCombinedScore = combinedScoreParam.Value;
			if (firstGroupInds.Length < 2 || truncation != TestTruncation.PermutationBased) {
				calcCombinedScore = false;
			}
			combinedScoreMode = CombinedScoreMode.Product;
			if (calcCombinedScore) {
				Parameters px = combinedScoreParam.GetSubParameters();
				combinedScoreQvalue = px.GetParam<bool>("Combined q-value").Value;
				int combinedScoreBestInd = px.GetParam<int>("Mode").Value;
				switch (combinedScoreBestInd) {
					case 0:
						combinedScoreMode = CombinedScoreMode.Product;
						break;
					case 1:
						combinedScoreMode = CombinedScoreMode.Best;
						break;
					default: throw new Exception("Never get here.");
				}
			}
			if (truncation != TestTruncation.Pvalue) {
				qval = truncSubParams.GetParam<bool>("Report q-value").Value;
			}
			nrand = -1;
			if (truncation == TestTruncation.PermutationBased) {
				nrand = truncSubParams.GetParam<int>("Number of randomizations").Value;
				preserveGroupInd = truncSubParams.GetParam<int>("Preserve grouping in randomizations").Value - 1;
			}
			if (preserveGroupInd >= 0 && calcCombinedScore) {
				processInfo.ErrString = "Combination of preserved subgroups and combined score is not yet supported.";
			}
			if (paired && combinedScoreQvalue) {
				processInfo.ErrString = "Combined q-value is not supported for paired tests.";
			}
		}

		public static double[] Process(IList<int> firstGroupInds, IList<int> secondGroupInds,
			SecondGroupMode secondGroupMode, IList<string> groupNames, IMatrixData mdata, int groupInd, ProcessInfo processInfo,
			bool log, double threshold, TestTruncation truncation, int preserveGroupInd, int nrand, TwoSampleTest test,
			OneSampleTest test1, TestSide side, double s0, bool filterValidValues, int minNumValidValues,
			int minNumValidValuesMode, int minPercValidValues, int minPercValidValuesMode, bool calcCombinedScore,
			CombinedScoreMode combinedScoreMode, IDictionary<int, int> indMap, bool qval, string[] plotNames,
			string[] pvals1Name, string[] fdrs1Name, string[] diffs1Name, string[] statCol1Name, string[] significant1Name,
			double[][] pvals1, double[][] fdrs1, double[][] diffs1, double[][] statCol1, string[][][] significant1,
			out string[][] sig, out string[] testNames, string mainSuffix, bool paired) {
			int ntests = firstGroupInds.Count;
			double[][] pvalsS0 = new double[ntests][];
			List<string>[] sigCol = null;
			if (significant1 != null) {
				sigCol = new List<string>[mdata.RowCount];
				for (int i = 0; i < sigCol.Length; i++) {
					sigCol[i] = new List<string>();
				}
			}
			testNames = new string[ntests];
			for (int itest = 0; itest < ntests; itest++) {
				string firstGroup = groupNames[firstGroupInds[itest]];
				string secondGroup = secondGroupMode == SecondGroupMode.Complement ? null : groupNames[secondGroupInds[itest]];
				string err = null;
				PerformSingleTest(firstGroup, secondGroup, secondGroupMode, mdata, groupInd, ref err, log, threshold, truncation,
					nrand, preserveGroupInd, test, test1, side, s0, filterValidValues, minNumValidValues, minNumValidValuesMode,
					minPercValidValues, minPercValidValuesMode, out pvalsS0[itest], indMap, qval, plotNames, pvals1Name, fdrs1Name,
					diffs1Name, statCol1Name, significant1Name, pvals1, fdrs1, diffs1, statCol1, significant1, itest, sigCol,
					out testNames[itest], mainSuffix, paired);
				if (!string.IsNullOrEmpty(err)) {
					processInfo.ErrString = err;
					sig = null;
					return null;
				}
			}
			sig = null;
			if (significant1 != null) {
				sig = new string[sigCol.Length][];
				for (int i = 0; i < sig.Length; i++) {
					sigCol[i].Sort();
					sig[i] = sigCol[i].ToArray();
				}
			}
			if (calcCombinedScore) {
				double[] combinedPvalsS0 = new double[mdata.RowCount];
				for (int i = 0; i < mdata.RowCount; i++) {
					double[] x = ExtractRow(pvalsS0, i);
					switch (combinedScoreMode) {
						case CombinedScoreMode.Product:
							combinedPvalsS0[i] = ArrayUtils.Product(x);
							break;
						case CombinedScoreMode.ProductOfSignificant: throw new NotImplementedException();
						case CombinedScoreMode.Best:
							combinedPvalsS0[i] = ArrayUtils.Min(x);
							break;
						default: throw new Exception("Never get here");
					}
				}
				return combinedPvalsS0;
			}
			return null;
		}

		private static void PerformSingleTest(string firstGroup, string secondGroup, SecondGroupMode secondGroupMode,
			IMatrixData mdata, int groupInd, ref string err, bool log, double threshold, TestTruncation truncation, int nrand,
			int preserveGroupInd, TwoSampleTest test, OneSampleTest test1, TestSide side, double s0, bool filterValidValues,
			int minNumValidValues, int minNumValidValuesMode, int minPercValidValues, int minPercValidValuesMode,
			out double[] pvalsS0, IDictionary<int, int> indMap, bool qval, string[] plotNames, string[] pvals1Name,
			string[] fdrs1Name, string[] diffs1Name, string[] statCol1Name, string[] significant1Name, double[][] pvals1,
			double[][] fdrs1, double[][] diffs1, double[][] statCol1, string[][][] significant1, int ind, List<string>[] sigCol,
			out string suffix, string mainSuffix, bool paired) {
			bool randomized = indMap != null;
			bool addQval = qval && truncation != TestTruncation.Pvalue;
			string[][] groupCol = mdata.GetCategoryRowAt(groupInd);
			int[] colInds1;
			int[] colInds2;
			if (secondGroupMode == SecondGroupMode.Complement) {
				int[][] colInds = PerseusPluginUtils.GetMainColIndices(groupCol, new[] {firstGroup});
				colInds1 = colInds[0];
				colInds2 = ArrayUtils.Complement(colInds1, mdata.ColumnCount);
			} else {
				int[][] colInds = PerseusPluginUtils.GetMainColIndices(groupCol, new[] {firstGroup, secondGroup});
				colInds1 = colInds[0];
				colInds2 = colInds[1];
			}
			if (indMap != null) {
				Transform(colInds1, indMap);
				Transform(colInds2, indMap);
			}
			Array.Sort(colInds1);
			Array.Sort(colInds2);
			suffix = firstGroup;
			if (secondGroupMode != SecondGroupMode.Complement) {
				suffix += "_" + secondGroup;
			}
			if (paired && colInds1.Length != colInds2.Length) {
				err = "Group sizes have to be equal for paired test.";
				pvalsS0 = null;
				return;
			}
			List<int[]> colIndsPreserve1 = null;
			List<int[]> colIndsPreserve2 = null;
			if (truncation == TestTruncation.PermutationBased) {
				if (preserveGroupInd >= 0) {
					if (paired) {
						err = "Preserved subgroups are not supported for paired tests.";
						pvalsS0 = null;
						return;
					}
					string[][] preserveGroupCol = mdata.GetCategoryRowAt(preserveGroupInd);
					string[] allGroupsPreserve = ArrayUtils.UniqueValuesPreserveOrder(ArrayUtils.Concat(preserveGroupCol));
					int[][] colIndsPreserve = PerseusPluginUtils.GetMainColIndices(preserveGroupCol, allGroupsPreserve);
					int[] allInds = ArrayUtils.Concat(colIndsPreserve);
					int[] allIndsUnique = ArrayUtils.UniqueValues(allInds);
					if (allInds.Length != allIndsUnique.Length) {
						err = "The grouping for randomizations is not unique";
						pvalsS0 = null;
						return;
					}
					if (allInds.Length != colInds1.Length + colInds2.Length) {
						err = "The grouping for randomizations is not valid because it does not cover all samples.";
						pvalsS0 = null;
						return;
					}
					colIndsPreserve1 = new List<int[]>();
					colIndsPreserve2 = new List<int[]>();
					foreach (int[] inds in colIndsPreserve) {
						int index = DetermineGroup(colInds1, colInds2, inds);
						if (index == 0) {
							err = "The grouping for randomizations is not hierarchical with respect to the main grouping.";
							pvalsS0 = null;
							return;
						}
						switch (index) {
							case 1:
								colIndsPreserve1.Add(inds);
								break;
							case 2:
								colIndsPreserve2.Add(inds);
								break;
						}
					}
				}
			}
			TwoSamplesTest1(colInds1, colInds2, truncation, threshold, test, test1, side, log, mdata, s0, nrand,
				colIndsPreserve1, colIndsPreserve2, suffix, mainSuffix, filterValidValues, minNumValidValues, minNumValidValuesMode,
				minPercValidValues, minPercValidValuesMode, out pvalsS0, randomized, addQval, plotNames, pvals1Name, fdrs1Name,
				diffs1Name, statCol1Name, significant1Name, pvals1, fdrs1, diffs1, statCol1, significant1, ind, paired);
			if (significant1 != null) {
				for (int i = 0; i < significant1[ind].Length; i++) {
					if (significant1[ind][i].Length > 0) {
						sigCol[i].Add(suffix);
					}
				}
			}
		}

		private static void TwoSamplesTest1(int[] colInd1, int[] colInd2, TestTruncation truncation, double threshold,
			TwoSampleTest test, OneSampleTest test1, TestSide side, bool log, IMatrixData data, double s0, int nrand,
			List<int[]> colIndsPreserve1, List<int[]> colIndsPreserve2, string suffix, string mainSuffix, bool filterValidValues,
			int minNumValidValues, int minNumValidValuesMode, int minPercValidValues, int minPercValidValuesMode,
			out double[] pvalsS0, bool randomized, bool addQval, IList<string> plotNames, IList<string> pvals1Name,
			IList<string> fdrs1Name, IList<string> diffs1Name, IList<string> statCol1Name, IList<string> significant1Name,
			IList<double[]> pvals1, IList<double[]> fdrs1, IList<double[]> diffs1, IList<double[]> statCol1,
			IList<string[][]> significant1, int ind, bool paired) {
			double[] pvals = new double[data.RowCount];
			pvalsS0 = new double[data.RowCount];
			double[] diffs = new double[data.RowCount];
			double[] statCol = new double[data.RowCount];
			List<int> validRows = new List<int>();
			for (int i = 0; i < data.RowCount; i++) {
				bool valid = IsValidRow(filterValidValues, minNumValidValues, minNumValidValuesMode, minPercValidValues,
					minPercValidValuesMode, i, colInd1, colInd2, data);
				if (valid) {
					validRows.Add(i);
				}
				double[] vals1;
				double[] vals2;
				if (!paired) {
					vals1 = GetValues(i, colInd1, data, true);
					vals2 = GetValues(i, colInd2, data, true);
				} else {
					vals1 = GetValues(i, colInd1, data, false);
					vals2 = GetValues(i, colInd2, data, false);
				}
				if (valid) {
					CalcTest(test, test1, paired, vals1, vals2, s0, side, out diffs[i], out pvals[i], out pvalsS0[i], out statCol[i]);
				} else {
					pvals[i] = double.NaN;
					pvalsS0[i] = double.NaN;
					statCol[i] = double.NaN;
				}
			}
			if (!randomized) {
				string[][] significant;
				double[] fdrs = null;
				switch (truncation) {
					case TestTruncation.Pvalue:
						significant = PerseusPluginUtils.CalcPvalueSignificance(pvals, threshold);
						break;
					case TestTruncation.BenjaminiHochberg:
						significant = PerseusPluginUtils.CalcBenjaminiHochbergFdr(pvals, threshold, out fdrs);
						break;
					case TestTruncation.PermutationBased:
						double[] fdrsX;
						string[][] significantX = CalcPermutationBasedFdr(pvalsS0.SubArray(validRows), nrand, data, test,
							test1, side, colInd1, colInd2, s0, threshold, colIndsPreserve1, colIndsPreserve2, validRows, out fdrsX, paired);
						significant = new string[data.RowCount][];
						fdrs = new double[data.RowCount];
						for (int i = 0; i < significant.Length; i++) {
							significant[i] = new string[0];
							fdrs[i] = 1;
						}
						for (int i = 0; i < validRows.Count; i++) {
							significant[validRows[i]] = significantX[i];
							fdrs[validRows[i]] = fdrsX[i];
						}
						break;
					default: throw new Exception("Never get here.");
				}
				string x = test.Name + " p-value";
				if (log) {
					x = "-Log " + x;
					for (int i = 0; i < pvals.Length; i++) {
						pvals[i] = -Math.Log10(Math.Max(pvals[i], double.Epsilon));
					}
				}
				plotNames[ind] = suffix + mainSuffix;
				pvals1Name[ind] = x + " " + suffix + mainSuffix;
				fdrs1Name[ind] = test.Name + " q-value " + suffix + mainSuffix;
				diffs1Name[ind] = test.Name + " Difference " + suffix + mainSuffix;
				statCol1Name[ind] = test.Name + " Test statistic " + suffix + mainSuffix;
				significant1Name[ind] = test.Name + " Significant " + suffix + mainSuffix;
				pvals1[ind] = pvals;
				if (addQval) {
					fdrs1[ind] = fdrs;
				}
				diffs1[ind] = diffs;
				statCol1[ind] = statCol;
				significant1[ind] = significant;
			}
		}

		private static void CalcTest(TwoSampleTest test, OneSampleTest test1, bool paired, double[] vals1, double[] vals2,
			double s0, TestSide side, out double diff, out double pval, out double pvalS0, out double statS0) {
			double both;
			double left;
			double right;
			double bothS0;
			double leftS0;
			double rightS0;
			if (paired) {
				test1.Test(ArrayUtils.ExtractValidValues(Diff(vals1, vals2)), 0, out _, out statS0, out both, out left, out right,
					out diff, s0, out bothS0, out leftS0, out rightS0);
			} else {
				test.Test(vals1, vals2, out _, out _, out _, out statS0, out both, out left, out right, out diff, s0, out bothS0,
					out leftS0, out rightS0);
			}
			pval = side == TestSide.Both ? both : (side == TestSide.Left ? right : left);
			pvalS0 = side == TestSide.Both ? bothS0 : (side == TestSide.Left ? rightS0 : leftS0);
		}

		private static double[] Diff(double[] vals1, double[] vals2) {
			double[] result = new double[vals1.Length];
			for (int i = 0; i < result.Length; i++) {
				result[i] = vals1[i] - vals2[i];
			}
			return result;
		}

		private static bool IsValidRow(bool filterValidValues, int minNumValidValues, int minNumValidValuesMode,
			int minPercValidValues, int minPercValidValuesMode, int i, ICollection<int> colInd1, ICollection<int> colInd2,
			IMatrixData data) {
			if (!filterValidValues) {
				return true;
			}
			int v1 = GetValidCount(i, colInd1, data);
			int v2 = GetValidCount(i, colInd2, data);
			int t1 = colInd1.Count;
			int t2 = colInd2.Count;
			bool numValid = NumValid(v1, v2, minNumValidValues, minNumValidValuesMode);
			bool percValid = PercValid(v1, v2, t1, t2, minPercValidValues, minPercValidValuesMode);
			return numValid && percValid;
		}

		private static bool PercValid(int v1, int v2, int t1, int t2, int minPercValidValues, int minPercValidValuesMode) {
			switch (minPercValidValuesMode) {
				case 0: return Math.Round((v1 + v2) * 100f / (t1 + t2)) >= minPercValidValues;
				case 1: return Math.Round(v1 * 100f / t1) >= minPercValidValues && Math.Round(v2 * 100f / t2) >= minPercValidValues;
				case 2: return Math.Round(v1 * 100f / t1) >= minPercValidValues || Math.Round(v2 * 100f / t2) >= minPercValidValues;
				case 3: return Math.Round(v1 * 100f / t1) >= minPercValidValues;
				case 4: return Math.Round(v2 * 100f / t2) >= minPercValidValues;
				default: throw new Exception("Never get here.");
			}
		}

		private static bool NumValid(int v1, int v2, int minNumValidValues, int minNumValidValuesMode) {
			switch (minNumValidValuesMode) {
				case 0: return v1 + v2 >= minNumValidValues;
				case 1: return v1 >= minNumValidValues && v2 >= minNumValidValues;
				case 2: return v1 >= minNumValidValues || v2 >= minNumValidValues;
				case 3: return v1 >= minNumValidValues;
				case 4: return v2 >= minNumValidValues;
				default: throw new Exception("Never get here.");
			}
		}

		private static void BalancedPermutationsSubgroups(List<int[]> preserve1, List<int[]> preserve2, out int[] inds1Out,
			out int[] inds2Out, Random2 r2) {
			PermBasedFdrUtil.BalancedPermutationsSubgroups(new[] {preserve1.ToArray(), preserve2.ToArray()}, out int[][] out1,
				r2);
			inds1Out = out1[0];
			inds2Out = out1[1];
		}

		private static void BalancedPermutations(int[] inds1, int[] inds2, out int[] inds1Out, out int[] inds2Out,
			Random2 r2) {
			PermBasedFdrUtil.BalancedPermutations(new[] {inds1, inds2}, out int[][] out1, r2);
			inds1Out = out1[0];
			inds2Out = out1[1];
		}

		private static string[][] CalcPermutationBasedFdr(IList<double> pvalsS0, int nperm, IMatrixData data,
			TwoSampleTest test, OneSampleTest test1, TestSide side, int[] colInd1, int[] colInd2, double s0, double threshold,
			List<int[]> colIndsPreserve1, List<int[]> colIndsPreserve2, List<int> validRows, out double[] fdrs, bool paired) {
			List<double> pq = new List<double>();
			List<int> indices = new List<int>();
			for (int i = 0; i < pvalsS0.Count; i++) {
				pq.Add(pvalsS0[i]);
				indices.Add(i);
			}
			Random2 r2 = new Random2(7);
			for (int p = 0; p < nperm; p++) {
				int[] colInd1P = null;
				int[] colInd2P = null;
				bool[] pairedPerm = null;
				if (!paired) {
					if (colIndsPreserve1 != null) {
						BalancedPermutationsSubgroups(colIndsPreserve1, colIndsPreserve2, out colInd1P, out colInd2P, r2);
					} else {
						BalancedPermutations(colInd1, colInd2, out colInd1P, out colInd2P, r2);
					}
				} else {
					pairedPerm = new bool[colInd1.Length];
					for (int i = 0; i < colInd1.Length / 2; i++) {
						pairedPerm[i] = true;
					}
					pairedPerm = pairedPerm.SubArray(r2.NextPermutation(pairedPerm.Length));
				}
				foreach (int row in validRows) {
					double[] vals1;
					double[] vals2;
					if (!paired) {
						vals1 = GetValues(row, colInd1P, data, true);
						vals2 = GetValues(row, colInd2P, data, true);
					} else {
						vals1 = GetValues(row, colInd1, data, false);
						vals2 = GetValues(row, colInd2, data, false);
						for (int i = 0; i < pairedPerm.Length; i++) {
							if (pairedPerm[i]) {
								double tmp = vals2[i];
								vals2[i] = vals1[i];
								vals1[i] = tmp;
							}
						}
					}
					CalcTest(test, test1, paired, vals1, vals2, s0, side, out double _, out double _, out double p1, out double _);
					pq.Add(p1);
					indices.Add(-1);
				}
			}
			double[] pv = pq.ToArray();
			int[] inds = indices.ToArray();
			int[] o = pv.Order();
			double forw = 0;
			double rev = 0;
			int lastind = -1;
			fdrs = new double[pvalsS0.Count];
			foreach (int ind in o) {
				if (inds[ind] == -1) {
					rev++;
				} else {
					forw++;
					double fdr = Math.Min(1, rev / forw / nperm);
					fdrs[inds[ind]] = fdr;
					if (fdr <= threshold) {
						lastind = (int) Math.Round(forw - 1);
					}
				}
			}
			string[][] result = new string[pvalsS0.Count][];
			for (int i = 0; i < result.Length; i++) {
				result[i] = new string[0];
			}
			int[] o1 = pvalsS0.Order();
			for (int i = 0; i <= lastind; i++) {
				result[o1[i]] = new[] {"+"};
			}
			return result;
		}

		private static int GetValidCount(int row, IEnumerable<int> cols, IMatrixData data) {
			int c = 0;
			foreach (int col in cols) {
				double val = data.Values.Get(row, col);
				if (!double.IsNaN(val) && !double.IsInfinity(val)) {
					bool imp = data.IsImputed[row, col];
					if (!imp) {
						c++;
					}
				}
			}
			return c;
		}

		private static double[] GetValues(int row, IEnumerable<int> cols, IMatrixData data, bool removeInvalids) {
			List<double> result = new List<double>();
			foreach (int col in cols) {
				double val = data.Values.Get(row, col);
				if (!removeInvalids || (!double.IsNaN(val) && !double.IsInfinity(val))) {
					result.Add(val);
				}
			}
			return result.ToArray();
		}

		private static double[] ExtractRow(IList<double[]> x, int row) {
			double[] result = new double[x.Count];
			for (int i = 0; i < result.Length; i++) {
				result[i] = Math.Min(x[i][row], 1);
			}
			return result;
		}

		private static void Transform(IList<int> colInds1, IDictionary<int, int> indMap) {
			for (int i = 0; i < colInds1.Count; i++) {
				colInds1[i] = indMap[colInds1[i]];
			}
		}

		private static int DetermineGroup(int[] colInds1, int[] colInds2, IEnumerable<int> inds) {
			if (CompletelyContained(colInds1, inds)) {
				return 1;
			}
			return CompletelyContained(colInds2, inds) ? 2 : 0;
		}

		private static bool CompletelyContained(int[] colInds1, IEnumerable<int> inds) {
			foreach (int ind in inds) {
				if (Array.BinarySearch(colInds1, ind) < 0) {
					return false;
				}
			}
			return true;
		}
	}
}