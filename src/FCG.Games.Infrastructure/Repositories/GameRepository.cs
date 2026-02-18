using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using FCG.Games.Infrastructure.Repositories;
using FCG.Games.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FCG.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly AppDbContext _context;

    public GameRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Game>> GetAllAsync()

    {
        return await _context.Games.AsNoTracking().ToListAsync();
    }
    public async Task<Game?> GetByIdAsync(Guid id) => await _context.Games.FindAsync(id);
    public async Task AddAsync(Game game)
    {
        await _context.Games.AddAsync(game);
        await _context.SaveChangesAsync();
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Game game)
    {
        throw new NotImplementedException();
    }


}