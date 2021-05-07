using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;
using PerseusApi.Utils;
using PerseusPluginLib.Join;
using PerseusPluginLib.Utils;

namespace PluginMetis
{
	public class MetabolicNetwork
	{
		//Network with Reactions is defined: akes the values present in the column selected by the user as Reaction Column
		//Then the values are filtered based on the values that are already present in the Category Column
		public static INetworkDataAnnColumns ReactionsNetwork(INetworkDataAnnColumns inData,
			ParameterWithSubParams<int> netReactionscolParam, int[] netReactionsselValues)
		{
			INetworkDataAnnColumns networkReactions = (INetworkDataAnnColumns)inData.Clone();
			(string[] netReactionscols, string[][] netReactionscolValues) = networkReactions.CommonNodeAnnotationColumns();

			string netReactionscol = netReactionscols[netReactionscolParam.Value];
			string[] netReactionsvalues = netReactionscolValues[netReactionscolParam.Value];


			netReactionsselValues = Enumerable.Range(0, netReactionsvalues.Length).Except(netReactionsselValues).ToArray();

			HashSet<string> networkReactionsgoodValues = ValuesSelected(netReactionsvalues, netReactionsselValues);

			foreach (INetworkInfo network in networkReactions)
			{
				string[][] categories = network.NodeTable.GetCategoryColumn(netReactionscol);
				IEnumerable<INode> badNodes = network.Graph.Where(node => {
					int row = network.NodeIndex[node];
					return categories[row].All(cat => networkReactionsgoodValues.Contains(cat));
				});
				network.RemoveNodes(badNodes.ToArray());
			}
			return networkReactions;
		}

		//Values selected are taken as HashSet
		public static HashSet<string> ValuesSelected(string[] netReactionsvalues, int[] netReactionsselValues)
		{
			HashSet<string> networkReactionsgoodValues = netReactionsvalues.SubArray(netReactionsselValues).ToHashSet();
			return networkReactionsgoodValues;
		}


		public static INetworkDataAnnColumns ProductsNetwork(INetworkDataAnnColumns inData, ParameterWithSubParams<int> ReactProdReactionscolParam,
			int[] networkReactantsReactionsselectedValues, int[] ProdReactionsselectedValues, int[] netReactionsselValues, string[] netReactionsvalues)
		{
			INetworkDataAnnColumns ReactProdReactions = (INetworkDataAnnColumns)inData.Clone();
			(string[] ReactProdReactionscolumns, string[][] ReactProdReactionscolumnValues) = ReactProdReactions.CommonNodeAnnotationColumns();

			string ReactProdReactionscolumn = ReactProdReactionscolumns[ReactProdReactionscolParam.Value];
			string[] ReactProdReactionsvalues = ReactProdReactionscolumnValues[ReactProdReactionscolParam.Value];
			int[] ReactProdReactionsselectedValues = (networkReactantsReactionsselectedValues.Concat(ProdReactionsselectedValues)).ToArray();


			ReactProdReactionsselectedValues = Enumerable.Range(0, ReactProdReactionsvalues.Length).Except(ReactProdReactionsselectedValues).ToArray();
			HashSet<string> ReactProdReactionsgoodValues = ReactProdReactionsvalues.SubArray(ReactProdReactionsselectedValues).ToHashSet();

			HashSet<string> intersectReactantsProductsReactions = ValuesSelected(netReactionsvalues, netReactionsselValues).Intersect(ReactProdReactionsgoodValues).ToHashSet();


			foreach (INetworkInfo network in ReactProdReactions)
			{
				string[][] categories = network.NodeTable.GetCategoryColumn(ReactProdReactionscolumn);
				IEnumerable<INode> badNodes = network.Graph.Where(node => {
					int row = network.NodeIndex[node];
					return categories[row].All(cat => intersectReactantsProductsReactions.Contains(cat));
				});
				network.RemoveNodes(badNodes.ToArray());
			}

			return ReactProdReactions;
		}


