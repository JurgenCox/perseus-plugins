
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PerseusApi.Generic;

namespace PerseusPluginLib.AnnotCols.AnnotationProvider
{
    public class StreamAnnotationProvider : IAnnotationProvider
    {
        private readonly StreamReader[] _readers;

        public StreamAnnotationProvider(params (string name, StreamReader reader)[] sources)
        {
            Sources = sources.Select(source =>
            {
                if (!source.reader.BaseStream.CanSeek)
                {
                    throw new ArgumentException($"{nameof(StreamAnnotationProvider)} requires seekable stream.");
                }
                source.reader.BaseStream.Seek(0, SeekOrigin.Begin);
                source.reader.DiscardBufferedData();
                var (id, annotations) = AnnotationFormat.Annotations(source.reader);
                return (source.name, id, annotations);
            }).ToArray();
            _readers = sources.Select(reader => reader.reader).ToArray();
        }

        public (string source, string id, (string name, AnnotType type)[] annotation)[] Sources { get; }
        public string[] BadSources => new string[0];
        public IEnumerable<(string fromId, string[][] toIds)> ReadMappings(int sourceIndex, int fromColumn, int[] toColumns)
        {
            var reader = _readers[sourceIndex];
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.DiscardBufferedData();
            return AnnotationFormat.ReadMappings(_readers[sourceIndex], fromColumn, toColumns);
        }
    }
}