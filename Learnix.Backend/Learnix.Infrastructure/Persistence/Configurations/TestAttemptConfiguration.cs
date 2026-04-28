using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Learnix.Infrastructure.Persistence.Configurations;

public sealed class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    public void Configure(EntityTypeBuilder<TestAttempt> builder)
    {
        builder.ToTable("TestAttempts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CourseId).IsRequired();
        builder.Property(a => a.TestLessonId).IsRequired();
        builder.Property(a => a.StudentId).IsRequired();
        builder.Property(a => a.AttemptNumber).IsRequired();
        builder.Property(a => a.StartedAt).IsRequired();

        builder.OwnsMany(a => a.Answers, ab => ab.ToJson());

        builder.Navigation(a => a.Answers)
            .HasField("_answers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(a => a.TestLessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.StudentId, a.TestLessonId });
    }
}
