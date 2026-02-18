using FCG.Games.Domain.Entities;

namespace FCG.Games.Domain.Interfaces;

public interface IGameRepository
{
    Task<IEnumerable<Game>> GetAllAsync();
    Task<Game?> GetByIdAsync(Guid id);

    Task UpdateAsync(Game game);
    Task AddAsync(Game game);
    Task DeleteAsync(Guid id);


}