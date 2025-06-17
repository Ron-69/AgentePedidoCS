using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentePedidoCS.Functions
{
    public class BatchOrderRequest
    {
        public List<string> Items { get; set; }
        public string Customer { get; set; }
    }

    public class BatchOrderFunction
    {
        private static readonly ConcurrentDictionary<string, (string Customer, List<string> Items)> _batchOrderDatabase = new();
        private readonly ILogger<BatchOrderFunction> _logger;

        public BatchOrderFunction(ILogger<BatchOrderFunction> logger)
        {
            _logger = logger;
        }

        [Function("RegisterBatchOrderFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "batchorder")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to RegisterBatchOrderFunction.");

            string requestBody;
            try
            {
                requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading request body.");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Error reading request body.");
                return errorResponse;
            }

            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogWarning("Request body is null or empty.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Please pass a request body.");
                return badRequestResponse;
            }

            BatchOrderRequest data;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                data = JsonSerializer.Deserialize<BatchOrderRequest>(requestBody, options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing request body.");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid JSON format in request body.");
                return errorResponse;
            }

            if (data == null)
            {
                _logger.LogWarning("Deserialized data is null.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body could not be deserialized correctly.");
                return badRequestResponse;
            }

            if (data.Items == null || data.Items.Count == 0)
            {
                _logger.LogWarning("Items list is null or empty.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("The 'items' field is required and cannot be empty.");
                return badRequestResponse;
            }

            if (string.IsNullOrWhiteSpace(data.Customer))
            {
                _logger.LogWarning("Customer is null or whitespace.");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("The 'customer' field is required.");
                return badRequestResponse;
            }

            var batchOrderId = Guid.NewGuid().ToString();
            var orderData = (Customer: data.Customer, Items: data.Items);

            if (_batchOrderDatabase.TryAdd(batchOrderId, orderData))
            {
                _logger.LogInformation($"Batch order {batchOrderId} for customer {data.Customer} registered successfully with {data.Items.Count} items.");
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                // Creating a simple object for the response body for better structure
                var successPayload = new { BatchOrderId = batchOrderId, Message = $"Pedido em lote registrado com sucesso. O ID do seu pedido em lote é: {batchOrderId}" };
                await response.WriteStringAsync(JsonSerializer.Serialize(successPayload));
                return response;
            }
            else
            {
                _logger.LogError($"Failed to add batch order {batchOrderId} to the database. This should not happen with GUIDs.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An unexpected error occurred while registering the batch order.");
                return errorResponse;
            }
        }
    }
}
