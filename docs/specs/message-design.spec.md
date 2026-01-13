# Message Design Specification

## Document Information

- **Version**: 1.0
- **Last Updated**: 2026-01-12
- **Status**: Active
- **Related Documents**: 
  - [Implementation Specification](../implementation.spec.md)
  - [Subscription Design Specification](./subscription-design.spec.md)

## 1. Overview

This specification defines the requirements for high-performance message design in C# microservices using Redis Pub/Sub for event-driven communication. The design focuses on efficiency, schema evolution, and maintainability for enterprise systems with 100+ message types.

## 2. Requirements

### REQ-MSG-001: Message Envelope Structure

**Requirement**: All messages MUST use a consistent envelope pattern that separates transport concerns from business payload.

**Rationale**: Separation of transport metadata from business data enables consistent processing, routing, and tracing across all message types while maintaining payload flexibility.

**Acceptance Criteria**:
- AC-MSG-001.1: Message envelope MUST contain a `Header` property of type `MessageHeader`
- AC-MSG-001.2: Message envelope MUST contain a `Payload` property containing the business message
- AC-MSG-001.3: The envelope MUST be generic: `MessageEnvelope<TPayload> where TPayload : IMessage`
- AC-MSG-001.4: The payload type MUST implement the `IMessage` marker interface

### REQ-MSG-002: Message Header Fields

**Requirement**: Message headers MUST contain standardized metadata for routing, tracing, and idempotency.

**Rationale**: Consistent metadata enables distributed tracing, event correlation, cycle detection, and exactly-once processing semantics.

**Acceptance Criteria**:
- AC-MSG-002.1: Header MUST include `MessageType` (string) for type discrimination
- AC-MSG-002.2: Header MUST include `MessageId` (string, GUID) for idempotency
- AC-MSG-002.3: Header MUST include `CorrelationId` (string, GUID) for distributed tracing
- AC-MSG-002.4: Header MUST include `CausationId` (string) for event chain tracking
- AC-MSG-002.5: Header MUST include `TimestampUnixMs` (long) for creation time
- AC-MSG-002.6: Header MUST include `SourceService` (string) for origin identification
- AC-MSG-002.7: Header MUST include `SchemaVersion` (int) with default value of 1
- AC-MSG-002.8: Header MAY include `Metadata` (Dictionary<string, string>) for extensibility

### REQ-MSG-003: Message Type Registry

**Requirement**: The system MUST maintain a registry mapping message type names to CLR types for deserialization.

**Rationale**: With 100+ message types, manual type resolution is error-prone. A centralized registry ensures consistent type discrimination.

**Acceptance Criteria**:
- AC-MSG-003.1: Registry MUST provide `Register<T>(string messageType)` method
- AC-MSG-003.2: Registry MUST provide `GetType(string messageType)` method returning Type or null
- AC-MSG-003.3: Registry MUST provide `GetMessageType<T>()` method returning string or null
- AC-MSG-003.4: Registry MUST maintain bidirectional mappings (type name ↔ CLR type)
- AC-MSG-003.5: All message types MUST be registered at application startup
- AC-MSG-003.6: Registration MAY be generated via source generator or reflection

### REQ-MSG-004: Domain Message Markers

**Requirement**: Messages MUST implement marker interfaces to distinguish between events and commands.

**Rationale**: Type-based discrimination enables different processing strategies for commands (single handler) vs events (multiple subscribers).

**Acceptance Criteria**:
- AC-MSG-004.1: Domain events MUST implement `IDomainEvent : IMessage`
- AC-MSG-004.2: Domain commands MUST implement `ICommand : IMessage`
- AC-MSG-004.3: `IDomainEvent` MUST expose `AggregateId` property
- AC-MSG-004.4: `IDomainEvent` MUST expose `AggregateType` property
- AC-MSG-004.5: `ICommand` MUST expose `TargetId` property

### REQ-MSG-005: Serialization Technology

**Requirement**: Messages MUST be serialized using MessagePack with LZ4 compression.

**Rationale**: MessagePack provides optimal balance of performance, C# ecosystem fit, schema evolution support, and maintainability. LZ4 compression reduces payload size by ~25% with minimal CPU overhead.

**Acceptance Criteria**:
- AC-MSG-005.1: All message classes MUST be decorated with `[MessagePackObject]` attribute
- AC-MSG-005.2: All serialized properties MUST be decorated with `[Key(n)]` attribute with unique integer keys
- AC-MSG-005.3: Non-serialized properties MUST be decorated with `[IgnoreMember]` attribute
- AC-MSG-005.4: Serialization MUST use MessagePackCompression.Lz4BlockArray
- AC-MSG-005.5: Serialization MUST use MessagePackSecurity.UntrustedData for safety
- AC-MSG-005.6: Enums MUST use byte or int as underlying type for efficiency
- AC-MSG-005.7: Collections SHOULD use `IReadOnlyList<T>` for immutability

### REQ-MSG-006: Schema Evolution Rules

