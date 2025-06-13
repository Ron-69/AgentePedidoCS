// AgentePedidoCS/AgentePedidoCS.Agent/OrderPrioritizerAgent.cs
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AgentePedidoCS.Agent
{
    /// <summary>
    /// Agente responsável por determinar se um pedido deve ser priorizado.
    /// A lógica de priorização é baseada no tipo de item do pedido e no status do cliente (ex: VIP).
    /// </summary>
    public class OrderPrioritizerAgent
    {
        private readonly ILogger<OrderPrioritizerAgent> _logger;

        /// <summary>
        /// Identificador para clientes VIP.
        /// </summary>
        public const string VipCustomerIdentifier = "VIP_CUSTOMER";
        /// <summary>
        /// Nome do item que sempre aciona priorização.
        /// </summary>
        public const string LaptopGamerItem = "Laptop Gamer";

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="OrderPrioritizerAgent"/>.
        /// </summary>
        /// <param name="logger">O logger para esta classe.</param>
        public OrderPrioritizerAgent(ILogger<OrderPrioritizerAgent> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Decide se um pedido deve ser priorizado com base no item do pedido e no ID do cliente.
        /// A priorização ocorre se o item for um "Laptop Gamer" ou se o cliente for identificado como VIP.
        /// </summary>
        /// <param name="orderItem">O item no pedido a ser verificado.</param>
        /// <param name="customerId">O ID do cliente que fez o pedido, usado para verificar se é VIP.</param>
        /// <returns>Um <see cref="Task{Boolean}"/> que resulta em <c>true</c> se o pedido deve ser priorizado, e <c>false</c> caso contrário.</returns>
        public async Task<bool> ShouldPrioritizeAsync(string orderItem, string customerId)
        {
            _logger.LogInformation("OrderPrioritizerAgent: Verificando priorização para o item de pedido \"{OrderItem}\" do cliente \"{CustomerId}\".", orderItem, customerId);

            bool isVipCustomer = customerId == VipCustomerIdentifier;

            bool shouldPrioritize = false;
            if (orderItem.Equals(LaptopGamerItem, StringComparison.OrdinalIgnoreCase))
            {
                shouldPrioritize = true;
                _logger.LogInformation("OrderPrioritizerAgent: Item de pedido \"{OrderItem}\" acionou priorização.", orderItem);
            }
            else if (isVipCustomer)
            {
                shouldPrioritize = true;
                _logger.LogInformation("OrderPrioritizerAgent: Cliente \"{CustomerId}\" é VIP, acionando priorização.", customerId);
            }
            else
            {
                _logger.LogInformation("OrderPrioritizerAgent: Item de pedido \"{OrderItem}\" para o cliente \"{CustomerId}\" não atende aos critérios de priorização.", orderItem, customerId);
            }

            // Simula uma pequena latência, como se estivesse consultando um sistema externo.
            await Task.Delay(50);

            return shouldPrioritize;
        }
    }
}
