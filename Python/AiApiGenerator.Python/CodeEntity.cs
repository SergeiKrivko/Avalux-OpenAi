namespace AiApiGenerator.Python;

public class CodeEntity
{
    public string Source { get; private set; }
    public List<ICodeImport> Imports { get; }

    public CodeEntity(string source)
    {
        Source = source;
        Imports = [];
    }

    public CodeEntity(IEnumerable<ICodeImport> imports, string source)
    {
        Source = source;
        Imports = imports.ToList();
    }

    public string WriteCode()
    {
        return Source;
    }

    public string WriteCode(int indent)
    {
        var indentString = new string(' ', indent * 4);
        return Source.Split('\n').Select(e => indentString + e).Aggregate((a, b) => a + b);
    }
}