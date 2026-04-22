using Learnix.Domain.Common;

namespace Learnix.Domain.Entities;

public class StudentAnswers : BaseEntity
{
    private StudentAnswers() { }

    public Guid StudentId {  get; set; }
}
