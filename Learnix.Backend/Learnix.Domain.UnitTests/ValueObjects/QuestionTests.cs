using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.UnitTests.ValueObjects;

public class QuestionTests
{
    // Choice questions

    [Fact]
    public void IsAnsweredCorrectly_WhenSingleChoicePicksTheCorrectOption_ShouldBeTrue()
    {
        // Arrange
        var question = SingleChoice(correctOrder: 1);

        // Act
        var result = question.IsAnsweredCorrectly(Answer(selected: [1]));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAnsweredCorrectly_WhenSingleChoiceSelectsTheCorrectOptionPlusAnother_ShouldBeFalse()
    {
        // Arrange
        var question = SingleChoice(correctOrder: 1);

        // Act
        var result = question.IsAnsweredCorrectly(Answer(selected: [0, 1]));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAnsweredCorrectly_WhenMultipleChoiceMatchesEveryCorrectOptionInAnyOrder_ShouldBeTrue()
    {
        // Arrange
        var question = MultipleChoice(correctOrders: [0, 2]);

        // Act
        var result = question.IsAnsweredCorrectly(Answer(selected: [2, 0]));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAnsweredCorrectly_WhenMultipleChoiceMissesOneCorrectOption_ShouldBeFalse()
    {
        // Arrange
        var question = MultipleChoice(correctOrders: [0, 2]);

        // Act
        var result = question.IsAnsweredCorrectly(Answer(selected: [0]));

        // Assert
        result.Should().BeFalse();
    }

    // Text answers — exact and case handling

    [Fact]
    public void IsAnsweredCorrectly_WhenCaseDiffersAndIgnoreCaseIsOff_ShouldBeFalse()
    {
        // Arrange
        var question = TextInput("Paris", ignoreCase: false, allowFuzzy: false);

        // Act & Assert
        question.IsAnsweredCorrectly(TextAnswer("paris")).Should().BeFalse();
        question.IsAnsweredCorrectly(TextAnswer("Paris")).Should().BeTrue();
    }

    [Fact]
    public void IsAnsweredCorrectly_WhenCaseDiffersAndIgnoreCaseIsOn_ShouldBeTrue()
    {
        // Arrange
        var question = TextInput("Paris", ignoreCase: true, allowFuzzy: false);

        // Act & Assert
        question.IsAnsweredCorrectly(TextAnswer("pArIs")).Should().BeTrue();
    }

    [Theory]
    [InlineData("  Paris")]
    [InlineData("Paris  ")]
    [InlineData("\tParis\n")]
    public void IsAnsweredCorrectly_WhenAnswerHasSurroundingWhitespace_ShouldTrimEvenWithIgnoreCaseOff(string given)
    {
        // Arrange — whitespace used to only be trimmed when IgnoreCase was on
        var question = TextInput("Paris", ignoreCase: false, allowFuzzy: false);

        // Act
        var result = question.IsAnsweredCorrectly(TextAnswer(given));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAnsweredCorrectly_WhenTextValueIsNull_ShouldBeFalseRatherThanThrow()
    {
        // Arrange
        var question = TextInput("Paris", ignoreCase: true, allowFuzzy: true);

        // Act
        var result = question.IsAnsweredCorrectly(new StudentAnswer(0, [], null));

        // Assert
        result.Should().BeFalse();
    }

    // Text answers — fuzzy matching

    [Fact]
    public void IsAnsweredCorrectly_WhenFuzzyIsOffAndAnswerHasATypo_ShouldBeFalse()
    {
        // Arrange
        var question = TextInput("Paris", ignoreCase: true, allowFuzzy: false);

        // Act
        var result = question.IsAnsweredCorrectly(TextAnswer("Pariss"));

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    // 1-2 characters: no tolerance, one edit would turn "C#" into "C" or "C++"
    [InlineData("C#", "C", false)]
    [InlineData("C#", "C++", false)]
    [InlineData("C#", "C#", true)]
    // 3-5 characters: one edit
    [InlineData("cat", "bat", true)]
    [InlineData("cat", "bet", false)]
    [InlineData("Paris", "Pariss", true)]
    [InlineData("Paris", "Parriss", false)]
    // 6+ characters: two edits
    [InlineData("Python", "Pyton", true)]
    [InlineData("Python", "Pithin", true)]
    [InlineData("Python", "Pithins", false)]
    [InlineData("JavaScript", "JavaScrip", true)]
    [InlineData("JavaScript", "JvScript", true)]
    [InlineData("JavaScript", "JvScrpt", false)]
    public void IsAnsweredCorrectly_WhenFuzzyIsOn_ShouldTolerateEditsAccordingToExpectedAnswerLength(
        string correctAnswer, string given, bool expected)
    {
        // Arrange
        var question = TextInput(correctAnswer, ignoreCase: true, allowFuzzy: true);

        // Act
        var result = question.IsAnsweredCorrectly(TextAnswer(given));

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsAnsweredCorrectly_WhenFuzzyIsOnAndAnswerIsEmpty_ShouldBeFalse()
    {
        // Arrange — an empty answer is within two edits of a two-letter word, but must never pass
        var question = TextInput("Go", ignoreCase: true, allowFuzzy: true);

        // Act
        var result = question.IsAnsweredCorrectly(TextAnswer(""));

        // Assert
        result.Should().BeFalse();
    }

    // Fixtures

    private static Question SingleChoice(int correctOrder) => new()
    {
        Text = "Pick one",
        Type = QuestionType.SingleChoice,
        Order = 0,
        Options = BuildOptions(3, correctOrder)
    };

    private static Question MultipleChoice(params int[] correctOrders) => new()
    {
        Text = "Pick all that apply",
        Type = QuestionType.MultipleChoice,
        Order = 0,
        Options = BuildOptions(3, correctOrders)
    };

    private static Question TextInput(string correctAnswer, bool ignoreCase, bool allowFuzzy) => new()
    {
        Text = "Type the answer",
        Type = QuestionType.TextInput,
        Order = 0,
        TextAnswer = new TextAnswerConfig
        {
            CorrectAnswer = correctAnswer,
            IgnoreCase = ignoreCase,
            AllowFuzzy = allowFuzzy
        }
    };

    private static List<QuestionOption> BuildOptions(int count, params int[] correctOrders) =>
        Enumerable.Range(0, count)
            .Select(i => new QuestionOption
            {
                Text = $"Option {i}",
                Order = i,
                IsCorrect = correctOrders.Contains(i)
            })
            .ToList();

    private static StudentAnswer Answer(params int[] selected) => new(0, [.. selected], null);

    private static StudentAnswer TextAnswer(string value) => new(0, [], value);
}
