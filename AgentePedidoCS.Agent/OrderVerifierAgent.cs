// AgentePedidoCS/AgentePedidoCS.Agent/OrderVerifierAgent.cs
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentePedidoCS.Agent.Tools;
using Microsoft.Extensions.Logging;
using System;

namespace AgentePedidoCS.Agent
{
    /// <summary>
    /// Agente principal responsável por interagir com o usuário, orquestrar o uso do Semantic Kernel
    /// para compreensão da linguagem natural e delegação de lógica de negócios de pedidos.
    /// Ele utiliza o Kernel para chamadas de LLM e ferramentas, e o <see cref="OrderProcessingService"/>
    /// para lógica de negócios adicional após a resposta inicial do LLM.
    /// </summary>
    public class OrderVerifierAgent
    {
        private readonly Kernel _kernel;
        private readonly ILogger<OrderVerifierAgent> _logger;
        private readonly OrderProcessingService _orderProcessingService;
        private readonly OrderStatusTool _orderStatusTool;
        private readonly BatchOrderTool _batchOrderTool;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="OrderVerifierAgent"/>.
        /// </summary>
        /// <param name="logger">O logger para esta classe.</param>
        /// <param name="kernel">A instância do Semantic Kernel já configurada e pronta para uso.</param>
        /// <param name="orderProcessingService">Serviço para processamento de lógica de negócios de pedidos.</param>
        /// <param name="orderStatusTool">Ferramenta para verificar o status de pedidos, será importada no Kernel.</param>
        /// <param name="batchOrderTool">Ferramenta para registrar pedidos em lote, será importada no Kernel.</param>
        /// <exception cref="ArgumentNullException">Lançada se qualquer uma das dependências injetadas for nula.</exception>
        public OrderVerifierAgent(
            ILogger<OrderVerifierAgent> logger,
            Kernel kernel,
            OrderProcessingService orderProcessingService,
            OrderStatusTool orderStatusTool,
            BatchOrderTool batchOrderTool)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _orderProcessingService = orderProcessingService ?? throw new ArgumentNullException(nameof(orderProcessingService));
            _orderStatusTool = orderStatusTool ?? throw new ArgumentNullException(nameof(orderStatusTool));
            _batchOrderTool = batchOrderTool ?? throw new ArgumentNullException(nameof(batchOrderTool));

            // Importa as ferramentas (plugins) para o Kernel para que possam ser usadas pelo LLM.
            _kernel.ImportPluginFromObject(_orderStatusTool, "OrderStatusTool");
            _kernel.ImportPluginFromObject(_batchOrderTool, "BatchOrderTool");
            _logger.LogInformation("OrderVerifierAgent inicializado e plugins (OrderStatusTool, BatchOrderTool) importados para o Kernel.");
        }

        /// <summary>
        /// Processa a mensagem do usuário, interage com o Semantic Kernel para obter uma resposta inicial
        /// e, em seguida, utiliza o <see cref="OrderProcessingService"/> para aplicar lógica de negócios adicional
        /// e refinar a resposta, se necessário.
        /// </summary>
        /// <param name="userMessage">A mensagem de entrada do usuário.</param>
        /// <returns>Uma string contendo a resposta do agente para o usuário.</returns>
        public async Task<string> ProcessRequestAsync(string userMessage)
        {
            _logger.LogInformation($"OrderVerifierAgent: Mensagem do usuário recebida: \"{userMessage}\"");

            var history = new ChatHistory();
            // O prompt do sistema instrui o LLM sobre suas capacidades e como usar as ferramentas.
            history.AddSystemMessage("Você é o OrderVerifierAgent, um assistente inteligente especializado em verificar o status de pedidos e registrar novos pedidos em lote. " +
                                     "Use a ferramenta 'OrderStatusTool.CheckOrderStatus' para obter informações sobre pedidos existentes quando um ID de pedido é fornecido. " +
                                     "Se o pedido não for encontrado usando a ferramenta, informe ao usuário que o pedido pode estar em processamento inicial e sugira que tente novamente mais tarde ou entre em contato por outro canal. " +
                                     "Use a ferramenta 'BatchOrderTool.RegisterBatchOrder' para criar um novo pedido em lote quando o usuário solicitar o registro de múltiplos itens para um cliente. Você precisará extrair a lista de itens e o nome do cliente da solicitação do usuário. " +
                                     "Responda de forma clara e concisa. Se o ID do pedido não for fornecido ou for inválido para verificação, peça um ID de pedido válido. Se informações para registrar um pedido em lote estiverem faltando, peça os detalhes necessários (itens e cliente).");
            history.AddUserMessage(userMessage);

            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,
                Temperature = 0.7,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions // Permite que o Kernel chame as ferramentas automaticamente.
            };

            try
            {
                _logger.LogDebug("OrderVerifierAgent: Solicitando resposta do chat completion ao Kernel.");
                // Obtém a resposta inicial do LLM, que pode ter invocado ferramentas.
                var result = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(
                    history,
                    executionSettings: promptExecutionSettings,
                    kernel: _kernel
                );

                string initialAgentResponse = result.Content ?? "Desculpe, o agente não conseguiu gerar uma resposta significativa.";
                _logger.LogInformation($"OrderVerifierAgent: Resposta inicial do LLM: \"{initialAgentResponse}\"");

                // Delega para o OrderProcessingService para aplicar lógica de negócios adicional
                // (como extração de ID, verificações de priorização) à resposta.
                string finalResponse = await _orderProcessingService.HandleOrderRequestAsync(userMessage, initialAgentResponse);

                _logger.LogInformation($"OrderVerifierAgent: Resposta final após OrderProcessingService: \"{finalResponse}\"");
                return finalResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderVerifierAgent: Ocorreu um erro crítico ao processar a solicitação do usuário.");
                return "Desculpe, ocorreu um erro ao processar sua solicitação. Por favor, tente novamente mais tarde.";
            }
        }
    }
}
