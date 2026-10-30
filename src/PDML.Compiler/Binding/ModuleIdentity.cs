namespace PDML.Compiler.Binding;

public class ModuleIdentity
{
    public string FilePath { get; }
    public IReadOnlyList<string> Segments { get; }

    public ModuleIdentity(string filePath, IEnumerable<string> segments)
    {
        FilePath = filePath;
        Segments = segments.ToList();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ModuleIdentity other)
        {
            return false;
        }

        if (ReferenceEquals(obj, other))
        {
            return true;
        }

        if (other.Segments.Count != Segments.Count)
        {
            return false;
        }

        for (int i = 0; i < Segments.Count; i++)
        {
            if (other.Segments[i] != Segments[i])
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return Segments.GetHashCode();
    }

    public static bool operator ==(ModuleIdentity left, ModuleIdentity right) => left.Equals(right);
    public static bool operator !=(ModuleIdentity left, ModuleIdentity right) => !left.Equals(right);

    public static ModuleIdentity FromPath(string rootPath, string filePath)
    {
        if (filePath.EndsWith(".pdml"))
        {
            filePath = filePath.Substring(0, filePath.Length - ".pdml".Length);
        }

        if (rootPath.Length > filePath.Length)
        {
            throw new InvalidOperationException("RootPath cannot be longer then the FilePath");
        }

        string remaining = filePath.Substring(rootPath.Length);
        string[] parts = remaining.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        return new ModuleIdentity(filePath, parts);
    }
}
