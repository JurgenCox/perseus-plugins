using System.Collections.Generic;
using PerseusApi.Generic;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public interface IAnnotationProvider
    {
        string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files, out List<string> badFiles);
    }
}