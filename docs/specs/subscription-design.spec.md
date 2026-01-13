# Subscription and Routing Specification

## Document Information

- **Version**: 1.0
- **Last Updated**: 2026-01-12
- **Status**: Active
- **Related Documents**: 
  - [Implementation Specification](../implementation.spec.md)
  - [Message Design Specification](./message-design.spec.md)

## 1. Overview

This specification defines the requirements for selective message subscription and efficient routing in Redis Pub/Sub-based microservices. The design addresses the challenge of 100+ message types where each service only needs ~10 messages, requiring efficient filtering without unnecessary deserialization or cyclical event handling.

## 2. Requirements

### REQ-SUB-001: Subscription Registry

**Requirement**: Each microservice MUST declare its message subscriptions through a registry at startup.

**Rationale**: Declarative configuration enables compile-time verification, prevents runtime subscription errors, and provides clear documentation of service dependencies.

**Acceptance Criteria**:
- AC-SUB-001.1: Registry MUST provide `GetSubscriptions()` returning list of SubscriptionDescriptor
- AC-SUB-001.2: Registry MUST provide `ShouldHandle(string messageType)` for O(1) lookup
- AC-SUB-001.3: Registry MUST provide `GetHandlerType(string messageType)` returning handler CLR type
- AC-SUB-001.4: SubscriptionDescriptor MUST include: ChannelPattern, MessageType, HandlerType
- AC-SUB-001.5: SubscriptionDescriptor MAY include: MaxConcurrency, RetryPolicy
- AC-SUB-001.6: Registry MUST be built at application startup before worker services start

### REQ-SUB-002: Subscription Builder Fluent API

**Requirement**: Services MUST configure subscriptions using a fluent builder API.

**Rationale**: Fluent API provides type-safe, discoverable configuration with compile-time checking of message and handler types.

**Acceptance Criteria**:
- AC-SUB-002.1: Builder MUST provide `Subscribe<TMessage, THandler>()` method
- AC-SUB-002.2: Builder MUST enforce that TMessage implements IMessage
- AC-SUB-002.3: Builder MUST enforce that THandler implements IMessageHandler<TMessage>
- AC-SUB-002.4: Builder MUST support optional configuration via Action<SubscriptionOptions>
- AC-SUB-002.5: Builder MUST support method chaining
- AC-SUB-002.6: Builder MUST produce immutable registry via `Build()` method

### REQ-SUB-003: Channel Pattern Subscriptions

**Requirement**: Services MUST subscribe to specific channels, not wildcard patterns, unless explicitly needed.

**Rationale**: Specific channel subscriptions reduce Redis overhead and prevent processing irrelevant messages.

**Acceptance Criteria**:
- AC-SUB-003.1: By default, subscribe to exact channel from MessageChannel attribute
- AC-SUB-003.2: Pattern subscriptions (e.g., "vehicles.*") MAY be used for domain-wide listening
- AC-SUB-003.3: Pattern subscriptions MUST use header peek to filter before full deserialization
- AC-SUB-003.4: Services SHOULD minimize pattern subscriptions (prefer specific channels)
- AC-SUB-003.5: Each subscription MUST map to exactly one Redis Pub/Sub subscription

### REQ-SUB-004: Message Handler Interface

**Requirement**: Message handlers MUST implement a standardized interface for processing messages.

**Rationale**: Consistent interface enables generic handler resolution, middleware composition, and testability.

**Acceptance Criteria**:
- AC-SUB-004.1: MUST define `IMessageHandler<TMessage>` interface with `HandleAsync(TMessage, MessageContext, CancellationToken)` method
- AC-SUB-004.2: MUST define non-generic `IMessageHandler` base interface for polymorphic invocation
- AC-SUB-004.3: MessageContext MUST include: Header, Channel, ReceivedAt, RetryCount, Services (IServiceProvider), Activity (tracing)
- AC-SUB-004.4: Handlers MUST be registered in DI container with scoped or transient lifetime
- AC-SUB-004.5: Handlers SHOULD derive from `MessageHandlerBase<TMessage>` for common functionality

### REQ-SUB-005: Background Worker Service

**Requirement**: Each microservice MUST run a background worker that subscribes to Redis and dispatches messages to handlers.

**Rationale**: Centralized worker enables lifecycle management, graceful shutdown, and consistent error handling.

