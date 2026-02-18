using FCG.Games.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCG.Games.Infrastructure.Context;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<Game> Games { get; set; }

  public DbSet<UserGameLibrary> UserLibraries { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configuração da Entidade Game
    modelBuilder.Entity<Game>(entity =>
    {
      entity.HasKey(g => g.Id);

      entity.Property(g => g.Title)
                .IsRequired()
                .HasMaxLength(200);

      entity.Property(g => g.Description)
                .HasMaxLength(500);

      entity.Property(g => g.Category)
                .IsRequired()
                .HasMaxLength(100);

      entity.Property(g => g.Price)
                .IsRequired();


      entity.Property(g => g.SalesCount)
                .HasDefaultValue(0);
    });

    // Configuração da Entidade UserGameLibrary
    modelBuilder.Entity<UserGameLibrary>(entity =>
    {
      entity.HasKey(ugl => ugl.Id);

      entity.Property(ugl => ugl.UserId)
                .IsRequired();

      entity.Property(ugl => ugl.GameId)
                .IsRequired();

      entity.Property(ugl => ugl.PurchaseDate)
                .IsRequired();
    });
  }
}