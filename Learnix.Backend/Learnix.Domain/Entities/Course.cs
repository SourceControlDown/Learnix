using Learnix.Domain.Common;
using Learnix.Domain.Enums;
using Learnix.Domain.Events;

namespace Learnix.Domain.Entities;

public class Course : BaseEntity, ISoftDeletable
{
    private readonly List<Section> _sections = [];

    // EF
    private Course() { }

    private Course(
        Guid instructorId,
        Guid categoryId,
        string title,
        string description,
        decimal price,
        IEnumerable<string>? tags)
    {
        InstructorId = instructorId;
        CategoryId = categoryId;
        Title = title;
        Description = description;
        Price = price;
        Status = CourseStatus.Draft;
        EnrollmentsCount = 0;
        Tags = tags?.ToList() ?? [];

        RaiseDomainEvent(new CourseCreatedDomainEvent(Id, instructorId, categoryId));
    }

    public Guid InstructorId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? CoverImageUrl { get; private set; }
    public decimal Price { get; private set; }
    public CourseStatus Status { get; private set; }

    /// <summary>
    /// Denormalized counter. Update strategy (event-driven vs nightly job) — TBD, see ADR-041.
    /// Currently not maintained; defaults to 0.
    /// </summary>
    public int EnrollmentsCount { get; private set; }

    public List<string> Tags { get; private set; } = [];
    public IReadOnlyCollection<Section> Sections => _sections.AsReadOnly();

    public bool IsDeleted { get; private set; } = false;
    public DateTime? DeletedAt { get; private set; } = null;

    public static Course Create(
        Guid instructorId,
        Guid categoryId,
        string title,
        string description,
        decimal price,
        IEnumerable<string>? tags = null)
        => new(instructorId, categoryId, title, description, price, tags);

    public void UpdateDetails(
        Guid categoryId,
        string title,
        string description,
        decimal price,
        IEnumerable<string> tags)
    {
        CategoryId = categoryId;
        Title = title;
        Description = description;
        Price = price;
        Tags = tags.ToList();
    }

    public void SetCoverImage(string? coverImageUrl) => CoverImageUrl = coverImageUrl;

    /// <summary>
    /// Transitions course to Published. Invariants (see ADR-040):
    /// - CoverImageUrl must be set
    /// - Must have at least one section
    /// - At least one section must contain at least one lesson
    ///
    /// Handlers should pre-validate for UX; these throw as a last-line defence.
    /// </summary>
    public void Publish()
    {
        if (Status == CourseStatus.Published)
            return;

        if (string.IsNullOrWhiteSpace(CoverImageUrl))
            throw new InvalidOperationException("Course cannot be published without a cover image.");

        if (_sections.Count == 0)
            throw new InvalidOperationException("Course cannot be published without at least one section.");

        if (_sections.All(s => s.Lessons.Count == 0))
            throw new InvalidOperationException("Course cannot be published without at least one lesson.");

        Status = CourseStatus.Published;
        RaiseDomainEvent(new CoursePublishedDomainEvent(Id));
    }

    public void Unpublish()
    {
        if (Status != CourseStatus.Published)
            return;

        Status = CourseStatus.Draft;
        RaiseDomainEvent(new CourseUnpublishedDomainEvent(Id));
    }

    public void Archive()
    {
        if (Status == CourseStatus.Archived)
            return;

        Status = CourseStatus.Archived;
        RaiseDomainEvent(new CourseArchivedDomainEvent(Id));
    }

    /// <summary>
    /// Raises CourseDeletedDomainEvent. Call BEFORE repository.Delete() so the event
    /// is picked up by ChangeTracker.Entries&lt;IHasDomainEvents&gt;() during dispatch.
    /// SoftDeleteInterceptor will flip EntityState.Deleted → Modified with IsDeleted = true.
    /// </summary>
    public void MarkForDeletion()
        => RaiseDomainEvent(new CourseDeletedDomainEvent(Id));
}