using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Persistence.EntityFramework.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.UnitTests.Persistence.Interceptors;

/// <summary>
/// A soft-deleted principal is loaded with its children (CourseCommandHandler always includes Sections),
/// and EF cascades every loaded child into Deleted the moment the principal is. Turning only the
/// principal back into an UPDATE leaves those children on their way to a real DELETE — a course whose
/// row survives but whose content is gone, so Recover() returns an empty shell.
/// </summary>
public class SoftDeleteCascadeTests
{
    [Fact]
    public async Task SoftDeletingACourse_KeepsItsSectionsInTheDatabase()
    {
        await using var context = Context();
        var course = CourseWithContent();

        context.Add(course);
        await context.SaveChangesAsync();

        context.Remove(course);
        await context.SaveChangesAsync();

        var sections = await context.Sections.IgnoreQueryFilters().CountAsync();
        sections.Should().Be(1);
    }

    [Fact]
    public async Task SoftDeletingACourse_KeepsTheLessonsUnderItsSections()
    {
        await using var context = Context();
        var course = CourseWithContent();

        context.Add(course);
        await context.SaveChangesAsync();

        context.Remove(course);
        await context.SaveChangesAsync();

        var lessons = await context.Lessons.IgnoreQueryFilters().CountAsync();
        lessons.Should().Be(1);
    }

    [Fact]
    public async Task SoftDeletingACourse_StillSoftDeletesTheCourse()
    {
        await using var context = Context();
        var course = CourseWithContent();

        context.Add(course);
        await context.SaveChangesAsync();

        context.Remove(course);
        await context.SaveChangesAsync();

        var saved = await context.Courses.IgnoreQueryFilters().SingleAsync();
        saved.IsDeleted.Should().BeTrue();
    }

    private static Course CourseWithContent()
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "C#", "Course", 10m);
        course.AddSection("Basics");
        course.AddLesson(VideoLesson.Create(course.Sections.Single().Id, "Intro", "videos/intro.mp4"));

        return course;
    }

    private static ApplicationDbContext Context()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor())
            .Options;

        return new ApplicationDbContext(options);
    }
}
