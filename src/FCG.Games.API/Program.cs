using FCG.Games.Domain.Interfaces;
using FCG.Games.Infrastructure.Context;
using FCG.Infrastructure.Repositories;
using FCG.Games.Application.Services;
using FCG.Games.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FCG.API.Middlewares;
using FCG.Games.Infrastructure.Repositories;
using Elastic.Clients.Elasticsearch;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using MassTransit;
using FCG.Games.API.Consumers;

var builder = WebApplication.CreateBuilder(args);


// --- Registro de Dependências (DI) ---
var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
    .DefaultIndex("games-index");

var client = new ElasticsearchClient(settings);

builder.Services.AddSingleton<ElasticsearchClient>(client);
builder.Services.AddScoped<IGameSearchRepository, ElasticGameRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// --- Banco de Dados (SQLite) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddMassTransit(x =>
{
    // 1. Ele consome a aprovação do pagamento
    x.AddConsumer<PaymentApprovedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["MessageBroker:Host"] ?? "localhost";

        cfg.Host(host, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // 2. Cria a fila para receber a aprovação
        cfg.ReceiveEndpoint("game-payment-approved-queue", e =>
        {
            e.ConfigureConsumer<PaymentApprovedConsumer>(context);
        });
        cfg.ConfigureEndpoints(context);
    });
});

// --- Configuração de Autenticação JWT ---
var key = Encoding.ASCII.GetBytes("ChaveSuperSecretaDaFiapCloudGames2026!");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

// --- Configuração de Autorização por Níveis ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("User"));
});

// --- Swagger ---
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG Games API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira apenas o token JWT."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "FCG-Games-Service")
            )
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

var app = builder.Build();

// --- INÍCIO DO BLOCO DE AUTO-MIGRATION ---
// No Program.cs, após o var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Tenta aplicar a migration com política de retry simples
    for (int i = 0; i < 5; i++)
    {
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            logger.LogInformation("--> Verificando banco de dados (Tentativa {0})...", i + 1);

            context.Database.Migrate();

            logger.LogInformation("--> Migrations aplicadas com sucesso!");
            break; // Sucesso, sai do loop
        }
        catch (Exception ex)
        {
            logger.LogWarning("--> Banco ainda não disponível. Aguardando 5 segundos...");
            Thread.Sleep(5000); // Aguarda o Postgres "acordar"

            if (i == 4) // Se for a última tentativa e falhar...
            {
                logger.LogCritical(ex, "--> Erro fatal ao tentar migrar o banco.");
                throw;
            }
        }
    }
}
// --- FIM DO BLOCO ---

// Bloco de Inicialização de Infraestrutura (Elasticsearch)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var searchRepository = services.GetRequiredService<IGameSearchRepository>();
        Console.WriteLine("🔍 Verificando integridade do índice no Elasticsearch...");
        await searchRepository.InitIndexAsync();
    }
    catch (Exception ex)
    {
        // Log de erro sênior: avisa o que houve sem derrubar a API imediatamente
        Console.WriteLine($"⚠️ Falha ao inicializar o índice do Elastic: {ex.Message}");
    }
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();