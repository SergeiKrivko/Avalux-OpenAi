using System;
using System.Collections.Generic;
using System.Linq;
using AiApiGenerator.Protocol.Schemas;

namespace AiApiGenerator.Protocol.Models
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
            return $"{{ {string.Join(", ", Fields.Keys.Select(item => $"\"{item}\": {GetFieldExample(item)}"))} }}";
        }

        private string GetFieldExample(string fieldName)
        {
            var source = FieldsSource.FirstOrDefault(item => item.Name == fieldName);
            if (!string.IsNullOrWhiteSpace(source?.Example))
                return source.Example;
            return Fields[fieldName].JsonExample();
        }
    }
}