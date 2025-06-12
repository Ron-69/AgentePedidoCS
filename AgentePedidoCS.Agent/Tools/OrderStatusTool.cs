// AgentePedidoCS/AgentePedidoCS.Agent/Tools/OrderStatusTool.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.ComponentModel; // Importe para o DescriptionAttribute

namespace AgentePedidoCS.Agent.Tools
{
    public class OrderStatusTool
    {
        // Simula um banco de dados de pedidos em memória
        private static readonly Dictionary<string, (string Status, string Item)> _orderDatabase = new()
        {
            { "12345", ("Processando", "Laptop Gamer") },
            { "67890", ("Enviado", "Monitor Curvo") },
            { "11223", ("Entregue", "Teclado Mecânico") },
            { "44556", ("Cancelado", "Mouse Sem Fio") }
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
            await Task.Delay(100); //Agurada 100milissegundos
            // Lógica para verificar o status do pedido
            if (_orderDatabase.TryGetValue(orderId, out var orderInfo))
            {
                return $"O pedido {orderId} está com status: {orderInfo.Status} ({orderInfo.Item}).";
            }
            else
            {
                return $"Pedido {orderId} não encontrado em nossos registros.";
            }
        }
    }
}