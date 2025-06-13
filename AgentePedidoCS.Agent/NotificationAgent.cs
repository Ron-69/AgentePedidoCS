// AgentePedidoCS/AgentePedidoCS.Agent/NotificationAgent.cs
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AgentePedidoCS.Agent
{
    /// <summary>
    /// Agente responsável por simular o envio de notificações aos clientes.
    /// Atualmente, ele registra as informações da notificação usando o logger injetado.
    /// </summary>
    public class NotificationAgent
    {
        private readonly ILogger<NotificationAgent> _logger;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="NotificationAgent"/>.
        /// </summary>
        /// <param name="logger">O logger para esta classe.</param>
        public NotificationAgent(ILogger<NotificationAgent> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Simula o envio de uma notificação de priorização para o cliente.
        /// Em um cenário real, isso poderia envolver a chamada a um serviço de e-mail, SMS ou outro sistema de mensagens.
        /// Atualmente, apenas registra a tentativa de envio e a mensagem no log.
        /// </summary>
        /// <param name="orderId">O ID do pedido que foi priorizado.</param>
        /// <param name="customerId">O ID do cliente a ser notificado.</param>
        /// <returns>Um <see cref="Task"/> que representa a conclusão da operação assíncrona de envio (simulada).</returns>
        public async Task SendPrioritizationNotificationAsync(string orderId, string customerId)
        {
            _logger.LogInformation("NotificationAgent: Preparando para enviar notificação de priorização para o pedido \"{OrderId}\" do cliente \"{CustomerId}\".", orderId, customerId);

            // Simula a construção e envio da notificação.
            string notificationMessage = $"Dear Customer {customerId}, your order {orderId} has been prioritized and will be processed with urgency.";

            // Em um sistema real, aqui ocorreria a chamada a um serviço externo (ex: SendGrid, Twilio).
            // Por exemplo: await _emailService.SendEmailAsync(customerEmail, "Order Prioritized", notificationMessage);

            _logger.LogInformation("NotificationAgent: Notificação simulada enviada com a mensagem: \"{NotificationMessage}\"", notificationMessage);

            // Simula uma pequena latência, como se estivesse aguardando a resposta de um serviço de notificação.
            await Task.Delay(50);
        }
    }
}
