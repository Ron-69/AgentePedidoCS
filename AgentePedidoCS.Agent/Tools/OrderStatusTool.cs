// AgentePedidoCS/AgentePedidoCS.Agent/Tools/OrderStatusTool.cs
using System; // For Console.WriteLine
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.ComponentModel; // Importe para o DescriptionAttribute

namespace AgentePedidoCS.Agent.Tools
{
    public class OrderStatusTool
    {
        // Fields for retry simulation
        private static readonly Dictionary<string, int> _retryAttempts = new Dictionary<string, int>();
        private const int MAX_ATTEMPTS_BEFORE_SUCCESS = 2; // Simulate 2 failures before success for a specific order
        public const string ORDER_ID_FOR_RETRY_TEST = "RETRY123";

        // Simula um banco de dados de pedidos em memória
        private static readonly Dictionary<string, (string Status, string Item)> _orderDatabase = new()
        {
            { "12345", ("Processando", "Laptop Gamer") },
            { "67890", ("Enviado", "Monitor Curvo") },
            { "11223", ("Entregue", "Teclado Mecânico") },
            { "44556", ("Cancelado", "Mouse Sem Fio") },
            { "77777", ("Processando", "Webcam HD") },
            { ORDER_ID_FOR_RETRY_TEST, ("Processando", "Retry Item") } // Order for retry test
        };

        /// <summary>
        /// Verifica o status de um pedido com base no ID do pedido.
        /// Use esta ferramenta para consultar o status de pedidos no sistema.
        /// </summary>
        /// <param name="orderId">O ID único do pedido a ser verificado.</param>
        /// <returns>Uma string descrevendo o status do pedido, ou uma mensagem de erro se o pedido não for encontrado.</returns>
        [KernelFunction, Description("Verifica o status de um pedido com base no ID do pedido.")]
        public static async Task<string> CheckOrderStatus([Description("O ID único do pedido a ser verificado.")] string orderId)
        {
            const int maxRetries = 3;
            const int delayMilliseconds = 200;

            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(100); // Simulate some base latency

                // Simulate transient failure for a specific order ID
                if (orderId == ORDER_ID_FOR_RETRY_TEST)
                {
                    if (!_retryAttempts.ContainsKey(orderId))
                    {
                        _retryAttempts[orderId] = 0;
                    }

                    if (_retryAttempts[orderId] < MAX_ATTEMPTS_BEFORE_SUCCESS)
                    {
                        _retryAttempts[orderId]++;
                        if (i < maxRetries - 1)
                        {
                             Console.WriteLine($"OrderStatusTool: Simulating transient failure for order {orderId}, attempt {_retryAttempts[orderId]}. Retrying...");
                             await Task.Delay(delayMilliseconds);
                             continue;
                        }
                        else
                        {
                            Console.WriteLine($"OrderStatusTool: Simulating final transient failure for order {orderId}, attempt {_retryAttempts[orderId]}.");
                        }
                    }
                    else
                    {
                         Console.WriteLine($"OrderStatusTool: Transient issue resolved for order {orderId} after {_retryAttempts[orderId]} attempts.");
                    }
                }

                if (_orderDatabase.TryGetValue(orderId, out var orderInfo))
                {
                    if(orderId == ORDER_ID_FOR_RETRY_TEST)
                    {
                        _retryAttempts.Remove(orderId); // Reset for next independent test run
                    }
                    return $"O pedido {orderId} está com status: {orderInfo.Status} ({orderInfo.Item}).";
                }
                else
                {
                    if (i < maxRetries - 1)
                    {
                        Console.WriteLine($"OrderStatusTool: Pedido {orderId} não encontrado, tentativa {i + 1}/{maxRetries}. Retrying...");
                        await Task.Delay(delayMilliseconds);
                    }
                }
            }

            if(orderId == ORDER_ID_FOR_RETRY_TEST) // Reset if it failed all retries
            {
                _retryAttempts.Remove(orderId);
            }
            return $"Pedido {orderId} não encontrado em nossos registros após {maxRetries} tentativas.";
        }

    /// <summary>
    /// Obtém os dados brutos do status do pedido, incluindo se foi encontrado.
    /// </summary>
    /// <param name="orderId">O ID único do pedido.</param>
    /// <returns>Uma tupla contendo status, item e um booleano indicando se o pedido foi encontrado.</returns>
    public static async Task<(string Status, string Item, bool Found)> GetOrderStatusDataAsync(string orderId)
    {
        const int maxRetries = 3;
        const int delayMilliseconds = 100; // Shorter delay for internal data access

        for (int i = 0; i < maxRetries; i++)
        {
            await Task.Delay(50); // Simulate some base latency

            if (_orderDatabase.TryGetValue(orderId, out var orderInfo))
            {
                return (orderInfo.Status, orderInfo.Item, true);
            }
            else
            {
                 if (i < maxRetries - 1)
                {
                    // Console.WriteLine($"GetOrderStatusDataAsync: Pedido {orderId} não encontrado, tentativa {i + 1}/{maxRetries}. Retrying...");
                    await Task.Delay(delayMilliseconds);
                }
            }
        }
        return (string.Empty, string.Empty, false); // Not found after retries
    }
    }
}