// Install the .NET library via NuGet: dotnet add package Azure.AI.OpenAI --prerelease
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using Roachagram.API.Models;
using static System.Environment;

namespace Roachagram.API.BL
{
    public class AIBL
    {
        // Cache the JsonSerializerOptions instance
        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new() { WriteIndented = true };
        private readonly IConfiguration _configuration;

        public AIBL(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task<string> GetAIResult(AnagramResult anagramResult)
        {
            // Retrieve the OpenAI endpoint from environment variables
            var endpoint = _configuration["roach-machine-ai-foundry-open-ai-endpoint"];
            var key = _configuration["roach-machine-ai-foundry-key"];

            AzureKeyCredential credential = new(key);

            // Initialize the AzureOpenAIClient
            AzureOpenAIClient azureClient = new(new Uri(endpoint), credential);

            // Initialize the ChatClient with the specified deployment name
            ChatClient chatClient = azureClient.GetChatClient("gpt-4.1-mini-anagram");

            // List of messages to send
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"You are a fun anagram reviewer. You refer to yourself as ""Roachagram"".
I will provide the input as well as the anagram words in JSON.
You analyze the anagram in a casual way and provide a list
with explanation for rare words. In the ending summary, pick your favorite few."),
                new UserChatMessage($"{JsonSerializer.Serialize(anagramResult)}") };

            // Create chat completion options

            var options = new ChatCompletionOptions
            {
                Temperature = (float)0.7,
                MaxOutputTokenCount = 13107,
                TopP = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0                
            };

            try
            {

                // Create the chat completion request
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

                // Print the response
                if (completion != null)
                {
                    return JsonSerializer.Serialize(completion.Content.FirstOrDefault().Text, CachedJsonSerializerOptions);
                }
                else
                {
                    return "No response from AI service.";
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }
    }
}
