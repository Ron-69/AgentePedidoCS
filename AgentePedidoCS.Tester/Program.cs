using System;
using System.Threading.Tasks;
using AgentePedidoCS.Agent;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Configuração da Aplicação
        // Cria um builder de configuração para carregar settings de várias fontes.
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory) // Define o diretório base para procurar arquivos de configuração
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Carrega appsettings.json (opcional, recarrega se mudar)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // Carrega appsettings.Development.json (para dev local)
            .AddEnvironmentVariables(); // Permite que variáveis de ambiente sobrescrevam configurações

        IConfiguration config = builder.Build(); // Constrói a configuração final

        // 2. Inicialização do Agente
        Console.WriteLine("Inicializando o OrderVerifierAgent...");
        OrderVerifierAgent agent;
        try
        {
            // Tenta criar uma instância do seu agente, passando a configuração.
            // Isso irá puxar as chaves do Azure OpenAI de appsettings.json ou variáveis de ambiente.
            agent = new OrderVerifierAgent(config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inicializar o agente: {ex.Message}");
            Console.WriteLine("Verifique se suas configurações do Azure OpenAI (Endpoint, ApiKey, DeploymentName) estão corretas em appsettings.json, appsettings.Development.json ou variáveis de ambiente.");
            return; // Sai do programa se o agente não puder ser inicializado
        }

        // --- Teste de Registro de Pedido em Lote ---
        Console.WriteLine("\n--- Teste de Registro de Pedido em Lote ---");
        string batchOrderUserMessage = "Olá, gostaria de registrar um pedido em lote para o cliente 'Empresa X' com os seguintes itens: um teclado, um mouse e dois monitores.";
        Console.WriteLine($"Usuário: {batchOrderUserMessage}");
        string batchOrderResponse = await agent.ProcessRequestAsync(batchOrderUserMessage);
        Console.WriteLine($"Agente: {batchOrderResponse}");
        // --- Fim do Teste de Registro de Pedido em Lote ---

        // 3. Loop de Interação com o Usuário
        Console.WriteLine("\nAgente pronto. Digite sua pergunta sobre o status do pedido (ou 'sair' para encerrar):"); // Added \n for better spacing

        while (true) // Loop infinito para manter o chat ativo
        {
            Console.Write("Você: "); // Prompt para o usuário
            string? input = Console.ReadLine(); // Lê a entrada do usuário

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "sair")
            {
                break; // Sai do loop se o usuário digitar "sair" ou entrada vazia
            }

            // Chama o método ProcessRequestAsync do seu agente e aguarda a resposta
            string response = await agent.ProcessRequestAsync(input);
            Console.WriteLine($"Agente: {response}"); // Exibe a resposta do agente
        }

        // 4. Mensagem de Encerramento
        Console.WriteLine("Chat encerrado.");
    }
}
