using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.AnnotCols{
	public class CategoryCounting : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "For each term in a categorical column one counts the number of occurrences.";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Category counting";
		public string Heading => "Annot. columns";
		public bool IsActive => true;
		public float DisplayRank => 3;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Annotcolumns:CategoryCounting";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			List<string> choice = mdata.CategoryColumnNames;
			int[] selection = ArrayUtils.ConsecutiveInts(choice.Count);
			string[] sel = ArrayUtils.Concat(mdata.CategoryColumnNames.ToArray(), "<None>");
			return new Parameters(new MultiChoiceParam("Categories"){Values = choice, Value = selection},
				new IntParam("Min. count", 1), new SingleChoiceParam("Selection"){Values = sel, Value = sel.Length - 1},
				new StringParam("Value", "+"));
		}

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int minCount = param.GetParam<int>("Min. count").Value;
			int selCol = param.GetParam<int>("Selection").Value;
			string value = param.GetParam<string>("Value").Value;
			int[] catIndices = param.GetParam<int[]>("Categories").Value;
			bool[] selection = null;
			if (selCol < mdata.CategoryColumnCount){
				selection = new bool[mdata.RowCount];
				string[][] x = mdata.GetCategoryColumnAt(selCol);
				for (int i = 0; i < selection.Length; i++){
					if (x[i] != null){
						for (int j = 0; j < x[i].Length; j++){
							if (x[i][j].Equals(value)){
								selection[i] = true;
								break;
							}
						}
					}
				}
			}
			CountingResult result = CountCategories(mdata, selection, selCol, catIndices);
			CreateMatrixData(result, mdata, minCount, selection);
		}

		private static void CreateMatrixData(CountingResult result, IMatrixData data, int minCount, IEnumerable selection){
			List<string[]> type = new List<string[]>();
			List<string[]> name = new List<string[]>();
			List<double> count = new List<double>();
			List<double> percOfTotal = new List<double>();
			List<double> selCount = new List<double>();
			List<double> selPerc = new List<double>();
			for (int i = 0; i < result.Count; i++){
				int c = result.GetTotalCountAt(i);
				if (c < minCount){
					continue;
				}
				type.Add(new[]{result.GetType1At(i)});
				name.Add(new[]{result.GetName1At(i)});
				count.Add(c);
				percOfTotal.Add(Math.Round(10000.0*c/data.RowCount)/100.0);
				if (selection != null){
					int c1 = result.GetSelectCountAt(i);
					selCount.Add(c1);
					selPerc.Add(Math.Round(1000.0*c1/c)/10.0);
				}
			}
			float[,] ex = new float[type.Count, 0];
			List<string[][]> catCols = new List<string[][]>{type.ToArray(), name.ToArray()};
			List<string> catColNames = new List<string>(new[]{"Type", "Name"});
			List<double[]> numCols = new List<double[]>{count.ToArray(), percOfTotal.ToArray()};
			if (selection != null){
				numCols.Add(selCount.ToArray());
				numCols.Add(selPerc.ToArray());
			}
			List<string> numColNames = new List<string>(new[]{"Count", "Percentage of total"});
			if (selection != null){
				numColNames.AddRange(new[]{"Selection count", "Selection percentage"});
			}
			data.Name = "Count";
			data.ColumnNames = new List<string>();
			data.Values.Set(ex);
			data.SetAnnotationColumns(new List<string>(), new List<string[]>(), catColNames, catCols, numColNames, numCols,
				new List<string>(), new List<double[][]>());
		}

		private static CountingResult CountCategories(IMatrixData data, bool[] selection, int selCol,
			IEnumerable<int> catIndices){
			CountingResult result = new CountingResult();
			foreach (int i in catIndices.Where(i => i != selCol)){
				CountTerms(data.CategoryColumnNames[i], data.GetCategoryColumnAt(i), result, selection);
			}
			result.Sort();
			return result;
		}

		private static void CountTerms(string categoryName2, IList<string[]> annot2, CountingResult result,
			IList<bool> selection){
			foreach (string[] t in annot2){
				Array.Sort(t);
			}
			string[] allTerms2 = GetAllTerms(annot2);
			int n2 = allTerms2.Length;
			int[] allTotal = new int[n2];
			int[] selectTotal = new int[n2];
			for (int i2 = 0; i2 < n2; i2++){
				for (int j = 0; j < annot2.Count; j++){
					if (Array.BinarySearch(annot2[j], allTerms2[i2]) >= 0){
						allTotal[i2]++;
						if (selection != null && selection[j]){
							selectTotal[i2]++;
						}
					}
				}
			}
			result.Add(categoryName2, allTerms2, allTotal, selectTotal);
		}

		private static string[] GetAllTerms(IEnumerable<string[]> annot){
			HashSet<string> all = new HashSet<string>();
			foreach (string[] x in annot){
				all.UnionWith(x);
			}
			string[] y = ArrayUtils.ToArray(all);
			Array.Sort(y);
			y = ArrayUtils.Remove(y, "");
			return y;
		}
	}
}