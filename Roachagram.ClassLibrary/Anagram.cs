using System;
using System.Collections.Generic;
using System.Linq;

namespace Roachagram.ClassLibrary
{
    /// <summary>
    /// Represents a class for generating and managing anagrams based on input text and dictionary constraints.
    /// </summary>
    public class Anagram
    {
        /// <summary>
        /// Gets or sets the minimum length of a word in the anagram.
        /// </summary>
        public int MinimumLengthOfWord { get; set; }

        /// <summary>
        /// Gets or sets the input text for generating anagrams.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of words allowed in an anagram.
        /// </summary>
        public int MaximumNumberOfWords { get; set; }

        /// <summary>
        /// Gets or sets the type of dictionary used for generating anagrams.
        /// </summary>
        public int DictionaryType { get; set; }

        /// <summary>
        /// List of keys derived from the dictionary for anagram matching.
        /// </summary>
        private readonly List<string> KeyList = [];

        /// <summary>
        /// Dictionary containing words and their possible anagrams.
        /// </summary>
        private readonly Dictionary<string, List<string>> Dictionary = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="Anagram"/> class with specified parameters.
        /// </summary>
        /// <param name="inputText">The input text for generating anagrams.</param>
        /// <param name="minWordLength">The minimum length of a word in the anagram.</param>
        /// <param name="maxNumWords">The maximum number of words allowed in an anagram.</param>
        /// <param name="anagramDictionary">The dictionary used for anagram generation.</param>
        public Anagram(string inputText, int minWordLength, int maxNumWords, Dictionary<string, List<string>> anagramDictionary)
        {
            Input = inputText;
            MinimumLengthOfWord = minWordLength;
            MaximumNumberOfWords = maxNumWords;
            Dictionary = anagramDictionary ?? throw new ArgumentNullException(nameof(anagramDictionary));
            KeyList = [.. Dictionary.Keys];
            KeyList.Sort();
        }

        /// <summary>
        /// Generates all possible anagrams based on the input text and dictionary constraints.
        /// </summary>
        /// <returns>A list of all valid anagrams.</returns>
        public List<string> GetAllAnagrams()
        {
            if (string.IsNullOrEmpty(Input))
            {
                return [];
            }

            string sortedInput = new([.. Input.OrderBy(c => c)]);

            // Local list of matched anagram key sequences to keep the instance stateless across calls.
            var runningListOfMatchedAnagrams = new List<List<string>>();

            // Iterate through the key list to find matching anagrams.
            for (int index = 0; index < KeyList.Count; index++)
            {
                FindAnagrams(index, sortedInput, new List<string>(), runningListOfMatchedAnagrams);
            }

            // Generate the final output list of anagrams.
            return BuildOutput(runningListOfMatchedAnagrams);
        }

