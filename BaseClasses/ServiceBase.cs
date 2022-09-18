using Microsoft.EntityFrameworkCore;

namespace EfVueMantle;

public class ServiceBase<TModel>
    where TModel : ModelBase
{
    private readonly GenericServiceHelper<TModel> _informHelper;
    private readonly DbSet<TModel> _dynamic;
    private readonly DbContext _context;

    public ServiceBase(DbSet<TModel> dbBaseSet, DbContext context)
    {
        _dynamic = dbBaseSet;
        _context = context;
        _informHelper = new GenericServiceHelper<TModel>(dbBaseSet);
    }

    /**
     * One record by Id
     */
    public virtual TModel? Get(int id)
    {
        return _dynamic.Where(x => x.Id == id).FirstOrDefault();
    }

    /**
     * All records 
     */
    public virtual List<TModel> GetAll()
    {
        return _dynamic.ToList();
    }

    /**
     * List of records by id list
     */
    public virtual List<TModel> GetList(List<int> ids)
    {
        var list = _dynamic.Where(x => ids.Any(id => id == x.Id)).ToList();
        return list;
    }

    /**
     * List of all Ids
     */
    public virtual List<int> GetAllIds()
    {
        return _dynamic.Select(x => x.Id).ToList();
    }

    /**
     * List of all Ids in a particular order
     */
    internal virtual List<int> Order(string field, int direction)
    {
        return _informHelper.Order(field, direction);
    }

    /**
     * List of Ids where prop.value == spec
     */
    public virtual List<int> Equals(string prop, string spec)
    {
        return _informHelper.Equals(prop, spec);
    }

    /**
     * List of Ids where prop.value contains spec
     */
    public virtual List<int> Contains(string prop, string spec)
    {
        return _informHelper.Contains(prop, spec);
    }

    /*
     * Add a record, return updated data object
     */
    public virtual TModel Add(TModel data)
    {
        _dynamic.Add(data);
        _context.SaveChanges();
        return data;
    }

    /*
     * Remove a record, return bool
     */
    public virtual bool Delete(int id)
    {
        if (Activator.CreateInstance(typeof(TModel)) is not TModel toDelete) return false;
        toDelete.Id = id;
        _dynamic.Remove(toDelete);
        _context.SaveChanges();
        return true;
    }
}
