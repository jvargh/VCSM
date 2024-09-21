using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI.VectorStores;
using OpenAI.Files;
using Resources;
using Microsoft.Extensions.Configuration;

namespace SemanticKernelApp._virtualcsm
{
    public class VCSM_TailoredRecommendations
    {
        static IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        private string DeploymentName = config["AzureOpenAI:DeploymentName"];
        private string Endpoint = config["AzureOpenAI:Endpoint"];
        private string ApiKey = config["AzureOpenAI:ApiKey"];

        // Define OpenAIClientProvider based on usage configuration
        protected OpenAIClientProvider GetClientProvider() =>
            OpenAIClientProvider.ForAzureOpenAI(ApiKey, new Uri(Endpoint));

        // Define metadata for file-based data search
        protected const string RecommendationMetadataKey = "recommendations";
        protected static readonly IReadOnlyDictionary<string, string> RecommendationMetadata =
            new Dictionary<string, string>
            {
            { RecommendationMetadataKey, bool.TrueString }
            };

        public async Task RunAsync()
        {
            // Build the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            IKernelBuilder kernelBuilder = builder.AddAzureOpenAIChatCompletion(
                deploymentName: DeploymentName,
                endpoint: Endpoint,
                apiKey: ApiKey
            );

            // Define the agent
            OpenAIClientProvider provider = GetClientProvider();
            OpenAIAssistantAgent agent = await OpenAIAssistantAgent.CreateAsync(
                kernel: kernelBuilder.Build(),
                clientProvider: provider,
                new(DeploymentName)
                {
                    EnableFileSearch = true,
                    Metadata = RecommendationMetadata,
                });

            // Upload file - Onboarding Guide PDF
            FileClient fileClient = provider.Client.GetFileClient();
            await using Stream stream = EmbeddedResource.ReadStream("teamsrecommendations.pdf")!;
            OpenAIFileInfo fileInfo = await fileClient.UploadFileAsync(stream, "teamsrecommendations.pdf", FileUploadPurpose.Assistants);

            // Create a vector-store for the uploaded file data
            VectorStoreClient vectorStoreClient = provider.Client.GetVectorStoreClient();
            VectorStore vectorStore =
                await vectorStoreClient.CreateVectorStoreAsync(
                    new VectorStoreCreationOptions()
                    {
                        FileIds = [fileInfo.Id],
                        Metadata = { { RecommendationMetadataKey, bool.TrueString } }
                    });

            // Create a thread associated with the vector-store for the agent conversation.
            string threadId =
                await agent.CreateThreadAsync(
                    new OpenAIThreadCreationOptions
                    {
                        VectorStoreId = vectorStore.Id,
                        Metadata = RecommendationMetadata,
                    });

            // Respond to user input
            try
            {
                //await InvokeAgentAsync("Summarize my current usage of Microsoft Teams?");
                //await InvokeAgentAsync("What features should I focus on based on my usage statistics?");
                await InvokeAgentAsync("Provide some personalized recommendations based on my existing Teams usage?");
            }
            finally
            {
                await agent.DeleteThreadAsync(threadId);
                await agent.DeleteAsync(CancellationToken.None);
                await vectorStoreClient.DeleteVectorStoreAsync(vectorStore);
                await fileClient.DeleteFileAsync(fileInfo.Id);
            }

            // Local function to invoke agent and display the conversation messages.
            async Task InvokeAgentAsync(string input)
            {
                ChatMessageContent message = new(AuthorRole.User, input);
                await agent.AddChatMessageAsync(threadId, message);
                this.WriteAgentChatMessage(message);

                await foreach (ChatMessageContent response in agent.InvokeAsync(threadId))
                {
                    this.WriteAgentChatMessage(response);
                }
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
