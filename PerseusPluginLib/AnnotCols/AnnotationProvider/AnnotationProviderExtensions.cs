using System;
using System.Collections.Generic;
using System.Linq;
using PerseusApi.Generic;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public static class AnnotationProviderExtensions
    {
        /// <summary>
        /// Return all sources and their text annotations.
        /// </summary>
        /// <param name="annotationProvider"></param>
        /// <returns></returns>
        public static (string source, string id, string[] names)[] TextSources(this IAnnotationProvider annotationProvider)
        {
            return annotationProvider.Sources
                .Select(source => (source.source, source.id, source.annotation
                    .Where(annot => annot.type == AnnotType.Text)
                    .Select(annot => annot.name).ToArray()))
                .ToArray();
        }

        /// <summary>
        /// Map annotation back to base identifiers according to the source.
        /// </summary>
        /// <param name="annotationProvider"></param>
        /// <param name="identifiers"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="annotationIndex"></param>
        /// <returns></returns>
        public static string[] MapToBaseIdentifiers(this IAnnotationProvider annotationProvider, string[] identifiers, int sourceIndex, int annotationIndex)
        {
            var selection = new[] {0};
            var mapping = annotationProvider.ReadMappings(sourceIndex, annotationIndex + 1, selection);
            var outerLeftJoin = OuterLeftJoin(identifiers, mapping, selection.Length);
            return outerLeftJoin.Select(toIds => string.Join(";", toIds.Single())).ToArray();
        }

        /// <summary>
        /// Helper function for general mapping between identifiers and annotations. All strings are split on ';' and values are aggregated accordingly.
        /// </summary>
        /// <param name="identifiers"></param>
        /// <param name="mapping"></param>
        /// <param name="numberOfMappings"></param>
        /// <returns></returns>
        public static IEnumerable<string[][]> OuterLeftJoin(string[] identifiers, IEnumerable<(string fromId, string[][] toIds)> mapping, int numberOfMappings)
        {
            var split = new[] {';'};
            var noMatch = new[] {new string[0]};
            var emptyId = "";
            var outerLeftJoin = identifiers
                // split identifiers and keep track of rows
                .SelectMany((ids, row) =>
                    ids.Split(split, StringSplitOptions.RemoveEmptyEntries).DefaultIfEmpty(emptyId).Select(id => (id: id.Trim().ToLower(), row: row)))
                // join with mapping
                .GroupJoin(mapping, idRow => idRow.id, map => map.fromId,
                    (idRow, maps) =>
                    {
                        if (string.IsNullOrWhiteSpace(idRow.id))
                        {
                            return (row: idRow.row, toIds: maps.SelectMany(_ => noMatch).ToArray());
                        }
                        var toIdsMaps = maps.DefaultIfEmpty((fromId: idRow.id, toIds: noMatch)).Select(map => map.toIds);
                        var aggregators = Enumerable.Range(0, numberOfMappings).Select(_ => new HashSet<string>()).ToArray();
                        foreach (var idMaps in toIdsMaps)
                        {
                            for (int i = 0; i < idMaps.Length; i++)
                            {
                                aggregators[i].UnionWith(idMaps[i]);
                            }
                        }
                        var toIds = aggregators.Select(idSet => idSet.Select(id => id.Trim()).OrderBy(id => id).ToArray()).ToArray();
                        return (row: idRow.row, toIds: toIds);
                    })
                // aggregate values per row
                .GroupBy(grp => grp.row, grp => grp.toIds, (row, allIds) =>
                {
                    var aggregator = Enumerable.Range(0, numberOfMappings).Select(_ => new HashSet<string>()).ToArray();
                    foreach (var rowIds in allIds)
                    {
                        for (int i = 0; i < rowIds.Length; i++)
                        {
                            aggregator[i].UnionWith(rowIds[i]);
                        }
                    }
                    var toIds = aggregator.Select(idSet => idSet.Select(id => id.Trim()).OrderBy(id => id).ToArray()).ToArray();
                    return (row: row, toIds: toIds);
                })
                .OrderBy(tup => tup.row)
                .Select(tup => tup.toIds);
            return outerLeftJoin;
        }

        /// <summary>
        /// Annotate the provided identifiers by selecting the source, and the annotations
        /// </summary>
        /// <param name="annotationProvider"></param>
        /// <param name="identifiers"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="annotationIndices"></param>
        /// <returns></returns>
        public static ((string name, string[] values)[] text, (string name, string[][] values)[] category, (string name, double[] values)[] numeric)
            ReadAnnotations(this IAnnotationProvider annotationProvider, string[] identifiers, int sourceIndex, int[] annotationIndices)
        {
            var adjustedAnnotationIndices = annotationIndices.Select(i => i + 1).ToArray();
            var mappings = annotationProvider.ReadMappings(sourceIndex, 0, adjustedAnnotationIndices);
            var indexedAnnotations = annotationProvider.Sources[sourceIndex].annotation.Select((annotation, i) => (annotation:annotation, i:i));
            var annotations = annotationIndices.Join(indexedAnnotations, i => i, iAnnotation => iAnnotation.i, (i, iAnnotation) => iAnnotation.annotation).ToArray();
            var text = new List<(string name, string[] values)>();
            var category = new List<(string name, string[][] values)>();
            var numeric = new List<(string name, double[] values)>();
            var outerLeftJoin = OuterLeftJoin(identifiers, mappings, adjustedAnnotationIndices.Length);
            var columns = Enumerable.Range(0, annotationIndices.Length).Select(_ => new List<string[]>()).ToArray();
            foreach (var map in outerLeftJoin)
            {
                for (int i = 0; i < annotationIndices.Length; i++)
                {
                    columns[i].Add(map[i].Select(x => x.Trim()).ToArray());
                }
            }
            for (int i = 0; i < annotationIndices.Length; i++)
            {
                var (name, type) = annotations[i];
                switch (type)
                {
                    case AnnotType.Text:
                        text.Add((name, columns[i].Select(s => string.Join(";", s.OrderBy(x => x))).ToArray()));
                        break;
                    case AnnotType.Numerical:
                        numeric.Add((name, columns[i].Select(s => double.Parse(s.DefaultIfEmpty("NaN").Single())).ToArray()));
                        break;
                    case AnnotType.Categorical:
                        category.Add((name, columns[i].ToArray()));
                        break;
                }
            }
            return (text.ToArray(), category.ToArray(), numeric.ToArray());

        }
    }
}