using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;
using PerseusApi.Utils;
using PerseusPluginLib.Join;
using PerseusPluginLib.Utils;

namespace PluginMetis.Combine
{
	public class AnnotateNodes : PerseusApi.Network.INetworkMergeWithMatrixAnnColumns
	{
		public string Name => "Annotate nodes";
		public string Description => "Transfer node annotations from a matrix to the network.";
		public float DisplayRank => 1;
		public bool IsActive => false;

		public int GetMaxThreads(Parameters parameters)
		{
			return 1;
		}

		public bool HasButton => false;
		public string Url => null;
		public Bitmap2 DisplayImage => null;
		public string Heading => "Annotate";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public INetworkDataAnnColumns ProcessData(INetworkDataAnnColumns data, IMatrixData inMatrix, Parameters param,
			ref IData[] supplTables, ProcessInfo processInfo)
		{
			((int m1, int m2) first, (int m1, int m2)? second, bool outer, bool ignoreCase) matching = MatchingRowsByName.ParseMatchingColumns(param);
			if (matching.outer && !data.First().NodeTable.StringColumnNames[matching.first.m1].Equals("Node"))
			{
				processInfo.ErrString = "Outer join only implemented for joins on the 'Node' column of the network.";
				return null;
			}
			((int[] copy, int combine) main, int[] text, (int[] copy, int combine) numeric, int[] category) annotation = MatchingRowsByName.ParseCopyParameters(param);
			if (annotation.main.copy.Length > 0 && annotation.numeric.copy.Length > 0 &&
				annotation.main.combine != annotation.numeric.combine)
			{
				processInfo.ErrString = "Please choose consistent values for combining main/numeric columns.";
				return null;
			}
			bool addOriginalRowNumbers = param.GetParam<bool>("Add original row numbers").Value;
			INetworkDataAnnColumns ndata = (INetworkDataAnnColumns)data.Clone();
			IMatrixData mdata = (IMatrixData)inMatrix.Clone();
			annotation = ConvertMainToNumericAnnotation(annotation, mdata);
			IDataWithAnnotationColumns[] nodeTables = ndata.Select(network => network.NodeTable).ToArray();
			IEnumerable<(int[][] indexMap, int[] unmappedRightIndices)> matchIndices = PerseusUtils.MatchDataWithAnnotationRows(annotation, nodeTables, mdata, matching);
			foreach ((int[][] indexMap, int[] unmappedIndices, INetworkInfo network) in matchIndices.Zip(ndata,
				(mI, network) => (mI.indexMap, unmappedIndices: mI.unmappedRightIndices, network)))
			{
				int[][] localIndexMap = indexMap; // cannot directly change loop variable indexMap
				if (matching.outer)
				{
					localIndexMap = UpdateNetworkAndIndexMap(network, unmappedIndices,
						(matching.first, matching.second), mdata, localIndexMap);
				}
				MatchingRowsByName.AddAnnotationColumns(network.NodeTable, mdata, localIndexMap, annotation.text,
					annotation.numeric, annotation.category);
				if (addOriginalRowNumbers)
				{
					network.NodeTable.AddMultiNumericColumn("Original row numbers",
						$"Row numbers in {mdata.Name} that were mapped here",
						localIndexMap.Select(indices => indices.Select(i => (double)i).ToArray()).ToArray());
				}
			}
			return ndata;
		}

		/// <summary>
		/// Add the missing nodes to the network to enable the outer join.
		/// Manually copy the values of the 'matching' columns to the node table.
		/// Returns an extended version of the index map that includes the newly added nodes.
		/// </summary>
		private int[][] UpdateNetworkAndIndexMap(INetworkInfo network, int[] unmappedIndices,
			((int m1, int m2) first, (int m1, int m2)? second) matching, IMatrixData mdata, int[][] localIndexMap)
		{
			int baseIndex = network.NodeTable.RowCount;
			network.NodeTable.AddEmptyRows(unmappedIndices.Length);
			string[] firstNetwork = GetStringOrNumericColumn(network.NodeTable, matching.first.m1);
			string[] firstMatrix = GetStringOrNumericColumn(mdata, matching.first.m2);
			string[] secondNetwork = matching.second.HasValue
				? GetStringOrNumericColumn(network.NodeTable, matching.second.Value.m1)
				: null;
			string[] secondMatrix = matching.second.HasValue
				? GetStringOrNumericColumn(network.NodeTable, matching.second.Value.m2)
				: null;
			for (int i = 0; i < unmappedIndices.Length; i++)
			{
				INode node = network.Graph.AddNode();
				network.NodeIndex[node] = baseIndex + i;
				firstNetwork[baseIndex + i] = firstMatrix[unmappedIndices[i]];
				if (matching.second.HasValue && secondNetwork != null && secondMatrix != null)
				{
					secondNetwork[baseIndex + i] = secondMatrix[unmappedIndices[i]];
				}
			}
			SetStringOrNumericColumn(network.NodeTable, matching.first.m1, firstNetwork);
			if (matching.second.HasValue)
			{
				SetStringOrNumericColumn(network.NodeTable, matching.second.Value.m1, secondNetwork);
			}
			localIndexMap = localIndexMap.Concat(unmappedIndices.Select(i => new[] { i })).ToArray();
			return localIndexMap;
		}

		public string[] GetStringOrNumericColumn(IDataWithAnnotationColumns data, int index)
		{
			return index < data.StringColumnCount
				? data.StringColumns[index]
				: data.NumericColumns[index - data.StringColumnCount].Select(Convert.ToString).ToArray();
		}

		public void SetStringOrNumericColumn(IDataWithAnnotationColumns data, int index, string[] values)
		{
			if (index < data.StringColumnCount)
			{
				data.StringColumns[index] = values;
			}
			else
			{
				data.NumericColumns[index - data.StringColumnCount] = values.Select(Convert.ToDouble).ToArray();
			}
		}

		/// <summary>
		/// Convert main columns to numeric columns and return updated annotations
		/// </summary>
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
				mdata.AddNumericColumn(mdata.ColumnNames[i], mdata.ColumnDescriptions[i],
					mdata.Values.GetColumn(i).Unpack());
			}
			// avoid mutating tuples and rather create new ones
			return (annotation.main, annotation.text, (copy.ToArray(), combine), annotation.category);
		}

		public Parameters GetParameters(INetworkDataAnnColumns ndata, IMatrixData inMatrix, ref string errString)
		{
			if (!ndata.Any())
			{
				errString = "Network collection does not contain any networks";
				return null;
			}
			Parameter[] matchParams = MatchingRowsByName.CreateMatchParameters(
				ndata.Intersect(network =>
					network.NodeTable.StringColumnNames.Concat(network.NodeTable.NumericColumnNames)).ToList(),
				inMatrix.StringColumnNames.Concat(inMatrix.NumericColumnNames).ToList());
			Parameter[] annotParams = MatchingRowsByName.CreateCopyParameters(inMatrix);
			return new Parameters(matchParams
				.Concat(new[]{
					new BoolParam("Add original row numbers"){
						Help = "If checked, a multi-numerical column will be added in which the " +
							   "original row indices of the mapped data are listed."
					}
				}).Concat(annotParams).ToArray());
		}
	}
}

