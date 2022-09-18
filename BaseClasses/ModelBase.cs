namespace EfVueMantle;

public class ModelBase
{
    public ModelBase() { }
    public int Id { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime? Updated { get; set; }
}
