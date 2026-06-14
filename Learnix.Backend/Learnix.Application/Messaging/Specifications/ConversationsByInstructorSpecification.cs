using Ardalis.Specification;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Application.Messaging.Specifications;

public sealed class ConversationsByInstructorSpecification : Specification<CourseConversation>
{
    public ConversationsByInstructorSpecification(Guid instructorId, int skip, int take, string? searchQuery)
    {
        Query
            .Where(c => c.InstructorId == instructorId)
            .Include(c => c.Course)
            .Include(c => c.Student)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var searchPattern = $"%{searchQuery}%";
            Query.Where(c => 
                EF.Functions.ILike(c.Student!.FirstName + " " + c.Student!.LastName, searchPattern) ||
                EF.Functions.ILike(c.Course!.Title, searchPattern)
            );
        }
    }
}
