// AgentePedidoCS/AgentePedidoCS.Agent/OrderVerifierAgent.cs
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI; // Para o conector OpenAI
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Configuration;       // Para carregar configurações (appsettings.json)
using AgentePedidoCS.Agent.Tools;               // Para importar as ferramentas que você criou
using Microsoft.Extensions.Logging;             // Para logging
using System.Linq;                              // Para LastOrDefault
using System.Text.RegularExpressions;           // Para Regex

namespace AgentePedidoCS.Agent
{
    public class OrderVerifierAgent
    {
        private readonly Kernel _kernel;        // A interface principal do Semantic Kernel
        private readonly ILogger _logger;        // Logger para mensagens de depuração/informação
        private readonly ILoggerFactory _loggerFactory; // Adicionado para criar loggers para outros agentes

        public OrderVerifierAgent(IConfiguration config)
        {
            // 1. Configuração do Logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning) // Filtra logs excessivos do .NET
                    .AddFilter("System", LogLevel.Warning)    // Filtra logs excessivos do Sistema
                    .AddConsole();                            // Adiciona o provedor de log para console
            });
            _logger = loggerFactory.CreateLogger<OrderVerifierAgent>(); // Cria um logger para esta classe
            _loggerFactory = loggerFactory; // Store the factory for other agents

            // 2. Recuperar Configurações do Azure OpenAI
            var azureOpenAiEndpoint = config["AzureOpenAI:Endpoint"];
            var azureOpenAiApiKey = config["AzureOpenAI:ApiKey"];
            var azureOpenAiDeploymentName = config["AzureOpenAI:DeploymentName"];

            if (string.IsNullOrEmpty(azureOpenAiEndpoint) ||
                string.IsNullOrEmpty(azureOpenAiApiKey) ||
                string.IsNullOrEmpty(azureOpenAiDeploymentName))
            {
                _logger.LogError("As configurações do Azure OpenAI (Endpoint, ApiKey, DeploymentName) não foram encontradas. Verifique appsettings.json, appsettings.Development.json ou variáveis de ambiente.");
                throw new InvalidOperationException("Configurações do Azure OpenAI ausentes. Por favor, preencha-as.");
            }

            _logger.LogInformation("Inicializando o Semantic Kernel com Azure OpenAI.");

