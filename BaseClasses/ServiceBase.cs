using Microsoft.EntityFrameworkCore;

namespace EfVueMantle;

public class ServiceBase<T>
    where T : ModelBase
{
    private readonly GenericServiceHelper<T> _informHelper;
    private readonly DbSet<T> _dynamic;

    public ServiceBase(DbSet<T> dbBaseSet)
    {
        _dynamic = dbBaseSet;
        _informHelper = new GenericServiceHelper<T>(dbBaseSet);
    }

    /**
     * One record by Id
     */
    public T? Get(int id)
    {
        return _dynamic.Where(x => x.Id == id).FirstOrDefault();
    }

    /**
     * All records 
     */
    public List<T> GetAll()
    {
        return _dynamic.ToList();
    }

    /**
     * List of records by id list
     */
    public List<T> GetList(List<int> ids)
    {
        var list = _dynamic.Where(x => ids.Any(id => id == x.Id)).ToList();
        return list;
    }

    /**
     * List of all Ids
     */
    public List<int> GetAllIds()
    {
        return _dynamic.Select(x => x.Id).ToList();
    }

    /**
     * List of all Ids in a particular order
     */
    internal List<int> Order(string field, int direction)
    {
        return _informHelper.Order(field, direction);
    }

    /**
     * List of Ids where prop.value == spec
     */
    public List<int> Equals(string prop, string spec)
    {
        return _informHelper.Equals(prop, spec);
    }

    /**
     * List of Ids where prop.value contains spec
     */
    public List<int> Contains(string prop, string spec)
    {
        return _informHelper.Contains(prop, spec);
    }
}
