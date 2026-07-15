using Learnix.Application.Common.Events;
using Learnix.Domain.Common;
using Learnix.Domain.Entities;
using Learnix.Domain.Events.Category;
using Learnix.Domain.Events.Lessons;
using Learnix.Infrastructure.Modules;
using Learnix.Infrastructure.Outbox;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Learnix.Infrastructure.UnitTests.Persistence.Interceptors;

public class PrepareForDeletionInterceptorTests
{
    private const string ImagePath = "categories/programming.png";

    [Fact]
    public async Task HardDeletedEntity_ReleasesItsBlob()
    {
        // The whole point: a delete handler that never heard of PrepareForDeletion still gets its
        // category's image released instead of orphaning it in blob storage.
        await using var context = ContextWith(new PrepareForDeletionInterceptor());
        var category = await SeedCategoryWithImage(context);

        context.Remove(category);
        await context.SaveChangesAsync();

        category.DomainEvents.Should().ContainSingle(e => e is CategoryImageRemovedDomainEvent);
    }

    [Fact]
    public async Task SoftDeletedEntity_KeepsItsBlob()
    {
        // A soft-deleted row can be recovered, so releasing its blob would resurrect an entity whose
        // file no longer exists.
        await using var context = ContextWith(new PrepareForDeletionInterceptor());
        var recoverable = new RecoverableEntity();
        context.Add(recoverable);
        await context.SaveChangesAsync();
        recoverable.ClearDomainEvents();

        context.Remove(recoverable);
        await context.SaveChangesAsync();

        recoverable.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task EntityThatIsOnlyUpdated_KeepsItsBlob()
    {
        await using var context = ContextWith(new PrepareForDeletionInterceptor());
        var category = await SeedCategoryWithImage(context);

        category.Rename("Backend", "backend");
        await context.SaveChangesAsync();

        category.DomainEvents.Should().BeEmpty();
        category.ImageBlobPath.Should().Be(ImagePath);
    }

    [Fact]
    public async Task ReleasingTheSameBlobIsRequestedOnlyOnce()
    {
        // Course.RemoveLesson prepares its child explicitly and the interceptor sweeps it again — the
        // blob must not be queued for deletion twice.
        await using var context = ContextWith(new PrepareForDeletionInterceptor());
        var category = await SeedCategoryWithImage(context);

        category.PrepareForDeletion();
        context.Remove(category);
        await context.SaveChangesAsync();

        category.DomainEvents.Should().ContainSingle(e => e is CategoryImageRemovedDomainEvent);
    }

    [Fact]
    public async Task RunningBeforeTheDomainEventsInterceptor_GetsTheReleaseDispatched()
    {
        // The registration order PersistenceModule uses. Events raised here are still in front of the
        // sweep that collects them, so the release reaches the outbox.
        var publisher = Substitute.For<IPublisher>();
        await using var context = ContextWith(
            new PrepareForDeletionInterceptor(),
            DomainEventsInterceptorWith(publisher));

        var category = await SeedCategoryWithImage(context);

        context.Remove(category);
        await context.SaveChangesAsync();

        await publisher.Received(1).Publish(ImageReleaseNotification(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunningAfterTheDomainEventsInterceptor_LosesTheRelease()
    {
        // Guards the ordering contract itself: flip the two and the release is raised after the sweep
        // that collects domain events, so nothing ever dispatches it and the blob leaks. If someone
        // reorders PersistenceModule, this is what they would be buying.
        var publisher = Substitute.For<IPublisher>();
        await using var context = ContextWith(
            DomainEventsInterceptorWith(publisher),
            new PrepareForDeletionInterceptor());

        var category = await SeedCategoryWithImage(context);

        context.Remove(category);
        await context.SaveChangesAsync();

        await publisher.DidNotReceive().Publish(ImageReleaseNotification(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LessonsCascadedAwayWithTheirSection_ReleaseTheirVideos()
    {
        // Course.RemoveSection drops the section from the aggregate and never touches the lessons under
        // it — EF cascades them into Deleted, and the sweep is the only thing that asks them for their
        // videos. Uses the real ApplicationDbContext so the cascade runs through the real mapping.
        await using var context = ApplicationContextWith(new PrepareForDeletionInterceptor());

        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "C#", "Course", 10m);
        course.AddSection("Basics");
        var section = course.Sections.Single();
        var lesson = VideoLesson.Create(section.Id, "Intro", "videos/intro.mp4");
        course.AddLesson(lesson);

        context.Add(course);
        await context.SaveChangesAsync();
        lesson.ClearDomainEvents();

        course.RemoveSection(section.Id);
        await context.SaveChangesAsync();

        lesson.DomainEvents.Should().ContainSingle(e => e is LessonVideoReleasedDomainEvent);
    }

    [Fact]
    public async Task SoftDeletingACourse_KeepsTheVideosOfItsLessons()
    {
        // The course row survives and can be recovered, so its lessons — and their videos — must too.
        await using var context = ApplicationContextWith(new PrepareForDeletionInterceptor());

        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "C#", "Course", 10m);
        course.AddSection("Basics");
        var lesson = VideoLesson.Create(course.Sections.Single().Id, "Intro", "videos/intro.mp4");
        course.AddLesson(lesson);

        context.Add(course);
        await context.SaveChangesAsync();
        lesson.ClearDomainEvents();

        context.Remove(course);
        await context.SaveChangesAsync();

        lesson.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PersistenceModule_RegistersTheInterceptorsInTheOrderTheReleaseDependsOn()
    {
        // The two dispatch tests above show what the wrong order buys; this one is what actually holds
        // production to the right one — the rescue of cascaded children first, the release second, the
        // dispatch of what the release raised last.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=learnix;Username=u;Password=p"
            })
            .Build();

        var provider = new ServiceCollection()
            .AddLogging()
            .AddPersistence(configuration)
            .BuildServiceProvider();

        var interceptors = provider
            .GetRequiredService<DbContextOptions<ApplicationDbContext>>()
            .FindExtension<CoreOptionsExtension>()!
            .Interceptors!
            .Select(i => i.GetType())
            .ToList();

        interceptors.Should().ContainInOrder(
            typeof(SoftDeleteInterceptor),
            typeof(PrepareForDeletionInterceptor),
            typeof(DomainEventsInterceptor));
    }

    /// <summary>
    /// The interceptor builds the notification by reflection, so it lands on IPublisher's object
    /// overload rather than the generic one — match on the runtime type.
    /// </summary>
    private static object ImageReleaseNotification()
        => Arg.Is<object>(n => n is DomainEventNotification<CategoryImageRemovedDomainEvent>);

    private static async Task<Category> SeedCategoryWithImage(TestDbContext context)
    {
        var category = Category.Create("Programming", "programming");
        category.SetImage(ImagePath);

        context.Add(category);
        await context.SaveChangesAsync();
        category.ClearDomainEvents();

        return category;
    }

    /// <summary>
    /// Wired the way production is: SoftDelete rescues the children EF cascaded behind a soft-deleted
    /// principal, and only then does the sweep decide whose blobs are really leaving.
    /// </summary>
    private static ApplicationDbContext ApplicationContextWith(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors([new SoftDeleteInterceptor(), .. interceptors])
            .Options;

        return new ApplicationDbContext(options);
    }

    private static TestDbContext ContextWith(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptors)
            .Options;

        return new TestDbContext(options);
    }

    /// <summary>The real interceptor, wired to a publisher we can assert on.</summary>
    private static DomainEventsInterceptor DomainEventsInterceptorWith(IPublisher publisher)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => publisher);
        services.AddScoped<OutboxDbContextHolder>();

        return new DomainEventsInterceptor(services.BuildServiceProvider());
    }

    /// <summary>
    /// Stands in for Course/User: soft-deletable, and owning an external resource it would release if
    /// anything ever asked it to. Nothing may.
    /// </summary>
    private sealed class RecoverableEntity : SoftDeletableEntity
    {
        protected override void OnPreparingForDeletion()
            => RaiseDomainEvent(new RecoverableEntityDeletedEvent(Id));
    }

    private sealed record RecoverableEntityDeletedEvent(Guid EntityId) : DomainEvent;

    /// <summary>
    /// A minimal context: the interceptor only ever looks at entity state and CLR types, so the real
    /// ApplicationDbContext (Identity, Npgsql, query filters) would only add noise.
    /// </summary>
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Category>().Ignore(c => c.DomainEvents);
            builder.Entity<RecoverableEntity>().Ignore(e => e.DomainEvents);
        }
    }
}
