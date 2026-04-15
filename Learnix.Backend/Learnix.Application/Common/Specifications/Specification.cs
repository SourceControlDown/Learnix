using System.Linq.Expressions;

namespace Learnix.Application.Common.Specifications;

public abstract class Specification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
    public int Take { get; protected set; }
    public int Skip { get; protected set; }
    public bool IsPagingEnabled { get; protected set; }
    public bool AsNoTracking { get; protected set; } = true;

    protected void AddInclude(Expression<Func<T, object>> include) => Includes.Add(include);
    protected void AddInclude(string includeString) => IncludeStrings.Add(includeString);
    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderBy) => OrderByDescending = orderBy;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void DisableTracking() => AsNoTracking = false;
}
