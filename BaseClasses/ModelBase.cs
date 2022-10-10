namespace EfVueMantle;

public class ModelBase : ModelBase<long>
{
}
public class ModelBase<TKey> 
    where TKey : IEquatable<TKey>
{
    public ModelBase() { }
    public TKey Id { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime? Updated { get; set; }
}
