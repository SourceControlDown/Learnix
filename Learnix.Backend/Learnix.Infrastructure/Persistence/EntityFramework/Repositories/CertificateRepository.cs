using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class CertificateRepository(ApplicationDbContext context)
    : RepositoryBase<Certificate>(context), ICertificateRepository
{
    public void Add(Certificate certificate) => context.Set<Certificate>().Add(certificate);
}
