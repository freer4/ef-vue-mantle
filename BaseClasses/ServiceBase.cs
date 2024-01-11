using EfVueMantle.Helpers;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

    /**
     * List of Ids where prop.value is in the array spec
     */
    public virtual List<TKey> Any(string prop, string spec)
    {
        var series = JsonConvert.DeserializeObject<List<dynamic>>($"[{spec}]");
        return _informHelper.Any(prop, series);
    }

    /*
     * Add a record, return updated data object
     */
    public virtual TModel Save(TModel data)
    {
        var state = data.Id.Equals(default(TKey))
                ? EntityState.Added
                : EntityState.Modified;
        _dynamic.Add(data);
        _context.Entry(data).State = state;            
        _context.SaveChanges();
        return data;
    }

    /*
     * Add or update many records, return updated data objects
     */
    public virtual List<TModel> SaveAll(List<TModel> datas)
    {
        var states = datas.ToDictionary(x => x, x => x.Id.Equals(default(TKey))
                ? EntityState.Added
                : EntityState.Modified);
        _dynamic.AddRange(datas);
        datas.ForEach(data => _context.Entry(data).State = states[data]);
        _context.SaveChanges();
        return datas;
    }

    /*
     * Insert a record 
     */
    public virtual TModel Insert(TModel data)
    {
        _dynamic.Add(data);
        _context.Entry(data).State = EntityState.Added;
        _context.SaveChanges();
        return data;
    }
    /*
     * Insert several records     
     */
    public virtual List<TModel> InsertMany(List<TModel> datas)
    {
        _dynamic.AddRange(datas);
        datas.ForEach(data => _context.Entry(data).State = EntityState.Added);
        _context.SaveChanges();
        return datas;
    }

    /*
     * Update a record
     */
    public virtual TModel Update(TModel data)
    {
        _dynamic.Add(data);
        _context.Entry(data).State = EntityState.Modified;
        _context.SaveChanges();
        return data;
    }
    /*
     * Update several records     
     */
    public virtual List<TModel> UpdateMany(List<TModel> datas)
    {
        _dynamic.AddRange(datas);
        datas.ForEach(data => _context.Entry(data).State = EntityState.Modified);
        _context.SaveChanges();
        return datas;
    }

    /*
     * Remove a record, return bool
     */
    public virtual bool Delete(TKey id)
    {
        var toDelete = _dynamic.First(x => x.Id.Equals(id));
        _context.Remove(toDelete);
        _context.SaveChanges();
        return true;
    }
}
