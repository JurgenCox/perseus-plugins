using System;
using System.Collections.Generic;
using System.Linq;
using PerseusApi.Generic;
using PerseusPluginLib.AnnotCols.AnnotationProvider;

namespace PerseusPluginLib.Test.Annot
{
    public class MockAnnotationProvider : IAnnotationProvider
    {
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
            Sources = new[] {("source", "id", new[]
                {
                    ("textannot", AnnotType.Text),
                    ("textannot2", AnnotType.Text),
                    ("catannot", AnnotType.Categorical),
                    ("numannot", AnnotType.Numerical)
                })};
        }


        public (string source, string id, (string name, AnnotType type)[] annotation)[] Sources { get; }
        public string[] BadSources => new string[0];
        public IEnumerable<(string fromId, string[][] toIds)> ReadMappings(int sourceIndex, int fromColumn, int[] toColumns)
        {
            return flatIds(fromColumn, toColumns).GroupBy(tup => tup.fromId, tup => tup.toIds,
                (fromId, toIds) => (fromId, toIds.SelectMany(ids => ids).ToArray()));
        }

        private IEnumerable<(string fromId, string[][] toIds)> flatIds(int fromColumn, int[] toColumns)
        {
            var split = new[] {';'};
            for (int i = 0; i < idCol.Length; i++)
            {
                var toIds = toColumns.Select(j => annots[j][i].Split(split, StringSplitOptions.RemoveEmptyEntries)).ToArray();
                foreach (var id in annots[fromColumn][i].Split(';'))
                {
                    yield return (id.Trim(), toIds);
                }
            }
        }
    }
}