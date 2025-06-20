using System.Collections.Generic;

namespace Avalux.OpenAi.Protocol.Models
{
    public interface IProtocolType
    {
        bool IsString { get; }

        string JsonExample();
        string JsonExample(Dictionary<string, int> recurse);
        bool IsRecurseMaximumExceeded(Dictionary<string, int> recurse);
    }
}