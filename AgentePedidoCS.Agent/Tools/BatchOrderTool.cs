// AgentePedidoCS/AgentePedidoCS.Agent/Tools/BatchOrderTool.cs
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System; // Required for Guid

namespace AgentePedidoCS.Agent.Tools
{
    public class BatchOrderTool
    {
        // Simulates a database of batch orders in memory
        private static readonly Dictionary<string, (string Customer, List<string> Items)> _batchOrderDatabase = new();

        /// <summary>
        /// Registers a new batch order for a customer with a list of items.
        /// Use this tool to create a new batch order in the system.
        /// </summary>
        /// <param name="items">The list of items to include in the batch order.</param>
        /// <param name="customer">The name of the customer placing the batch order.</param>
        /// <returns>A unique batch order ID for the newly created batch order.</returns>
        [KernelFunction, Description("Registers a new batch order for a customer with a list of items.")]
        public static async Task<string> RegisterBatchOrder(
            [Description("The list of items for the order.")] List<string> items,
            [Description("The customer name.")] string customer)
        {
            await Task.Delay(100); // Simulate some async work

            if (items == null || items.Count == 0)
            {
                return "Cannot register a batch order with no items.";
            }

            if (string.IsNullOrWhiteSpace(customer))
            {
                return "Cannot register a batch order without a customer name.";
            }

            var batchOrderId = Guid.NewGuid().ToString();
            _batchOrderDatabase[batchOrderId] = (customer, items);

            Console.WriteLine($"[BatchOrderTool] Registered batch order {batchOrderId} for customer {customer} with items: {string.Join(", ", items)}");
            return $"Batch order registered successfully. Your batch order ID is: {batchOrderId}";
        }
    }
}
