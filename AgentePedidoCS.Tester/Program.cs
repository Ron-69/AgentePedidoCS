using System;
using System.Threading.Tasks;
using AgentePedidoCS.Agent;
using AgentePedidoCS.Agent.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

class Program
{
    // Constantes para IDs de Teste para facilitar a leitura e manutenção dos casos de teste
    private const string TestOrderIdVipProcessing = "12345";     // Simula um pedido de cliente VIP que está "Processando"
    private const string TestOrderIdRegularProcessing = "77777"; // Simula um pedido de cliente regular que está "Processando"
    private const string TestOrderIdNotFound = "99999";          // Simula um pedido que não será encontrado
    private const string TestOrderIdShipped = "67890";           // Simula um pedido que já foi enviado
    // OrderStatusTool.ORDER_ID_FOR_RETRY_TEST (RETRY123) é usado diretamente para o teste de retry.

    static async Task Main(string[] args)
    {
        // 1. Configuração da Aplicação: Carrega configurações de appsettings.json, appsettings.Development.json e variáveis de ambiente.
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // Sobrescreve configurações para ambiente de desenvolvimento
            .AddEnvironmentVariables(); // Sobrescreve configurações com variáveis de ambiente
        IConfiguration config = builder.Build();

        // 2. Configuração da Injeção de Dependência (DI)
        var services = new ServiceCollection();

        // Registrar serviço de Logging: Adiciona ILogger<T> e ILoggerFactory para serem injetados.
        services.AddLogging(logBuilder =>
        {
            logBuilder.AddConsole(); // Configura o log para sair no console
            // logBuilder.SetMinimumLevel(LogLevel.Debug); // Descomente para logs mais detalhados durante o desenvolvimento
        });

        // Registrar IConfiguration para que outras classes possam acessar as configurações da aplicação.
        services.AddSingleton<IConfiguration>(config);

        // Registrar Ferramentas (Tools) como Singletons, pois geralmente não mantêm estado específico da requisição
        // e podem ser compartilhadas entre múltiplos consumidores.
        services.AddSingleton<OrderStatusTool>();
        services.AddSingleton<BatchOrderTool>();

        // Registrar Agentes e Serviços como Transientes. Uma nova instância será criada cada vez que forem solicitados.
        // Isso é bom para classes que podem manter algum estado durante uma operação ou que não são thread-safe para serem singletons.
        services.AddTransient<NotificationAgent>();
        services.AddTransient<OrderPrioritizerAgent>();
        services.AddTransient<OrderProcessingService>();
        services.AddTransient<OrderVerifierAgent>();

        // 3. Configuração e Registro do Kernel do Semantic Kernel
        // É comum criar um logger específico para a configuração do Kernel, especialmente se ocorrer antes do ServiceProvider estar totalmente construído.
        var loggerFactoryForKernelSetup = LoggerFactory.Create(logBuilder => logBuilder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var kernelLogger = loggerFactoryForKernelSetup.CreateLogger<Kernel>(); // Logger para o setup do Kernel

        var azureOpenAiEndpoint = config["AzureOpenAI:Endpoint"];
        var azureOpenAiApiKey = config["AzureOpenAI:ApiKey"];
        var azureOpenAiDeploymentName = config["AzureOpenAI:DeploymentName"];

        if (string.IsNullOrEmpty(azureOpenAiEndpoint) ||
            string.IsNullOrEmpty(azureOpenAiApiKey) ||
            string.IsNullOrEmpty(azureOpenAiDeploymentName))
        {
            kernelLogger.LogError("Configurações do Azure OpenAI (Endpoint, ApiKey, DeploymentName) não foram encontradas. Verifique a configuração.");
            Console.WriteLine("Erro crítico: Configurações do Azure OpenAI ausentes. O programa será encerrado.");
            return; // Encerrar se o Kernel não pode ser configurado
        }

        kernelLogger.LogInformation("Inicializando o Semantic Kernel com Azure OpenAI no Program.cs.");

        Kernel kernelInstance;
        try
        {
            kernelInstance = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: azureOpenAiDeploymentName,
                    endpoint: azureOpenAiEndpoint,
                    apiKey: azureOpenAiApiKey
                )
                // Adicionar ILoggerFactory ao Kernel para que seus componentes internos possam criar loggers.
                // Opcional: .Services.AddSingleton(loggerFactoryForKernelSetup) se o Kernel precisar explicitamente de ILoggerFactory em seu DI interno.
                // Geralmente, o Kernel e seus componentes usam o logging configurado globalmente se não for fornecido um específico.
                .Build();
            kernelLogger.LogInformation("Instância do Kernel do Semantic Kernel criada com sucesso.");
        }
        catch (Exception ex)
        {
            kernelLogger.LogError(ex, "Erro ao construir o Kernel do Semantic Kernel.");
            Console.WriteLine($"Erro crítico ao construir o Kernel: {ex.Message}. O programa será encerrado.");
            return;
        }

        // Registrar a instância do Kernel como Singleton para que a mesma instância seja usada em toda a aplicação.
        services.AddSingleton<Kernel>(kernelInstance);
        kernelLogger.LogInformation("Kernel registrado como serviço singleton no ServiceCollection.");

        // Construir o ServiceProvider após todos os serviços terem sido registrados.
        var serviceProvider = services.BuildServiceProvider();

        // 4. Inicialização do Agente principal (OrderVerifierAgent) a partir do ServiceProvider
        Console.WriteLine("Inicializando o OrderVerifierAgent via DI...");
        OrderVerifierAgent agent;
        try
        {
            // Obter a instância do OrderVerifierAgent; a DI cuidará de injetar todas as suas dependências.
            agent = serviceProvider.GetRequiredService<OrderVerifierAgent>();
            Console.WriteLine("OrderVerifierAgent inicializado com sucesso.");
        }
        catch (Exception ex)
        {
            // Se houver um problema ao resolver o OrderVerifierAgent ou suas dependências.
            var programLogger = serviceProvider.GetRequiredService<ILogger<Program>>(); // Obter um logger para Program
            programLogger.LogError(ex, "Erro ao inicializar o OrderVerifierAgent a partir do ServiceProvider. Verifique o registro das dependências.");
            Console.WriteLine($"Erro fatal ao inicializar o agente: {ex.Message}");
            return;
        }

        // --- Início dos Casos de Teste Automatizados ---
        Console.WriteLine("\n--- Teste de Registro de Pedido em Lote ---");
        string batchOrderUserMessage = "Olá, gostaria de registrar um pedido em lote para o cliente 'Empresa X' com os seguintes itens: um teclado, um mouse e dois monitores.";
        Console.WriteLine($"Usuário: {batchOrderUserMessage}");
        string batchOrderResponse = await agent.ProcessRequestAsync(batchOrderUserMessage);
        Console.WriteLine($"Agente: {batchOrderResponse}");

        Console.WriteLine("\n--- Novos Casos de Teste Específicos ---");

        string testMsg1 = $"Qual o status do pedido {TestOrderIdVipProcessing}?";
        Console.WriteLine($"\nUsuário (Teste Priorização VIP - Pedido {TestOrderIdVipProcessing}): {testMsg1}");
        string response1 = await agent.ProcessRequestAsync(testMsg1);
        Console.WriteLine($"Agente: {response1}");

        string testMsg2 = $"Verifique o pedido {TestOrderIdRegularProcessing}.";
        Console.WriteLine($"\nUsuário (Teste Processando Normal - Pedido {TestOrderIdRegularProcessing}): {testMsg2}");
        string response2 = await agent.ProcessRequestAsync(testMsg2);
        Console.WriteLine($"Agente: {response2}");

        string testMsg3 = $"Status do pedido {TestOrderIdNotFound}, por favor.";
        Console.WriteLine($"\nUsuário (Teste Pedido Não Encontrado - Pedido {TestOrderIdNotFound}): {testMsg3}");
        string response3 = await agent.ProcessRequestAsync(testMsg3);
        Console.WriteLine($"Agente: {response3}");

        string testMsg4 = $"Como está o pedido {TestOrderIdShipped}?";
        Console.WriteLine($"\nUsuário (Teste Status Enviado - Pedido {TestOrderIdShipped}): {testMsg4}");
        string response4 = await agent.ProcessRequestAsync(testMsg4);
        Console.WriteLine($"Agente: {response4}");

        string testMsg5 = $"Qual é o status do pedido {OrderStatusTool.ORDER_ID_FOR_RETRY_TEST}?";
        Console.WriteLine($"\nUsuário (Teste Retry - Pedido {OrderStatusTool.ORDER_ID_FOR_RETRY_TEST}): {testMsg5}");
        Console.WriteLine($"(Observação: Espera-se que o OrderStatusTool simule falhas transitórias para este pedido antes de um eventual sucesso.)");
        string response5 = await agent.ProcessRequestAsync(testMsg5);
        Console.WriteLine($"Agente: {response5}");

        Console.WriteLine("\n--- Fim dos Novos Casos de Teste Específicos ---");

        // 5. Loop de Interação com o Usuário para testes manuais
        Console.WriteLine("\nAgente pronto para interação manual. Digite sua pergunta (ou 'sair' para encerrar):");

        while (true)
        {
            Console.Write("Você: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "sair")
            {
                break;
            }
            string response = await agent.ProcessRequestAsync(input);
            Console.WriteLine($"Agente: {response}");
        }

        // 6. Mensagem de Encerramento e Dispose do ServiceProvider
        Console.WriteLine("Chat encerrado.");

        // Dispose do ServiceProvider para liberar recursos, se ele implementar IDisposable.
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
