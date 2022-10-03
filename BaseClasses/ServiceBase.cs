using EfVueMantle.Helpers;
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
        return GetQuery(id).FirstOrDefault();
    }
    public virtual IQueryable<TModel> GetQuery(int id)
    {
        return _dynamic.Where(x => x.Id == id).ApplyCustomProjection(_context);
    }

    /**
     * All records 
     */
    public virtual List<TModel> GetAll()
    {
        return GetAllQuery().ToList();
    }
    public virtual IQueryable<TModel> GetAllQuery()
    {
        return _dynamic.ApplyCustomProjection(_context);
    }

    /**
     * List of records by id list
     */
    public virtual List<TModel> GetList(List<int> ids)
    {
        return GetListQuery(ids).ToList();
    }
    public virtual IQueryable<TModel> GetListQuery(List<int> ids)
    {
        return _dynamic.Where(x => ids.Any(id => id == x.Id)).ApplyCustomProjection(_context);
    }

    /**
     * List of all Ids
     */
    public virtual List<int> GetAllIds()
    {
        return GetAllIdsQuery().Select(x => x.Id).ToList();
    }
    public virtual IQueryable<TModel> GetAllIdsQuery()
    {
        return _dynamic;
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
    public virtual TModel Save(TModel data)
    {
        _dynamic.Add(data);
        _context.Entry(data).State = data.Id == 0 ? EntityState.Added : EntityState.Modified;
        _context.SaveChanges();
        return data;
    }


    /*
     * Update a record, return updated data object
     */
    public virtual int Update()
    {
        return _context.SaveChanges();
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
