using System;

namespace AiApiGenerator.Protocol.Models
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
    }
}