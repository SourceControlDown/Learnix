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

    public string Content { get; private set; } = null!;

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
