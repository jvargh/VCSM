using SemanticKernelApp._vcsm;
using SemanticKernelApp._virtualcsm;

while (true)
{
    // Display choices to the user
    Console.WriteLine("\nPlease choose an option (type 'end' to exit):");
    Console.WriteLine("1. Personalized Onboarding using Semantic Kernel Plugins");    
    Console.WriteLine("2. Proactive Support using Multi-Agents");
    Console.WriteLine("3. Tailored Recommendations using Vector Store");

    // Read user input
    Console.Write("Enter input (1, 2, 3, end to exit): ");
    string userInput = Console.ReadLine();

    // Check if user wants to exit
    if (userInput?.ToLower() == "end")
    {
        Console.WriteLine("Program terminated.");
        break;
    }

    // Process the user input
    switch (userInput)
    {
        case "1":
            Console.WriteLine("\nYou chose: Personalized Onboarding using Semantic Kernel Plugins\n");
            VCSM_PersonalizedOnboarding onboarding = new();
            await onboarding.RunAsync();
            break;
        case "2":
            Console.WriteLine("\nYou chose: Proactive Support using Multi-Agents\n");
            VCSM_ProactiveSupportWithAgents proactiveInsights = new();
            await proactiveInsights.RunAsync();
            break;
        case "3":
            Console.WriteLine("\nYou chose: Tailored Recommendations using Vector Store\n");
            VCSM_TailoredRecommendations tailoredRecommendations = new();
            await tailoredRecommendations.RunAsync();
            break;
        default:
            Console.WriteLine("Invalid choice, please select 1, 2, or 3.");
            break;
    }
}