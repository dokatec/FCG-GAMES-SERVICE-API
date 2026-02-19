using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;

namespace FCG.Games.Infrastructure.Repositories;

public class ElasticGameRepository : IGameSearchRepository
{
    private readonly ElasticsearchClient _client;
    private const string IndexName = "games-index";

    public ElasticGameRepository(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task IndexGameAsync(Game game)
    {
        var existsResponse = await _client.Indices.ExistsAsync("games-index");

        if (!existsResponse.Exists)
        {
            await _client.Indices.CreateAsync("games-index");
            Console.WriteLine("🚀 Índice 'games-index' criado automaticamente.");
        }

        await _client.IndexAsync(game, i => i.Index("games-index").Id(game.Id.ToString()));
    }

    public async Task<IEnumerable<Game>> SearchAsync()
    {
        var response = await _client.SearchAsync<Game>(s => s.Index(IndexName).Query(q => q.MatchAll(m => { })));
        return response.IsValidResponse ? response.Documents : Enumerable.Empty<Game>();
    }

    public async Task<IEnumerable<Game>> SearchByTitleAsync(string title)
    {
        var response = await _client.SearchAsync<Game>(s => s
            .Index(IndexName)
            .Query(q => q.Match(m => m.Field(f => f.Title).Query(title).Fuzziness(new Fuzziness("AUTO"))))
        );
        return response.IsValidResponse ? response.Documents : Enumerable.Empty<Game>();
    }

    public async Task<IEnumerable<GamePopularityMetric>> GetPopularCategoriesAsync()
    {
        var response = await _client.SearchAsync<Game>(s => s
            .Index(IndexName)
            .Size(0)
            .Aggregations(a => a
                .Terms("popular_categories", t => t.Field("category.keyword").Size(10))
            ));
        if (!response.IsValidResponse) return Enumerable.Empty<GamePopularityMetric>();
        var termsAgg = response.Aggregations.GetStringTerms("popular_categories");
        if (termsAgg == null) return Enumerable.Empty<GamePopularityMetric>();
        return termsAgg.Buckets.Select(b => new GamePopularityMetric { Category = b.Key.ToString(), Count = (int)b.DocCount });
    }

    public async Task<IEnumerable<Game>> GetRecommendationsForUserAsync(List<string> favoriteCategories)
    {
        var response = await _client.SearchAsync<Game>(s => s
            .Index(IndexName)
            .Query(q => q.Terms(t => t
                .Field("category.keyword")
                .Terms(new TermsQueryField(favoriteCategories.Select(c => (FieldValue)c).ToArray()))
            ))
        );

        return response.IsValidResponse ? response.Documents : Enumerable.Empty<Game>();
    }

    // RESOLVIDO: Unificando os métodos de vendas
    public async Task<IEnumerable<Game>> GetTopSellingAsync(int size)
    {
        var response = await _client.SearchAsync<Game>(s => s
            .Index(IndexName)
            .Size(size)
            .Sort(sort => sort.Field(f => f.SalesCount, d => d.Order(SortOrder.Desc)))
        );
        return response.IsValidResponse ? response.Documents : Enumerable.Empty<Game>();
    }


    public async Task<double?> GetMaxSalesCountAsync()
    {
        var response = await GetTopSellingAsync(1);
        return (double?)response.FirstOrDefault()?.SalesCount;
    }

    public async Task<IEnumerable<Game>> GetTopSellingGamesAsync(int size)
    {
        var response = await _client.SearchAsync<Game>(s => s
            .Index(IndexName)
            .Size(size)
            .Sort(sort => sort.Field(f => f.SalesCount, d => d.Order(SortOrder.Desc)))
        );
        return response.IsValidResponse ? response.Documents : Enumerable.Empty<Game>();
    }

    public async Task InitIndexAsync()
    {
        var existsResponse = await _client.Indices.ExistsAsync(IndexName);

        if (!existsResponse.Exists)
        {
            var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Mappings(m => m
                    .Properties<Game>(p => p
                      .Keyword(n => n.Id)
                    .Text(n => n.Title, t => t.Analyzer("portuguese"))
                    .Text(n => n.Description, t => t.Analyzer("portuguese"))
                    .Keyword(n => n.Category)
                    // Substituindo .Number por tipos específicos:
                    .DoubleNumber(n => n.Price)
                    .IntegerNumber(n => n.SalesCount)
                    )
                )
            );

            if (createResponse.IsValidResponse)
                Console.WriteLine($"🚀 Índice '{IndexName}' criado com sucesso na AWS/Local.");
            else
                Console.WriteLine($"❌ Erro ao criar índice: {createResponse.DebugInformation}");
        }
    }


}