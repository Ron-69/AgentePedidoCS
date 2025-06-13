// AgentePedidoCS/AgentePedidoCS.Agent/Tools/BatchOrderTool.cs
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System; // Required for Guid

namespace AgentePedidoCS.Agent.Tools
{
    /// <summary>
    /// Ferramenta (Plugin do Semantic Kernel) para registrar pedidos em lote.
    /// Permite a criação de um novo pedido em lote para um cliente especificado, contendo múltiplos itens.
    /// Simula o armazenamento do pedido em um "banco de dados" em memória.
    /// </summary>
    public class BatchOrderTool
    {
        private readonly ILogger<BatchOrderTool> _logger;
        // Simula um banco de dados de pedidos em lote em memória. Chave: BatchOrderId, Valor: Tupla de (Cliente, Lista de Itens).
        private readonly Dictionary<string, (string Customer, List<string> Items)> _batchOrderDatabase = new();

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="BatchOrderTool"/>.
        /// </summary>
        /// <param name="logger">O logger para esta classe.</param>
        public BatchOrderTool(ILogger<BatchOrderTool> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registra um novo pedido em lote para um cliente com uma lista de itens.
        /// Este método é exposto como uma função do Kernel para o LLM.
        /// </summary>
        /// <param name="items">A lista de itens a serem incluídos no pedido em lote. Não pode ser nula ou vazia.</param>
        /// <param name="customer">O nome do cliente que está fazendo o pedido em lote. Não pode ser nulo ou vazio.</param>
        /// <returns>
        /// Uma string contendo o ID do pedido em lote se o registro for bem-sucedido,
        /// ou uma mensagem de erro se os dados de entrada forem inválidos.
        /// </returns>
        [KernelFunction, Description("Registra um novo pedido em lote para um cliente com uma lista de itens. Use esta ferramenta para criar um novo pedido em lote no sistema.")]
        public async Task<string> RegisterBatchOrder(
            [Description("A lista de itens para o pedido.")] List<string> items,
            [Description("O nome do cliente.")] string customer)
        {
            _logger.LogInformation("BatchOrderTool: Tentativa de registrar pedido em lote para o cliente \"{Customer}\" com itens: {ItemsCount} itens.", customer, items?.Count ?? 0);

            // Simula uma pequena latência de processamento.
            await Task.Delay(100);

            if (items == null || items.Count == 0)
            {
                _logger.LogWarning("BatchOrderTool: Não é possível registrar um pedido em lote sem itens.");
                return "Não é possível registrar um pedido em lote sem itens.";
            }

            if (string.IsNullOrWhiteSpace(customer))
            {
                _logger.LogWarning("BatchOrderTool: Não é possível registrar um pedido em lote sem o nome do cliente.");
                return "Não é possível registrar um pedido em lote sem um nome de cliente.";
            }

            var batchOrderId = Guid.NewGuid().ToString();
            _batchOrderDatabase[batchOrderId] = (customer, items);

            _logger.LogInformation("BatchOrderTool: Pedido em lote registrado com sucesso. ID: \"{BatchOrderId}\", Cliente: \"{Customer}\", Itens: \"{Items}\".",
                                   batchOrderId, customer, string.Join(", ", items));
            return $"Pedido em lote registrado com sucesso. O ID do seu pedido em lote é: {batchOrderId}";
        }
    }
}
