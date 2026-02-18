using FCG.Games.Domain.Entities;

namespace FCG.Games.Application.Interfaces;

public interface IGameService
{
    Task<IEnumerable<Game>> GetAllAsync();
    Task<Game?> GetByIdAsync(Guid id);
    Task CreateAsync(Game game);
    Task UpdateAsync(Game game);
    Task DeleteAsync(Guid id);

    // ADICIONE ESTA LINHA AQUI:
    Task SyncGamesAsync();

    Task<IEnumerable<Game>> GetPopularGamesAsync(int count);
}