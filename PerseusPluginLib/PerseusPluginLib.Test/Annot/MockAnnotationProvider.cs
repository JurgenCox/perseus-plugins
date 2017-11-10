using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PerseusApi.Generic;
using PerseusPluginLib.AnnotCols.AnnotationProvider;

namespace PerseusPluginLib.Test.Annot
{
    public class MockAnnotationProvider : IAnnotationProvider
    {
        private IAnnotationProvider _provider;
        public string[] idCol;
        public string[] textannot;
        public string[] textannot2;
        public string[][] catannot;
        public double[] numannot;
        private string[][] annots;

        public MockAnnotationProvider()
        {
            idCol = new[] { "1", "2", "3", "4;5", "6; 7" };
            textannot = new[] { "a; b", "c", "", "b;e", "f" };
            textannot2 = new[] { "a;b", "c", "", "e", "f" };
            catannot = new[] { new[] { "x", "y" }, new[] { "z" }, new string[0], new[] { "z" }, new[] { "z" } };
            numannot = new[] { 0.0, -1, 1, 0.0, 0.1 };
            annots = new[] { idCol, textannot, textannot2, catannot.Select(cats => string.Join(";", cats)).ToArray(), numannot.Select(d => $"{d}").ToArray() };
            string result;
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                AnnotationFormat.WriteMapping(("id", idCol),
                    new[] { ("textannot", textannot), ("textannot2", textannot2) }, new[] { ("catannot", catannot) },
                    new[] { ("numannot", numannot) }, writer);
                result = Encoding.UTF8.GetString(stream.ToArray());
            }
            var memstream = new MemoryStream(Encoding.UTF8.GetBytes(result));
            var reader = new StreamReader(memstream);
            _provider = new StreamAnnotationProvider(("test", reader));
        }


        public (string source, string id, (string name, AnnotType type)[] annotation)[] Sources => _provider.Sources;
        public string[] BadSources => _provider.BadSources;
        public IEnumerable<(string fromId, string[][] toIds)> ReadMappings(int sourceIndex, int fromColumn, int[] toColumns)
        {
            return _provider.ReadMappings(sourceIndex, fromColumn, toColumns);
        }
    }
}