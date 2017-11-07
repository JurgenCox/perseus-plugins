using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public interface IAnnotationProvider
    {
        /// <summary>
        /// Parse headers of all annotation files for available annotations.
        /// </summary>
        /// <param name="baseNames">Identifier names such as "Uniprot", "GeneID".</param>
        /// <param name="types">One Type per annotation.</param>
        /// <param name="files">Path of the annotation file corresponding to the <see cref="baseNames"/>.</param>
        /// <param name="badFiles">Annotation files which could not be parsed</param>
        /// <returns>List of annotations, such as <code>{"GO", "KEGG"}</code> per file.</returns>
        string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files, out List<string> badFiles);
    }
}