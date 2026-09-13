namespace FrostWoodTech.API.DTOs.Public;

/// <summary>Only an id: echoing the row would expose admin fields.</summary>
public sealed class ContactSubmissionResponse
{
    public required Guid Id { get; init; }
}
