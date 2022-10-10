using Newtonsoft.Json;

namespace EfVueMantle;

public interface IModelBase : IModelBase<long> { }

public interface IModelBase<TKey>
    where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; }
    public DateTime Created { get; set; } 
    public DateTime? Updated { get; set; }
}

public class ModelBase : ModelBase<long>
{
}

public class ModelBase<TKey> : IModelBase<TKey>
    where TKey : IEquatable<TKey>
{
    public ModelBase() { }

    public TKey Id { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime? Updated { get; set; }
}