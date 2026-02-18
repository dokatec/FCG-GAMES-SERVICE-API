using FCG.Shared.Events;
using MassTransit;

namespace FCG.Games.API.Consumers;

public class PaymentApprovedConsumer : IConsumer<IPaymentApprovedEvent>
{
    public async Task Consume(ConsumeContext<IPaymentApprovedEvent> context)
    {
        // Lógica de Arquiteto: Aqui você atualiza o banco (Postgres/MongoDB)
        // marcando que o usuário agora "possui" o jogo.
        Console.WriteLine($"[GAMES] SUCESSO: Jogo {context.Message.GameId} liberado para o usuário {context.Message.UserId}");
        await Task.CompletedTask;
    }
}