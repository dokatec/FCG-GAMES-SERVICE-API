using FCG.Games.Domain.Entities;

namespace FCG.Games.Domain.Interfaces;

public interface IGameSearchRepository
{
    Task IndexGameAsync(Game game);
    Task<IEnumerable<Game>> SearchAsync();
    Task<IEnumerable<Game>> SearchByTitleAsync(string title);
    Task<IEnumerable<Game>> GetTopSellingGamesAsync(int size);
    Task<IEnumerable<Game>> GetTopSellingAsync(int size);
    Task<double?> GetMaxSalesCountAsync();

    // AJUSTADO: Agora retornam dados para o Controller
    Task<IEnumerable<GamePopularityMetric>> GetPopularCategoriesAsync();
    Task<IEnumerable<Game>> GetRecommendationsForUserAsync(List<string> categoryList);
}

