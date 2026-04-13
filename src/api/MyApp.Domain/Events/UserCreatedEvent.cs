using MyApp.Domain.Common;

namespace MyApp.Domain.Events;

public record UserCreatedEvent(Guid UserId, string Email) : IDomainEvent;
