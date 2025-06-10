namespace TestClient;

public class ApiClient : AiDocClientBase
{
    public ApiClient() : base(new Uri("https://api.ai.io/"))
    {
    }

    protected override async Task<string> GetFile(IAiDocClient.GetFileRequest? param)
    {
        throw new NotImplementedException();
    }
}