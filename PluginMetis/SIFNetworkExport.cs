using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num.Matrix;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;
using PerseusPluginLib.Rearrange;
using PluginMetis.Combine;
using PerseusApi.Document;

namespace PluginMetis
{
	class SIFNetworkExport : INetworkToMatrix
	{
		public string Name => "SIF export for metabolic reactions";
		public float DisplayRank => 1;
		public bool IsActive => true;
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Export the metabolic reactions in the SIF format (directed) for upload and use in third party software.";
		public string Heading => "Metabolic networks";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => null;

		public int GetMaxThreads(Parameters parameters)
		{
			return 1;
		}


		public Parameters GetParameters(INetworkData ndata, ref string errString)
		{
			(string[] columns, string[][] values) = ndata.CommonNodeAnnotationColumns();
			if (columns.Length < 1)
			{
				errString = "Please add at least one common category column to all node tables for filtering";
				return null;
			}
			if (!ndata.Any())
			{
				errString = "Network collection does not contain any networks";
				return null;
			}
			return new Parameters(new SingleChoiceWithSubParams("Column", 0)
			{
				Values = columns
			});
		}

		public void ProcessData(INetworkData inData, IMatrixData outData, Parameters param, ref IData[] supplTables, ProcessInfo processInfo)
		{
			string errString = string.Empty;
			var pinfo = new ProcessInfo(new Settings(), s => { }, i => { }, 1);
			IData[] supplData = null;
			IMatrixData[] suppTable = null;
			IDocumentData[] documentData = null;
			RenameColumnMetabolomics renameColumn = new RenameColumnMetabolomics();
			AnnotateEdges annotateEdges = new AnnotateEdges();
			CombineCategoricalColumns combineColumns = new CombineCategoricalColumns();
			ReorderRemoveColumns removeColumn = new ReorderRemoveColumns();

			//clone input network
			INetworkData network = (INetworkData)inData.Clone();

			//create annotation in the new cloned network
			//(string[] networkColumns, string[][] networkColumnValues) = network.CommonEdgeAnnotationColumns();

			//use AnnotateEdges to match the source column of the edges to their type
			IMatrixData networkNodeTable = ToMatrixData(network.First().NodeTable, outData);
			Parameters annotationSourceParams = annotateEdges.GetParameters(network, networkNodeTable, ref errString);
			annotationSourceParams.GetParam<int>("Matching column in table 2").Value = 0;
			annotationSourceParams.GetParam<int[]>("Copy categorical columns").Value = new[] { 0 };
			INetworkData edgesSource = annotateEdges.ProcessData(network, networkNodeTable, annotationSourceParams, ref supplData, pinfo);

			//use AnnotateEdges to match the target column of the edges to their type
			Parameters annotationTargetParams = annotateEdges.GetParameters(edgesSource, networkNodeTable, ref errString);
			annotationTargetParams.GetParam<int>("Matching column in table 1").Value = 1;
			annotationTargetParams.GetParam<int>("Matching column in table 2").Value = 0;
			annotationTargetParams.GetParam<int[]>("Copy categorical columns").Value = new[] { 0 };
			INetworkData networkModifiers = annotateEdges.ProcessData(edgesSource, networkNodeTable, annotationTargetParams, ref supplData, pinfo);

			//format output matrix
			IDataWithAnnotationColumns table = networkModifiers.First().EdgeTable;
			IMatrixData matrix = ToMatrixData(table, outData);

			//combine columns
			Parameters combineColumnsParams = combineColumns.GetParameters(matrix, ref errString);
			combineColumnsParams.GetParam<int>("First column").Value = 0;
			combineColumnsParams.GetParam<int>("Second column").Value = 1;
			combineColumns.ProcessData(matrix, combineColumnsParams, ref suppTable, ref documentData, pinfo);

			//rename columns
			Parameters renameParameters = renameColumn.GetParameters(matrix, ref errString);

			renameParameters.GetParam<string>("Type_Type_").Value = "Relationship Type";

			renameColumn.ProcessData(matrix, renameParameters, ref suppTable, ref documentData, pinfo);

			//remove column
			Parameters removeColumnParams = removeColumn.GetParameters(matrix, ref errString);
			removeColumnParams.GetParam<int[]>("Categorical columns").Value = new[] { 2 };
			removeColumnParams.GetParam<int[]>("Text columns").Value = new[] { 0, 1 };
			removeColumn.ProcessData(matrix, removeColumnParams, ref suppTable, ref documentData, pinfo);

			//prepare out
			outData.CopyAnnotationColumnsFrom(matrix);
			outData.Values = new FloatMatrixIndexer(new float[matrix.RowCount, 0]);
		}


