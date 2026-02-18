using FCG.Games.Application.Interfaces;
using FCG.Games.Domain.Entities;
using FCG.Games.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MassTransit; // NOVO: Para mensageria
using FCG.Shared.Events;
using Microsoft.AspNetCore.Authorization; // NOVO: Contratos da sua Class Library

namespace FCG.Games.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IGameSearchRepository _searchRepository;
    private readonly IPublishEndpoint _publishEndpoint; // NOVO: Ponto de publicação

    public GamesController(
        IGameService gameService,
        IGameSearchRepository searchRepository,
        IPublishEndpoint publishEndpoint) // Inicializar no construtor
    {
        _gameService = gameService;
        _searchRepository = searchRepository;
        _publishEndpoint = publishEndpoint;
    }

    // --- FASE 4: MÉTODO DE COMPRA ASSÍNCRONA ---

    [HttpPost("buy")]
    public async Task<IActionResult> BuyGame([FromBody] BuyGameRequest request)
    {
        // 1. Validar se o jogo existe na base (Fase 1/3)
        var game = await _gameService.GetByIdAsync(request.GameId);
        if (game == null) return NotFound("Jogo não encontrado no catálogo.");

        // 2. DISPARAR EVENTO (Fase 4 - Resiliência e Desacoplamento)
        // Em vez de esperar o Pagamento responder, avisamos o RabbitMQ
        await _publishEndpoint.Publish<IOrderPlacedEvent>(new
        {
            OrderId = Guid.NewGuid(),
            UserId = request.UserId,
            GameId = game.Id,
            Price = game.Price,
            Timestamp = DateTime.UtcNow
        });

        // Retornamos 202 (Accepted) para indicar que o processamento começou em background
        return Accepted(new { message = "Sua intenção de compra foi registrada e está sendo processada!" });
    }

    // --- ÁREA ADMINISTRATIVA: CRUD DE JOGOS (Somente Admin) ---

    [HttpPost]
    [Authorize(Roles = "Admin")] // Proteção RBAC
    public async Task<IActionResult> Create([FromBody] Game game)
    {
        game.Id = Guid.NewGuid();
        await _gameService.CreateAsync(game);
        await _searchRepository.IndexGameAsync(game);
        return CreatedAtAction(nameof(GetAll), new { id = game.Id }, game);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Game gameUpdate)
    {
        var existingGame = await _gameService.GetByIdAsync(id);
        if (existingGame == null) return NotFound("Jogo não encontrado.");

        // Atualiza os dados
        existingGame.Title = gameUpdate.Title;
        existingGame.Description = gameUpdate.Description;
        existingGame.Price = gameUpdate.Price;
        existingGame.Category = gameUpdate.Category;

        await _gameService.UpdateAsync(existingGame); // Necessário implementar no Service
        await _searchRepository.IndexGameAsync(existingGame); // Reindexa no Elasticsearch

        return Ok(new { message = "Jogo atualizado com sucesso!" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var game = await _gameService.GetByIdAsync(id);
        if (game == null) return NotFound("Jogo não encontrado.");

        await _gameService.DeleteAsync(id); // Necessário implementar no Service
        // Opcional: Remover do Elasticsearch também
        return Ok(new { message = "Jogo removido do catálogo!" });
    }


    // --- MÉTODOS PÚBLICOS (Qualquer um pode ver o catálogo) ---

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var games = await _gameService.GetAllAsync();
        return Ok(games);
    }


    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var results = await _searchRepository.SearchByTitleAsync(query);
        return Ok(results);
    }

    [HttpGet("metrics/popular")]
    public async Task<IActionResult> GetPopularMetrics()
    {
        var result = await _searchRepository.GetPopularCategoriesAsync();
        return Ok(result);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromQuery] string categories)
    {
        if (string.IsNullOrEmpty(categories)) return BadRequest("Categorias são necessárias.");
        var categoryList = categories.Split(',').ToList();
        var result = await _searchRepository.GetRecommendationsForUserAsync(categoryList);
        return Ok(result);
    }


    // [HttpPost("sync")]
    // public async Task<IActionResult> SyncElasticsearch()
    // {
    //     try
    //     {
    //         var games = await _gameService.GetAllAsync();
    //         if (games == null || !games.Any())
    //             return NotFound("Nenhum jogo encontrado para sincronizar.");

    //         foreach (var game in games)
    //         {
    //             await _searchRepository.IndexGameAsync(game);
    //         }

    //         return Ok(new { message = $"{games.Count()} jogos sincronizados com sucesso no Elasticsearch!" });
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(500, $"Erro na sincronização: {ex.Message}");
    //     }
    // }
}

// DTO necessário para o Request de Compra
public record BuyGameRequest(Guid UserId, Guid GameId);