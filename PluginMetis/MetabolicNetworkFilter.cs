using System.Collections.Generic;
using System.Linq;
using BaseLibS.Calc;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Network;
using PerseusApi.Utils;

namespace PluginMetis
{
	public class MetabolicNetworksFilter : INetworkProcessingAnnColumns
	{
		public string Name => "Filter for metabolic reactions";
		public float DisplayRank => 0;
		public bool IsActive => true;
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;
		public string Description => "Filters for metabolic reactions with node/s passing the specified filter/s.";
		public string Heading => "Metabolic networks";
		public string HelpOutput => "";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 1;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => null;

		public int GetMaxThreads(Parameters parameters)
		{
			return 1;
		}

		public void ProcessData(INetworkDataAnnColumns ndata, Parameters param, ref IData[] supplTables,
			ProcessInfo processInfo)
		{
			Relation[] relations =
				PerseusUtils.GetRelationsNumFilter(param, out string errString, out int[] colInds, out bool and);
			if (errString != null)
			{
				processInfo.ErrString = errString;
				return;
			}
			string[] columns = ndata.Intersect(network => network.NodeTable.NumericColumnNames).SubArray(colInds);
			foreach (INetworkInfo network in ndata)
			{
				double[][] numericalRows = network.NodeTable.NumericalRows(columns);
				IEnumerable<INode> filteredNodes = network.Graph.Where(node1 =>
					PerseusUtils.IsValidRowNumFilter(numericalRows[network.NodeIndex[node1]], relations, and));
				IEnumerable<INode> filteredNodeNeighbors = filteredNodes.SelectMany(node2 => node2.Neighbors);
				IEnumerable<INode> goodNodes = filteredNodes.Concat(filteredNodeNeighbors);
				INode[] badNodes = network.Graph.Except(goodNodes).ToArray();
				network.RemoveNodes(badNodes);
			}
		}

		public Parameters GetParameters(INetworkDataAnnColumns ndata, ref string errString)
		{
			string[] columns = ndata.Intersect(network => network.NodeTable.NumericColumnNames);
			if (columns.Length < 1)
			{
				errString = "Please add at least one common numerical column to all node tables for filtering";
				return null;
			}
			if (ndata.Select(network => network.Name).Distinct().Count() >= ndata.Count())
				return new Parameters(PerseusUtils.GetNumFilterParams(columns).ToArray());
			errString = "Please make sure that all networks have distinct names.";
			return null;
		}
	}
}

