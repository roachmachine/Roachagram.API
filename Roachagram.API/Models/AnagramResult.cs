

using System.Collections.Generic;

namespace Roachagram.API.Models
{
    /// <summary>
    /// Represents the result of an anagram generation operation.
    /// </summary>
    public class AnagramResult
    {
        /// <summary>
        /// Gets or sets the input string used to generate anagrams.
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// Gets or sets the list of generated anagrams.
        /// </summary>
        public List<string> Anagrams { get; set; }
    }
}
