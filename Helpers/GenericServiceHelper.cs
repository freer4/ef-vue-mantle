using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace EfVueMantle;

public class GenericServiceHelper<TModel> : GenericServiceHelper<TModel, long> 
    where TModel : ModelBase
{ 
    public GenericServiceHelper(DbSet<TModel> dbSet) : base(dbSet) { }
}
public class GenericServiceHelper<TModel, TKey>
    where TModel : ModelBase<TKey>
    where TKey : IEquatable<TKey>
{
    public DbSet<TModel> _dbSet;

    public GenericServiceHelper(DbSet<TModel> dbSet)
    {
        _dbSet = dbSet;
    }


    /*
     * Takes a property name (or path in dot notation) and a string to look for,
     * returns a list of ids for any case-insensitive exact matches
     */
    public List<TKey> Equals(string propertyPath, string spec)
    {
        //case-insensitive
        //TODO create a case-sensitive version
        spec = spec.Trim().ToLower();

        var props = PathToParts(propertyPath);
        var rebuild = String.Join(".", props);
        IQueryable<TModel> queryBuilder = _dbSet;

        if (rebuild.IndexOf(".") != -1)
        {
            queryBuilder = _dbSet.Include(rebuild.Remove(rebuild.LastIndexOf(".")));
        }

        return queryBuilder.AsEnumerable()
            .Where(x =>
            {
                var y = TraversePropertyTree(x, props);

                if (IsNumeric(y))
                {
                    return Convert.ToDouble(y) == double.Parse(spec);
                }
                return y?.ToString()?.ToLower() == spec;
            })
            .Select(x => x.Id)
            .ToList();
    }
    public static bool IsNumeric(object? expression)
    {
        if (expression == null)
            return false;

        return double.TryParse(
            Convert.ToString(expression, CultureInfo.InvariantCulture),
            NumberStyles.Any,
            NumberFormatInfo.InvariantInfo,
            out _);
    }

    /*
     * Takes a property name (or path in dot notation) and a string to look for,
     * returns a list of ids for values containing spec
     */
    public List<TKey> Contains(string propertyPath, dynamic spec)
    {

        //case-insensitive
        //TODO create a case-sensitive version
        var props = PathToParts(propertyPath);
        var rebuild = String.Join(".", props);
        IQueryable<TModel> queryBuilder = _dbSet;

        if (rebuild.IndexOf(".") != -1)
        {
            queryBuilder = _dbSet.Include(rebuild.Remove(rebuild.LastIndexOf(".")));
        }

        var data = queryBuilder.AsEnumerable()
            .Where(x =>
            {
                var y = TraversePropertyTree(x, props);
                if (y == null) return false;
                return y.ToString()?.Contains(spec, StringComparison.InvariantCultureIgnoreCase) ?? false;
            })
            .ToList();

        return data.Select(x => x.Id).ToList();

    }

    public List<TKey> Any(string propertyPath, List<dynamic> spec)
    {
        var props = PathToParts(propertyPath);
        var rebuild = String.Join(".", props);
        IQueryable<TModel> queryBuilder = _dbSet;

        if (rebuild.IndexOf(".") != -1)
        {
            queryBuilder = _dbSet.Include(rebuild.Remove(rebuild.LastIndexOf(".")));
        }

        var data = queryBuilder.AsEnumerable()
            .Where(x =>
            {
                var y = TraversePropertyTree(x, props);
                if (y == null) return false;
                return spec.Contains(y);
            })
            .ToList();

        return data.Select(x => x.Id).ToList();
    }

    /*
     * Order everything by property and direction
     * EfVueCrust will use the full order to order subsets
     */
    public List<TKey> Order(string propertyPath, int direction)
    {

        var props = PathToParts(propertyPath);
        var rebuild = String.Join(".", props);
        IQueryable<TModel> queryBuilder = _dbSet;

        if (rebuild.IndexOf(".") != -1)
        {
            queryBuilder = _dbSet.Include(rebuild.Remove(rebuild.LastIndexOf(".")));
        }

        if (direction == 1)
        {
            return queryBuilder.AsEnumerable()
                .OrderBy(x =>
                {
                    return TraversePropertyTree(x, props);
                })
                .Select(x => x.Id)
                .ToList();
        }
        return queryBuilder.AsEnumerable()
            .OrderByDescending(x =>
            {
                return TraversePropertyTree(x, props);
            })
            .Select(x => x.Id)
            .ToList();
    }

    /*
     * turn our camel-cased path into an array of title-cased strings
     */
    public static string[] PathToParts(string propertyPath)
    {
        if(propertyPath.IndexOf(".") == -1)
        {
            return new string[] { char.ToUpper(propertyPath[0]) + propertyPath[1..] };
        }
        var props = propertyPath.Split(".");
        for (int i = 0, l = props.Length; i < l; ++i)
        {
            props[i] = char.ToUpper(props[i][0]) + props[i][1..];
        }
        return props;
    }


    //steps through the properties and returns whatever it finds at the end
    public static object? TraversePropertyTree(object? y, string[] props)
    {
        foreach (var prop in props)
        {
            if (y == null)
            {
                break;
            }
            if (y?.GetType().Namespace == "System.Collections.Generic")
            {
                var o = (dynamic)y;
                foreach (object m in o)
                {
                    y = m;
                    break;
                }
            }
            y = y?.GetType().GetProperty(prop)?.GetValue(y);
        }
        return y;
    }
}
