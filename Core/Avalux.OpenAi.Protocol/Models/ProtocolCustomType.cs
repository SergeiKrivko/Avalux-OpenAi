using System;
using System.Collections.Generic;
using System.Linq;
using Avalux.OpenAi.Protocol.Schemas;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolCustomType : IProtocolType
    {
        public Dictionary<string, IProtocolType> Fields { get; } = new Dictionary<string, IProtocolType>();
        private ApiSchemaFieldSchema[] FieldsSource { get; }
        public string Name { get; }

        public ProtocolCustomType(string name, ApiSchemaFieldSchema[] fields)
        {
            if (ProtocolBuiltInType.BuiltInTypes.Contains(name))
                throw new ArgumentException($"The type {name} is built-in.");
            Name = name;
            FieldsSource = fields;
        }

        internal void ResolveFields(Protocol protocol)
        {
            foreach (var item in FieldsSource)
            {
                Fields[item.Name] = protocol.ResolveType(item.Type);
            }
        }

        public bool IsString => false;

        public string JsonExample()
        {
            return JsonExample(new Dictionary<string, int>());
        }

        public string JsonExample(Dictionary<string, int> recurse)
        {
            recurse = recurse.ToDictionary(e => e.Key, e => e.Value);
            if (recurse.TryGetValue(Name, out var level))
                recurse[Name] = level + 1;
            else
                recurse.Add(Name, 1);

            return
                $"{{ {string.Join(", ", Fields.Keys.Select(item => $"\"{item}\": {GetFieldExample(item, recurse)}"))} }}";
        }

        private string GetFieldExample(string fieldName, Dictionary<string, int> recurse)
        {
            var source = FieldsSource.FirstOrDefault(item => item.Name == fieldName);
            if (!string.IsNullOrWhiteSpace(source?.Example))
                return source.Example;
            return Fields[fieldName].JsonExample(recurse);
        }

        private const int MaxRecurseDepth = 1;

        public bool IsRecurseMaximumExceeded(Dictionary<string, int> recurse)
        {
            if (recurse.TryGetValue(Name, out var level))
            {
                return level > MaxRecurseDepth;
            }

            return false;
        }
    }
}