		public static IMatrixData ToMatrixData(IDataWithAnnotationColumns data, IData template)
		{
			return ToMatrixData(data, new int[] { }, template);
		}


		public static IMatrixData ToMatrixData(IDataWithAnnotationColumns data, int[] toMainIdx, IData template)
		{
			double[,] vals = new double[data.RowCount, toMainIdx.Length];
			List<string> names = new List<string>();
			for (int j = 0; j < toMainIdx.Length; j++)
			{
				int idx = toMainIdx[j];
				double[] col = data.NumericColumns[idx];
				names.Add(data.NumericColumnNames[idx]);
				for (int i = 0; i < data.RowCount; i++)
				{
					vals[i, j] = (float)col[i];
				}
			}
			IMatrixData mdata = CreateMatrixData(vals, names, template);
			for (int i = 0; i < data.NumericColumnCount; i++)
			{
				if (toMainIdx.Contains(i))
				{
					continue;
				}
				mdata.AddNumericColumn(data.NumericColumnNames[i], data.NumericColumnDescriptions[i], data.NumericColumns[i]);
			}
			for (int i = 0; i < data.StringColumnCount; i++)
			{
				mdata.AddStringColumn(data.StringColumnNames[i], data.StringColumnDescriptions[i], data.StringColumns[i]);
			}
			for (int i = 0; i < data.MultiNumericColumnCount; i++)
			{
				mdata.AddMultiNumericColumn(data.MultiNumericColumnNames[i], data.MultiNumericColumnDescriptions[i], data.MultiNumericColumns[i]);
			}
			for (int i = 0; i < data.CategoryColumnCount; i++)
			{
				mdata.AddCategoryColumn(data.CategoryColumnNames[i], data.CategoryColumnDescriptions[i], data.GetCategoryColumnAt(i));
			}
			return mdata;
		}


		public static IMatrixData CreateMatrixData(double[,] values, List<string> columnNames, IData template)
		{
			IMatrixData mdata = (IMatrixData)template.CreateNewInstance(DataType.Matrix);
			mdata.Values.Set(values);
			mdata.ColumnNames = columnNames;
			BoolMatrixIndexer imputed = new BoolMatrixIndexer();
			imputed.Init(mdata.RowCount, mdata.ColumnCount);
			mdata.IsImputed = imputed;
			return mdata;
		}


		public static ((int[] copy, int combine) main, int[] text, (int[] copy, int combine) numeric, int[] category)
			ConvertMainToNumericAnnotation(
				((int[] copy, int combine) main, int[] text, (int[] copy, int combine) numeric, int[] category)
					annotation, IMatrixData mdata)
		{
			int combine = annotation.numeric.combine;
			if (annotation.main.copy.Length > 0)
			{
				combine = annotation.main.combine;
			}
			List<int> copy = new List<int>(annotation.numeric.copy);
			foreach (int i in annotation.main.copy)
			{
				copy.Add(mdata.NumericColumnCount);
				mdata.AddNumericColumn(mdata.ColumnNames[i], mdata.ColumnDescriptions[i], mdata.Values.GetColumn(i).Unpack());
			}
			// avoid mutating tuples and rather create new ones
			return (annotation.main, annotation.text, (copy.ToArray(), combine), annotation.category);
		}
	}
}