using Learnix.Domain.Enums;

namespace Learnix.Domain.Entities;

public class PostLesson : Lesson
{
    private PostLesson() { }

    private PostLesson(Guid sectionId, string title, string content)
        : base(sectionId, title, LessonType.Post)
    {
        Content = content;
    }

    /// <summary>Average adult reading speed for prose, in words per minute.</summary>
    private const int WordsPerMinute = 200;

    public string Content { get; private set; } = null!;

    /// <summary>
    /// How long the post takes to read, so it can sit next to a video's real duration in the
    /// curriculum. An estimate from the word count, never below a minute — a reader who opens a
    /// lesson spends some time on it even if it is two sentences long.
    /// </summary>
    public int EstimatedReadingSeconds
    {
        get
        {
            var words = Content.Split(
                [' ', '\t', '\n', '\r'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

            var seconds = (int)Math.Ceiling(words * 60d / WordsPerMinute);

            return Math.Max(seconds, 60);
        }
    }

    public override bool IsPublishReady() => !string.IsNullOrWhiteSpace(Content);

    public static PostLesson Create(
        Guid sectionId,
        string title,
        string content)
    {
        var lesson = new PostLesson(sectionId, title, content);

        if (lesson.IsPublishReady())
        {
            lesson.IsHidden = false;
        }

        return lesson;
    }

    public void UpdatePost(string title, string content)
    {
        UpdateTitle(title);
        Content = content;
        EvaluateVisibility();
    }
}
