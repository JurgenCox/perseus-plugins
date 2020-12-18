using System;
using System.Collections.Generic;
using System.Linq;
using PerseusApi.Generic;

namespace PerseusApi.Network{
	public static class NetworkUtils{
		/// <summary>
		/// Find common edge annotation columns and their values.
		/// </summary>
		/// <param name="ndata"></param>
		/// <returns></returns>
		public static (string[] columns, string[][] values) CommonEdgeAnnotationColumns(
			this INetworkDataAnnColumns ndata){
			return ndata.CommonAnnotationColumns(network => network.EdgeTable);
		}

		/// <summary>
		/// Find common node annotation columns and their values.
		/// </summary>
		/// <param name="ndata"></param>
		/// <returns></returns>
		public static (string[] columns, string[][] values) CommonNodeAnnotationColumns(
			this INetworkDataAnnColumns ndata){
			return ndata.CommonAnnotationColumns(network => network.NodeTable);
		}

		private static (string[] columns, string[][] values) CommonAnnotationColumns(this INetworkDataAnnColumns ndata,
			Func<INetworkInfo, IDataWithAnnotationColumns> getTable){
			IDataWithAnnotationColumns first = getTable(ndata.First());
			List<string> seed = first.CategoryColumnNames;
			IEnumerable<List<string>> rest = ndata.Skip(1).Select(network => getTable(network).CategoryColumnNames);
			string[] columns = rest.Aggregate(seed.AsEnumerable(), Enumerable.Intersect).ToArray();
			string[][] values = columns.Select(name => {
				return ndata.SelectMany(network => {
					int col = getTable(network).CategoryColumnNames.FindIndex(name.Equals);
					return getTable(network).GetCategoryColumnValuesAt(col);
				}).Distinct().ToArray();
			}).ToArray();
			return (columns, values);
		}

		/// <summary>
		/// Collect attributes common to all networks. <code>ndata.Intersect(network => network.NodeTable.StringColumnNames)</code>
		/// </summary>
		/// <param name="ndata"></param>
		/// <param name="selector"></param>
		/// <returns></returns>
		public static T[] Intersect<T>(this INetworkDataAnnColumns ndata, Func<INetworkInfo, IEnumerable<T>> selector){
			return Aggregate(ndata.ToList(), selector, Enumerable.Intersect);
		}

		/// <summary>
		/// Collect attributes common to all networks. <code>ndata.Intersect(network => network.NodeTable.StringColumnNames)</code>
		/// </summary>
		/// <param name="ndata"></param>
		/// <param name="selector"></param>
		/// <returns></returns>
		public static TOut[] Intersect<TIn, TOut>(this ICollection<TIn> ndata, Func<TIn, IEnumerable<TOut>> selector){
			return Aggregate(ndata, selector, Enumerable.Intersect);
		}

		/// <summary>
		/// Collect attributes from all networks. <code>ndata.Union(network => network.NodeTable.StringColumnNames)</code>
		/// </summary>
		/// <param name="ndata"></param>
		/// <param name="selector"></param>
		/// <returns></returns>
		public static T[] Union<T>(this INetworkDataAnnColumns ndata, Func<INetworkInfo, IEnumerable<T>> selector){
			return Aggregate(ndata.ToList(), selector, Enumerable.Union);
		}

		private static TOut[] Aggregate<TIn, TOut>(this IEnumerable<TIn> ndata, Func<TIn, IEnumerable<TOut>> selector,
			Func<IEnumerable<TOut>, IEnumerable<TOut>, IEnumerable<TOut>> aggregator){
			return ndata.Select(selector).Aggregate(aggregator).ToArray();
		}
	}
}