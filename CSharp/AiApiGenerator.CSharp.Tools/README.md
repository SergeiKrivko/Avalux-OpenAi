# AiApiGenerator.CSharp.Tools

Инструмент для генерации клиентов на C#.

## Использование

### Добавление библиотеки в проект:

```shell
dotnet add package AiApiGenerator.CSharp.Tools
```

Или:

```csproj
<ItemGroup>
    <PackageReference Include="AiApiGenerator.CSharp.Tools" Version="..." PrivateAssets="all" />
</ItemGroup>
```

### Подключение файла протокола

```csproj
<ItemGroup>
    <ProtocolFiles Include="..\Protocol.yml" />
</ItemGroup>
```

### Генерация кода

Будут автоматически созданы интерфейс `I<Название протокола>Client` 
и абстрактный класс `<Название протокола>ClientBase`.
Все схемы будут созданы как вложенные классы в `I<Название протокола>Client`.

Код должен обновляться автоматически при изменении файла протокола и при сборке проекта.

### Реализация функций

Вам нужно наследовать абстрактный класс `<Название протокола>ClientBase` 
и реализовать в нем конструктор и все методы, соответсвующее функциям. Например:

```csharp
public class ExampleClient : ExampleClientBase
{
    public ExampleClient() : base(new Uri("https://example.com"))
    {
    }

    protected override Task<string> GetFile(IExampleClient.GetFileRequest? param)
    {
        return File.ReadAllTextAsync(param?.Path);
    }
}
```

### Вызов API

С использованием Dependency Injection:

```csharp
builder.Services.AddScoped<IExampleClient, ExampleClient>();
```

```csharp
public class Service(IExampleClient exampleClient)
{
    public async Task DoSomething()
    {
        var response = await exampleClient.ExampleRequest(...);
    }
}
```

Без Dependency Injection:

```csharp
IExampleClient client = new ExampleClient();
var response = await exampleClient.ExampleRequest(...);
```