		//Network with Modifiers is defined: takes the values present in the column selected by the user as Reaction Column
		//Then the values are filtered based on the values that are already present in the Category Column
		public static INetworkDataAnnColumns ModifiersNetwork(INetworkDataAnnColumns inData, ParameterWithSubParams<int> netModifiersReactionscolParam,
			int[] netModifiersReactionsselectedValues, int[] netReactionsselValues, string[] netReactionsvalues
			)
		{
			INetworkDataAnnColumns netModifiersReactions = (INetworkDataAnnColumns)inData.Clone();
			(string[] netModifiersReactionscols, string[][] netModifiersReactionscolValues) = netModifiersReactions.CommonNodeAnnotationColumns();

			string netModifiersReactionscol = netModifiersReactionscols[netModifiersReactionscolParam.Value];
			string[] netModifiersReactionsvalues = netModifiersReactionscolValues[netModifiersReactionscolParam.Value];
			netModifiersReactionsselectedValues = Enumerable.Range(0, netModifiersReactionsvalues.Length).Except(netModifiersReactionsselectedValues).ToArray();
			HashSet<string> netModifiersReactionsgoodValues = netModifiersReactionsvalues.SubArray(netModifiersReactionsselectedValues).ToHashSet();

			HashSet<string> intersectModifiersReactions = ValuesSelected(netReactionsvalues, netReactionsselValues).Intersect(netModifiersReactionsgoodValues).ToHashSet();

			foreach (INetworkInfo network in netModifiersReactions)
			{
				string[][] categories = network.NodeTable.GetCategoryColumn(netModifiersReactionscol);
				IEnumerable<INode> badNodes = network.Graph.Where(node => {
					int row = network.NodeIndex[node];
					return categories[row].All(cat => intersectModifiersReactions.Contains(cat));
				});
				network.RemoveNodes(badNodes.ToArray());
			}
			return netModifiersReactions;
		}

		//Match of the columns given as input (for example reactions vs products) so that to generate 
		//a new updated networks and relative map indexed
		public static void SetNetworkAndIndexMap(Parameters productAnnotationParameters, INetworkDataAnnColumns ndataReactions, IMatrixData mdataProductsReactions)
		{
			((int m1, int m2) first, (int m1, int m2)? second, bool outer, bool ignoreCase) productMatching = MatchingRowsByName.ParseMatchingColumns(productAnnotationParameters);
			((int[] copy, int combine) main, int[] text, (int[] copy, int combine) numeric, int[] category) productAnnotation = MatchingRowsByName.ParseCopyParameters(productAnnotationParameters);
			bool productAddOriginalRowNumbers = productAnnotationParameters.GetParam<bool>("Add original row numbers").Value;
			productAnnotation = MetabolicReactionsToMatrix.ConvertMainToNumericAnnotation(productAnnotation, mdataProductsReactions);
			IDataWithAnnotationColumns[] productNodeTables = ndataReactions.Select(network => network.NodeTable).ToArray();
			IEnumerable<(int[][] indexMap, int[] unmappedRightIndices)> productMatchIndices = PerseusUtils.MatchDataWithAnnotationRows(productAnnotation, productNodeTables, mdataProductsReactions, productMatching);
			foreach ((int[][] indexMap, int[] unmappedIndices, INetworkInfo network) in productMatchIndices.Zip(ndataReactions, (mI, network) => (mI.indexMap, unmappedIndices: mI.unmappedRightIndices, network)))
			{
				int[][] localIndexMap = indexMap; // cannot directly change loop variable indexMap
				if (productMatching.outer)
				{
					localIndexMap = UpdateNetworkAndIndexMap(network, unmappedIndices, (productMatching.first, productMatching.second), mdataProductsReactions, localIndexMap);
				}
				MatchingRowsByName.AddAnnotationColumns(network.NodeTable, mdataProductsReactions, localIndexMap, productAnnotation.text, productAnnotation.numeric, productAnnotation.category);
				if (productAddOriginalRowNumbers)
				{
					network.NodeTable.AddMultiNumericColumn("Original row numbers", $"Row numbers in {mdataProductsReactions.Name} that were mapped here", localIndexMap.Select(indices => indices.Select(i => (double)i).ToArray()).ToArray());
				}
			}
		}


		//Common targets are detected based on the matrixData given as input
		public static void CommonTargets(IMatrixData mdata, Parameters renameTargetReactantParameters, ProcessInfo processInfo)
		{
			HashSet<string> reactantTaken = new HashSet<string>();
			reactantTaken = new HashSet<string>();
			List<string> reactantStringColumnNames = new List<string>();
			for (int i = 0; i < mdata.StringColumnCount; i++)
			{
				string newName = renameTargetReactantParameters
					.GetParam<string>(mdata.StringColumnNames[i]).Value;
				if (reactantTaken.Contains(newName))
				{
					processInfo.ErrString = "Name " + newName + " is contained multiple times";
					return;
				}
				reactantTaken.Add(newName);
				reactantStringColumnNames.Add(newName);
			}
			mdata.StringColumnNames = reactantStringColumnNames;
		}

		public static int[][] UpdateNetworkAndIndexMap(INetworkInfo network, int[] unmappedIndices,
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

		public static void SetStringOrNumericColumn(IDataWithAnnotationColumns data, int index, string[] values)
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

		public static string[] GetStringOrNumericColumn(IDataWithAnnotationColumns data, int index)
		{
			return index < data.StringColumnCount
				? data.StringColumns[index]
				: data.NumericColumns[index - data.StringColumnCount].Select(Convert.ToString).ToArray();
		}

	}
}

