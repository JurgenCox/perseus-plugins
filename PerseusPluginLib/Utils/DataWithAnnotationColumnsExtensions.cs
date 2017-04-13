using System.Linq;
using BaseLibS.Num;
using BaseLibS.Util;
using PerseusApi.Generic;

namespace PerseusPluginLib.Utils
{
    public static class DataWithAnnotationColumnsExtensions
    {
        public static void UniqueValues(this IDataWithAnnotationColumns mdata, int[] stringCols)
        {
            foreach (string[] col in stringCols.Select(stringCol => mdata.StringColumns[stringCol]))
            {
                for (int i = 0; i < col.Length; i++)
                {
                    string q = col[i];
                    if (q.Length == 0)
                    {
                        continue;
                    }
                    string[] w = q.Split(';');
                    w = ArrayUtils.UniqueValues(w);
                    col[i] = StringUtils.Concat(";", w);
                }
            }
        }
    }
}