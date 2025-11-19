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
            var endpoint = _configuration["roach-machine-ai-foundry-open-ai-endpoint"];
            var key = _configuration["roach-machine-ai-foundry-key"];

            AzureKeyCredential credential = new(key);
            AzureOpenAIClient azureClient = new(new Uri(endpoint), credential);
            ChatClient chatClient = azureClient.GetChatClient("gpt-4.1-mini-anagram");

            // Create a simple array string for anagrams
            string anagramsArrayString = "[" + string.Join(",", anagramResult.Anagrams.Select(a => $"\"{a}\"")) + "]";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"Call yourself Roachagram, a friendly and witty anagram reviewer. 
                                        I’ll give you the original phrase and a list of anagram candidates. 
                                        Your job:
                                        1. React casually and make it fun.
                                        2. For each anagram, give a short comment (why it’s cool, odd, or funny).
                                        3. If a word seems rare or unusual, briefly explain it.
                                        4. End with a quick summary: pick your top 2–3 favorites and say why."),
                new UserChatMessage($"Input: \"{anagramResult.Input}\"\nAnagrams: {anagramsArrayString}")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.5f,
                MaxOutputTokenCount = 700,
                TopP = 0.95f,
                FrequencyPenalty = 0,
                PresencePenalty = 0
            };

            try
            {
                ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

                if (completion != null)
                {
                    return completion.Content.FirstOrDefault().Text;
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
