# JKangEventSourcingWithCap.EfCoreAndSqlServer
 Extends the JKang.EventSourcing libraries with "Outbox Pattern" functionality provided by the DotNetCore.CAP library. This allows integration events to be published when (and only when) a domain event is successfully persisted to the event stream. This library adds functionality specific to using EntityFramework Core with SqlServer.
