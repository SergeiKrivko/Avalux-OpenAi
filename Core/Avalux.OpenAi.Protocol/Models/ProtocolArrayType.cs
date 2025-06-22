using System.Collections.Generic;

namespace Avalux.OpenAi.Protocol.Models
{
    public class ProtocolArrayType : IProtocolType
    {
        public IProtocolType ArrayType { get; set; }

        public ProtocolArrayType(IProtocolType arrayType)
        {
            ArrayType = arrayType;
        }

        public bool IsString => false;

        public string JsonExample()
        {
            return $"[ {ArrayType.JsonExample()} ]";
        }

        public string JsonExample(Dictionary<string, int> recurse)
        {
            if (ArrayType.IsRecurseMaximumExceeded(recurse))
                return "[]";
            return $"[ {ArrayType.JsonExample(recurse)} ]";
        }

        public bool IsRecurseMaximumExceeded(Dictionary<string, int> recurse)
        {
            return ArrayType.IsRecurseMaximumExceeded(recurse);
        }
    }
}