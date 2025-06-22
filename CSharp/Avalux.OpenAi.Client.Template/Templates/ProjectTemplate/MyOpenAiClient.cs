namespace MyOpenAiClient;

public class MyOpenAiClient : MyOpenAiClientClientBase
{
    public MyOpenAiClient(Uri apiUri) : base(apiUri)
    {
    }

    public MyOpenAiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    protected override Task<IMyOpenAiClient.WeatherForecast[]> GetWeather(DateOnly from, DateOnly to)
    {
        var res = new List<IMyOpenAiClient.WeatherForecast>();
        while (from <= to)
        {
            res.Add(new IMyOpenAiClient.WeatherForecast
            {
                Date = from,
                Temperature = Random.Shared.Next(-20, 30),
                Rain = Random.Shared.Next(0, 3) < 1,
            });
            from = from.AddDays(1);
        }
        return Task.FromResult(res.ToArray());
    }
}