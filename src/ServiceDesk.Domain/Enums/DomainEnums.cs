namespace ServiceDesk.Domain.Enums;

public enum TenantStatus
{
    Active,
    Suspended,
    Inactive
}

public enum CustomerOrganisationStatus
{
    Active,
    Inactive
}

public enum TicketType
{
    Incident,
    ServiceRequest,
    Change
}

public enum TicketStatus
{
    New,
    Triage,
    InProgress,
    Resolved,
    Closed,
    WaitingForRequester
}

public enum TicketPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum TicketActivityKind
{
    PublicReply,
    InternalNote,
    System
}

public enum TicketActivityVisibility
{
    Public,
    Internal
}

public enum CatalogueItemStatus
{
    Draft,
    Published,
    Retired
}

public enum KnowledgeArticleStatus
{
    Draft,
    Published,
    Retired
}
