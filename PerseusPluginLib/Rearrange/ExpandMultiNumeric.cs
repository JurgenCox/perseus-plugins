using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Rearrange{
	public class ExpandMultiNumeric : IMatrixProcessing{
		//TODO: optionally distribute values into multiple columns.
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"Distribute multiple values per cell in a multi-numeric column over multiple rows. For each row in the" +
				" original matrix there will be as many rows created as there are numbers in the cell of the multi-numeric " +
				"column. If multiple multi-numeric columns are selected they have to have the same number of values in every " +
				"row. Elements of text columns, if one is selected, are interpreted as semicolon-separated. They also have " +
				"to have the same number of semicolon-separated elements as there are values in the cell(s) " +
				"of the multi-numeric columns(s).";

		public string Name => "Expand multi-numeric and text columns";
		public string Heading => "Rearrange";
		public bool IsActive => true;
		public float DisplayRank => 12;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string HelpOutput => "Columns are the same. The number of rows increases due to the expansion.";
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public string Url
			=> "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixProcessing:Rearrange:ExpandMultiNumeric";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void ProcessData(IMatrixData mdata, Parameters param1, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] multiNumCols = param1.GetParam<int[]>("Multi-numeric columns").Value;
			Array.Sort(multiNumCols);
			int[] stringCols = param1.GetParam<int[]>("Text columns").Value;
			Array.Sort(stringCols);
			HashSet<int> multinumCols2 = new HashSet<int>(multiNumCols);
			HashSet<int> stringCols2 = new HashSet<int>(stringCols);
			if (multiNumCols.Length + stringCols.Length == 0){
				processInfo.ErrString = "Please select some columns.";
				return;
			}
			int rowCount = GetNewRowCount(mdata, multiNumCols, stringCols);
			bool[,] impVals = new bool[rowCount, mdata.ColumnCount];
			double[,] qualVals = new double[rowCount, mdata.ColumnCount];
			double[,] expVals = new double[rowCount, mdata.ColumnCount];
			List<string[]> stringC = new List<string[]>();
			for (int i = 0; i < mdata.StringColumnCount; i++){
				stringC.Add(new string[rowCount]);
			}
			List<double[]> numC = new List<double[]>();
			for (int i = 0; i < mdata.NumericColumnCount; i++){
				numC.Add(new double[rowCount]);
			}
			List<string[][]> catC = new List<string[][]>();
			for (int i = 0; i < mdata.CategoryColumnCount; i++){
				catC.Add(new string[rowCount][]);
			}
			List<double[][]> multiNumC = new List<double[][]>();
			for (int i = 0; i < mdata.MultiNumericColumnCount; i++){
				multiNumC.Add(new double[rowCount][]);
			}
			int count = 0;
			for (int i = 0; i < mdata.RowCount; i++){
				int entryCount = GetEntryCount(i, mdata, multiNumCols, stringCols, out string err);
				if (err != null){
					processInfo.ErrString = err;
					return;
				}
				bool empty = entryCount == 0;
				entryCount = Math.Max(entryCount, 1);
				for (int j = 0; j < entryCount; j++){
					for (int k = 0; k < mdata.ColumnCount; k++){
						expVals[count + j, k] = mdata.Values.Get(i, k);
					}
				    if (mdata.HasQuality)
				    {
                        for (int k = 0; k < mdata.ColumnCount; k++){
                            qualVals[count + j, k] = mdata.Quality.Get(i, k);
                        }
				    }
					for (int k = 0; k < mdata.ColumnCount; k++){
						impVals[count + j, k] = mdata.IsImputed[i, k];
					}
					for (int k = 0; k < mdata.NumericColumnCount; k++){
						numC[k][count + j] = mdata.NumericColumns[k][i];
					}
					for (int k = 0; k < mdata.CategoryColumnCount; k++){
						catC[k][count + j] = mdata.GetCategoryColumnEntryAt(k, i);
					}
				}
				for (int k = 0; k < mdata.MultiNumericColumnCount; k++){
					if (multinumCols2.Contains(k)){
						if (empty){
							multiNumC[k][count] = new double[0];
						} else{
							double[] vals = mdata.MultiNumericColumns[k][i];
							for (int j = 0; j < entryCount; j++){
								multiNumC[k][count + j] = new[]{vals[j]};
							}
						}
					} else{
						for (int j = 0; j < entryCount; j++){
							multiNumC[k][count + j] = mdata.MultiNumericColumns[k][i];
						}
					}
				}
				for (int k = 0; k < mdata.StringColumnCount; k++){
					if (stringCols2.Contains(k)){
						if (empty){
							stringC[k][count] = "";
						} else{
							string[] vals = mdata.StringColumns[k][i].Split(';');
							for (int j = 0; j < entryCount; j++){
								stringC[k][count + j] = vals[j];
							}
						}
					} else{
						for (int j = 0; j < entryCount; j++){
							stringC[k][count + j] = mdata.StringColumns[k][i];
						}
					}
				}
				count += entryCount;
			}
			int[] multiNumComplement = ArrayUtils.Complement(multiNumCols, mdata.MultiNumericColumnCount);
			List<double[][]> toBeTransformed = multiNumC.SubList(multiNumCols);
			multiNumC = multiNumC.SubList(multiNumComplement);
			foreach (double[][] d in toBeTransformed){
				numC.Add(Transform(d));
			}
			mdata.ColumnNames = mdata.ColumnNames;
			mdata.Values.Set(expVals);
		    if (mdata.HasQuality)
		    {
		        mdata.Quality.Set(qualVals);
		    }
            mdata.IsImputed.Set(impVals);
			mdata.SetAnnotationColumns(mdata.StringColumnNames, stringC, mdata.CategoryColumnNames, catC,
				new List<string>(ArrayUtils.Concat(mdata.NumericColumnNames,
					mdata.MultiNumericColumnNames.SubList(multiNumCols))), numC,
				new List<string>(mdata.MultiNumericColumnNames.SubArray(multiNumComplement)), multiNumC);
		}

		private static double[] Transform(IList<double[]> doubles){
			double[] result = new double[doubles.Count];
			for (int i = 0; i < result.Length; i++){
				result[i] = doubles[i].Length > 0 ? doubles[i][0] : double.NaN;
			}
			return result;
		}

		private int GetEntryCount(int row, IMatrixData mdata, IList<int> multiNumCols, IList<int> stringCols, out string err){
			int[] multiNumEntryCount = new int[multiNumCols.Count];
			for (int i = 0; i < multiNumEntryCount.Length; i++){
				multiNumEntryCount[i] = GetEntryCount(mdata.MultiNumericColumns[multiNumCols[i]][row]);
			}
			int[] stringEntryCount = new int[stringCols.Count];
			for (int i = 0; i < stringEntryCount.Length; i++){
				stringEntryCount[i] = GetEntryCount(mdata.StringColumns[stringCols[i]][row]);
			}
			int[] v = ArrayUtils.UniqueValues(ArrayUtils.Concat(multiNumEntryCount, stringEntryCount));
			if (v.Length > 1){
				err = "Inconsistent number of values in row " + row + ".";
				return -1;
			}
			err = null;
			return v[0];
		}

		private int GetEntryCount(string s){
			string[] q = s.Length > 0 ? s.Split(';') : new string[0];
			return q.Length;
		}

		private static int GetEntryCount(ICollection<double> x){
			return x.Count;
		}

		private static int GetNewRowCount(IMatrixData mdata, IList<int> multiNumCols, IList<int> stringCols){
			return multiNumCols.Count > 0
				? GetNewRowCount(mdata.MultiNumericColumns[multiNumCols[0]])
				: GetNewRowCount(mdata.StringColumns[stringCols[0]]);
		}

		private static int GetNewRowCount(IEnumerable<string> stringColumn){
			int count = 0;
			foreach (string s in stringColumn){
				if (s.Length > 0){
					count += s.Split(';').Length;
				} else{
					count++;
				}
			}
			return count;
		}

		private static int GetNewRowCount(IEnumerable<double[]> multiNumericColumn){
			int count = 0;
			foreach (double[] s in multiNumericColumn){
				if (s.Length > 0){
					count += s.Length;
				} else{
					count++;
				}
			}
			return count;
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new MultiChoiceParam("Multi-numeric columns"){
					Values = mdata.MultiNumericColumnNames,
					Value = new int[0],
					Help = "Select here the multi-numeric colums that should be expanded."
				}, new MultiChoiceParam("Text columns"){
					Values = mdata.StringColumnNames,
					Value = new int[0],
					Help = "Select here the text colums that should be expanded."
				});
		}
	}
}