**Acceptance Criteria**:
- AC-SUB-005.1: Worker MUST implement `BackgroundService` from Microsoft.Extensions.Hosting
- AC-SUB-005.2: Worker MUST subscribe to all channels from registry during ExecuteAsync
- AC-SUB-005.3: Worker MUST create a processing channel (System.Threading.Channels) per subscription
- AC-SUB-005.4: Worker MUST spawn processor tasks based on MaxConcurrency setting
- AC-SUB-005.5: Worker MUST support graceful shutdown (complete in-flight messages)
- AC-SUB-005.6: Worker MUST handle Redis disconnection and automatic reconnection

### REQ-SUB-006: Efficient Message Routing

**Requirement**: Workers MUST use PeekHeader to inspect message type before full deserialization.

**Rationale**: Deserializing messages the service doesn't handle wastes CPU. Header inspection enables early filtering.

**Acceptance Criteria**:
- AC-SUB-006.1: Upon receiving message, MUST call `PeekHeader(byte[])` to extract header and message type
- AC-SUB-006.2: MUST check `ShouldHandle(messageType)` before full deserialization
- AC-SUB-006.3: If message type not subscribed, MUST skip without deserialization
- AC-SUB-006.4: Only after determining message should be handled, MUST call `Deserialize<T>(byte[])`
- AC-SUB-006.5: Routing decision MUST complete in < 1 microsecond (O(1) lookup)

### REQ-SUB-007: Concurrency Control

**Requirement**: Subscriptions MUST support configurable concurrency per message type.

**Rationale**: Different messages have different processing times and resource needs. Some benefit from parallel processing, others must be sequential.

**Acceptance Criteria**:
- AC-SUB-007.1: SubscriptionDescriptor MUST include MaxConcurrency property (default: 1)
- AC-SUB-007.2: MaxConcurrency = 1 MUST process messages sequentially (order preserved)
- AC-SUB-007.3: MaxConcurrency > 1 MUST spawn multiple processor tasks per subscription
- AC-SUB-007.4: Processor tasks MUST read from shared channel (System.Threading.Channels)
- AC-SUB-007.5: Channel capacity MUST be bounded to prevent memory exhaustion (default: 1000)

### REQ-SUB-008: Retry Policy

**Requirement**: Subscriptions MUST support configurable retry policies for transient failures.

**Rationale**: Network blips, database deadlocks, and temporary resource unavailability should not lose messages.

**Acceptance Criteria**:
- AC-SUB-008.1: RetryPolicy MUST include: MaxRetries, InitialDelay, BackoffMultiplier
- AC-SUB-008.2: Default policy MUST be: MaxRetries=3, InitialDelay=1s, BackoffMultiplier=2.0
- AC-SUB-008.3: After MaxRetries exhausted, MUST log error and optionally send to dead-letter queue
- AC-SUB-008.4: Retry delay MUST use exponential backoff: delay = InitialDelay * (BackoffMultiplier ^ attempt)
- AC-SUB-008.5: Retries MUST increment MessageContext.RetryCount
- AC-SUB-008.6: CancellationToken MUST be honored during retry delays

### REQ-SUB-009: Idempotency

**Requirement**: Handlers MUST implement idempotency checks to prevent duplicate processing.

**Rationale**: Redis Pub/Sub provides at-most-once delivery. Combined with retries, this can lead to duplicates. Idempotency ensures exactly-once semantics.

**Acceptance Criteria**:
- AC-SUB-009.1: MUST provide `IIdempotencyStore` abstraction with HasProcessed/MarkProcessed methods
- AC-SUB-009.2: Idempotency key MUST combine: message type, aggregate ID, message ID
- AC-SUB-009.3: Before processing, MUST check `HasProcessedAsync(key)`
- AC-SUB-009.4: After successful processing, MUST call `MarkProcessedAsync(key, retention)`
- AC-SUB-009.5: For atomic check-and-set, MUST provide `TryProcessAsync(key)` returning bool
- AC-SUB-009.6: Retention period MUST be configurable (recommended: 7 days)
- AC-SUB-009.7: Implementation SHOULD use Redis with SET NX EX for atomic operations

### REQ-SUB-010: Cycle Detection

**Requirement**: System MUST detect and break cyclical event chains.

**Rationale**: Event-driven systems can create feedback loops (A publishes B, B publishes A). Unchecked, this causes infinite loops.

**Acceptance Criteria**:
- AC-SUB-010.1: MUST provide `ICycleDetector` interface with IsInCycle/RecordProcessing methods
- AC-SUB-010.2: Detection MUST use correlation ID to track event chains
- AC-SUB-010.3: Detection MUST count occurrences of (correlationId, messageType) tuple
- AC-SUB-010.4: If count exceeds MaxCycleDepth (default: 10), MUST return true from IsInCycle
- AC-SUB-010.5: Cycle detection state MUST expire after CycleWindowDuration (default: 5 minutes)
- AC-SUB-010.6: Handlers SHOULD be wrapped with CycleAwareHandlerDecorator
- AC-SUB-010.7: When cycle detected, MUST log warning and skip processing

