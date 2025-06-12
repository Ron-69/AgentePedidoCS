# AgentePedidoCS - Order Verifier Agent

An AI agent built with Microsoft Semantic Kernel to verify order statuses using Azure OpenAI.

## Core Functionality

The `AgentePedidoCS` project implements an intelligent agent (`OrderVerifierAgent`) that processes user inquiries about order statuses. Key features include:

-   **`AgentePedidoCS.Agent/OrderVerifierAgent.cs`**: Handles the primary interaction logic with the user and coordinates with the language model and tools.
-   **`AgentePedidoCS.Agent/Tools/OrderStatusTool.cs`**: A Semantic Kernel tool responsible for fetching order information. Currently, it uses an in-memory dictionary to simulate a database of orders.
-   **Azure OpenAI Integration**: Leverages Azure OpenAI's language models (e.g., GPT-3.5-Turbo, GPT-4) for natural language understanding and generating coherent responses. The agent is instructed via a system prompt on how to behave and when to use available tools.

## Project Structure

The repository is organized as follows:

-   **`AgentePedidoCS.Agent/`**: Contains the core agent logic.
    -   `OrderVerifierAgent.cs`: The main class defining the agent's behavior.
    -   `Tools/OrderStatusTool.cs`: The tool for checking order statuses.
-   **`AgentePedidoCS.Tester/`**: A console application for interacting with and testing the agent.
    -   `Program.cs`: The entry point for the tester application.
    -   `appsettings.json`: (and `appsettings.Development.json`) for configuration.
-   **`AgentePedidoCS.sln`**: The Visual Studio solution file for the project.
-   **`.gitignore`**: Specifies files and directories that Git should ignore.
-   **`README.md`**: This file, providing an overview and instructions for the project.

## Setup and Configuration

To run this project, you need to have access to Azure OpenAI services.

### Prerequisites

1.  **Azure Subscription**: You'll need an active Azure subscription.
2.  **Azure OpenAI Resource**: Create an Azure OpenAI resource in your subscription.
3.  **Deployed Model**: Deploy a chat model (e.g., `gpt-35-turbo`, `gpt-4`, `phi-3`) within your Azure OpenAI resource. Note the deployment name.

### Configuration Steps

The agent requires credentials and endpoint information for your Azure OpenAI resource. These are loaded via `Microsoft.Extensions.Configuration` from:

1.  `appsettings.json` (typically for production or shared settings)
2.  `appsettings.Development.json` (for local development, overrides `appsettings.json`)
3.  Environment variables (can override file settings)

Create an `appsettings.json` file in the `AgentePedidoCS.Tester` directory with the following structure, replacing the placeholder values with your actual Azure OpenAI details:

```json
{
  "AzureOpenAI": {
    "Endpoint": "YOUR_AZURE_OPENAI_ENDPOINT",
    "ApiKey": "YOUR_AZURE_OPENAI_API_KEY",
    "DeploymentName": "YOUR_AZURE_OPENAI_DEPLOYMENT_NAME"
  }
}
```

-   `YOUR_AZURE_OPENAI_ENDPOINT`: The endpoint URL for your Azure OpenAI resource (e.g., `https://your-resource-name.openai.azure.com/`).
-   `YOUR_AZURE_OPENAI_API_KEY`: One of the API keys for your Azure OpenAI resource.
-   `YOUR_AZURE_OPENAI_DEPLOYMENT_NAME`: The name you gave to your model deployment in Azure OpenAI Studio.

## How to Run the Tester Application

1.  **Configure `appsettings.json`**: Ensure you have created and configured the `appsettings.json` file in the `AgentePedidoCS.Tester` directory as described above.
2.  **Build the Solution**: Open `AgentePedidoCS.sln` in Visual Studio or use the .NET CLI to build the solution (`dotnet build AgentePedidoCS.sln`).
3.  **Run the Tester**:
    *   **Using Visual Studio**: Set `AgentePedidoCS.Tester` as the startup project and run it.
    *   **Using .NET CLI**: Navigate to the `AgentePedidoCS.Tester` directory and run `dotnet run`.

Once the application starts, it will initialize the `OrderVerifierAgent`. You can then type your questions about order statuses.

**Example Interaction:**

```
Inicializando o OrderVerifierAgent...
Agente pronto. Digite sua pergunta sobre o status do pedido (ou 'sair' para encerrar):
Você: Qual é o status do pedido 12345?
Agente: O pedido 12345 está com status: Processando (Laptop Gamer).
Você: E o pedido 67890?
Agente: O pedido 67890 está com status: Enviado (Monitor Curvo).
Você: Existe um pedido 00000?
Agente: Pedido 00000 não encontrado em nossos registros.
Você: sair
Chat encerrado.
```

## Tooling and Dependencies

This project relies on several key libraries and technologies:

-   **Microsoft Semantic Kernel**: For building the AI agent, managing prompts, and integrating tools.
-   **Microsoft.Extensions.Configuration**: For loading application settings.
-   **Microsoft.Extensions.Logging**: For application logging.
-   **Azure OpenAI SDK**: For interacting with the Azure OpenAI language models.

## Potential Future Enhancements

-   **Real Database Integration**: Replace the in-memory `OrderStatusTool` with one that connects to a real database (e.g., SQL Server, Cosmos DB) to fetch order statuses.
-   **Expanded Toolset**: Add more tools to the agent to handle a wider range of order-related queries (e.g., order modification, cancellation, tracking details).
-   **Error Handling and Resilience**: Implement more robust error handling and retry mechanisms.
-   **User Authentication/Authorization**: For scenarios requiring secure access to order information.
-   **Deployment**: Package and deploy the agent as a web service or a bot.

This `README.md` provides a comprehensive guide to understanding, setting up, and running the `AgentePedidoCS` project.
