using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using System.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

//Thanks to Svyatoslav Danyliv @stackoverflow.com
namespace EfVueMantle.Helpers;

public static class ProjectionExtensions
{
    public const string CustomProjectionAnnotation = "custom:member_projection";

    public class ProjectionInfo
    {
        public ProjectionInfo(MemberInfo member, LambdaExpression expression)
        {
            Member = member;
            Expression = expression;
        }

        public MemberInfo Member { get; }
        public LambdaExpression Expression { get; }
    }
    public static bool DesignTime { get; }
        = Process.GetProcesses().Where(x => x.ProcessName == "dotnet-ef").Any();
        

        public static EntityTypeBuilder<TEntity> WithProjection<TEntity, TValue>(
        this EntityTypeBuilder<TEntity> entity,
        Expression<Func<TEntity, TValue>> propExpression,
        Expression<Func<TEntity, TValue>> assignmentExpression)
        where TEntity : class
    {

        if (DesignTime)
            return entity;

        var annotation = entity.Metadata.FindAnnotation(CustomProjectionAnnotation);
        var projections = annotation?.Value as List<ProjectionInfo> ?? new List<ProjectionInfo>();

        if (propExpression.Body is not MemberExpression memberExpression)
            throw new InvalidOperationException($"'{propExpression.Body}' is not member expression");

        if (memberExpression.Expression is not ParameterExpression)
            throw new InvalidOperationException($"'{memberExpression.Expression}' is not parameter expression. Only single nesting is allowed");

        // removing duplicate
        projections.RemoveAll(p => p.Member == memberExpression.Member);

        projections.Add(new ProjectionInfo(memberExpression.Member, assignmentExpression));
        return entity.HasAnnotation(CustomProjectionAnnotation, projections);
    }

    public static IQueryable<TEntity> ApplyCustomProjection<TEntity>(this IQueryable<TEntity> query, DbContext context)
        where TEntity : class
    {
        var et = context.Model.FindEntityType(typeof(TEntity));
        var projections = et?.FindAnnotation(CustomProjectionAnnotation)?.Value as List<ProjectionInfo>;

        // nothing to do
        if (projections == null || et == null)
            return query;

        var propertiesForProjection = et.GetProperties().Where(p =>
            p.PropertyInfo != null && projections.All(pr => pr.Member != p.PropertyInfo))
            .ToList();

        var entityParam = Expression.Parameter(typeof(TEntity), "e");

        var memberBinding = new MemberBinding[propertiesForProjection.Count + projections.Count];
        for (int i = 0; i < propertiesForProjection.Count; i++)
        {
            var propertyInfo = propertiesForProjection[i].PropertyInfo!;
            memberBinding[i] = Expression.Bind(propertyInfo, Expression.MakeMemberAccess(entityParam, propertyInfo));
        }

        for (int i = 0; i < projections.Count; i++)
        {
            var projection = projections[i];
            var expression = projection.Expression.Body;

            var assignExpression = ReplacingExpressionVisitor.Replace(projection.Expression.Parameters[0], entityParam, expression);

            memberBinding[propertiesForProjection.Count + i] = Expression.Bind(projection.Member, assignExpression);
        }

        var memberInit = Expression.MemberInit(Expression.New(typeof(TEntity)), memberBinding);

        var selectLambda = Expression.Lambda<Func<TEntity, TEntity>>(memberInit, entityParam);

        var newQuery = query.Select(selectLambda);
        return newQuery;
    }
}