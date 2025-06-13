// AgentePedidoCS/AgentePedidoCS.Agent/OrderPrioritizerAgent.cs
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AgentePedidoCS.Agent
{
    public class OrderPrioritizerAgent
    {
        private readonly ILogger _logger;

        public OrderPrioritizerAgent(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderPrioritizerAgent>();
        }

        /// <summary>
        /// Decides if an order should be prioritized based on item and simulated customer history.
        /// </summary>
        /// <param name="orderItem">The item in the order.</param>
        /// <param name="customerId">The ID of the customer (used for simulated history).</param>
        /// <returns>True if the order should be prioritized, false otherwise.</returns>
        public async Task<bool> ShouldPrioritizeAsync(string orderItem, string customerId)
        {
            _logger.LogInformation($"OrderPrioritizerAgent: Checking prioritization for order item '{orderItem}' for customer '{customerId}'.");

            // Simulate customer history check - for now, let's assume a VIP customer
            bool isVipCustomer = customerId == "VIP_CUSTOMER"; // Simplified logic

            // Prioritization Logic:
            // Prioritize if the item is a "Laptop Gamer" OR if the customer is a VIP.
            bool shouldPrioritize = false;
            if (orderItem.Equals("Laptop Gamer", StringComparison.OrdinalIgnoreCase))
            {
                shouldPrioritize = true;
                _logger.LogInformation($"OrderPrioritizerAgent: Order item '{orderItem}' triggered prioritization.");
            }
            else if (isVipCustomer)
            {
                shouldPrioritize = true;
                _logger.LogInformation($"OrderPrioritizerAgent: Customer '{customerId}' is a VIP, triggering prioritization.");
            }
            else
            {
                _logger.LogInformation($"OrderPrioritizerAgent: Order item '{orderItem}' for customer '{customerId}' does not meet prioritization criteria.");
            }

            // Simulate some processing time
            await Task.Delay(50);

            return shouldPrioritize;
        }
    }
}
