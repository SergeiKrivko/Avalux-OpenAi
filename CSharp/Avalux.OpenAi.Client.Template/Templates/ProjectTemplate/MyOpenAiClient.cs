namespace MyOpenAiClient;

public class MyOpenAiClient : MyOpenAiClientClientBase
{
    public MyOpenAiClient(Uri apiUri) : base(apiUri)
    {
    }

    public MyOpenAiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    protected override Task<IMyOpenAiClientClient.MyOpenAiClientForecast[]> GetMyOpenAiClient(IMyOpenAiClientClient.DateRange? param)
    {
        if (param == null)
            return Task.FromResult<IMyOpenAiClientClient.MyOpenAiClientForecast[]>([]);

        var res = new List<IMyOpenAiClientClient.MyOpenAiClientForecast>();
        var date = param.From;
        while (date <= param.To)
        {
            res.Add(new IMyOpenAiClientClient.MyOpenAiClientForecast
            {
                Date = date,
                Temperature = Random.Shared.Next(-20, 30),
                Rain = Random.Shared.Next(0, 3) < 1,
            });
            date.AddDays(1);
        }
        return Task.FromResult(res.ToArray());
    }
}