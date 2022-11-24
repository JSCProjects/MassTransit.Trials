using MassTransit;

namespace SetEntityNameFormatter.MassTransit.Worker;

public class MyEntityNameFormatter : IEntityNameFormatter
{
    private readonly string _prefix;
    private readonly string _separator;

    public MyEntityNameFormatter(string prefix, string separator = "/")
    {
        _prefix = prefix;
        _separator = separator;
    }
    
    public string FormatEntityName<T>()
    {
        return $"{_prefix}{_separator}{typeof(T).Name}";
    }
}

public class MyEntityNameFormatter2 : IEntityNameFormatter
{
    public string FormatEntityName<T>()
    {
        return typeof(T).Name;
    }
}