using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public interface IAnnotationProvider
    {
        /// <summary>
        /// Annotation sources. Identifier <c>id</c> e.g. Uniprot, GeneID. Annotation <c>name</c> and <c>type</c> e.g. GOMF and Category.
        /// </summary>
        (string source, string id, (string name, AnnotType type)[] annotation)[] Sources { get; }

        /// <summary>
        /// Sources which failed to load due to e.g. parsing errors.
        /// </summary>
        string[] BadSources { get; }

        /// <summary>
        /// Read mappings from the selected source. Implemented by derived class.
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="fromColumn"></param>
        /// <param name="toColumns"></param>
        /// <returns></returns>
        IEnumerable<(string fromId, string[][] toIds)> ReadMappings(int sourceIndex, int fromColumn, int[] toColumns);
    }
}