**Requirement**: Message schemas MUST support backward-compatible evolution without breaking consumers.

**Rationale**: With multiple microservices deployed independently, schema changes must not require coordinated deployments.

**Acceptance Criteria**:
- AC-MSG-006.1: SAFE CHANGE: Adding new optional fields with new Key indices is allowed
- AC-MSG-006.2: SAFE CHANGE: Adding new enum values at the end is allowed
- AC-MSG-006.3: SAFE CHANGE: Renaming fields (Key index preserved) is allowed
- AC-MSG-006.4: SAFE CHANGE: Changing from required to optional is allowed
- AC-MSG-006.5: UNSAFE CHANGE: Removing fields is prohibited (mark as [Obsolete] instead)
- AC-MSG-006.6: UNSAFE CHANGE: Changing field types is prohibited
- AC-MSG-006.7: UNSAFE CHANGE: Changing Key indices is prohibited
- AC-MSG-006.8: UNSAFE CHANGE: Reordering enum values is prohibited
- AC-MSG-006.9: UNSAFE CHANGE: Changing from optional to required is prohibited
- AC-MSG-006.10: Deprecated fields MUST never have their Key index reused

### REQ-MSG-007: Message Channel Naming

**Requirement**: Messages MUST use hierarchical channel naming for Redis pub/sub routing.

**Rationale**: Structured naming enables pattern-based subscriptions, allowing services to subscribe to domains while filtering irrelevant messages.

**Acceptance Criteria**:
- AC-MSG-007.1: Channel names MUST follow pattern: `{domain}.{aggregate}.{event-type}.v{version}`
- AC-MSG-007.2: Domain MUST be lowercase (e.g., "vehicles", "inspections", "dealers")
- AC-MSG-007.3: Aggregate MUST be lowercase (e.g., "listing", "report", "account")
- AC-MSG-007.4: Event type MUST be lowercase verb/action (e.g., "created", "updated", "completed")
- AC-MSG-007.5: Version MUST start at v1 and increment for breaking changes
- AC-MSG-007.6: Messages SHOULD use `[MessageChannel]` attribute to declare channel
- AC-MSG-007.7: Example: "vehicles.listing.created.v1"

### REQ-MSG-008: Message Serializer Interface

**Requirement**: A serializer abstraction MUST provide methods for full serialization and partial header inspection.

**Rationale**: Services need to inspect message headers for routing decisions without the cost of full deserialization.

**Acceptance Criteria**:
- AC-MSG-008.1: MUST provide `Serialize<T>(MessageEnvelope<T> envelope)` returning byte[]
- AC-MSG-008.2: MUST provide `Deserialize<T>(ReadOnlyMemory<byte> data)` returning MessageEnvelope<T>
- AC-MSG-008.3: MUST provide `PeekHeader(ReadOnlyMemory<byte> data)` returning (MessageHeader, Type?) tuple
- AC-MSG-008.4: PeekHeader MUST deserialize only the header portion for efficiency
- AC-MSG-008.5: PeekHeader MUST use message type registry to resolve CLR type from message type name
- AC-MSG-008.6: All methods MUST be thread-safe

### REQ-MSG-009: Message Size Limits

**Requirement**: Messages MUST be designed to stay within Redis pub/sub size limits.

**Rationale**: Redis has a 512MB message size limit, but practical limits are much lower for performance.

**Acceptance Criteria**:
- AC-MSG-009.1: Individual messages SHOULD NOT exceed 1MB uncompressed
- AC-MSG-009.2: Messages approaching 100KB SHOULD be investigated for optimization
- AC-MSG-009.3: Large payloads (files, images) MUST use reference pattern (store in blob storage, send URL)
- AC-MSG-009.4: Collections in messages SHOULD be paginated if they can grow unbounded
- AC-MSG-009.5: Serialization MUST use LZ4 compression to reduce wire size

### REQ-MSG-010: Message Validation

**Requirement**: Message models MUST include validation attributes for data integrity.

**Rationale**: Validation at message boundaries prevents invalid data from propagating through the system.

