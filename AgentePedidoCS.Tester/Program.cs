using System;
using System.Threading.Tasks;
using AgentePedidoCS.Agent;
using AgentePedidoCS.Agent.Tools; // Adicionado para ORDER_ID_FOR_RETRY_TEST
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Configuração da Aplicação
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        IConfiguration config = builder.Build();

        // 2. Inicialização do Agente
        Console.WriteLine("Inicializando o OrderVerifierAgent...");
        OrderVerifierAgent agent;
        try
        {
            agent = new OrderVerifierAgent(config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar o agente: {ex.Message}");
            Console.WriteLine("Verifique se suas configurações do Azure OpenAI (Endpoint, ApiKey, DeploymentName) estão corretas em appsettings.json, appsettings.Development.json ou variáveis de ambiente.");
            return;
        }

        // --- Teste de Registro de Pedido em Lote ---
        Console.WriteLine("\n--- Teste de Registro de Pedido em Lote ---");
        string batchOrderUserMessage = "Olá, gostaria de registrar um pedido em lote para o cliente 'Empresa X' com os seguintes itens: um teclado, um mouse e dois monitores.";
        Console.WriteLine($"Usuário: {batchOrderUserMessage}");
        string batchOrderResponse = await agent.ProcessRequestAsync(batchOrderUserMessage);
        Console.WriteLine($"Agente: {batchOrderResponse}");
        // --- Fim do Teste de Registro de Pedido em Lote ---

        // --- Novos Casos de Teste Específicos ---
        Console.WriteLine("\n--- Novos Casos de Teste Específicos ---");

        // Teste 1: Pedido "Processando" e deve ser priorizado (ID: 12345, Item: Laptop Gamer, Cliente: VIP_CUSTOMER)
        string testMsg1 = "Qual o status do pedido 12345?";
        Console.WriteLine($"\nUsuário (Teste Priorização VIP): {testMsg1}");
        string response1 = await agent.ProcessRequestAsync(testMsg1);
        Console.WriteLine($"Agente: {response1}");
        // Expected: Status 'Processando', priorizado, notificação enviada.

        // Teste 2: Pedido "Processando" e NÃO deve ser priorizado (ID: 77777, Item: Webcam HD, Cliente: REGULAR_CUSTOMER)
        string testMsg2 = "Verifique o pedido 77777.";
        Console.WriteLine($"\nUsuário (Teste Processando Normal): {testMsg2}");
        string response2 = await agent.ProcessRequestAsync(testMsg2);
        Console.WriteLine($"Agente: {response2}");
        // Expected: Status 'Processando', não priorizado.

        // Teste 3: Pedido não encontrado (ID: 99999)
        string testMsg3 = "Status do pedido 99999, por favor.";
        Console.WriteLine($"\nUsuário (Teste Pedido Não Encontrado): {testMsg3}");
        string response3 = await agent.ProcessRequestAsync(testMsg3);
        Console.WriteLine($"Agente: {response3}");
        // Expected: Mensagem de fallback para pedido não encontrado.

        // Teste 4: Pedido com status não "Processando" (ID: 67890, Status: Enviado)
        string testMsg4 = "Como está o pedido 67890?";
        Console.WriteLine($"\nUsuário (Teste Status Enviado): {testMsg4}");
        string response4 = await agent.ProcessRequestAsync(testMsg4);
        Console.WriteLine($"Agente: {response4}");
        // Expected: Status 'Enviado', sem menção a priorização.

        // Teste 5: Teste de Lógica de Retry (ID: ORDER_ID_FOR_RETRY_TEST)
        string testMsg5 = $"Qual é o status do pedido {OrderStatusTool.ORDER_ID_FOR_RETRY_TEST}?";
        Console.WriteLine($"\nUsuário (Teste Retry): {testMsg5}");
        Console.WriteLine($"(Esperando falhas simuladas e depois sucesso para {OrderStatusTool.ORDER_ID_FOR_RETRY_TEST})");
        string response5 = await agent.ProcessRequestAsync(testMsg5);
        Console.WriteLine($"Agente: {response5}");
        // Expected: Logs de retry do OrderStatusTool, depois status 'Processando', não priorizado.

        Console.WriteLine("\n--- Fim dos Novos Casos de Teste Específicos ---");
        // --- Fim dos Novos Casos de Teste Específicos ---

        // 3. Loop de Interação com o Usuário
        Console.WriteLine("\nAgente pronto. Digite sua pergunta sobre o status do pedido (ou 'sair' para encerrar):");

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

        // 4. Mensagem de Encerramento
        Console.WriteLine("Chat encerrado.");
    }
}
