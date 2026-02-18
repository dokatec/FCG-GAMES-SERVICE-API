namespace FCG.Games.Domain.Entities;

public class UserGameLibrary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; } // Referência do Microsserviço de Usuários
    public Guid GameId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
}