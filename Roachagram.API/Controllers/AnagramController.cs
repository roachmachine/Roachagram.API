using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Roachagram.API.BL;
using Roachagram.API.Models;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;

namespace Roachagram.API.Controllers
{
    /// <summary>
    /// API Controller for handling anagram-related operations.
    /// </summary>
    /// <author>Michael</author>
    /// <datetime>5/25/2017 7:00 PM</datetime>
    /// <remarks>Provides endpoints for generating anagrams based on user input.</remarks>
    /// <seealso cref="Controller" />
    /// <remarks>
    /// Initializes a new instance of the <see cref="AnagramController"/> class.
    /// </remarks>
    /// <param name="memoryCache">The memory cache for caching dictionary data.</param>
    /// <param name="db">The database context for accessing dictionary data.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="telemetryClient">The telemetry client.</param>
    /// <param name="env">The web host environment.</param>
    [Route("api/[controller]")]
    public class AnagramController(IMemoryCache memoryCache, IConfiguration configuration, TelemetryClient telemetryClient, IWebHostEnvironment env) : Controller
    {
        // Default values and constants
        private const string DefaultInput = "roachmachine";
        private const string BasicDictionaryCacheKey = "BasicEnglishDictionary";
        private const int DefaultMinWordLength = 2;
        private const int DefaultMaxNumWords = 3;
        private const int MaxInputLetters = 20;

        // Dependencies
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly IConfiguration _configuration = configuration;
        private readonly TelemetryClient _telemetryClient = telemetryClient;
        private readonly IWebHostEnvironment _env = env;

        /// <summary>
        /// Endpoint to generate anagrams based on the input string and constraints.
        /// </summary>
        /// <param name="input">The input string to generate anagrams for.</param>
        /// <param name="minwordlength">The minimum length of words in the anagrams. Defaults to 2.</param>
        /// <param name="maxnumwords">The maximum number of words in the anagrams. Defaults to 3.</param>
        /// <returns>A list of anagrams or an error message if the operation fails.</returns>
        [HttpGet]
        public async Task<string> Get([FromQuery] string input, int minwordlength = 2, int maxnumwords = 3)
        {
            try
            {
                #region Validate Input
                if (string.IsNullOrEmpty(input))
                {
                    input = DefaultInput;
                }

                input = input.ToLowerInvariant();

#pragma warning disable SYSLIB1045
                Regex rgx = new("[^a-z]", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045

                input = rgx.Replace(input, "");

                if (input.Length > MaxInputLetters)
                {
                    throw new Exception($"Input greater than {MaxInputLetters} characters");
                }

                if (minwordlength <= 0)
                {
                    minwordlength = DefaultMinWordLength;
                }
                else if (minwordlength > input.Length)
                {
                    minwordlength = input.Length;
                }

                if (maxnumwords <= 0 || maxnumwords > 4)
                {
                    maxnumwords = DefaultMaxNumWords;
                }
                #endregion

                // Dictionary to store cached or fetched dictionary data
                Dictionary<string, string> dictionaryItems;

                // Attempt to retrieve dictionary data from cache
                if (!_memoryCache.TryGetValue(BasicDictionaryCacheKey, out dictionaryItems))
                {
                    dictionaryItems = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    // Determine CSV path from configuration or use default
                    string csvPath = Path.Combine(_env.ContentRootPath, "Files", "default-dictionary.csv");

                    if (!System.IO.File.Exists(csvPath))
                    {
                        throw new FileNotFoundException($"Dictionary CSV not found at path: {csvPath}");
                    }

                    // Read CSV and populate dictionary
                    foreach (var rawLine in System.IO.File.ReadLines(csvPath))
                    {
                        if (string.IsNullOrWhiteSpace(rawLine))
                            continue;

                        // Split into up to 3 parts: id, word, orderedletters
                        var parts = rawLine.Split(new[] { ',' }, 4);
                        if (parts.Length < 2)
                            continue;

                        // column 1 is the Word, column 2 is the ordered letters (if present)
                        string word = parts.Length > 1 ? parts[1].Trim().Trim('"') : string.Empty;
                        string ordered = parts.Length > 2 ? parts[2].Trim().Trim('"') : string.Empty;

                        if (string.IsNullOrEmpty(word))
                            continue;

                        if (!dictionaryItems.ContainsKey(word))
                        {
                            dictionaryItems[word] = ordered;
                        }
                    }

                    // Cache the fetched dictionary data
                    _memoryCache.Set(BasicDictionaryCacheKey, dictionaryItems);
                }

                // Generate anagrams using the business logic layer
                List<string> output = AnagramBL.GetAnagrams(input.Trim(), minwordlength, maxnumwords, dictionaryItems);

                // Remove the original input from the list (ignoring spaces and case)
                output.RemoveAll(anagram => anagram.Replace(" ", "").Equals(input, StringComparison.OrdinalIgnoreCase));

                // Partition outputs
                List<string> finalAnagrams = output.Where(anagram => anagram.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 1).ToList();
                List<string> twoWordAnagrams = output.Where(anagram => anagram.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 2).ToList();
                List<string> threeWordAnagrams = output.Where(anagram => anagram.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 3).ToList();

                var rand = new Random();

                // Fill with two-word anagrams (reverse order) if needed
                if (finalAnagrams.Count < 10 && twoWordAnagrams.Count > 0)
                {
                    int needed = 10 - finalAnagrams.Count;
                    var shuffledTwoWordAnagrams = twoWordAnagrams
                        .OrderBy(x => rand.Next())
                        .Take(needed)
                        .Select(anagram =>
                        {
                            var words = anagram.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length == 2)
                                return $"{words[1]} {words[0]}";
                            return anagram;
                        })
                        .ToList();
                    finalAnagrams.AddRange(shuffledTwoWordAnagrams);
                }

                // Fill with three-word anagrams (reverse order) if still needed
                if (finalAnagrams.Count < 10 && threeWordAnagrams.Count > 0)
                {
                    int needed = 10 - finalAnagrams.Count;
                    var shuffledThreeWordAnagrams = threeWordAnagrams
                        .OrderBy(x => rand.Next())
                        .Take(needed)
                        .Select(anagram =>
                        {
                            var words = anagram.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length == 3)
                                return $"{words[2]} {words[1]} {words[0]}";
                            return anagram;
                        })
                        .ToList();
                    finalAnagrams.AddRange(shuffledThreeWordAnagrams);
                }

                // Randomize word order inside each anagram (single pass)
                finalAnagrams = finalAnagrams
                    .Select(anagram =>
                    {
                        var words = anagram.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        return string.Join(" ", words.OrderBy(_ => rand.Next()));
                    })
                    .ToList();

                var distinctTop = finalAnagrams.Distinct(StringComparer.OrdinalIgnoreCase).Take(10).ToList();

                AnagramResult anagramResult = new()
                {
                    Input = input,
                    Anagrams = distinctTop
                };

                AIBL aIBL = new(_configuration);
                var response = await aIBL.GetAIResult(anagramResult);

                // Trace the request and results for diagnostics
                _telemetryClient.TrackTrace(
                    "AnagramsGenerated",
                    new Dictionary<string, string>
                    {
                        ["AnagramInput"] = (input).ToString(),
                        ["AnagramCount"] = (anagramResult.Anagrams?.Count ?? 0).ToString(),
                        ["AnagramSample"] = string.Join(", ", (anagramResult.Anagrams ?? new List<string>()).Take(10))
                    });

                return response?.Trim('\"') ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"error: {ex.StackTrace}";
            }
        }
    }
}