using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Roachagram.API.BL;
using Roachagram.API.Models;

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
    [Route("api/[controller]")]
    public class AnagramController(IMemoryCache memoryCache, DictionaryDBContext db, IConfiguration configuration) : Controller
    {
        // Default values and constants
        private const string DefaultInput = "roachmachine";
        private const string BasicDictionaryCacheKey = "BasicEnglishDictionary";
        private const int DefaultMinWordLength = 2;
        private const int DefaultMaxNumWords = 3;
        private const int MaxInputLetters = 20;

        // Dependencies
        private readonly IMemoryCache _memoryCache = memoryCache;
        private readonly DictionaryDBContext _db = db;

        /// <summary>
        /// Endpoint to generate anagrams based on the input string and constraints.
        /// </summary>
        /// <param name="input">The input string to generate anagrams for.</param>
        /// <param name="minwordlength">The minimum length of words in the anagrams. Defaults to 2.</param>
        /// <param name="maxnumwords">The maximum number of words in the anagrams. Defaults to 3.</param>
        /// <param name="psuedonymn">Reserved for future use (e.g., pseudonym search).</param>
        /// <returns>A list of anagrams or an error message if the operation fails.</returns>
        /// <exception cref="Exception">Thrown if the input exceeds the maximum allowed length.</exception>
        [HttpGet]
        public async Task<string> Get([FromQuery] string input, int minwordlength = 2, int maxnumwords = 3)
        {
            try
            {
                #region Validate Input
                // Handle null or empty input by using a default value
                if (string.IsNullOrEmpty(input))
                {
                    input = DefaultInput;
                }

                // Normalize input to lowercase and remove non-alphabetic characters
                input = input.ToLower();
                Regex rgx = new Regex("[^a-z]", RegexOptions.Compiled);
                input = rgx.Replace(input, "");

                // Ensure input length does not exceed the maximum allowed
                if (input.Length > MaxInputLetters)
                {
                    throw new Exception("Input greater than 15 characters");
                }


                // Validate and adjust minimum word length
                if (minwordlength <= 0)
                {
                    minwordlength = DefaultMinWordLength;
                }
                else if (minwordlength > input.Length)
                {
                    minwordlength = input.Length;
                }

                // Validate and adjust maximum number of words
                if (maxnumwords <= 0 || maxnumwords > 4)
                {
                    maxnumwords = DefaultMaxNumWords;
                }
                #endregion

                // Dictionary to store cached or fetched dictionary data
                Dictionary<string, string> dictionaryItems = [];

                // Attempt to retrieve dictionary data from cache
                if (!_memoryCache.TryGetValue(BasicDictionaryCacheKey, out dictionaryItems))
                {
                    // Fetch dictionary data from the database if not cached
                    int retryCount = 3;
                    while (retryCount > 0)
                    {
                        try
                        {
                            dictionaryItems = _db.Dictionary
                                .FromSqlRaw("exec get_basic_english_dictionary")
                                .ToDictionary(kvp => kvp.Word, kvp => kvp.Word_ordered_array);
                            break; // Exit loop if successful
                        }
                        catch (Exception ex)
                        {
                            await Task.Delay(30000);

                            retryCount--;
                            if (retryCount == 0)
                            {
                                throw new Exception("Failed to fetch dictionary data after multiple attempts.", ex);
                            }
                        }
                    }

                    // Cache the fetched dictionary data
                    _memoryCache.Set(BasicDictionaryCacheKey, dictionaryItems);
                }

                // Generate anagrams using the business logic layer
                List<string> output = AnagramBL.GetAnagrams(input.Trim(), minwordlength, maxnumwords, dictionaryItems);

                //remove the original input from the list
                output.RemoveAll(anagram => anagram.Replace(" ", "").Equals(input, StringComparison.OrdinalIgnoreCase));

                //output contains a list of multipe word anagrams, grab all 1 word angrams
                List<string> finalAnagrams = [.. output.Where(anagram => anagram.Split(' ').Length == 1)];

                //output contains a list of multipe word anagrams, grab all 2 word angrams
                List<string> twoWordAnagrams = [.. output.Where(anagram => anagram.Split(' ').Length == 2)];

                //output contains a list of multipe word anagrams, grab all 2 word angrams
                List<string> threeWordAnagrams = [.. output.Where(anagram => anagram.Split(' ').Length == 3)];

                //if there are less than 10 one word anagrams fill the rest with two word anagrams with random word order
                if (finalAnagrams.Count < 10)
                {
                    Random rand = new();
                    int needed = 10 - finalAnagrams.Count;
                    var shuffledTwoWordAnagrams = twoWordAnagrams
                        .OrderBy(x => rand.Next())
                        .Take(needed)
                        .Select(anagram =>
                        {
                            var words = anagram.Split(' ');
                            return $"{words[1]} {words[0]}"; // Reverse the order of the two words
                        })
                        .ToList();
                    finalAnagrams.AddRange(shuffledTwoWordAnagrams);
                }

                //if our list still has less than 10 anagrams fill the rest with three word anagrams with random word order
                if (finalAnagrams.Count < 10)
                {
                    Random rand = new();
                    int needed = 10 - finalAnagrams.Count;
                    var shuffledThreeWordAnagrams = threeWordAnagrams
                        .OrderBy(x => rand.Next())
                        .Take(needed)
                        .Select(anagram =>
                        {
                            var words = anagram.Split(' ');
                            return $"{words[2]} {words[1]} {words[0]}"; // Reverse the order of the three words
                        })
                        .ToList();
                    finalAnagrams.AddRange(shuffledThreeWordAnagrams);
                }

                foreach (string anagram in finalAnagrams)
                {

                    Random rand = new();
                    finalAnagrams = [.. finalAnagrams.Select(anagram =>
                    {
                        var words = anagram.Split(' ');
                        return string.Join(" ", words.OrderBy(_ => rand.Next()));
                    })];
                }

                AnagramResult anagramResult = new()
                {
                    Input = input,
                    Anagrams = [.. finalAnagrams.Distinct().Take(10)]
                };

                AIBL aIBL = new(configuration);
                var response = await aIBL.GetAIResult(anagramResult); ;
                return response.Trim('\"');
            }
            catch (Exception ex)
            {
                // Return an error message in case of failure
                return $"error: {ex.StackTrace}";
            }
        }
    }
}