            _kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: azureOpenAiDeploymentName,
                    endpoint: azureOpenAiEndpoint,
                    apiKey: azureOpenAiApiKey
                )
                .Build();

            _logger.LogInformation("Adicionando ferramentas ao Kernel.");
            _kernel.ImportPluginFromObject(new OrderStatusTool(), "OrderStatusTool");
            _kernel.ImportPluginFromObject(new BatchOrderTool(), "BatchOrderTool");
        }

        public async Task<string> ProcessRequestAsync(string userMessage)
        {
            _logger.LogInformation($"Mensagem do usuário: {userMessage}");

            var history = new ChatHistory();
            history.AddSystemMessage("Você é o OrderVerifierAgent, um assistente inteligente especializado em verificar o status de pedidos e registrar novos pedidos em lote. " +
                                     "Use a ferramenta 'OrderStatusTool.CheckOrderStatus' para obter informações sobre pedidos existentes quando um ID de pedido é fornecido. " +
                                     "Se o status do pedido for 'Processando', informe ao usuário que você verificará a priorização e o notificará. " +
                                     "Se o pedido não for encontrado usando a ferramenta, informe ao usuário que o pedido pode estar em processamento inicial e sugira que tente novamente mais tarde ou entre em contato por outro canal. " +
                                     "Use a ferramenta 'BatchOrderTool.RegisterBatchOrder' para criar um novo pedido em lote quando o usuário solicitar o registro de múltiplos itens para um cliente. Você precisará extrair a lista de itens e o nome do cliente da solicitação do usuário. " +
                                     "Responda de forma clara e concisa. Se o ID do pedido não for fornecido ou for inválido para verificação, peça um ID de pedido válido. Se informações para registrar um pedido em lote estiverem faltando, peça os detalhes necessários (itens e cliente).");
            history.AddUserMessage(userMessage);

            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,
                Temperature = 0.7,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            try
            {
                // 3. Obter Resposta do LLM (que pode incluir uma chamada de ferramenta)
                var result = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(
                    history,
                    executionSettings: promptExecutionSettings,
                    kernel: _kernel
                );

                string agentResponse = result.Content ?? "Desculpe, o agente não conseguiu gerar uma resposta significativa.";
                _logger.LogInformation($"Resposta inicial do LLM: {agentResponse}");

                string? orderIdForManualCheck = null;
                // Regex para encontrar um ID de pedido de 5 dígitos na mensagem do usuário.
                // A expressão @"\b\d{5}\b" garante que estamos pegando um número de 5 dígitos como uma palavra isolada.
                var match = Regex.Match(userMessage, @"\b\d{5}\b");
                if (match.Success)
                {
                    orderIdForManualCheck = match.Value;
                    _logger.LogInformation($"Possível Order ID extraído da mensagem do usuário: {orderIdForManualCheck}");

                    // Verificar manualmente o status para a lógica de priorização
                    (string status, string item, bool found) currentOrderData = await OrderStatusTool.GetOrderStatusDataAsync(orderIdForManualCheck);

                    if (!currentOrderData.found)
                    {
                        _logger.LogInformation($"OrderVerifierAgent: Pedido '{orderIdForManualCheck}' não encontrado. Usando mensagem de fallback do system prompt.");
                        // O LLM já foi instruído sobre o que dizer. Se quisermos sobrescrever ou garantir:
                        // agentResponse = $"O pedido {orderIdForManualCheck} não foi encontrado. Pode ser que ainda esteja em processamento inicial. Por favor, tente novamente mais tarde ou entre em contato conosco por outro canal.";
                        // No entanto, o prompt do sistema já cobre isso. A resposta do LLM (result.Content) já deve ser adequada.
                    }
                    else if (currentOrderData.status == "Processando")
                    {
                        _logger.LogInformation($"OrderVerifierAgent: Pedido '{orderIdForManualCheck}' ({currentOrderData.item}) está 'Processando'. Verificando priorização.");
                        var prioritizerAgent = new OrderPrioritizerAgent(_loggerFactory);

                        // Simulando um customerId. Em um cenário real, isso viria de dados do usuário ou sessão.
                        string customerId = (orderIdForManualCheck == "12345") ? "VIP_CUSTOMER" : "REGULAR_CUSTOMER";

                        bool shouldPrioritize = await prioritizerAgent.ShouldPrioritizeAsync(currentOrderData.item, customerId);

                        if (shouldPrioritize)
                        {
                            _logger.LogInformation($"OrderVerifierAgent: Pedido '{orderIdForManualCheck}' DEVE ser priorizado.");
                            var notificationAgent = new NotificationAgent(_loggerFactory);
                            await notificationAgent.SendPrioritizationNotificationAsync(orderIdForManualCheck, customerId);
                            agentResponse = $"O status do seu pedido {orderIdForManualCheck} ('{currentOrderData.item}') é '{currentOrderData.status}'. Ele foi priorizado e você será notificado em breve.";
                        }
                        else
                        {
                            _logger.LogInformation($"OrderVerifierAgent: Pedido '{orderIdForManualCheck}' NÃO será priorizado.");
                            agentResponse = $"O status do seu pedido {orderIdForManualCheck} ('{currentOrderData.item}') é '{currentOrderData.status}'. Ele está sendo processado normalmente.";
                        }
                    }
                    // Para outros status (Enviado, Entregue, Cancelado), a resposta original do LLM (que já usou a ferramenta) é geralmente suficiente.
                    // O LLM já foi instruído a usar OrderStatusTool.CheckOrderStatus e reportar o status.
                    // Se o status não for "Processando", a `agentResponse` do LLM já deve conter o status correto.
                }
                // Se nenhum ID de pedido foi detectado na mensagem do usuário, ou se a ferramenta não foi chamada por outros motivos,
                // a resposta original do LLM (provavelmente pedindo um ID, ou respondendo a uma saudação, etc.) é usada.

                _logger.LogInformation($"Resposta final do agente: {agentResponse}");
                return agentResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao processar a solicitação.");
                return "Desculpe, ocorreu um erro ao processar sua solicitação. Por favor, tente novamente mais tarde.";
            }
        }
    }
}
