using System.Collections.Generic;
using PerseusApi.Generic;
using PerseusApi.Utils;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public class ConfFolderAnnotationProvider : IAnnotationProvider
    {
        public string[][] GetAvailableAnnots(out string[] baseNames, out AnnotType[][] types, out string[] files, out List<string> badFiles)
        {
            return PerseusUtils.GetAvailableAnnots(out baseNames, out types, out files, out badFiles);
        }
    }
}