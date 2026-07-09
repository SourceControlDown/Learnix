using System.Runtime.CompilerServices;

// Section/Lesson mutators are internal so that Course stays the single entry point for
// structure mutations (ADR-009). Tests need that same access to build an aggregate whose
// published-state invariants can be exercised.
[assembly: InternalsVisibleTo("Learnix.Domain.UnitTests")]
