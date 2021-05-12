using System.Collections.Generic;
using System.Linq;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Num.Matrix;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Network;
using PerseusPluginLib.Rearrange;
using PluginMetis.Combine;

namespace PluginMetis
{
	public class MetabolicReactionsToMatrix : INetworkToMatrixAnnColumns
	{
		public string Name => "Metabolic reactions to matrix";
		public float DisplayRank => 0;
		public bool IsActive => true;
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Extract metabolic reactions within the network to a separate matrix in the workflow.";
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


		public Parameters GetParameters(INetworkDataAnnColumns ndata, ref string errString)
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
				Values = columns,
				SubParams = values.Select(value => new Parameters(new MultiChoiceParam("Reactions") { Values = value },
					new MultiChoiceParam("Modifiers") { Values = value },
					new MultiChoiceParam("Reactants") { Values = value },
					new MultiChoiceParam("Products") { Values = value })).ToArray()
			});
		}

		public void ProcessData(INetworkDataAnnColumns inData, IMatrixData outData, Parameters param, ref IData[] supplTables, ProcessInfo processInfo)
		{
			RenameColumnMetabolomics renameColumn = new RenameColumnMetabolomics();
			AnnotateNodes annotateReactions = new AnnotateNodes();
			var pinfo = new ProcessInfo(new Settings(), s => { }, i => { }, 1);
			IMatrixData[] suppT = null;
			IDocumentData[] documentData = null;
			string errString = string.Empty;

			//filter network to get reactions
			INetworkDataAnnColumns networkReactions = (INetworkDataAnnColumns)inData.Clone();

			//create annotation in the new cloned network
			(string[] networkColumns, string[][] networkColumnValues) = networkReactions.CommonNodeAnnotationColumns();

			//take network category column selected by the user
			ParameterWithSubParams<int> networkParam = param.GetParamWithSubParams<int>("Column");
			string networkColumn = networkColumns[networkParam.Value];
			string[] networkValue = networkColumnValues[networkParam.Value];

			//take categories in the category column of the network which are designated to be reactions by the user
			int[] networkReactionsselectedValues = networkParam.GetSubParameters().GetParam<int[]>("Reactions").Value;
			networkReactionsselectedValues = Enumerable.Range(0, networkValue.Length).Except(networkReactionsselectedValues).ToArray();
			string[] networkReactionsgoodValues = networkValue.SubArray(networkReactionsselectedValues);

			//considering the categories selected by the user to be "Reactions", filter the network 
			foreach (INetworkInfo network in networkReactions)
			{
				string[][] categories = network.NodeTable.GetCategoryColumn(networkColumn);
				IEnumerable<INode> badNodes = network.Graph.Where(node => {
					int row = network.NodeIndex[node];
					return categories[row].All(cat => networkReactionsgoodValues.Contains(cat));
				});
				network.RemoveNodes(badNodes.ToArray());
			}

			//filter network to get reactions and modifiers
			INetworkDataAnnColumns networkModifiersReactions = (INetworkDataAnnColumns)inData.Clone();

			//create annotation in the new cloned network
			(string[] networkModifiersReactionscolumns, string[][] networkModifiersReactionscolumnValues) = networkModifiersReactions.CommonNodeAnnotationColumns();

			//take categories in the category column of the network which are designated to be modifiers by the user
			int[] networkModifiersReactionsselectedValues = networkParam.GetSubParameters().GetParam<int[]>("Modifiers").Value;
			networkModifiersReactionsselectedValues = Enumerable.Range(0, networkValue.Length).Except(networkModifiersReactionsselectedValues).ToArray();
			string[] networkModifiersReactionsgoodValues = networkValue.SubArray(networkModifiersReactionsselectedValues);

			//intersection of modifiers vs reactions
			var intersectModifiersReactions = networkReactionsgoodValues.Intersect(networkModifiersReactionsgoodValues);

			//considering the categories selected by the user to be "modifiers", filter the network 
			foreach (INetworkInfo network in networkModifiersReactions)
			{
				string[][] categories = network.NodeTable.GetCategoryColumn(networkColumn);
				IEnumerable<INode> badNodes = network.Graph.Where(node => {
					int row = network.NodeIndex[node];
					return categories[row].All(cat => intersectModifiersReactions.Contains(cat));
				});
				network.RemoveNodes(badNodes.ToArray());
			}

			//filter nodes to get reactions, reactants and products
			INetworkDataAnnColumns networkReactantsProductsReactions = (INetworkDataAnnColumns)inData.Clone();

			//create annotation in the new cloned network
			(string[] networkReactantsProductsReactionscolumns, string[][] networkReactantsProductsReactionscolumnValues) = networkReactantsProductsReactions.CommonNodeAnnotationColumns();

			//take categories in the category column of the network which are designated to be modifiers by the user
			int[] networkReactantsReactionsselectedValues = networkParam.GetSubParameters().GetParam<int[]>("Reactants").Value;
			int[] networkProductsReactionsselectedValues = networkParam.GetSubParameters().GetParam<int[]>("Products").Value;
			int[] networkReactantsProductsReactionsselectedValues = (networkReactantsReactionsselectedValues.Concat(networkProductsReactionsselectedValues)).ToArray();
			networkReactantsProductsReactionsselectedValues = Enumerable.Range(0, networkValue.Length).Except(networkReactantsProductsReactionsselectedValues).ToArray();
			string[] networkReactantsProductsReactionsgoodValues = networkValue.SubArray(networkReactantsProductsReactionsselectedValues);

			//intersection of reactants and products vs reactions
			var intersectReactantsProductsReactions = networkReactionsgoodValues.Intersect(networkReactantsProductsReactionsgoodValues);

			//considering the categories selected by the user to be "reactants" and "products", filter the network 
			foreach (INetworkInfo network in networkReactantsProductsReactions)
			{
				string[][] categories = network.NodeTable.GetCategoryColumn(networkColumn);
				IEnumerable<INode> badNodes = network.Graph.Where(node => {
					int row = network.NodeIndex[node];
					return categories[row].All(cat => intersectReactantsProductsReactions.Contains(cat));
				});
				network.RemoveNodes(badNodes.ToArray());
			}

			//annotate reactions with modifiers
			INetworkDataAnnColumns ndataReactions = (INetworkDataAnnColumns)networkReactions.Clone();

			//get the edge table which only has reactions and modifiers created above according to the user choice
			IMatrixData mdataModifiersReactions = ToMatrixData(networkModifiersReactions.First().EdgeTable, outData);

			//rename columns
			Parameters renameTargetModifierParameters = renameColumn.GetParameters(mdataModifiersReactions, ref errString);
			renameTargetModifierParameters.GetParam<string>("Target").Value = "Modifiers";
			renameColumn.ProcessData(mdataModifiersReactions, renameTargetModifierParameters, ref suppT, ref documentData, pinfo);

			//use AnnotateNodes to match the modifiers to each reaction between node table of the network which only had reaction nodes and the edge table of the network with reactions and modifiers
			Parameters modifierAnnotationParameters = annotateReactions.GetParameters(ndataReactions, mdataModifiersReactions, ref errString);
			modifierAnnotationParameters.GetParam<int>("Matching column in table 2").Value = 0;
			modifierAnnotationParameters.GetParam<int[]>("Copy text columns").Value = new[] { 1 };
			IData[] supplData = null;
			INetworkDataAnnColumns networkModifiers = annotateReactions.ProcessData(ndataReactions, mdataModifiersReactions, modifierAnnotationParameters, ref supplData, pinfo);

			//get the edge table which only has the reactions and reactants created above according to the user choice
			IMatrixData mdataReactantsReactions = ToMatrixData(networkReactantsProductsReactions.First().EdgeTable, outData);

			//rename columns
			Parameters renameTargetReactantParameters = renameColumn.GetParameters(mdataReactantsReactions, ref errString);
			renameTargetReactantParameters.GetParam<string>("Source").Value = "Reactants";
			renameColumn.ProcessData(mdataReactantsReactions, renameTargetReactantParameters, ref suppT, ref documentData, pinfo);

			//use AnnotateNodes to match the modifiers to each reaction between node table of the network which only had reaction nodes and the edge table of the network with reactions and reactants
			Parameters reactantAnnotationParameters = annotateReactions.GetParameters(networkModifiers, mdataReactantsReactions, ref errString);
			reactantAnnotationParameters.GetParam<int>("Matching column in table 2").Value = 1;
			reactantAnnotationParameters.GetParam<int[]>("Copy text columns").Value = new[] { 0 };
			INetworkDataAnnColumns networkModifiersReactants = annotateReactions.ProcessData(networkModifiers, mdataReactantsReactions, reactantAnnotationParameters, ref supplData, pinfo);

			//annotate reactions with products
			//get the edge table which only has the reactions and products created above according to the user choice
			IMatrixData mdataProductsReactions = ToMatrixData(networkReactantsProductsReactions.First().EdgeTable, outData);
			Parameters renameTargetProductParameters = renameColumn.GetParameters(mdataProductsReactions, ref errString);
			renameTargetProductParameters.GetParam<string>("Target").Value = "Products";
			renameColumn.ProcessData(mdataProductsReactions, renameTargetProductParameters, ref suppT, ref documentData, pinfo);

			//use AnnotateNodes to match the modifiers to each reaction between node table of the network which only had reaction nodes and the edge table of the network with reactions and products
			Parameters productAnnotationParameters = annotateReactions.GetParameters(networkModifiersReactants, mdataProductsReactions, ref errString);
			productAnnotationParameters.GetParam<int>("Matching column in table 2").Value = 0;
			productAnnotationParameters.GetParam<int[]>("Copy text columns").Value = new[] { 1 };
			INetworkDataAnnColumns networkModifiersReactantsProducts = annotateReactions.ProcessData(networkModifiersReactants, mdataProductsReactions, productAnnotationParameters, ref supplData, pinfo);

			//output is a matrix in which each row has a reaction, a modifier, a product and a reactant column
			IDataWithAnnotationColumns table = networkModifiersReactantsProducts.First().NodeTable;
			outData.CopyAnnotationColumnsFrom(table);
			outData.Values = new FloatMatrixIndexer(new float[ndataReactions.First().NodeTable.RowCount, 0]);
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
			ConvertMainToNumericAnnotation(((int[] copy, int combine) main, int[] text, (int[] copy, int combine) numeric, int[] category) annotation, IMatrixData mdata)
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
