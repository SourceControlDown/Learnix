using Ardalis.Specification;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Application.Messaging.Specifications;

public sealed class ConversationsByStudentSpecification : Specification<CourseConversation>
{
    public ConversationsByStudentSpecification(Guid studentId, int skip, int take, string? searchQuery)
    {
        Query
            .Where(c => c.StudentId == studentId)
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchPattern = $"%{searchQuery}%";
            Query.Where(c => 
                EF.Functions.ILike(c.Instructor!.FirstName + " " + c.Instructor!.LastName, searchPattern) ||
                EF.Functions.ILike(c.Course!.Title, searchPattern)
            );
        }
    }
}
