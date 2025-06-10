namespace TestClient;

public class ApiClient : AiDocClientBase
{
    public ApiClient() : base(new Uri("https://api.ai.io/"))
    {
    }

    protected override Task<string> GetFile(IAiDocClient.GetFileRequest? param)
    {
        return File.ReadAllTextAsync(param?.Path);
    }
}