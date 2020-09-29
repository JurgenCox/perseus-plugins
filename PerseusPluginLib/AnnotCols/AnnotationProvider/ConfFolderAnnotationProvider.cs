using System.Collections.Generic;
using System.Linq;
using PerseusApi.Generic;
using PerseusApi.Utils;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public class ConfFolderAnnotationProvider : IAnnotationProvider
    {
        public (string source, string id, (string name, AnnotType type)[] annotation)[] Sources { get; }
        public string[] BadSources { get; }

        public ConfFolderAnnotationProvider()
        {
            var annots = PerseusUtils.GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types,
                out string[] files, out List<string> badFiles);
            BadSources = badFiles.ToArray();
            var sources = new List<(string, string, (string, AnnotType)[])>();
            for (int i = 0; i < files.Length; i++)
            {
                sources.Add((files[i], baseNames[i], annots[i].Zip(types[i], (name, type) => (name, type)).ToArray()));
            }
            Sources = sources.ToArray();
        }

        public IEnumerable<(string fromId, string[][] toIds)> ReadMappings(int sourceIndex, int fromColumn, int[] toColumns)
        {
            var file = Sources[sourceIndex].source;
            return AnnotationFormat.ReadMappings(file, fromColumn, toColumns);
        }

    }
}