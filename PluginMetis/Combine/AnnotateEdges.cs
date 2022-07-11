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
	/// <summary>
	/// Annotate edges should be a general purpose utility for mapping data from a matrix to a edge table.
	///
	/// Currently the only supported workflow is mapping phospho data on (Target, Modified residue) with an outer join.
	/// The outer join prevents the loss of data when it is mapped to the network. Methods, such as KSEA require
	/// estimating global mean/std from the entire data. If non-mapped data is discarded, these estimations are impossible.
	/// The outer join introduces complexity into the implementation, since novel nodes and edges have to be created.
	///
	/// Future workflows could include: mapping confidence values to edges etc.
	/// WRITE UNIT TESTS FIRST PluginNetwork.Test/Combine/AnnotationEdgesTest
	/// </summary>
	public class AnnotateEdges : INetworkMergeWithMatrix
	{
		public string Name => "Annotate edges";
		public string Description => "Transfer annotations from a matrix to the network.";
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

		public INetworkData ProcessData(INetworkData data, IMatrixData inMatrix, Parameters param,
			ref IData[] supplTables, ProcessInfo processInfo)
		{
			((int m1, int m2) first, (int m1, int m2)? second, bool outer, bool ignoreCase) matching =
				MatchingRowsByName.ParseMatchingColumns(param);
			((int[] copy, int combine) main, int[] text, (int[] copy, int combine) numeric, int[] category) annotation =
				MatchingRowsByName.ParseCopyParameters(param);
			if (annotation.main.copy.Length > 0 && annotation.numeric.copy.Length > 0 &&
				annotation.main.combine != annotation.numeric.combine)
			{
				processInfo.ErrString = "Please choose consistent values for combining main/numeric columns.";
				return null;
			}
			INetworkData ndata = (INetworkData)data.Clone();
			IMatrixData mdata = (IMatrixData)inMatrix.Clone();
			annotation = AnnotateNodes.ConvertMainToNumericAnnotation(annotation, mdata);
			IDataWithAnnotationColumns[] edgeTables = ndata.Select(network => network.EdgeTable).ToArray();
			IEnumerable<(int[][] indexMap, int[] unmappedRightIndices)> matchIndices =
			 	PerseusUtils.MatchDataWithAnnotationRows(annotation, edgeTables, mdata, matching);
			foreach ((int[][] indexMap, int[] unmappedIndices, INetworkInfo network) in matchIndices.Zip(ndata,
				(mI, network) => (mI.indexMap, unmappedIndices: mI.unmappedRightIndices, network)))
			{
				List<string> nodeColumn = network.NodeTable.GetStringColumn("Node").ToList();
				bool isMatchedToTargetColumn = matching.first.m1 < network.EdgeTable.StringColumnCount &&
											   network.EdgeTable.StringColumnNames[matching.first.m1].Equals("Target");
				if (isMatchedToTargetColumn && matching.outer)
				{
					List<int> nodesToAdd = new List<int>();
					INode unknownSource = network.Graph.AddNode();
					network.NodeIndex[unknownSource] = network.Graph.NumberOfNodes - 1;
					(string target, int row)[] targets = unmappedIndices
						.Select(i => mdata.StringColumns[matching.first.m2][i]).Select(target =>
							(target: target, row: nodeColumn.FindIndex(node => node.Equals(target)))).ToArray();
					Dictionary<int, INode> reverseNodeIndex =
						network.NodeIndex.ToDictionary(kv => kv.Value, kv => kv.Key);
					List<int> edgesToAdd = new List<int>();
					for (int i = 0; i < targets.Length; i++)
					{
						(string target, int index) = targets[i];
						if (index < 0)
						{
							index = network.Graph.NumberOfNodes;
							INode newNode = network.Graph.AddNode();
							network.NodeIndex[newNode] = index;
							reverseNodeIndex[index] = newNode;
							nodesToAdd.Add(i);
							targets[i] = (target, index);
						}
						INode targetNode = reverseNodeIndex[index];
						IEdge edge = network.Graph.AddEdge(unknownSource, targetNode);
						int edgeIndex = network.Graph.NumberOfEdges - 1;
						network.EdgeIndex[edge] = edgeIndex;
						edgesToAdd.Add(edgeIndex);
					}
					network.NodeTable.AddEmptyRows(nodesToAdd.Count + 1); // +1 for unknownSource node
					string[] nodeNames = network.NodeTable.GetStringColumn("Node");
					nodeNames[network.NodeIndex[unknownSource]] = "Unknown";
					foreach (int i in nodesToAdd)
					{
						(string target, int row) = targets[i];
						nodeNames[row] = target;
					}
					network.EdgeTable.AddEmptyRows(edgesToAdd.Count);
					// Fix-up source and target columns
					string[] sourceColumn = network.EdgeTable.GetStringColumn("Source");
					string[] targetColumn = network.EdgeTable.GetStringColumn("Target");
					foreach (KeyValuePair<IEdge, int> kv in network.EdgeIndex)
					{
						sourceColumn[kv.Value] = nodeNames[network.NodeIndex[kv.Key.Source]];
						targetColumn[kv.Value] = nodeNames[network.NodeIndex[kv.Key.Target]];
					}
					// Fix-up additional column if necessary
					if (matching.second.HasValue)
					{
						int col = matching.second.Value.m2;
						string[] secondColumn = col < mdata.StringColumnCount
							? mdata.StringColumns[col]
							: mdata.NumericColumns[col - mdata.StringColumnCount].Select(Convert.ToString).ToArray();
						int networkCol = matching.second.Value.m1;
						string[] networkColValues = networkCol < network.EdgeTable.StringColumnCount
							? network.EdgeTable.StringColumns[networkCol]
							: network.EdgeTable.NumericColumns[networkCol - network.EdgeTable.StringColumnCount]
								.Select(Convert.ToString).ToArray();
						for (int i = 0; i < unmappedIndices.Length; i++)
						{
							networkColValues[indexMap.Length + i] = secondColumn[unmappedIndices[i]];
						}
						if (networkCol < network.EdgeTable.StringColumnCount)
						{
							network.EdgeTable.StringColumns[networkCol] = networkColValues;
						}
						else
						{
							network.EdgeTable.NumericColumns[networkCol - network.EdgeTable.StringColumnCount] =
								networkColValues.Select(Convert.ToDouble).ToArray();
						}
					}
					int[][] extendedIndexMap = indexMap.Concat(unmappedIndices.Select(row => new[] { row })).ToArray();
					MatchingRowsByName.AddAnnotationColumns(network.EdgeTable, mdata, extendedIndexMap, annotation.text,
						annotation.numeric, annotation.category);
					if (param.GetParam<bool>("Add original row numbers").Value)
					{
						network.EdgeTable.AddMultiNumericColumn("Original row numbers",
							"Row numbers of the annotations in the matrix of origin. " +
							"Useful for de-duplication of data, e.g. for permuation schemes.",
							extendedIndexMap.Select(indices => indices.Select(i => Convert.ToDouble(i + 1)).ToArray())
								.ToArray());
					}
				}
				else if (!matching.outer)
				{
					MatchingRowsByName.AddAnnotationColumns(network.EdgeTable, mdata, indexMap, annotation.text,
						annotation.numeric, annotation.category);
					if (param.GetParam<bool>("Add original row numbers").Value)
					{
						network.EdgeTable.AddMultiNumericColumn("Original row numbers",
							"Row numbers of the annotations in the matrix of origin. " +
							"Useful for de-duplication of data, e.g. for permuation schemes.",
							indexMap.Select(indices => indices.Select(i => Convert.ToDouble(i + 1)).ToArray())
								.ToArray());
					}
				}
				else
				{
					processInfo.ErrString =
						"Annotating edges with join style 'outer' is currently only implemented for matches to the 'Target' column, " +
						"as used in the KSEA workflow";
					return null;
				}
			}
			return ndata;
		}

		public Parameters GetParameters(INetworkData ndata, IMatrixData inMatrix, ref string errString){
			Parameter[] matchParams = MatchingRowsByName.CreateMatchParameters(
				ndata.Intersect(network =>
					network.EdgeTable.StringColumnNames.Concat(network.EdgeTable.NumericColumnNames)).ToList(),
				inMatrix.StringColumnNames.Concat(inMatrix.NumericColumnNames).ToList());
			Parameter[] annotParams = MatchingRowsByName.CreateCopyParameters(inMatrix);
			return new Parameters(matchParams.Concat(new Parameter[]{
				new BoolParam("Add original row numbers"){
					Help = "If checked, a multi-numerical column will be added in which the " +
						   "original row indices of the mapped data are listed."
				},
			}).Concat(annotParams).ToArray());
		}
	}
}
