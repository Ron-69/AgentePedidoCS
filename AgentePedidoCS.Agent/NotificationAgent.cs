// AgentePedidoCS/AgentePedidoCS.Agent/NotificationAgent.cs
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AgentePedidoCS.Agent
{
    public class NotificationAgent
    {
        private readonly ILogger _logger;

        public NotificationAgent(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<NotificationAgent>();
        }

        /// <summary>
        /// Simulates sending a prioritization notification to the customer.
        /// </summary>
        /// <param name="orderId">The ID of the prioritized order.</param>
        /// <param name="customerId">The ID of the customer.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendPrioritizationNotificationAsync(string orderId, string customerId)
        {
            _logger.LogInformation($"NotificationAgent: Preparing to send prioritization notification for order '{orderId}' to customer '{customerId}'.");

            // Simulate sending notification
            // In a real scenario, this would involve calling an email/SMS service, etc.
            string notificationMessage = $"Dear Customer {customerId}, your order {orderId} has been prioritized and will be processed with urgency.";
            _logger.LogInformation($"NotificationAgent: Simulated notification sent: "{notificationMessage}"");

            // Simulate some processing time
            await Task.Delay(50);
        }
    }
}
