using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseLibS.Util;
using PerseusApi.Generic;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public static class AnnotationFormat
    {
        public static IEnumerable<(string fromId, string[][] toIds)> ReadMappings(StreamReader reader, int fromColumn, int[] toColumns)
        {
            char[] semicolon = {';'};
            var mapping = ReadLines(reader).Skip(2).SelectMany(line =>
            {
                var entries = line.Split('\t');
                var fromIdentifiers = entries[fromColumn].Split(semicolon, StringSplitOptions.RemoveEmptyEntries);
                var toIdentifiers = toColumns
                    .Select(toColumn => entries[toColumn].Split(semicolon, StringSplitOptions.RemoveEmptyEntries)).ToArray();
                return fromIdentifiers.Select(fromId => (fromId: fromId.Trim().ToLower(), toIdentifiers: toIdentifiers));
            });
            var groupedById = mapping.GroupBy(entry => entry.fromId, tuple => tuple.toIdentifiers,
                (idx, entries) =>
                {
                    var aggregator = Enumerable.Range(0, toColumns.Length).Select(_ => new HashSet<string>()).ToArray();
                    foreach (var rowIds in entries)
                    {
                        for (int i = 0; i < rowIds.Length; i++)
                        {
                            aggregator[i].UnionWith(rowIds[i]);
                        }
                    }
                    var toIds = aggregator.Select(idSet => idSet.OrderBy(id => id).ToArray()).ToArray();
                    return (idx, toIds);
                });
            return groupedById;
        }

        public static IEnumerable<(string fromId, string[][] toIds)> ReadMappings(string file, int fromColumn, int[] toColumns)
        {
            using (var reader = FileUtils.GetReader(file))
            {
                // explicity iterate over mappings to prevent reader from being disposed too early.
                foreach (var mapping in ReadMappings(reader, fromColumn, toColumns))
                {
                    yield return mapping;
                }
            }
        }

        private static IEnumerable<string> ReadLines(StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public static void WriteMapping((string name, string[] values) baseIdentifiers, (string name, string[] values)[] text,
            (string name, string[][] values)[] category, (string name, double[] values)[] numeric, string path)
        {
            using (var writer = new StreamWriter(path))
            {
                WriteMapping(baseIdentifiers, text, category, numeric, writer);
            }
        }

        public static void WriteMapping((string name, string[] values) baseIdentifiers, (string name, string[] values)[] text,
            (string name, string[][] values)[] category, (string name, double[] values)[] numeric, StreamWriter writer)
        {
            const string tab = "\t";
            var header = string.Join(tab,
                new [] {baseIdentifiers.name}
                .Concat(text.Select(t => t.name))
                .Concat(category.Select(cat => cat.name))
                .Concat(numeric.Select(num => num.name)));
            writer.WriteLine(header);
            var typerow = string.Join(tab,
                new [] {"#!{Type}"}
                .Concat(Enumerable.Repeat("Text", text.Length))
                .Concat(Enumerable.Repeat("Categorical", category.Length)
                .Concat(Enumerable.Repeat("Numerical", numeric.Length))));
            writer.WriteLine(typerow);
            for (int i = 0; i < baseIdentifiers.values.Length; i++)
            {
                var captureI = i;
                var row = new[] { baseIdentifiers.values[captureI] }
                    .Concat(text.Select(annot => annot.values[captureI]))
                    .Concat(category.Select(annot => string.Join(";", annot.values[captureI].Where(values => !string.IsNullOrEmpty(values)))))
                    .Concat(numeric.Select(annot => $"{annot.values[captureI]}"));
                writer.WriteLine(string.Join(tab, row));
            }
            writer.Flush();
        }

        public static (string id, (string name, AnnotType type)[] annotations) Annotations(StreamReader reader)
        {
            var headers = ReadLines(reader).Take(2).ToArray();
            var colnames = headers[0].Split('\t');
            var types = headers[1].Split('\t').Skip(1);
            var annotations = colnames.Skip(1).Zip(types, (name, type) => (name, ParseAnnotationType(type))).ToArray();
            return (colnames[0], annotations);
        }

        private static AnnotType ParseAnnotationType(string type)
        {
            if (type.Equals("Text"))
            {
                return AnnotType.Text;
            }
            if (type.Equals("Categorical"))
            {
                return AnnotType.Categorical;
            }
            if (type.Equals("Numerical"))
            {
                return AnnotType.Numerical;
            }
            throw new ArgumentException($"Could not parse type {type} to {nameof(AnnotType)}.");
        }
    }
}