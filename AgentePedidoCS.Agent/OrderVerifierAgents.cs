// AgentePedidoCS/AgentePedidoCS.Agent/OrderVerifierAgent.cs
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI; // Para o conector OpenAI
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Configuration;       // Para carregar configurações (appsettings.json)
using AgentePedidoCS.Agent.Tools;               // Para importar as ferramentas que você criou
using Microsoft.Extensions.Logging;             // Para logging


namespace AgentePedidoCS.Agent
{
    public class OrderVerifierAgent
    {
        private readonly Kernel _kernel;        // A interface principal do Semantic Kernel
        private readonly ILogger _logger;        // Logger para mensagens de depuração/informação

        public OrderVerifierAgent(IConfiguration config)
        {
            // 1. Configuração do Logger
            // Configura o logger para mostrar mensagens no console.
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning) // Filtra logs excessivos do .NET
                    .AddFilter("System", LogLevel.Warning)    // Filtra logs excessivos do Sistema
                    .AddConsole();                            // Adiciona o provedor de log para console
            });
            _logger = loggerFactory.CreateLogger<OrderVerifierAgent>(); // Cria um logger para esta classe

            // 2. Recuperar Configurações do Azure OpenAI
            // Pega as chaves e o endpoint do Azure OpenAI das configurações carregadas (appsettings.json ou variáveis de ambiente)
            var azureOpenAiEndpoint = config["AzureOpenAI:Endpoint"];
            var azureOpenAiApiKey = config["AzureOpenAI:ApiKey"];
            var azureOpenAiDeploymentName = config["AzureOpenAI:DeploymentName"];

            // Validação básica para garantir que as configurações foram encontradas
            if (string.IsNullOrEmpty(azureOpenAiEndpoint) ||
                string.IsNullOrEmpty(azureOpenAiApiKey) ||
                string.IsNullOrEmpty(azureOpenAiDeploymentName))
            {
                _logger.LogError("As configurações do Azure OpenAI (Endpoint, ApiKey, DeploymentName) não foram encontradas. Verifique appsettings.json, appsettings.Development.json ou variáveis de ambiente.");
                throw new InvalidOperationException("Configurações do Azure OpenAI ausentes. Por favor, preencha-as.");
            }

            _logger.LogInformation("Inicializando o Semantic Kernel com Azure OpenAI.");

            // 3. Configurar o Kernel com o Modelo de Linguagem
            // Cria uma instância do Kernel, que é o motor do Semantic Kernel.
            // Adiciona o conector para o Azure OpenAI Chat Completion (GPT-3.5 Turbo, GPT-4, Phi-3, etc.).
            _kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: azureOpenAiDeploymentName, // O nome da sua implantação do modelo no Azure OpenAI Studio
                    endpoint: azureOpenAiEndpoint,             // O endpoint do seu recurso Azure OpenAI
                    apiKey: azureOpenAiApiKey                  // Sua chave de API
                )
                .Build(); // Constrói a instância do Kernel

            _logger.LogInformation("Adicionando ferramentas ao Kernel.");

            // 4. Adicionar a Ferramenta ao Kernel
            // Importa a classe OrderStatusTool como um plugin/ferramenta no Kernel.
            // O nome "OrderStatusTool" aqui será usado pelo LLM para se referir a este conjunto de funções.
            _kernel.ImportPluginFromObject(new OrderStatusTool(), "OrderStatusTool");
            _kernel.ImportPluginFromObject(new BatchOrderTool(), "BatchOrderTool"); // Added this line
        }

        /// <summary>
        /// Processa a mensagem do usuário, interagindo com o LLM e usando ferramentas se necessário.
        /// </summary>
        /// <param name="userMessage">A mensagem de entrada do usuário.</param>
        /// <returns>A resposta gerada pelo agente.</returns>
        public async Task<string> ProcessRequestAsync(string userMessage)
        {
            _logger.LogInformation($"Mensagem do usuário: {userMessage}");

            // 1. Criar Histórico de Chat
            // O histórico é crucial para manter o contexto da conversa com o LLM.
            var history = new ChatHistory();
            // A System Message (Mensagem de Sistema) instrui o LLM sobre seu papel e como usar as ferramentas.
            // É VITAL que o nome da ferramenta (OrderStatusTool) e da função (CheckOrderStatus) estejam aqui
            // para o LLM saber como chamá-las.
            // Updated system message below
            history.AddSystemMessage("Você é o OrderVerifierAgent, um assistente inteligente especializado em verificar o status de pedidos e registrar novos pedidos em lote. " +
                                     "Use a ferramenta 'OrderStatusTool.CheckOrderStatus' para obter informações sobre pedidos existentes quando um ID de pedido é fornecido. " +
                                     "Use a ferramenta 'BatchOrderTool.RegisterBatchOrder' para criar um novo pedido em lote quando o usuário solicitar o registro de múltiplos itens para um cliente. Você precisará extrair a lista de itens e o nome do cliente da solicitação do usuário. " +
                                     "Responda de forma clara e concisa. Se o ID do pedido não for fornecido ou for inválido para verificação, peça um ID de pedido válido. Se informações para registrar um pedido em lote estiverem faltando, peça os detalhes necessários (itens e cliente).");
            history.AddUserMessage(userMessage); // Adiciona a mensagem atual do usuário ao histórico

            // 2. Configurações de Execução do Chat
            var promptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,  // Limite o número de tokens na resposta para controlar custos
                Temperature = 0.7, // Controla a criatividade/aleatoriedade da resposta (0.0 = previsível, 1.0 = criativo)
                // Isso é CRUCIAL: Diz ao Semantic Kernel para automaticamente invocar funções do Kernel
                // quando o LLM decidir que uma ferramenta precisa ser usada.
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            try
            {
                // 3. Obter Resposta do LLM
                // Pede ao serviço de chat do Kernel para gerar uma resposta baseada no histórico.
                // O 'kernel: _kernel' é importante para que o serviço de chat tenha acesso às ferramentas que você importou.
                var result = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(
                    history,
                    executionSettings: promptExecutionSettings,
                    kernel: _kernel // Passa o kernel para que as ferramentas sejam acessíveis ao serviço de chat
                );

                _logger.LogInformation($"Resposta do agente: {result.Content}");
                return result.Content ?? "Desculpe. o agente não conseguiu gerar uma resposta significativa."; // Retorna o conteúdo da resposta do LLM
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao processar a solicitação.");
                return "Desculpe, ocorreu um erro ao processar sua solicitação. Por favor, tente novamente mais tarde.";
            }
        }
    }
}
