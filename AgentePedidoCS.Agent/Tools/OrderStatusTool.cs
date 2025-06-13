// AgentePedidoCS/AgentePedidoCS.Agent/Tools/OrderStatusTool.cs
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgentePedidoCS.Agent.Tools
{
    /// <summary>
    /// Ferramenta (Plugin do Semantic Kernel) para verificar o status de pedidos e obter dados de pedidos.
    /// Simula um banco de dados de pedidos em memória e inclui lógica de retry para simular falhas transitórias.
    /// </summary>
    public class OrderStatusTool
    {
        private readonly ILogger<OrderStatusTool> _logger;

        // Campos para simulação de retry
        private readonly Dictionary<string, int> _retryAttempts = new Dictionary<string, int>();
        private const int MAX_ATTEMPTS_BEFORE_SUCCESS = 2; // Simula 2 falhas antes do sucesso para um pedido específico.

        /// <summary>
        /// ID de pedido específico usado para testar a lógica de retry simulada.
        /// </summary>
        public const string ORDER_ID_FOR_RETRY_TEST = "RETRY123";

        // Simula um banco de dados de pedidos em memória. Chave: OrderID, Valor: Tupla de (Status, Item).
        private readonly Dictionary<string, (string Status, string Item)> _orderDatabase = new()
        {
            { "12345", ("Processando", "Laptop Gamer") },
            { "67890", ("Enviado", "Monitor Curvo") },
            { "11223", ("Entregue", "Teclado Mecânico") },
            { "44556", ("Cancelado", "Mouse Sem Fio") },
            { "77777", ("Processando", "Webcam HD") },
            { ORDER_ID_FOR_RETRY_TEST, ("Processando", "Retry Item") }
        };

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="OrderStatusTool"/>.
        /// </summary>
        /// <param name="logger">O logger para esta classe.</param>
        public OrderStatusTool(ILogger<OrderStatusTool> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verifica o status de um pedido com base no ID do pedido.
        /// Este método é exposto como uma função do Kernel para o LLM.
        /// Inclui lógica de retry para o <see cref="ORDER_ID_FOR_RETRY_TEST"/>.
        /// </summary>
        /// <param name="orderId">O ID único do pedido a ser verificado.</param>
        /// <returns>Uma string descrevendo o status do pedido, ou uma mensagem de erro se o pedido não for encontrado após as tentativas.</returns>
        [KernelFunction, Description("Verifica o status de um pedido com base no ID do pedido. Use esta ferramenta para consultar o status de pedidos no sistema.")]
        public async Task<string> CheckOrderStatus([Description("O ID único do pedido a ser verificado.")] string orderId)
        {
            const int maxRetries = 3; // Número máximo de tentativas para encontrar um pedido ou para o caso de retry simulado.
            const int delayMilliseconds = 200; // Atraso entre tentativas.

            _logger.LogInformation("OrderStatusTool: Verificando status para o pedido \"{OrderId}\".", orderId);

            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(100); // Simula latência base.

                // Simula falha transitória para ORDER_ID_FOR_RETRY_TEST
                if (orderId == ORDER_ID_FOR_RETRY_TEST)
                {
                    // Controla o número de tentativas para o pedido de teste de retry.
                    if (!_retryAttempts.ContainsKey(orderId))
                    {
                        _retryAttempts[orderId] = 0;
                    }

                    if (_retryAttempts[orderId] < MAX_ATTEMPTS_BEFORE_SUCCESS)
                    {
                        _retryAttempts[orderId]++;
                        _logger.LogWarning("OrderStatusTool: Simulando falha transitória para o pedido \"{OrderId}\", tentativa {AttemptCount}/{MaxAttemptsBeforeSuccess}. Nova tentativa...", orderId, _retryAttempts[orderId], MAX_ATTEMPTS_BEFORE_SUCCESS);
                        if (i < maxRetries - 1) // Evita delay desnecessário na última iteração do loop principal se esta for a última tentativa de retry.
                        {
                            await Task.Delay(delayMilliseconds);
                            continue;
                        }
                         _logger.LogWarning("OrderStatusTool: Falha transitória final simulada para o pedido \"{OrderId}\", tentativa {AttemptCount}.", orderId, _retryAttempts[orderId]);
                    }
                    else
                    {
                        _logger.LogInformation("OrderStatusTool: Problema transitório resolvido para o pedido \"{OrderId}\" após {AttemptCount} tentativas.", orderId, _retryAttempts[orderId]);
                        // Prossegue para buscar no _orderDatabase
                    }
                }

                if (_orderDatabase.TryGetValue(orderId, out var orderInfo))
                {
                    _logger.LogInformation("OrderStatusTool: Pedido \"{OrderId}\" encontrado com status \"{Status}\" e item \"{Item}\".", orderId, orderInfo.Status, orderInfo.Item);
                    if(orderId == ORDER_ID_FOR_RETRY_TEST)
                    {
                        _retryAttempts.Remove(orderId); // Limpa o estado de retry para a próxima execução independente do teste.
                    }
                    return $"O pedido {orderId} está com status: {orderInfo.Status} ({orderInfo.Item}).";
                }
                else
                {
                    if (i < maxRetries - 1)
                    {
                        _logger.LogWarning("OrderStatusTool: Pedido \"{OrderId}\" não encontrado na tentativa {AttemptNumber}/{MaxRetries}. Nova tentativa...", orderId, i + 1, maxRetries);
                        await Task.Delay(delayMilliseconds);
                    }
                }
            }

            _logger.LogWarning("OrderStatusTool: Pedido \"{OrderId}\" não encontrado nos registros após {MaxRetries} tentativas.", orderId, maxRetries);
            if(orderId == ORDER_ID_FOR_RETRY_TEST) // Limpa o estado de retry se todas as tentativas falharem.
            {
                _retryAttempts.Remove(orderId);
            }
            return $"Pedido {orderId} não encontrado em nossos registros após {maxRetries} tentativas.";
        }

    /// <summary>
    /// Obtém os dados brutos do status do pedido (Status, Item) e um indicador se foi encontrado.
    /// Este método é usado internamente ou por outros serviços que precisam dos dados brutos em vez da string formatada.
    /// </summary>
    /// <param name="orderId">O ID único do pedido.</param>
    /// <returns>Uma tupla contendo o Status (string), o Item (string) e Found (bool).</returns>
    public async Task<(string Status, string Item, bool Found)> GetOrderStatusDataAsync(string orderId)
    {
        const int maxRetries = 3; // Número de tentativas para buscar os dados.
        const int delayMilliseconds = 100; // Atraso entre tentativas.
        _logger.LogDebug("OrderStatusTool: Obtendo dados para o pedido \"{OrderId}\".", orderId);

        for (int i = 0; i < maxRetries; i++)
        {
            await Task.Delay(50); // Simula latência base.

            if (_orderDatabase.TryGetValue(orderId, out var orderInfo))
            {
                _logger.LogDebug("OrderStatusTool: Dados encontrados para o pedido \"{OrderId}\": Status=\"{Status}\", Item=\"{Item}\".", orderId, orderInfo.Status, orderInfo.Item);
                return (orderInfo.Status, orderInfo.Item, true);
            }
            else
            {
                 if (i < maxRetries - 1)
                {
                    _logger.LogDebug("OrderStatusTool: Dados não encontrados para o pedido \"{OrderId}\" na tentativa {AttemptNumber}/{MaxRetries}. Nova tentativa...", orderId, i + 1, maxRetries);
                    await Task.Delay(delayMilliseconds);
                }
            }
        }
        _logger.LogDebug("OrderStatusTool: Dados não encontrados para o pedido \"{OrderId}\" após {MaxRetries} tentativas.", orderId, maxRetries);
        return (string.Empty, string.Empty, false); // Não encontrado após as tentativas.
    }
    }
}
