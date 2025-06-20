using System;
using System.Collections.Generic;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolNullableType : IProtocolType
    {
        public IProtocolType InnerType { get; }

        public ProtocolNullableType(IProtocolType innerType)
        {
            if (innerType is ProtocolNullableType)
                throw new ArgumentException("Cannot create nullable type from another nullable type.");
            InnerType = innerType;
        }

        public bool IsString => false;

        public string JsonExample()
        {
            return InnerType.JsonExample();
        }

        public string JsonExample(Dictionary<string, int> recurse)
        {
            if (InnerType.IsRecurseMaximumExceeded(recurse))
                return "null";
            return InnerType.JsonExample(recurse);
        }

        public bool IsRecurseMaximumExceeded(Dictionary<string, int> recurse)
        {
            return InnerType.IsRecurseMaximumExceeded(recurse);
        }
    }
}