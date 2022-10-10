using EfVueMantle.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EfVueMantle;

public class ServiceBase<TModel> : ServiceBase<TModel, long> 
    where TModel : ModelBase
{ 
    public ServiceBase(DbSet<TModel> dbBaseSet, DbContext context) : base(dbBaseSet, context)
    {
    }
}

public class ServiceBase<TModel, TKey>
    where TModel : ModelBase<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly GenericServiceHelper<TModel, TKey> _informHelper;
    private readonly DbSet<TModel> _dynamic;
    private readonly DbContext _context;

    public ServiceBase(DbSet<TModel> dbBaseSet, DbContext context)
    {
        _dynamic = dbBaseSet;
        _context = context;
        _informHelper = new GenericServiceHelper<TModel, TKey>(dbBaseSet);
    }

    /**
     * One record by Id
     */
    public virtual TModel? Get(TKey id)
    {
        return GetQuery(id).FirstOrDefault();
    }
    public virtual IQueryable<TModel> GetQuery(TKey id)
    {
        return _dynamic.Where(x => x.Id.Equals(id)).ApplyCustomProjection(_context);
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
    public virtual List<TModel> GetList(List<TKey> ids)
    {
        return GetListQuery(ids).ToList();
    }
    public virtual IQueryable<TModel> GetListQuery(List<TKey> ids)
    {
        return _dynamic.Where(x => ids.Any(id => id.Equals(x.Id))).ApplyCustomProjection(_context);
    }

    /**
     * List of all Ids
     */
    public virtual List<TKey> GetAllIds()
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
    internal virtual List<TKey> Order(string field, int direction)
    {
        return _informHelper.Order(field, direction);
    }

    /**
     * List of Ids where prop.value == spec
     */
    public virtual List<TKey> Equals(string prop, string spec)
    {
        return _informHelper.Equals(prop, spec);
    }

    /**
     * List of Ids where prop.value contains spec
     */
    public virtual List<TKey> Contains(string prop, string spec)
    {
        return _informHelper.Contains(prop, spec);
    }

    /*
     * Add a record, return updated data object
     */
    public virtual TModel Save(TModel data)
    {
        _dynamic.Add(data);
        _context.Entry(data).State =
            data.Id != null 
                ? EntityState.Added 
                : EntityState.Modified;
        _context.SaveChanges();
        return data;
    }

    /*
     * Add or update many records, return updated data objects
     */
    public virtual List<TModel> SaveAll(List<TModel> datas)
    {
        datas.ForEach(data => _dynamic.Add(data));
        datas.ForEach(data => _context.Entry(data).State =
            string.IsNullOrEmpty(data.Id.ToString()) || data.Id.Equals(0)
                ? EntityState.Added 
                : EntityState.Modified);
        _context.SaveChanges();
        return datas;
    }


    /*
     * Update a record, return number of records changed
     */
    public virtual int Update()
    {
        return _context.SaveChanges();
    }

    /*
     * Remove a record, return bool
     */
    public virtual bool Delete(TKey id)
    {
        if (Activator.CreateInstance(typeof(TModel)) is not TModel toDelete) return false;
        toDelete.Id = id;
        _dynamic.Remove(toDelete);
        _context.SaveChanges();
        return true;
    }
}
