using System;
using System.Collections.Generic;
using System.Linq;

namespace PerseusApi.Network{
	[Serializable]
	public sealed class Node : Identifiable, INode{
		public List<IEdge> InEdges{ get; }
		public List<IEdge> OutEdges{ get; }

		public Node(Guid guid) : base(guid){
			InEdges = new List<IEdge>();
			OutEdges = new List<IEdge>();
		}

		public Node() : this(Guid.NewGuid()){ }

		public object Clone(){
			throw new NotImplementedException();
		}

		public IEnumerable<INode> Neighbors =>
			new[]{InEdges.Select(e => e.Source), OutEdges.Select(e => e.Target)}.SelectMany(x => x);

		public int InDegree => InEdges.Count;
		public int OutDegree => OutEdges.Count;

		public override string ToString(){
			return Guid.ToString().Split('-')[0];
		}
	}
}