        /// <summary>
        /// Recursively finds anagrams by matching keys with the input text.
        /// </summary>
        /// <param name="keyListIndex">The current index in the key list.</param>
        /// <param name="remainingInput">The remaining input text to match.</param>
        /// <param name="currentWords">The current list of matched words forming an anagram.</param>
        /// <param name="matchedAnagramKeys">The accumulator for completed anagram key sequences.</param>
        private void FindAnagrams(int keyListIndex, string remainingInput, List<string> currentWords, List<List<string>> matchedAnagramKeys)
        {
            string searchWord = KeyList[keyListIndex];

            // Check for an exact match with the remaining input text.
            if (remainingInput.Equals(searchWord, StringComparison.Ordinal))
            {
                var completedAnagram = new List<string>(currentWords)
                {
                    searchWord
                };

                matchedAnagramKeys.Add(completedAnagram);
                return;
            }

            // Check if the search word can be formed from the remaining input text.
            if (TryRemoveLetters(remainingInput, searchWord, out string remainingAfterWord))
            {
                // Recursively find anagrams with the remaining input text.
                for (int index = keyListIndex + 1; index < KeyList.Count; index++)
                {
                    if (remainingAfterWord.Length >= MinimumLengthOfWord && currentWords.Count < MaximumNumberOfWords)
                    {
                        var clonedWords = new List<string>(currentWords)
                        {
                            searchWord
                        };

                        FindAnagrams(index, remainingAfterWord, clonedWords, matchedAnagramKeys);
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to remove the characters in <paramref name="word"/> from <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The input text to consume characters from.</param>
        /// <param name="word">The word whose characters should be removed.</param>
        /// <param name="remaining">The remaining characters after removal, if successful.</param>
        /// <returns><c>true</c> if all characters could be removed; otherwise, <c>false</c>.</returns>
        private static bool TryRemoveLetters(string input, string word, out string remaining)
        {
            if (string.IsNullOrEmpty(word))
            {
                remaining = input;
                return true;
            }

            var working = input;

            foreach (char letter in word)
            {
                int index = working.IndexOf(letter);
                if (index < 0)
                {
                    remaining = input;
                    return false;
                }

                // Remove the matched character from the working input text.
                working = working.Remove(index, 1);
            }

            remaining = working;
            return true;
        }

        /// <summary>
        /// Generates the final output list of anagrams after processing.
        /// </summary>
        /// <param name="matchedKeySequences">The matched key sequences representing anagram structures.</param>
        /// <returns>A list of formatted anagrams.</returns>
        private List<string> BuildOutput(IEnumerable<List<string>> matchedKeySequences)
        {
            List<string> outputList = [];
            List<FinalAnagram> finalAnagrams = [];

            // Helper function to generate combinations of words from each position's options.
            static IEnumerable<IEnumerable<string>> GenerateCombinations(IReadOnlyList<IEnumerable<string>> wordOptionsPerPosition, int index = 0)
            {
                if (index == wordOptionsPerPosition.Count)
                {
                    yield return Enumerable.Empty<string>();
                    yield break;
                }

                foreach (var word in wordOptionsPerPosition[index])
                {
                    foreach (var combination in GenerateCombinations(wordOptionsPerPosition, index + 1))
                    {
                        yield return new[] { word }.Concat(combination);
                    }
                }
            }

            // Helper function to format combinations into space-separated strings.
            static IEnumerable<string> BuildAnagramPhrases(IEnumerable<IEnumerable<string>> wordOptionsPerPosition)
            {
                var optionsList = wordOptionsPerPosition.ToList();
                return GenerateCombinations(optionsList).Select(words => string.Join(" ", words));
            }

            List<string[][]> listOfStringArrays = [];

            // Process each set of matched anagram keys into their corresponding word options.
            foreach (List<string> anagramSet in matchedKeySequences)
            {
                int counter = 0;
                string[][] outer = new string[anagramSet.Count][];
                foreach (string key in anagramSet)
                {
                    string[] inner = [.. Dictionary[key]];
                    outer[counter] = inner;
                    counter++;
                }

                listOfStringArrays.Add(outer);
            }

            // Generate and filter the final list of anagrams.
            foreach (var wordOptionsGrid in listOfStringArrays)
            {
                var results = BuildAnagramPhrases(wordOptionsGrid);
                foreach (var anagram in results)
                {
                    string[] sortedAnagram = anagram.Split(' ');
                    if (sortedAnagram.Length <= MaximumNumberOfWords)
                    {
                        bool minWordsPassed = true;
                        foreach (string word in sortedAnagram)
                        {
                            if (word.Length < MinimumLengthOfWord)
                            {
                                minWordsPassed = false;
                                break;
                            }
                        }
                        if (minWordsPassed)
                        {
                            Array.Sort(sortedAnagram, StringComparer.Ordinal);
                            finalAnagrams.Add(new FinalAnagram(string.Join(" ", sortedAnagram), sortedAnagram.Length));
                        }
                    }
                }
            }

            // Sort and format the final list of anagrams.
            finalAnagrams.Sort();
            foreach (FinalAnagram fa in finalAnagrams)
            {
                outputList.Add(fa.Anagram);
            }

            return outputList;
        }

        /// <summary>
        /// Represents a final anagram with its word count for sorting and output purposes.
        /// </summary>
        private sealed class FinalAnagram : IComparable<FinalAnagram>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FinalAnagram"/> class.
            /// </summary>
            /// <param name="stringAnagram">The formatted anagram string.</param>
            /// <param name="numberOfWords">The number of words in the anagram.</param>
            public FinalAnagram(string stringAnagram, int numberOfWords)
            {
                Anagram = stringAnagram;
                WordCount = numberOfWords;
            }

            /// <summary>
            /// Gets or sets the word count of the anagram.
            /// </summary>
            public int WordCount { get; set; }

            /// <summary>
            /// Gets or sets the formatted anagram string.
            /// </summary>
            public string Anagram { get; set; }

            /// <summary>
            /// Compares the current instance with another instance of the same type for sorting.
            /// </summary>
            /// <param name="other">The other <see cref="FinalAnagram"/> instance to compare with.</param>
            /// <returns>
            /// A value indicating the relative order of the objects being compared.
            /// </returns>
            public int CompareTo(FinalAnagram other)
            {
                if (other is null)
                {
                    return 1;
                }

                // Sort by word count in descending order.
                int wordCountComparison = other.WordCount.CompareTo(WordCount);
                if (wordCountComparison != 0)
                {
                    return wordCountComparison;
                }

                // Sort alphabetically if word counts are equal.
                return string.Compare(Anagram, other.Anagram, StringComparison.Ordinal);
            }
        }
    }
}