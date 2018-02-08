using System;

namespace PerseusApi.Network
{
    /// <summary>
    /// Identified by Guid. Used for graph, nodes and edges
    /// </summary>
    public interface IIdentifiable 
    {
        Guid Guid { get; }
    }

    /// <summary>
    /// Identified by Guid
    /// </summary>
    [Serializable]
    public abstract class Identifiable : IIdentifiable
    {
        /// <summary>
        /// Unique key
        /// </summary>
        public Guid Guid { get; protected set; }

        protected Identifiable(Guid guid)
        {
            Guid = guid;
        }

        protected Identifiable() : this(Guid.NewGuid()) { }

        public override bool Equals(object obj)
        {
            IIdentifiable other = obj as IIdentifiable;
            return other != null && other.Guid.Equals(Guid);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }
    }
}
