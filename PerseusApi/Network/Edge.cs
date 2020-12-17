using System;

namespace PerseusApi.Network{
	[Serializable]
	public sealed class Edge : Identifiable, IEdge{
		public INode Target{ get; }
		public INode Source{ get; }

		public Edge(INode source, INode target){
			Source = source;
			Target = target;
		}

		public object Clone(){
			throw new NotImplementedException();
		}

		public override string ToString(){
			return $"({Source}, {Target})";
		}
	}
}