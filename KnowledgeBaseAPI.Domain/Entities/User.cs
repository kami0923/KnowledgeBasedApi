namespace KnowledgeBaseAPI.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (we'll use these once Document/Chat exist)
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
