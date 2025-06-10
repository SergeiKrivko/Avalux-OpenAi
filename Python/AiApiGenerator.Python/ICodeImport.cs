namespace AiApiGenerator.Python;

public interface ICodeImport
{
    public string WriteCode();
}

public class BasicCodeImport : ICodeImport
{
    private string _library;
    private string? _alias;

    public BasicCodeImport(string library, string? alias = null)
    {
        _library = library;
        _alias = alias;
    }

    public string WriteCode()
    {
        if (_alias == null)
            return $"import {_library}";
        return $"import {_library} as {_alias}";
    }
}

public class FromCodeImport : ICodeImport
{
    internal readonly string Library;
    internal readonly string Object;
    internal readonly string? Alias;

    public FromCodeImport(string library, string obj, string? alias = null)
    {
        Library = library;
        Object = obj;
        Alias = alias;
    }

    public string WriteCode()
    {
        return $"from {Library} import {ObjectDefinition()}";
    }

    internal string ObjectDefinition()
    {
        if (Alias == null)
            return Object;
        return $"{Object} as {Alias}";
    }
}

public class GroupCodeImport : ICodeImport
{
    private readonly string _library;
    private readonly List<FromCodeImport> _imports = [];

    public GroupCodeImport(string library, List<FromCodeImport> imports)
    {
        _library = library;
        _imports = imports;
    }

    public GroupCodeImport(string library)
    {
        _library = library;
    }

    public void AddImport(FromCodeImport import)
    {
        if (import.Library != _library)
            throw new Exception();
        if (_imports.FirstOrDefault(x => x.Object == import.Object && x.Alias == import.Alias) == null)
            _imports.Add(import);
    }

    public string WriteCode()
    {
        return $"from {_library} import {string.Join(", ", _imports.Select(x => x.ObjectDefinition()))}";
    }
}