### REQ-SUB-011: Causation Tracking

**Requirement**: When a handler publishes new messages, those messages MUST preserve causation chain.

**Rationale**: Understanding which message caused another is critical for debugging, cycle detection, and distributed tracing.

**Acceptance Criteria**:
- AC-SUB-011.1: MUST provide `IMessageContextAccessor` using AsyncLocal for current context
- AC-SUB-011.2: Worker MUST set current context before invoking handler
- AC-SUB-011.3: Publisher MUST read current context to populate CorrelationId and CausationId
- AC-SUB-011.4: New message CorrelationId MUST equal current message CorrelationId
- AC-SUB-011.5: New message CausationId MUST equal current message MessageId
- AC-SUB-011.6: If no current context, generate new CorrelationId (start of chain)

### REQ-SUB-012: Dependency Injection Integration

**Requirement**: Handlers and related services MUST be resolved from DI container with proper scoping.

**Rationale**: Handlers often need database contexts, repositories, and other scoped services. Proper DI scoping prevents lifetime issues.

**Acceptance Criteria**:
- AC-SUB-012.1: Worker MUST inject `IServiceScopeFactory`
- AC-SUB-012.2: Worker MUST create new scope per message processing
- AC-SUB-012.3: Handler MUST be resolved from scoped service provider
- AC-SUB-012.4: MessageContext.Services MUST contain scoped service provider for handler use
- AC-SUB-012.5: Scope MUST be disposed after message processing completes
- AC-SUB-012.6: All handlers MUST be registered in DI during startup

### REQ-SUB-013: Observability

**Requirement**: Subscription infrastructure MUST emit metrics, logs, and traces for monitoring.

**Rationale**: Understanding message flow, processing times, and failures is critical for operating distributed systems.

**Acceptance Criteria**:
- AC-SUB-013.1: MUST emit metric: messages_received (counter) with tags: message_type, channel
- AC-SUB-013.2: MUST emit metric: messages_processed (counter) with tags: message_type
- AC-SUB-013.3: MUST emit metric: messages_failed (counter) with tags: message_type, error_type
- AC-SUB-013.4: MUST emit metric: messages_skipped (counter) with tags: message_type, reason
- AC-SUB-013.5: MUST emit metric: processing_duration (histogram) with tags: message_type
- AC-SUB-013.6: MUST create Activity (distributed trace) per message with kind=Consumer
- AC-SUB-013.7: Activity MUST include tags: messaging.message_id, messaging.destination
- AC-SUB-013.8: MUST log at Debug level for each message received and processed
- AC-SUB-013.9: MUST log at Warning level for retries and cycle detection
- AC-SUB-013.10: MUST log at Error level for exhausted retries

## 3. Non-Functional Requirements

### NFR-SUB-001: Performance

**Requirement**: Subscription infrastructure MUST meet performance targets.

**Acceptance Criteria**:
- AC-NFR-SUB-001.1: Message routing decision (peek + lookup) MUST complete in < 1 microsecond
- AC-NFR-SUB-001.2: End-to-end message processing (receive → handler) MUST complete in < 10ms for simple handlers
- AC-NFR-SUB-001.3: Throughput MUST support 10,000+ messages/second per service instance
- AC-NFR-SUB-001.4: Memory overhead per subscription MUST be < 100KB
- AC-NFR-SUB-001.5: Channel queue backpressure MUST prevent unbounded memory growth

### NFR-SUB-002: Reliability

**Requirement**: Subscription infrastructure MUST be resilient to failures.

**Acceptance Criteria**:
- AC-NFR-SUB-002.1: MUST survive Redis disconnection and automatically reconnect
- AC-NFR-SUB-002.2: MUST survive handler exceptions without crashing worker
- AC-NFR-SUB-002.3: MUST support graceful shutdown (stop accepting new, complete in-flight)
- AC-NFR-SUB-002.4: MUST support circuit breaker pattern for failing handlers (optional)
- AC-NFR-SUB-002.5: Dead-letter queue MUST be available for poison messages (optional)

### NFR-SUB-003: Testing

**Requirement**: Subscription infrastructure MUST be testable.

**Acceptance Criteria**:
- AC-NFR-SUB-003.1: Handlers MUST be unit testable without Redis
- AC-NFR-SUB-003.2: Worker MUST be integration testable with in-memory Redis (e.g., TestContainers)
- AC-NFR-SUB-003.3: Subscription registry MUST be testable in isolation
- AC-NFR-SUB-003.4: Idempotency store MUST have in-memory implementation for testing
- AC-NFR-SUB-003.5: Cycle detector MUST have in-memory implementation for testing

