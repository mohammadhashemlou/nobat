using MediatR;
using Nobat.Application.Common.Interfaces;
using Nobat.Domain.Common;

namespace Nobat.Application.Modules.Services.Commands;

public record DeleteServiceCommand(Guid Id) : IRequest<Unit>;

public class DeleteServiceHandler : IRequestHandler<DeleteServiceCommand, Unit>
{
    private readonly IApplicationDbContext _db;

    public DeleteServiceHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteServiceCommand request, CancellationToken ct)
    {
        var service = await _db.Services.FindAsync([request.Id], ct)
            ?? throw new DomainException("خدمت یافت نشد", "NOT_FOUND");

        // Soft delete to preserve appointment history integrity
        service.IsActive = false;
        service.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
