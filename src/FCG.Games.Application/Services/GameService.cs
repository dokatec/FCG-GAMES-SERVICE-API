using FCG.Games.Application.Interfaces;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using System.Linq;


namespace FCG.Games.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameSearchRepository _searchRepository;

    public GameService(IGameRepository gameRepository, IGameSearchRepository searchRepository)
    {
        _gameRepository = gameRepository;
        _searchRepository = searchRepository;
    }

    public async Task<IEnumerable<Game>> GetAllAsync()
    {

        return await _searchRepository.SearchAsync();
    }

    public async Task<Game?> GetByIdAsync(Guid id)
    {
        return await _gameRepository.GetByIdAsync(id);
    }

    public async Task CreateAsync(Game game)
    {
        await _gameRepository.AddAsync(game);

        await _searchRepository.IndexGameAsync(game);
    }

    public async Task UpdateAsync(Game game)
    {
        await _gameRepository.UpdateAsync(game);

        await _searchRepository.IndexGameAsync(game);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _gameRepository.DeleteAsync(id);

    }

    public async Task SyncGamesAsync()
    {
        var games = await _gameRepository.GetAllAsync();

        // LOG DE DIAGNÓSTICO
        Console.WriteLine($"🔍 DEBUG: Encontrados {games.Count()} jogos no Postgres.");

        if (!games.Any())
        {
            Console.WriteLine("❌ Erro: O banco Postgres parece estar vazio ou a conexão falhou.");
            return;
        }

        foreach (var game in games)
        {
            await _searchRepository.IndexGameAsync(game);
        }
    }

    public async Task<IEnumerable<Game>> GetPopularGamesAsync(int count)
    {
        var all = await _searchRepository.SearchAsync();
        return all.Take(count);
    }


}