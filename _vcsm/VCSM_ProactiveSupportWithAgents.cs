using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernelApp._vcsm
{
    public class VCSM_ProactiveSupportWithAgents
    {
        private const string TeamsAgentName = "TeamsAgent";
        private const string TeamsAgentInstructions =
            """
            You are a proactive support agent specialized in Microsoft Teams. 
            Your task is to analyze Teams usage patterns such as frequent disconnections and crashes. 
            The goal is to determine if the Microsoft Teams system state is optimal.
            If the system is not optimal, suggest solutions that will restore the system back to an optimal state.
            Once a solution is provided, the MasterAgent will decide if the solution is sufficient to restore the system to an optimal state.
            Keep response concise to 1 paragraph
            """;


        private const string AzureAgentName = "AzureAgent";
        private const string AzureAgentInstructions =
            """
            You are a proactive support agent specialized in Azure Kubernetes Service (AKS). 
            Your task is to analyze AKS usage patterns such as frequent disconnections and crashes. 
            The goal is to determine if the Azure Kubernetes Service system state is optimal.
            If the system is not optimal, suggest solutions that will restore the system back to an optimal state.
            Once a solution is provided, the MasterAgent will decide if the solution is sufficient to restore the system to an optimal state.
            Keep response concise to 1 paragraph
            """;

        private const string MasterAgentName = "MasterAgent";
        private const string MasterAgentInstructions =
            $$$"""
            You are the master agent. Your task is to evaluate the solutions provided by the {{{TeamsAgentName}}}. 
            Determine whether the provided solution will restore the system to an optimal state. 
            If the system can be restored based on the solution, respond with SUCCESS, indicating that the system will return to an optimal state.
            If the solution is insufficient, ask for further details or suggest improvements.
            You're laser focused on the goal at hand.
            Don't waste time with chit chat.
            Keep response concise to 1 paragraph
            """;

        static IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        private string DeploymentName = config["AzureOpenAI:DeploymentName"];
        private string Endpoint = config["AzureOpenAI:Endpoint"];
        private string ApiKey = config["AzureOpenAI:ApiKey"];

        public async Task RunAsync()
        {
            // Build the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            IKernelBuilder kernelBuilder = builder.AddAzureOpenAIChatCompletion(
                deploymentName: DeploymentName,
                endpoint: Endpoint,
                apiKey: ApiKey
            );

            // Define specialized agents for Teams
            ChatCompletionAgent teamsAgent = new()
            {
                Instructions = TeamsAgentInstructions,
                Name = TeamsAgentName,
                Kernel = kernelBuilder.Build(), 
            };

            // Define specialized agents for Azure
            ChatCompletionAgent azureAgent = new()
            {
                Instructions = AzureAgentInstructions,
                Name = AzureAgentName,
                Kernel = kernelBuilder.Build(),
            };

            // Define the master agent to orchestrate between Teams and Azure agents
            ChatCompletionAgent masterAgent = new()
            {
                Instructions = MasterAgentInstructions,
                Name = MasterAgentName,
                Kernel = kernelBuilder.Build(),
            };

            // Create Termination and Selection Functions using prompts
            KernelFunction terminationFunction = KernelFunctionFactory.CreateFromPrompt(
                """               
                Determine if the provided solution will restore the system to an optimal state. 
                If the solution will work and restore the system, respond with a single word: SUCCESS.
                If the solution is not sufficient, suggest improvements or ask for additional information.
                
                History: {{$history}}
                """);

            KernelFunction selectionFunction = KernelFunctionFactory.CreateFromPrompt(
                $$$"""
                Determine which participant takes the next turn in a conversation based on the the most recent participant.
                State only the name of the participant to take the next turn.
                No participant should take more than one turn in a row.
                
                Choose only from these participants:
                - {{{MasterAgentName}}}    
                - {{{TeamsAgentName}}}                
                - {{{AzureAgentName}}}   
                
                Always follow these rules when selecting the next participant:
                - After {{{TeamsAgentName}}}, it is {{{MasterAgentName}}}'s turn.
                - After {{{MasterAgentName}}}, it is {{{TeamsAgentName}}}'s turn.
                - After { { { AzureAgentName} } }, it is { { { MasterAgentName } } }'s turn.
                - After { { { MasterAgentName} } }, it is { { { AzureAgentName } } }'s turn.
               
                History: {{$history}}
                """);

 



            // Create a chat for agent interaction. This is Teams Agent Chat Group
            AgentGroupChat teamsChat = new(teamsAgent, masterAgent)
            {
                ExecutionSettings = new()
                {
                    // KernelFunctionTerminationStrategy will terminate when the MasterAgent has given its approval
                    //TerminationStrategy = new KernelFunctionTerminationStrategy(terminationFunction, kernelBuilder.Build())
                    TerminationStrategy = new ApprovalTerminationStrategy()
                    {
                        // Only the Master Agent may approve.
                        Agents = [masterAgent],
                        // Result parser to check if the result contains "SUCCESS"
                        //ResultParser = (result) => result.GetValue<string>()?.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ?? false,
                        // The prompt variable name for the history argument.
                        //HistoryVariableName = "history",
                        // Limit total number of turns to prevent endless loops
                        MaximumIterations = 10,
                    },
                    // KernelFunctionSelectionStrategy selects agents based on a prompt function. Option2 = SequentialSelectionStrategy
                    //SelectionStrategy = new SequentialSelectionStrategy(selectionFunction, kernelBuilder.Build())
                    SelectionStrategy = new SequentialSelectionStrategy()
                    {
                        // Start with the Teams agent.
                        InitialAgent = teamsAgent,
                        //// The result parser to decide the next agent based on prompt
                        //ResultParser = (result) => result.GetValue<string>() ?? TeamsAgentName,
                        //// The prompt variable name for the agents argument.
                        //AgentsVariableName = "agents",
                        //// The prompt variable name for the history argument.
                        //HistoryVariableName = "history",
                    },
                }
            };

            // Azure Agent Chat Group
            AgentGroupChat azureChat = new(azureAgent, masterAgent)
            {
                ExecutionSettings = new()
                {
                    // KernelFunctionTerminationStrategy will terminate when the MasterAgent has given its approval
                    //TerminationStrategy = new KernelFunctionTerminationStrategy(terminationFunction, kernelBuilder.Build())
                    TerminationStrategy = new ApprovalTerminationStrategy()
                    {
                        // Only the Master Agent may approve.
                        Agents = [masterAgent],
                        // Result parser to check if the result contains "SUCCESS"
                        //ResultParser = (result) => result.GetValue<string>()?.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ?? false,
                        // The prompt variable name for the history argument.
                        //HistoryVariableName = "history",
                        // Limit total number of turns to prevent endless loops
                        MaximumIterations = 10,
                    },
                    // KernelFunctionSelectionStrategy selects agents based on a prompt function. Option2 = SequentialSelectionStrategy
                    //SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernelBuilder.Build())
                    SelectionStrategy = new SequentialSelectionStrategy()
                    {
                        // Start with the Teams agent.
                        InitialAgent = azureAgent,
                        //// The result parser to decide the next agent based on prompt
                        //ResultParser = (result) => result.GetValue<string>() ?? TeamsAgentName,
                        //// The prompt variable name for the agents argument.
                        //AgentsVariableName = "agents",
                        //// The prompt variable name for the history argument.
                        //HistoryVariableName = "history",
                    },
                }
            };

            // Invoke chat and display messages
            string teamsIssue = "Teams issue: Critical issue detected. Frequent disconnections during video calls.";
            string aksIssue = "AKS issue: Frequent node resource exhaustion is causing application performance degradation and intermittent downtime";
            ChatMessageContent teamsInput = new(AuthorRole.User, teamsIssue);
            ChatMessageContent aksInput = new(AuthorRole.User, aksIssue);

            Console.WriteLine(teamsIssue);

            teamsChat.AddChatMessage(teamsInput);
            azureChat.AddChatMessage(aksInput);

            // Use below to aggregate the whole chat contents into a task
            //Task[] tasks = [InvokeAgent(teamsChat), InvokeAgent(azureChat)];
            //await Task.WhenAll(tasks);

            Task<ChatMessageContent[]>[] tasks =
                //[teamsChat.InvokeAsync().ToArrayAsync().AsTask()];
                [teamsChat.InvokeAsync().ToArrayAsync().AsTask(), azureChat.InvokeAsync().ToArrayAsync().AsTask()];
            var histories = await Task.WhenAll<ChatMessageContent[]>(tasks);

            foreach (ChatMessageContent response in histories[0])
            {
                this.WriteAgentChatMessage(response);
            }
            foreach (ChatMessageContent response in histories[1])
            {
                this.WriteAgentChatMessage(response);
            }

            // Get the last message from histories which is the approved version
            Console.WriteLine("\n========================== FINAL APPROVED SOLUTION ==========================");
            this.WriteAgentChatMessage(histories[0][histories.Length - 2]);
            this.WriteAgentChatMessage(histories[1][histories.Length - 2]);

            //Console.WriteLine($"\n[IS COMPLETED: {teamsChat.IsComplete}]");
            //Console.WriteLine($"\n[IS COMPLETED: {azureChat.IsComplete}]");

        }

        private sealed class ApprovalTerminationStrategy : TerminationStrategy
        {
            // Terminate when the final message contains the term "approve"
            protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
                => Task.FromResult(history[history.Count - 1].Content?.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private async Task InvokeAgent(AgentGroupChat chat)
        {
            await foreach (ChatMessageContent response in chat.InvokeAsync())
            {
                this.WriteAgentChatMessage(response);
            }

        }

        protected void WriteAgentChatMessage(ChatMessageContent message)
        {
            // Include ChatMessageContent.AuthorName in output, if present.
            string authorExpression = message.Role == AuthorRole.User ? string.Empty : $" - {message.AuthorName ?? "*"}";
            // Include TextContent (via ChatMessageContent.Content), if present.
            string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;

            bool isCode = message.Metadata?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false;
            string codeMarker = isCode ? "\n  [CODE]\n" : " ";
            Console.WriteLine($"\n# {message.Role}{authorExpression}:{codeMarker}{contentExpression}");

            // Provide visibility for inner content (that isn't TextContent).
            foreach (KernelContent item in message.Items)
            {
                if (item is AnnotationContent annotation)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {annotation.Quote}: File #{annotation.FileId}");
                }
                else if (item is FileReferenceContent fileReference)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] File #{fileReference.FileId}");
                }
                else if (item is ImageContent image)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
                }
                else if (item is FunctionCallContent functionCall)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {functionCall.Id}");
                }
                else if (item is FunctionResultContent functionResult)
                {
                    Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId}");
                }
            }

            if (message.Role != AuthorRole.User)
                Console.WriteLine($"===================================================================================");
        }



    }
}
