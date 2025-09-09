using System;

namespace Roachagram.API.Models
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AIModel"/> class.
    /// </remarks>
    /// <param name="word">The word.</param>
    /// <param name="minWordLength">Minimum length of the word.</param>
    /// <param name="maxNumWords">The maximum number words.</param>
    /// <param name="elapsedTime">The elapsed time.</param>
    [Serializable]
    public class AIModel(string word, int minWordLength, int maxNumWords, int elapsedTime)
    {

        /// <summary>
        /// Gets or sets the word.
        /// </summary>
        /// <value>
        /// The word.
        /// </value>
        public string Word { get; set; } = word;

        /// <summary>
        /// Gets or sets the minimum length of the word.
        /// </summary>
        /// <value>
        /// The minimum length of the word.
        /// </value>
        public int MinWordLength { get; set; } = minWordLength;

        /// <summary>
        /// Gets or sets the maximum number words.
        /// </summary>
        /// <value>
        /// The maximum number words.
        /// </value>
        public int MaxNumWords { get; set; } = maxNumWords;

        /// <summary>
        /// Gets or sets the elapsed time.
        /// </summary>
        /// <value>
        /// The elapsed time.
        /// </value>
        public int ElapsedTime { get; set; } = elapsedTime;
    }
}
