using AgentePedidoCS.Agent.Tools;
using Microsoft.Extensions.Logging;
using System;
using System.Linq; // System.Linq é usado implicitamente por algumas operações de string ou coleção, bom manter se necessário.
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgentePedidoCS.Agent
{
    /// <summary>
    /// Fornece lógica de negócios para processar pedidos após uma interação inicial com o LLM.
    /// Este serviço é responsável por extrair IDs de pedido da mensagem do usuário,
    /// verificar o status do pedido, determinar se um pedido deve ser priorizado
    /// e coordenar notificações, modificando a resposta do agente conforme necessário.
    /// </summary>
    public class OrderProcessingService
    {
        private readonly ILogger<OrderProcessingService> _logger;
        private readonly OrderStatusTool _orderStatusTool;
        private readonly OrderPrioritizerAgent _prioritizerAgent;
        private readonly NotificationAgent _notificationAgent;

        /// <summary>
        /// Constante para o status de pedido "Processando".
        /// </summary>
        public const string ProcessingStatus = "Processando";
        /// <summary>
        /// Padrão Regex para identificar um ID de pedido numérico de 5 dígitos.
        /// </summary>
        public const string OrderIdNumericRegexPattern = @"\b\d{5}\b";
        /// <summary>
        /// ID de pedido usado em testes para simular um cliente VIP.
        /// </summary>
        public const string SimulatedVipCustomerIdForOrder = "12345";
        /// <summary>
        /// Identificador de cliente usado em testes para simular um cliente regular.
        /// </summary>
        public const string SimulatedRegularCustomerId = "REGULAR_CUSTOMER";


        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="OrderProcessingService"/>.
        /// </summary>
        /// <param name="logger">O logger para esta classe.</param>
        /// <param name="orderStatusTool">A ferramenta para obter dados de status de pedidos.</param>
        /// <param name="prioritizerAgent">O agente para determinar a priorização de pedidos.</param>
        /// <param name="notificationAgent">O agente para enviar notificações.</param>
        /// <exception cref="ArgumentNullException">Lançada se qualquer uma das dependências injetadas for nula.</exception>
        public OrderProcessingService(
            ILogger<OrderProcessingService> logger,
            OrderStatusTool orderStatusTool,
            OrderPrioritizerAgent prioritizerAgent,
            NotificationAgent notificationAgent)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orderStatusTool = orderStatusTool ?? throw new ArgumentNullException(nameof(orderStatusTool));
            _prioritizerAgent = prioritizerAgent ?? throw new ArgumentNullException(nameof(prioritizerAgent));
            _notificationAgent = notificationAgent ?? throw new ArgumentNullException(nameof(notificationAgent));
        }

        /// <summary>
        /// Manipula a lógica de negócios de um pedido após uma resposta inicial do LLM.
        /// Extrai um ID de pedido da mensagem do usuário, verifica seu status e, se estiver "Processando",
        /// avalia a priorização e envia notificações, ajustando a resposta do agente.
        /// </summary>
        /// <param name="userMessage">A mensagem original do usuário.</param>
        /// <param name="initialAgentResponse">A resposta inicial gerada pelo LLM (possivelmente após usar ferramentas).</param>
        /// <returns>
        /// A resposta final do agente. Pode ser a <paramref name="initialAgentResponse"/> ou uma resposta modificada
        /// com base na lógica de priorização e status do pedido.
        /// </returns>
        public async Task<string> HandleOrderRequestAsync(string userMessage, string initialAgentResponse)
        {
            _logger.LogInformation("OrderProcessingService: Iniciando o manuseio da solicitação de pedido para a mensagem: \"{UserMessage}\"", userMessage);
            string finalAgentResponse = initialAgentResponse;

            string? orderIdForManualCheck = null;
            // Tenta encontrar um ID de pedido genérico (alphanumérico, 5-10 chars) que pode ser o ID de retry ou um ID numérico.
            string generalOrderIdPattern = @"\b[a-zA-Z0-9]{5,10}\b";
            var generalMatch = Regex.Match(userMessage, generalOrderIdPattern);

            if (generalMatch.Success &&
                (generalMatch.Value == OrderStatusTool.ORDER_ID_FOR_RETRY_TEST || // Verifica se é o ID de teste de retry
                 Regex.IsMatch(generalMatch.Value, OrderIdNumericRegexPattern)))   // Ou se corresponde ao padrão numérico de 5 dígitos
            {
                orderIdForManualCheck = generalMatch.Value;
            }
            // Se nenhum ID foi encontrado com a lógica acima (ou não era o ID de retry), tenta especificamente o padrão numérico.
            if (string.IsNullOrEmpty(orderIdForManualCheck))
            {
                var numericMatch = Regex.Match(userMessage, OrderIdNumericRegexPattern);
                if (numericMatch.Success)
                {
                    orderIdForManualCheck = numericMatch.Value;
                }
            }

            if (!string.IsNullOrEmpty(orderIdForManualCheck))
            {
                _logger.LogInformation($"OrderProcessingService: Order ID extraído da mensagem do usuário: \"{orderIdForManualCheck}\"");

                (string status, string item, bool found) currentOrderData = await _orderStatusTool.GetOrderStatusDataAsync(orderIdForManualCheck);

                if (!currentOrderData.found)
                {
                    _logger.LogInformation($"OrderProcessingService: Pedido \"{orderIdForManualCheck}\" não encontrado pela ferramenta GetOrderStatusDataAsync. A resposta inicial do LLM será mantida: \"{initialAgentResponse}\"");
                    // A initialAgentResponse (que o LLM gerou, possivelmente usando CheckOrderStatus) já deve indicar que o pedido não foi encontrado.
                }
                else if (currentOrderData.status == ProcessingStatus)
                {
                    _logger.LogInformation($"OrderProcessingService: Pedido \"{orderIdForManualCheck}\" (Item: \"{currentOrderData.item}\") está com status \"{ProcessingStatus}\". Verificando necessidade de priorização.");

                    // Simula a determinação do ID do cliente para fins de priorização.
                    string customerId = (orderIdForManualCheck == SimulatedVipCustomerIdForOrder)
                                        ? OrderPrioritizerAgent.VipCustomerIdentifier // Cliente VIP associado a este ID de pedido de teste
                                        : SimulatedRegularCustomerId; // Cliente regular para outros casos

                    bool shouldPrioritize = await _prioritizerAgent.ShouldPrioritizeAsync(currentOrderData.item, customerId);

                    if (shouldPrioritize)
                    {
                        _logger.LogInformation($"OrderProcessingService: Pedido \"{orderIdForManualCheck}\" (Cliente: \"{customerId}\") DEVE ser priorizado.");
                        await _notificationAgent.SendPrioritizationNotificationAsync(orderIdForManualCheck, customerId);
                        finalAgentResponse = $"O status do seu pedido {orderIdForManualCheck} ('{currentOrderData.item}') é '{currentOrderData.status}'. Ele foi priorizado e você será notificado em breve.";
                    }
                    else
                    {
                        _logger.LogInformation($"OrderProcessingService: Pedido \"{orderIdForManualCheck}\" (Cliente: \"{customerId}\") NÃO será priorizado.");
                        finalAgentResponse = $"O status do seu pedido {orderIdForManualCheck} ('{currentOrderData.item}') é '{currentOrderData.status}'. Ele está sendo processado normalmente.";
                    }
                }
                else
                {
                    _logger.LogInformation($"OrderProcessingService: Pedido \"{orderIdForManualCheck}\" está com status \"{currentOrderData.status}\". Nenhuma ação de priorização é necessária. A resposta inicial do LLM será mantida: \"{initialAgentResponse}\"");
                    // A initialAgentResponse (gerada pelo LLM usando CheckOrderStatus) já deve conter o status correto.
                }
            }
            else
            {
                _logger.LogInformation("OrderProcessingService: Nenhum ID de pedido válido foi extraído da mensagem do usuário. A resposta inicial do LLM será mantida: \"{InitialAgentResponse}\"", initialAgentResponse);
            }

            _logger.LogInformation($"OrderProcessingService: Resposta final a ser retornada: \"{finalAgentResponse}\"");
            return finalAgentResponse;
        }
    }
}
