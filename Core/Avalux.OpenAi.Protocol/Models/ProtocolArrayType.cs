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
    }
}