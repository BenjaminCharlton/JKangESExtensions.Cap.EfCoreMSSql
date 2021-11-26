using DotNetCore.CAP;
using JKang.EventSourcing.Domain;
using JKang.EventSourcing.Events;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JKang.EventSourcing.Persistence.EfCore;

/// <summary>
/// Decorator for an event store of type
/// <see cref="JKang.EventSourcing.Persistence.EfCore.EfCoreEventStore{TEventDbContext, TAggregate, TKey}"/>, used to add functionality
/// from the <see cref="DotNetCore.CAP"/> library so that integration events will be published whenever a domain event is successfully
/// saved to the database, as part of the same transaction. This follows the Outbox Pattern.
/// </summary>
/// <typeparam name="TEventDbContext">The type of the Entity Framework <see cref="DbContext"/> that will be used for database transactions.</typeparam>
/// <typeparam name="TAggregate">The type of the domain entity (aggregate) stored in the event store.</typeparam>
/// <typeparam name="TKey">The type of the key used to identify domain entities (aggregates) stored in the event store.</typeparam>
public class EventStoreCapEFCoreDecorator<TEventDbContext, TAggregate, TKey> : EventStoreCapDecoratorBase<TAggregate, TKey>
    where TEventDbContext : DbContext, IEventDbContext<TAggregate, TKey>
    where TAggregate : IAggregate<TKey>
{
    private readonly TEventDbContext _context;

    /// <summary>
    /// Constructor for this class.
    /// </summary>
    /// <param name="context">The Entity Framework <see cref="DbContext"/> that will be used for database transactions.</param>
    /// <param name="inner">The event store (implementing <see cref="JKang.EventSourcing.Persistence.IEventStore{TAggregate, TKey}"/>)
    /// that this class will decorate.</param>
    /// <param name="publisher">A message publisher (implementing <see cref="DotNetCore.CAP.ICapPublisher"/>) that uses the
    /// outbox pattern to publish integration events when a database transaction succeeds.</param>
    public EventStoreCapEFCoreDecorator(EfCoreEventStore<TEventDbContext, TAggregate, TKey> inner,
        TEventDbContext context,
        ICapPublisher publisher,
        IMapDomainEventsToIntegrationEvents? domainEventToIntegrationEventMappings = null) :
        base(inner, publisher, domainEventToIntegrationEventMappings)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Adds a domain event to the event stream. If a mapping from the domain event to an equivalent integration event has been
    /// configured in the dependency injection container, then the integration event will be published whenever the database transaction
    /// succeeds.
    /// </summary>
    /// <param name="event">The domain event to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public override async Task AddEventAsync(IAggregateEvent<TKey> @event,
        CancellationToken cancellationToken = default)
    {
        using var transaction = _context.Database.BeginTransaction(Publisher, autoCommit: true);
        await base.AddEventAsync(@event, cancellationToken);

        var domainEventType = @event.GetType();

        if (DomainEventToIntegrationEventMappings.CanConvert(domainEventType))
        {
            dynamic integrationEvent = DomainEventToIntegrationEventMappings[domainEventType].Invoke(@event);
            string? callbackName = default;
            await Publisher.PublishAsync(domainEventType.Name, integrationEvent, callbackName, cancellationToken);
        }
    }
}