## 4. Implementation Guidance

### 4.1 Alignment with Implementation Spec

All subscription-related code MUST follow the conventions in [implementation.spec.md](../implementation.spec.md):

- Use C# 11+ features (file-scoped namespaces, required keyword)
- Follow naming conventions (PascalCase for public members)
- Use nullable reference types
- Include copyright headers
- Follow project organization standards
- Use dependency injection for all dependencies

### 4.2 Database Considerations

- Idempotency tracking SHOULD use Redis for performance (O(1) lookups)
- Fallback to SQL Server Express for idempotency is acceptable but slower
- Event sourcing event stores MUST use SQL Server Express (per implementation spec)
- Handlers accessing databases MUST use EF Core with proper transaction management

## 5. Examples

### 5.1 Subscription Configuration Example

```csharp
// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace InventoryService;

public static class InventoryServiceSubscriptions
{
    public static ISubscriptionRegistry Configure()
    {
        return new SubscriptionRegistryBuilder("inventory-service")
            .Subscribe<VehicleListedEvent, VehicleListedHandler>(opt => 
            {
                opt.MaxConcurrency = 4;  // High volume expected
                opt.RetryPolicy = new RetryPolicy { MaxRetries = 3 };
            })
            .Subscribe<VehicleUpdatedEvent, VehicleUpdatedHandler>()
            .Subscribe<VehicleSoldEvent, VehicleSoldHandler>()
            .Subscribe<DealerVerifiedEvent, DealerVerifiedHandler>()
            .Build();
    }
}
```

### 5.2 Message Handler Example

```csharp
// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace InventoryService.Handlers;

public sealed class VehicleListedHandler : MessageHandlerBase<VehicleListedEvent>
{
    private readonly IVehicleRepository _repository;
    private readonly IIdempotencyStore _idempotency;

    public VehicleListedHandler(
        IVehicleRepository repository,
        IIdempotencyStore idempotency,
        ILogger<VehicleListedHandler> logger) : base(logger)
    {
        _repository = repository;
        _idempotency = idempotency;
    }

    public override async Task HandleAsync(
        VehicleListedEvent message, 
        MessageContext context, 
        CancellationToken ct)
    {
        // Idempotency check
        var key = $"vehicle-listed:{message.VehicleId}:{context.Header.MessageId}";
        if (!await _idempotency.TryProcessAsync(key, TimeSpan.FromDays(7), ct))
        {
            Logger.LogDebug("Duplicate message {MessageId}, skipping", context.Header.MessageId);
            return;
        }

        LogHandling(message, context);
        
        var vehicle = Vehicle.FromListedEvent(message);
        await _repository.AddAsync(vehicle, ct);
        
        Logger.LogInformation("Vehicle {VehicleId} listed successfully", message.VehicleId);
    }
}
```

### 5.3 Worker Service Example

```csharp
// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Messaging.Infrastructure;

public sealed class MessageSubscriberWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriptionRegistry _subscriptions;
    private readonly IMessageSerializer _serializer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageSubscriberWorker> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();
        var descriptors = _subscriptions.GetSubscriptions();

        foreach (var descriptor in descriptors)
        {
            var channel = descriptor.ChannelPattern;
            
            await subscriber.SubscribeAsync(
                RedisChannel.Literal(channel),
                async (redisChannel, message) =>
                {
                    await ProcessMessageAsync(descriptor, message, stoppingToken);
                });
            
            _logger.LogInformation("Subscribed to channel: {Channel}", channel);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(
        SubscriptionDescriptor descriptor,
        RedisValue message,
        CancellationToken ct)
    {
        // Peek header for routing decision
        var headerInfo = _serializer.PeekHeader(message);
        if (headerInfo == null) return;

        var (header, payloadType) = headerInfo.Value;
        
        // Check if we should handle this message
        if (payloadType != descriptor.MessageType)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        
        // Full deserialization
        var envelope = DeserializeEnvelope(descriptor.MessageType, message);
        var payload = GetPayload(envelope);
        var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(descriptor.HandlerType);
        
        var context = new MessageContext
        {
            Header = header,
            Channel = descriptor.ChannelPattern,
            ReceivedAt = DateTimeOffset.UtcNow,
            Services = scope.ServiceProvider
        };

        await handler.HandleAsync(payload, context, ct);
    }
}
```

## 6. Revision History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2026-01-12 | Initial specification based on sub-design.md |
