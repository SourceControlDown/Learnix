using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.UnitTests.Entities;

public class TestLessonTests
{
    private static TestLesson Create()
        => TestLesson.Create(Guid.NewGuid(), "Lesson");

    [Fact]
    public void ReplaceQuestions_ShouldUpdateQuestionsCount()
    {
        // Arrange
        var lesson = Create();
        var blueprint = new QuestionBlueprint(
            "Question?",
            QuestionType.SingleChoice,
            [
                new QuestionOptionBlueprint("A", true),
                new QuestionOptionBlueprint("B", false)
            ],
            null);

        // Act
        lesson.ReplaceQuestions([blueprint]);

        // Assert
        lesson.QuestionsCount.Should().Be(1);
        lesson.MaxScore.Should().Be(1);
    }
}