**Acceptance Criteria**:
- AC-MSG-010.1: Required fields MUST use `required` keyword (C# 11+)
- AC-MSG-010.2: String fields with constraints MUST use `[StringLength]`, `[RegularExpression]` attributes
- AC-MSG-010.3: Numeric fields with constraints MUST use `[Range]` attribute
- AC-MSG-010.4: Validation attributes MUST be generated from ICD spec
- AC-MSG-010.5: Publishers SHOULD validate messages before publishing (fail-fast)
- AC-MSG-010.6: Subscribers SHOULD validate messages after deserialization (defensive)

## 3. Non-Functional Requirements

### NFR-MSG-001: Performance

**Requirement**: Serialization and deserialization MUST meet performance targets.

**Acceptance Criteria**:
- AC-NFR-MSG-001.1: Serialization of typical message (200 bytes) MUST complete in < 2 microseconds
- AC-NFR-MSG-001.2: Deserialization of typical message MUST complete in < 3 microseconds
- AC-NFR-MSG-001.3: PeekHeader MUST complete in < 1 microsecond
- AC-NFR-MSG-001.4: Memory allocation per message SHOULD be < 1KB

### NFR-MSG-002: Code Generation

**Requirement**: Message classes SHOULD be generated from an Interface Control Document (ICD) specification.

**Rationale**: Manual message class maintenance across 100+ types is error-prone. Code generation ensures consistency.

**Acceptance Criteria**:
- AC-NFR-MSG-002.1: ICD spec SHOULD be in YAML or JSON format
- AC-NFR-MSG-002.2: Generated classes MUST include XML documentation comments
- AC-NFR-MSG-002.3: Generated classes MUST include validation attributes
- AC-NFR-MSG-002.4: Generated classes MUST include MessagePack attributes
- AC-NFR-MSG-002.5: Generated classes MUST include channel attributes
- AC-NFR-MSG-002.6: Generator MUST create type registry registration code
- AC-NFR-MSG-002.7: Generated code MUST include header comment: "// Auto-generated - DO NOT EDIT"

### NFR-MSG-003: Testing

**Requirement**: Message serialization MUST be thoroughly tested.

**Acceptance Criteria**:
- AC-NFR-MSG-003.1: Each message type MUST have round-trip serialization test
- AC-NFR-MSG-003.2: Schema evolution scenarios MUST be tested (add field, deprecate field)
- AC-NFR-MSG-003.3: Large message handling MUST be tested (near size limits)
- AC-NFR-MSG-003.4: Invalid message handling MUST be tested (deserialization errors)
- AC-NFR-MSG-003.5: PeekHeader efficiency MUST be verified (no full deserialization)

## 4. Implementation Guidance

### 4.1 Alignment with Implementation Spec

All message-related code MUST follow the conventions in [implementation.spec.md](../implementation.spec.md):

- Use C# 11+ features (required keyword, file-scoped namespaces)
- Follow naming conventions (PascalCase for public members)
- Use nullable reference types
- Include copyright headers
- Follow project organization standards

### 4.2 Database Considerations

While this spec focuses on messaging, persistence of events for event sourcing or audit:

- Event stores SHOULD use SQL Server Express for development (per implementation spec)
- Event store schema MUST include: EventId (GUID), AggregateId, EventType, Payload (JSON or MessagePack), Timestamp, Version
- Event replay MUST deserialize using MessageTypeRegistry

## 5. Examples

### 5.1 Message Model Example

```csharp
// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using MessagePack;

namespace Automotive.Messaging;

/// <summary>
/// Event raised when a vehicle is listed for sale.
/// Channel: vehicles.listing.created.v1
/// </summary>
[MessagePackObject]
[MessageChannel("vehicles", "listing", "created", version: 1)]
public sealed class VehicleListedEvent : IDomainEvent
{
    [Key(0)]
    public required string VehicleId { get; init; }

    [Key(1)]
    public required string DealerId { get; init; }

    [Key(2)]
    [StringLength(17, MinimumLength = 17)]
    public required string Vin { get; init; }

    [Key(3)]
    [Range(1900, 2100)]
    public required int Year { get; init; }

    [Key(4)]
    public required string Make { get; init; }

    [Key(5)]
    public required string Model { get; init; }

    [Key(6)]
    public required decimal AskingPrice { get; init; }

    [Key(7)]
    public IReadOnlyList<string> PhotoUrls { get; init; } = [];

    // IDomainEvent implementation
    [IgnoreMember]
    public string AggregateId => VehicleId;
    
    [IgnoreMember]
    public string AggregateType => "Vehicle";
}
```

### 5.2 Schema Evolution Example

```csharp
// Version 1 - Original
[MessagePackObject]
public sealed class InspectionCompletedEvent : IDomainEvent
{
    [Key(0)] public required string InspectionId { get; init; }
    [Key(1)] public required string VehicleId { get; init; }
    // ... other fields
}

// Version 2 - Added optional field (SAFE)
[MessagePackObject]
public sealed class InspectionCompletedEvent : IDomainEvent
{
    [Key(0)] public required string InspectionId { get; init; }
    [Key(1)] public required string VehicleId { get; init; }
    // ... other fields
    
    // NEW in v2 - old consumers will ignore this
    [Key(10)] public string? InspectorNotes { get; init; }
}

// Version 3 - Deprecated field (SAFE)
[MessagePackObject]
public sealed class InspectionCompletedEvent : IDomainEvent
{
    [Key(0)] public required string InspectionId { get; init; }
    [Key(1)] public required string VehicleId { get; init; }
    
    // DEPRECATED in v3 - do not remove, do not reuse Key(5)
    [Key(5)]
    [Obsolete("Use InspectorId instead. Removed in v5.")]
    public string? LegacyInspectorName { get; init; }
    
    // NEW in v3 - replacement for LegacyInspectorName
    [Key(11)] public string? InspectorId { get; init; }
}
```

## 6. Revision History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2026-01-12 | Initial specification based on high-perf-message-design.md |
