using Learnix.Domain.Common;

namespace Learnix.Domain.Entities;

public class Section : BaseEntity
{
    private readonly List<Lesson> _lessons = [];

    private Section() { }

    private Section(Guid courseId, string title, int order)
    {
        CourseId = courseId;
        Title = title;
        Order = order;
    }

    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = null!;
    public int Order { get; private set; }

    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    public static Section Create(Guid courseId, string title, int order)
        => new(courseId, title, order);

    public void UpdateTitle(string title) => Title = title;
    public void SetOrder(int order) => Order = order;
}