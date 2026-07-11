using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Certificates.Abstractions;

public interface ICertificateRepository : IRepositoryBase<Certificate>
{
    /// <summary>
    /// Stages the certificate without saving. Unlike <c>AddAsync</c>, which commits on its own, this
    /// leaves the unit of work to the caller — so the certificate lands in the same transaction as
    /// the enrollment and lesson progress that earned it.
    /// </summary>
    void Add(Certificate certificate);
}
