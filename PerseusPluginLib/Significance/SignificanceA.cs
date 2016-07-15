using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Test;
using BaseLibS.Num.Vector;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Significance{
	public class SignificanceA : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Significance A";
		public string Heading => "Outliers";
		public bool IsActive => true;
		public float DisplayRank => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Outliers:SignificanceA";

		public string Description
			=>
				"Determines which values are significant outliers relative to a certain population. For details see Cox and Mann " +
				"(2008) Nat. Biotech. 26, 1367-72.";

		public string HelpOutput
			=>
				"A numerical column is added containing the significance A value. Furthermore, a categorical column is added " +
				"indicating by '+' if a row is significant.";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] cols = param.GetParam<int[]>("Columns").Value;
			int truncIndex = param.GetParam<int>("Use for truncation").Value;
			TestTruncation truncation = truncIndex == 0
				? TestTruncation.Pvalue
				: (truncIndex == 1 ? TestTruncation.BenjaminiHochberg : TestTruncation.PermutationBased);
			double threshold = param.GetParam<double>("Threshold value").Value;
			int sideInd = param.GetParam<int>("Side").Value;
			TestSide side;
			switch (sideInd){
				case 0:
					side = TestSide.Both;
					break;
				case 1:
					side = TestSide.Left;
					break;
				case 2:
					side = TestSide.Right;
					break;
				default:
					throw new Exception("Never get here.");
			}
			foreach (int col in cols){
				BaseVector r = mdata.Values.GetColumn(col);
				double[] pvals = CalcSignificanceA(r, side);
				string[][] fdr;
				switch (truncation){
					case TestTruncation.Pvalue:
						fdr = PerseusPluginUtils.CalcPvalueSignificance(pvals, threshold);
						break;
					case TestTruncation.BenjaminiHochberg:
						double[] fdrs;
						fdr = PerseusPluginUtils.CalcBenjaminiHochbergFdr(pvals, threshold, pvals.Length, out fdrs);
						break;
					default:
						throw new Exception("Never get here.");
				}
				mdata.AddNumericColumn(mdata.ColumnNames[col] + " Significance A", "", pvals);
				mdata.AddCategoryColumn(mdata.ColumnNames[col] + " A significant", "", fdr);
			}
		}

		private static double[] CalcSignificanceA(BaseVector ratios, TestSide side){
			double[] result = new double[ratios.Length];
			for (int i = 0; i < result.Length; i++){
				result[i] = 1;
			}
			List<double> lRatio = new List<double>();
			List<double> lIntensity = new List<double>();
			List<int> indices = new List<int>();
			for (int i = 0; i < ratios.Length; i++){
				if (!double.IsNaN(ratios[i]) && !double.IsInfinity(ratios[i])){
					lRatio.Add(ratios[i]);
					lIntensity.Add(0);
					indices.Add(i);
				}
			}
			double[] ratioSignificanceA = NumUtils.MovingBoxPlot(lRatio.ToArray(), lIntensity.ToArray(), 1, side);
			for (int i = 0; i < indices.Count; i++){
				result[indices[i]] = ratioSignificanceA[i];
			}
			return result;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> choice = mdata.ColumnNames;
			return
				new Parameters(new Parameter[]{
					new MultiChoiceParam("Columns"){
						Values = choice,
						Help = "Columns for which the Significance A should be calculated."
					},
					new SingleChoiceParam("Side"){
						Values = new[]{"both", "right", "left"},
						Help =
							"'Both' stands for the two-sided test in which the the null hypothesis can be rejected regardless of the direction" +
							" of the effect. 'Left' and 'right' are the respective one sided tests."
					},
					new SingleChoiceParam("Use for truncation"){
						Value = 1,
						Values = new[]{"P value", "Benjamini-Hochberg FDR"},
						Help =
							"Choose here whether the truncation should be based on the p values or if the Benjamini Hochberg correction for " +
							"multiple hypothesis testing should be applied."
					},
					new DoubleParam("Threshold value", 0.05){
						Help =
							"Rows with a test result below this value are reported as significant. Depending on the choice made above this " +
							"threshold value is applied to the p value or to the Benjamini Hochberg FDR."
					}
				});
		}
	}
}