using Ardalis.Specification;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetCourseContextForAi;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.UnitTests.AiChat.Queries.GetCourseContextForAi;

public class GetCourseContextForAiQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly ICourseRepository _courseRepository = Substitute.For<ICourseRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ILessonRepository _lessonRepository = Substitute.For<ILessonRepository>();
    private readonly GetCourseContextForAiQueryHandler _sut;

    private static readonly Guid StudentId = Guid.NewGuid();

    public GetCourseContextForAiQueryHandlerTests()
    {
        _currentUser.UserId.Returns(StudentId);
        _sut = new GetCourseContextForAiQueryHandler(
            _currentUser, _enrollmentRepository, _courseRepository, _categoryRepository,
            _userRepository, _lessonRepository);
    }

    private void Enrolled(bool value) =>
        _enrollmentRepository
            .AnyAsync(Arg.Any<ISpecification<Enrollment>>(), Arg.Any<CancellationToken>())
            .Returns(value);

    private void CourseIs(Course course) =>
        _courseRepository
            .FirstOrDefaultAsync(Arg.Any<ISingleResultSpecification<Course>>(), Arg.Any<CancellationToken>())
            .Returns(course);

    private void Completed(Guid courseId, params Guid[] lessonIds) =>
        _lessonRepository
            .GetVisibleLessonCompletionAsync(StudentId, courseId, Arg.Any<CancellationToken>())
            .Returns(lessonIds.Select(id => new LessonCompletion(id, true)).ToList());

    private Task<FluentResults.Result<CourseContextForAiDto>> Act(Guid courseId, Guid? lessonId) =>
        _sut.Handle(new GetCourseContextForAiQuery(courseId, lessonId), CancellationToken.None);

    [Fact]
    public async Task Fails_with_forbidden_when_the_student_is_not_enrolled()
    {
        // Arrange
        Enrolled(false);

        // Act
        var result = await Act(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<ForbiddenError>();
        await _courseRepository.DidNotReceiveWithAnyArgs()
            .FirstOrDefaultAsync(default(ISingleResultSpecification<Course>)!, default);
    }

    [Fact]
    public async Task Fails_with_not_found_when_the_course_is_gone()
    {
        // Arrange
        Enrolled(true);
        CourseIs(null!);

        // Act
        var result = await Act(Guid.NewGuid(), null);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task Names_the_course_and_marks_the_lesson_the_student_has_open()
    {
        // Arrange
        Enrolled(true);
        var course = BuildCourse(sections: 2, lessonsPerSection: 3);
        var lessons = VisibleLessons(course);
        CourseIs(course);
        Completed(course.Id, lessons[0].Id, lessons[1].Id);

        // Act
        var dto = (await Act(course.Id, lessons[4].Id)).Value;

        // Assert
        dto.Title.Should().Be("C# Advanced");
        dto.TotalLessons.Should().Be(6);
        dto.CompletedLessons.Should().Be(2);
        dto.OutlineCollapsed.Should().BeFalse();

        var open = dto.Sections.SelectMany(s => s.Lessons!).Where(l => l.IsCurrent).ToList();
        open.Should().ContainSingle().Which.Title.Should().Be(lessons[4].Title);

        dto.Sections[0].Lessons![0].IsCompleted.Should().BeTrue();
        dto.Sections[0].Lessons![2].IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Hidden_lessons_never_reach_the_outline()
    {
        // Arrange
        Enrolled(true);
        var course = BuildCourse(sections: 1, lessonsPerSection: 3);
        var hidden = PostLesson.Create(course.Sections.First().Id, "Draft", "not ready");
        course.AddLesson(hidden);
        course.ToggleLessonVisibility(hidden, false);
        CourseIs(course);

        // Act
        var dto = (await Act(course.Id, null)).Value;

        // Assert
        dto.TotalLessons.Should().Be(3);
        dto.Sections[0].Lessons.Should().NotContain(l => l.Title == "Draft");
    }

    [Fact]
    public async Task A_course_too_large_to_list_keeps_titles_only_around_the_current_section()
    {
        // Arrange
        Enrolled(true);
        var course = BuildCourse(sections: 8, lessonsPerSection: 10);
        var lessons = VisibleLessons(course);
        CourseIs(course);

        // Act — section index 3 (the 4th), i.e. lessons 30..39
        var dto = (await Act(course.Id, lessons[35].Id)).Value;

        // Assert
        dto.TotalLessons.Should().Be(80);
        dto.TotalLessons.Should().BeGreaterThan(AiChatToolLimits.CourseOutlineExpandedLessons);
        dto.OutlineCollapsed.Should().BeTrue();

        // Every section is still named — only the titles of distant ones are dropped.
        dto.Sections.Should().HaveCount(8);
        dto.Sections.Should().OnlyContain(s => s.LessonCount == 10);
        dto.Sections.Where(s => s.Lessons is not null).Select(s => s.Number)
            .Should().Equal(3, 4, 5);
        dto.Sections[3].Lessons!.Should().Contain(l => l.IsCurrent);
    }

    [Fact]
    public async Task Without_an_open_lesson_the_collapsed_outline_falls_back_to_the_first_sections()
    {
        // Arrange
        Enrolled(true);
        var course = BuildCourse(sections: 8, lessonsPerSection: 10);
        CourseIs(course);

        // Act
        var dto = (await Act(course.Id, null)).Value;

        // Assert
        dto.OutlineCollapsed.Should().BeTrue();
        dto.Sections.Where(s => s.Lessons is not null).Select(s => s.Number).Should().Equal(1, 2);
        dto.Sections.SelectMany(s => s.Lessons ?? []).Should().NotContain(l => l.IsCurrent);
    }

    private static Course BuildCourse(int sections, int lessonsPerSection)
    {
        var course = Course.Create(Guid.NewGuid(), Guid.NewGuid(), "C# Advanced", "Deep dive", 10m);

        for (var s = 0; s < sections; s++)
        {
            var section = course.AddSection($"Section {s + 1}");

            for (var l = 0; l < lessonsPerSection; l++)
            {
                var lesson = PostLesson.Create(section.Id, $"Lesson {s + 1}.{l + 1}", "body");
                course.AddLesson(lesson);
                course.ToggleLessonVisibility(lesson, true);
            }
        }

        return course;
    }

    private static List<Lesson> VisibleLessons(Course course) => course.Sections
        .OrderBy(s => s.DisplayOrder)
        .SelectMany(s => s.Lessons.Where(l => !l.IsHidden).OrderBy(l => l.DisplayOrder))
        .ToList();
}
