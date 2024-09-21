using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System;


namespace SemanticKernelApp._vcsm
{
    public class VCSM_PersonalizedOnboarding
    {
        public async Task RunAsync()
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            // Build the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            IKernelBuilder kernelBuilder = builder.AddAzureOpenAIChatCompletion(
                deploymentName: config["AzureOpenAI:DeploymentName"] ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName not found in configuration."),
                endpoint: config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not found in configuration."),
                apiKey : config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not found in configuration.")
            );

            ChatCompletionAgent agent = new()
            {
                Instructions = "Provide personalized onboarding assistance for Microsoft products.",
                Name = "OnboardingAgent",
                Kernel = kernelBuilder.Build(),
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions }),
            };

            // Create and add the OnboardingPlugin to the kernel
            KernelPlugin onboardingPlugin = KernelPluginFactory.CreateFromType<TeamsOnboardingPlugin>();
            agent.Kernel.Plugins.Add(onboardingPlugin);


            // Simulate the onboarding interaction
            await InvokeAgentAsync("Hello. Can you help me get started on my Microsoft Teams licensing?");
            await InvokeAgentAsync("What are the key features of Microsoft Teams I should know about?");
            await InvokeAgentAsync("Any tips for better productivity?");
            await InvokeAgentAsync("Thanks for the help!");

            // Local function to invoke agent and display the conversation messages.
            async Task InvokeAgentAsync(string input)
            {
                // Create the chat history to capture the agent interaction
                ChatHistory chat = [];

                ChatMessageContent message = new(AuthorRole.User, input);
                chat.Add(message);
                this.WriteAgentChatMessage(message);

                await foreach (ChatMessageContent response in agent.InvokeAsync(chat))
                {
                    chat.Add(response);
                    this.WriteAgentChatMessage(response);
                }
            }

        }

        public sealed class TeamsOnboardingPlugin
        {
            [KernelFunction, Description("Welcomes new users to Microsoft Teams and starts the onboarding process.")]
            public string WelcomeUser()
            {
                return "Welcome to Microsoft Teams! I'm here to help you get started and make the most out of your Teams experience.";
            }

            [KernelFunction, Description("Guides user through the initial setup of Microsoft Teams")]
            public string GuideInitialSetup() =>
                """
                Here's how to set up Microsoft Teams:
                1. Set up your profile: Add a profile picture and configure your contact details.
                2. Adjust notification settings: Ensure you're notified about the important messages and meetings.
                3. Sync your calendar: Link your Outlook calendar to schedule and join meetings.
                4. Join or create teams: Start collaborating by joining your organization's teams or creating new ones.
                """;

            [KernelFunction, Description("Guides the user through setting up their Microsoft Teams profile.")]
            public string GuideProfileSetup()
            {
                return @"
                    Let's set up your Teams profile:
                    1. **Add a Profile Picture**: Click on your initials or photo in the top right corner and select 'Change picture'.
                    2. **Update Your Status**: Set your availability status so colleagues know when you're reachable.
                    3. **Configure Notifications**: Go to Settings > Notifications to customize how and when you receive alerts.
                    ";
            }

            [KernelFunction, Description("Explains essential features of Microsoft Teams")]
            public string ExplainFeatures() =>
                """
                Microsoft Teams key features:
                1. **Chat**: Instant messaging with individuals or groups. You can also share files and emojis.
                2. **Meetings**: Schedule, join, and conduct virtual meetings with video and audio conferencing.
                3. **Channels**: Organize conversations into channels within a team for focused discussions.
                4. **File Sharing**: Seamlessly collaborate on documents using integrated Microsoft 365 apps like Word, Excel, and PowerPoint.
                5. **Integrations**: Use built-in apps like OneNote, Planner, and SharePoint or add third-party apps to enhance productivity.
                """;

            [KernelFunction, Description("Provides tips for using Microsoft Teams more efficiently")]
            public string ProvideTips() =>
                """
                Here are some tips to make the most out of Microsoft Teams:
                1. Use **@mentions** to grab someone’s attention in a busy conversation.
                2. Organize your teams and channels by project or department for quick access.
                3. Pin important channels or chats for easy access.
                4. Set your status to **Do Not Disturb** to focus on work without interruptions.
                5. Use the **background blur** or custom background feature in video calls to maintain privacy.
                6. Install the **Teams mobile app** to stay connected on the go.
                """;

            [KernelFunction, Description("Provides a tutorial for scheduling a meeting in Microsoft Teams")]
            public string ScheduleMeeting() =>
                """
                To schedule a meeting in Microsoft Teams:
                1. Go to the **Calendar** tab in the left sidebar.
                2. Click **New Meeting** in the top right corner.
                3. Add a title, invite attendees, set the date and time, and add any necessary details.
                4. Click **Save** to send out the invites and schedule the meeting.
                """;

            [KernelFunction, Description("Explains how to share files and collaborate in Microsoft Teams")]
            public string FileSharingAndCollaboration() =>
                """
                In Microsoft Teams, you can easily share files during a chat or in a channel:
                1. In the chat or channel, click the **Attach** icon (paperclip).
                2. Upload a file from your computer or select one from OneDrive.
                3. The file will appear in the conversation, and your team can edit it in real time using Office apps like Word, Excel, or PowerPoint.
                """;

            [KernelFunction, Description("Answers frequently asked questions about Microsoft Teams.")]
            public string AnswerFAQs([Description("The user's question")] string userQuestion)
            {
                // In a real implementation, you might query a knowledge base.
                if (userQuestion.Contains("set status", StringComparison.OrdinalIgnoreCase))
                {
                    return "To set your status, click on your profile picture and select a status from the dropdown menu.";
                }
                else if (userQuestion.Contains("schedule meeting", StringComparison.OrdinalIgnoreCase))
                {
                    return "To schedule a meeting, go to the Calendar tab and click 'New Meeting'.";
                }
                else
                {
                    return "I'm sorry, I don't have an answer for that question right now. Please check the Teams help center for more information.";
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
