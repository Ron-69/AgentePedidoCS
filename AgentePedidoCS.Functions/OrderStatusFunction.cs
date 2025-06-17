using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AgentePedidoCS.Functions
{
    public class OrderStatusFunction
    {
        private static readonly ConcurrentDictionary<string, (string Status, string Item)> _orderDatabase;
        private static readonly ConcurrentDictionary<string, int> _retryAttempts = new();

        public const string ORDER_ID_FOR_RETRY_TEST = "RETRY123";
        private const int MAX_ATTEMPTS_BEFORE_SUCCESS = 2; // 0, 1 fail, 2 success

        private readonly ILogger<OrderStatusFunction> _logger;

        static OrderStatusFunction()
        {
            // Initialize _orderDatabase with sample data
            _orderDatabase = new ConcurrentDictionary<string, (string Status, string Item)>(
                new[] {
                    KeyValuePair.Create("12345", ("Processando", "Laptop Gamer")),
                    KeyValuePair.Create("67890", ("Enviado", "Monitor Curvo")),
                    KeyValuePair.Create("ABCDE", ("Entregue", "Mouse Sem Fio")),
                    KeyValuePair.Create("FGHIJ", ("Cancelado", "Teclado Mecânico")),
                    KeyValuePair.Create(ORDER_ID_FOR_RETRY_TEST, ("Processando", "Item de Teste com Retentativas"))
                });
        }

        public OrderStatusFunction(ILogger<OrderStatusFunction> logger)
        {
            _logger = logger;
        }

        [Function("CheckOrderStatusFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orderstatus/{orderId}")] HttpRequestData req,
            string orderId)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request for orderId: {OrderId}", orderId);

            if (string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("OrderId is null or whitespace.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Please provide an orderId in the route.");
                return badRequestResponse;
            }

            if (orderId.Equals(ORDER_ID_FOR_RETRY_TEST, StringComparison.OrdinalIgnoreCase))
            {
                int currentAttempt = _retryAttempts.AddOrUpdate(orderId, 0, (key, oldValue) => oldValue + 1);

                if (currentAttempt < MAX_ATTEMPTS_BEFORE_SUCCESS)
                {
                    _logger.LogInformation($"Simulating retry for orderId {orderId}. Attempt {currentAttempt + 1}/{MAX_ATTEMPTS_BEFORE_SUCCESS + 1}. Status: Falha na consulta.");
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    await response.WriteStringAsync($"O pedido {orderId} está com status: Falha na consulta (tentativa {currentAttempt + 1}).");
                    return response;
                }
                else
                {
                    // On successful attempt, reset for next cycle if needed, or just proceed
                    _logger.LogInformation($"Simulating successful fetch for orderId {orderId} after {currentAttempt} retries.");
                    // _retryAttempts.TryRemove(orderId, out _); // Optional: reset attempts after success
                }
            }

            if (_orderDatabase.TryGetValue(orderId, out var orderInfo))
            {
                _logger.LogInformation($"Order {orderId} found. Status: {orderInfo.Status}, Item: {orderInfo.Item}.");
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await response.WriteStringAsync($"O pedido {orderId} está com status: {orderInfo.Status} ({orderInfo.Item}).");
                return response;
            }
            else
            {
                _logger.LogWarning($"Order {orderId} not found.");
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await response.WriteStringAsync($"Pedido {orderId} não encontrado.");
                return response;
            }
        }
    }
}
