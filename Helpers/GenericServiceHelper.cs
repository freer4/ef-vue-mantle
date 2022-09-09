﻿using Microsoft.EntityFrameworkCore;

namespace EfVueMantle;

public class GenericServiceHelper<T>
    where T : ModelBase

{
    public DbSet<T> _dbSet;

    public GenericServiceHelper(DbSet<T> dbSet)
    {
        _dbSet = dbSet;
    }


    /*
     * Takes a property name (or path in dot notation) and a string to look for,
     * returns a list of ids for any case-insensitive exact matches
     */
    public List<int> Equals(string propertyPath, string spec)
    {
        //case-insensitive
        //TODO create a case-sensitive version
        spec = spec.Trim().ToLower();

        var props = PathToParts(propertyPath);

        var rebuild = String.Join(".", props);
        var includePath = rebuild.Remove(rebuild.LastIndexOf("."));

        IQueryable<T> queryBuilder = _dbSet.Include(includePath);

        return queryBuilder.AsEnumerable()
            .Where(x =>
            {
                var y = TraversePropertyTree(x, props);

                if (y?.GetType().IsEnum != null)
                {
                    return (int)y == int.Parse(spec);
                }
                return y?.ToString()?.ToLower() == spec;
            })
            .Select(x => x.Id)
            .ToList();
    }


    /*
     * Takes a property name (or path in dot notation) and a string to look for,
     * returns a list of ids for values containing spec
     */
    public List<int> Contains(string propertyPath, string spec)
    {

        //case-insensitive
        //TODO create a case-sensitive version
        spec = spec.Trim().ToLower();

        var props = PathToParts(propertyPath);

        var rebuild = String.Join(".", props);
        var includePath = rebuild.Remove(rebuild.LastIndexOf("."));

        IQueryable<T> queryBuilder = _dbSet.Include(includePath);

        return queryBuilder.AsEnumerable()
            .Where(x =>
            {
                var y = TraversePropertyTree(x, props);
                return y?.ToString()?.ToLower().Contains(spec.ToString()) ?? false;
            })
            .Select(x => x.Id)
            .ToList();
    }

    /*
     * Order everything by property and direction
     * EfVueCrust will use the full order to order subsets
     */
    public List<int> Order(string propertyPath, int direction)
    {

        var props = PathToParts(propertyPath);

        var rebuild = String.Join(".", props);
        var includePath = rebuild.Remove(rebuild.LastIndexOf("."));

            IQueryable<T> queryBuilder = _dbSet.Include(includePath);

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