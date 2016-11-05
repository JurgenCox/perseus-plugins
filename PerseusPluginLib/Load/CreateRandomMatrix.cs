using System;
using System.Collections.Generic;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Load{
	public class CreateRandomMatrix : IMatrixUpload{
		public bool HasButton => true;
		public Bitmap2 DisplayImage => Bitmap2.GetImage("dice.png");
		public string Name => "Create random matrix";
		public bool IsActive => true;
		public float DisplayRank => 6;
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixUpload:CreateRandomMatrix";

		public string Description
			=>
				"Create a matrix of given dimensions containg random " +
				"numbers drawn from a single or a superposition of several normal distributions.";

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public void LoadData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables, ref IDocumentData[] documents,
			ProcessInfo processInfo){
			int nrows = param.GetParam<int>("Number of rows").Value;
			int ncols = param.GetParam<int>("Number of columns").Value;
			int missingPerc = param.GetParam<int>("Percentage of missing values").Value;
			int ngroups = param.GetParam<int>("Number of groups").Value;
			ParameterWithSubParams<bool> setSeed = param.GetParamWithSubParams<bool>("Set seed");
			Random2 randy = setSeed.Value ? new Random2(setSeed.GetSubParameters().GetParam<int>("Seed").Value) : new Random2();
			ngroups = Math.Min(ngroups, ncols);
			float[,] m = new float[nrows, ncols];
			ParameterWithSubParams<int> x = param.GetParamWithSubParams<int>("Mode");
			Parameters subParams = x.GetSubParameters();
			List<string> catColNames = new List<string>();
			List<string[][]> catCols = new List<string[][]>();
			switch (x.Value){
				case 0:
					for (int i = 0; i < m.GetLength(0); i++){
						for (int j = 0; j < m.GetLength(1); j++){
							if (randy.NextDouble()*100 < missingPerc){
								m[i, j] = float.NaN;
							} else{
								m[i, j] = (float) randy.NextGaussian();
							}
						}
					}
					break;
				case 1:
					float dist = (float) subParams.GetParam<double>("Distance").Value;
					string[][] col = new string[m.GetLength(0)][];
					for (int i = 0; i < m.GetLength(0); i++){
						bool which = randy.NextDouble() < 0.5;
						for (int j = 0; j < m.GetLength(1); j++){
							if (randy.NextDouble()*100 < missingPerc){
								m[i, j] = float.NaN;
							} else{
								m[i, j] = (float) randy.NextGaussian();
							}
						}
						if (which){
							m[i, 0] += dist;
							col[i] = new[]{"Group1"};
						} else{
							col[i] = new[]{"Group2"};
						}
					}
					catColNames.Add("Grouping");
					catCols.Add(col);
					break;
				case 2:
					double boxLen = subParams.GetParam<double>("Box size").Value;
					int howMany = subParams.GetParam<int>("How many").Value;
					string[][] col1 = new string[m.GetLength(0)][];
					float[,] centers = new float[howMany, m.GetLength(1)];
					for (int i = 0; i < centers.GetLength(0); i++){
						for (int j = 0; j < centers.GetLength(1); j++){
							centers[i, j] = (float) (randy.NextDouble()*boxLen);
						}
					}
					for (int i = 0; i < m.GetLength(0); i++){
						int which = (int) (randy.NextDouble()*howMany);
						for (int j = 0; j < m.GetLength(1); j++){
							if (randy.NextDouble()*100 < missingPerc){
								m[i, j] = float.NaN;
							} else{
								m[i, j] = (float) randy.NextGaussian() + centers[which, j];
							}
						}
						col1[i] = new[]{"Group" + (which + 1)};
					}
					catColNames.Add("Grouping");
					catCols.Add(col1);
					break;
			}
			List<string> exprColumnNames = new List<string>();
			for (int i = 0; i < ncols; i++){
				exprColumnNames.Add("Column " + (i + 1));
			}
			mdata.Name = "Random matrix";
			mdata.ColumnNames = exprColumnNames;
			mdata.Values.Set(m);
			mdata.Quality.Set(new float[m.GetLength(0), m.GetLength(1)]);
			mdata.IsImputed.Set(new bool[m.GetLength(0), m.GetLength(1)]);
			mdata.SetAnnotationColumns(new List<string>(), new List<string[]>(), catColNames, catCols, new List<string>(),
				new List<double[]>(), new List<string>(), new List<double[][]>());
			mdata.Origin = "Random matrix";
			string[] names = new string[mdata.RowCount];
			for (int i = 0; i < names.Length; i++){
				names[i] = "Row " + (i + 1);
			}
			mdata.AddStringColumn("Name", "Name", names);
			string[][] grouping = new string[ncols][];
			for (int i = 0; i < ncols; i++){
				int ig = i*ngroups/ncols + 1;
				grouping[i] = new[]{"Group" + ig};
			}
			mdata.AddCategoryRow("Grouping", "Grouping", grouping);
		}

		public Parameters GetParameters(ref string errorString){
			Parameters oneNormalSubParams = new Parameters();
			Parameters twoNormalSubParams = new Parameters(new Parameter[]{new DoubleParam("Distance", 2)});
			Parameters manyNormalSubParams = new Parameters(new IntParam("How many", 3), new DoubleParam("Box size", 2));
			return new Parameters(new IntParam("Number of rows", 100), new IntParam("Number of columns", 15),
				new IntParam("Percentage of missing values", 0),
				new SingleChoiceWithSubParams("Mode"){
					Values = new[]{"One normal distribution", "Two normal distributions", "Many normal distributions"},
					SubParams = new[]{oneNormalSubParams, twoNormalSubParams, manyNormalSubParams},
					ParamNameWidth = 120,
					TotalWidth = 800
				}, new IntParam("Number of groups", 3),
				new BoolWithSubParams("Set seed"){
					Default = false,
					Help = "For a fixed seed the generated 'random' matrix will always be identical",
					SubParamsTrue = new Parameters(new Parameter[]{new IntParam("Seed", 0)})
				});
		}
	}
}