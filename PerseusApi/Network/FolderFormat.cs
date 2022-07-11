﻿using System;
using System.Collections.Generic;
using System.IO;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;
namespace PerseusApi.Network{
	/// <summary>
	/// Default folder format for importing/exporting a network collection in Perseus without loss of information.
	///
	/// The networks.txt table contains general information on all the networks in the collection.
	/// Each network is represented by two tables, {guid}_nodes.txt and {guid}_edges.txt, which
	/// are stored in the standard perseus matrix format.
	/// </summary>
	public static class FolderFormat{
		/// <summary>
		/// Unsafe shortcut for obtaining a string column.
		/// </summary>
		private static string[] GetStringColumn(this IDataWithAnnotationColumns data, string colname){
			return data.StringColumns[data.StringColumnNames.FindIndex(col => col.ToLower().Equals(colname.ToLower()))];
		}
		/// <summary>
		/// Read the network into an instance of <see cref="INetworkData"/>.
		/// To create an empty network see <see cref="PerseusFactory.CreateNetworkData"/>.
		/// </summary>
		/// <param name="ndata">Contains the parsed network after the function was run</param>
		/// <param name="folder">Path to the directory where the network is stored</param>
		/// <param name="processInfo"></param>
		public static void Read(INetworkData ndata, string folder, ProcessInfo processInfo){
			ReadMatrixDataInto(ndata, Path.Combine(folder, "networks.txt"), processInfo);
			string[] guids = ndata.StringRows[ndata.StringRowNames.IndexOf("guid")];
			string[] names = ndata.StringRows[ndata.StringRowNames.IndexOf("name")];
			for (int i = 0; i < guids.Length; i++){
				Guid guid = Guid.Parse(guids[i]);
				IDataWithAnnotationColumns nodeTable = PerseusFactory.CreateDataWithAnnotationColumns();
				IDataWithAnnotationColumns edgeTable = PerseusFactory.CreateDataWithAnnotationColumns();
				ReadMatrixDataInto(nodeTable, Path.Combine(folder, $"{guid}_nodes.txt"), processInfo);
				ReadMatrixDataInto(edgeTable, Path.Combine(folder, $"{guid}_edges.txt"), processInfo);
				IGraph graph = PerseusFactory.CreateGraph();
				Dictionary<INode, int> nodeIndex = new Dictionary<INode, int>();
				Dictionary<string, INode> nameToNode = new Dictionary<string, INode>();
				string[] nodeColumn = nodeTable.GetStringColumn("node");
				for (int row = 0; row < nodeTable.RowCount; row++){
					INode node = graph.AddNode();
					nodeIndex[node] = row;
					nameToNode[nodeColumn[row]] = node;
				}
				string[] sourceColumn = edgeTable.GetStringColumn("source");
				string[] targetColumn = edgeTable.GetStringColumn("target");
				Dictionary<IEdge, int> edgeIndex = new Dictionary<IEdge, int>();
				for (int row = 0; row < edgeTable.RowCount; row++){
					INode source = nameToNode[sourceColumn[row]];
					INode target = nameToNode[targetColumn[row]];
					IEdge edge = graph.AddEdge(source, target);
					edgeIndex[edge] = row;
				}
				INetworkInfo network = PerseusFactory.CreateNetworkInfo(graph, nodeTable, nodeIndex, edgeTable,
					edgeIndex, names[i], guid);
				ndata.AddNetworks(network);
			}
			// overwrite graph table, which was modified by `ndata.AddNetwork(network)`
			ReadMatrixDataInto(ndata, Path.Combine(folder, "networks.txt"), processInfo);
		}
		private static void ReadMatrixDataInto(IDataWithAnnotationRows data, string file, ProcessInfo processInfo) {
			IMatrixData mdata = PerseusFactory.CreateMatrixData();
			PerseusUtils.ReadMatrixFromFile(mdata, processInfo, file, '\t');
			data.CopyAnnotationRowsFromColumns(mdata);
		}
		private static void ReadMatrixDataInto(IDataWithAnnotationColumns data, string file, ProcessInfo processInfo) {
			IMatrixData mdata = PerseusFactory.CreateMatrixData();
			PerseusUtils.ReadMatrixFromFile(mdata, processInfo, file, '\t');
			data.CopyAnnotationColumnsFrom(mdata);
		}
		/// <summary>
		/// Write the fiven network to the specified folder.
		/// </summary>
		public static void Write(INetworkData ndata, string folder){
			if (!Directory.Exists(folder)){
				Directory.CreateDirectory(folder);
			}
			PerseusUtils.WriteDataWithAnnotationRows(ndata, Path.Combine(folder, "networks.txt"));
			foreach (INetworkInfo network in ndata){
				PerseusUtils.WriteDataWithAnnotationColumns(network.NodeTable,
					Path.Combine(folder, $"{network.Guid}_nodes.txt"));
				PerseusUtils.WriteDataWithAnnotationColumns(network.EdgeTable,
					Path.Combine(folder, $"{network.Guid}_edges.txt"));
			}
		}
	}
}