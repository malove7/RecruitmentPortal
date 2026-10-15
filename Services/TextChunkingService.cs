using System.Text;

namespace RecruitmentPortal.Services
{
    public class TextChunkingService : ITextChunkingService
    {
        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // Approximation for English/code: ~4 chars per token, with word count guard
            var charBased = (int)Math.Ceiling(text.Length / 4.0);
            var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var wordBased = (int)Math.Ceiling(words * 1.3);

            return Math.Max(1, Math.Max(charBased, wordBased));
        }

        public List<TextChunk> ChunkDocument(ExtractedDocument document, int chunkSizeTokens = 800, int chunkOverlapTokens = 120)
        {
            if (document == null || string.IsNullOrWhiteSpace(document.FullText))
            {
                return new List<TextChunk>();
            }

            if (chunkSizeTokens <= 0) chunkSizeTokens = 800;
            if (chunkOverlapTokens < 0 || chunkOverlapTokens >= chunkSizeTokens) chunkOverlapTokens = 120;

            var allChunks = new List<TextChunk>();

            if (document.Pages != null && document.Pages.Count > 0)
            {
                int runningIndex = 0;

                foreach (var page in document.Pages)
                {
                    if (string.IsNullOrWhiteSpace(page.Text))
                        continue;

                    var pageChunks = ChunkText(page.Text, page.PageNumber, chunkSizeTokens, chunkOverlapTokens);
                    foreach (var pc in pageChunks)
                    {
                        pc.ChunkIndex = runningIndex++;
                        allChunks.Add(pc);
                    }
                }
            }
            else
            {
                allChunks = ChunkText(document.FullText, null, chunkSizeTokens, chunkOverlapTokens);
            }

            return allChunks;
        }

        public List<TextChunk> ChunkText(string text, int? pageNumber = null, int chunkSizeTokens = 800, int chunkOverlapTokens = 120)
        {
            var results = new List<TextChunk>();
            if (string.IsNullOrWhiteSpace(text))
                return results;

            if (chunkSizeTokens <= 0) chunkSizeTokens = 800;
            if (chunkOverlapTokens < 0 || chunkOverlapTokens >= chunkSizeTokens) chunkOverlapTokens = 120;

            // Target characters based on ~4 chars per token
            int targetChars = chunkSizeTokens * 4;
            int overlapChars = chunkOverlapTokens * 4;
            int stepChars = Math.Max(100, targetChars - overlapChars);

            if (text.Length <= targetChars)
            {
                results.Add(new TextChunk
                {
                    ChunkIndex = 0,
                    Content = text.Trim(),
                    PageNumber = pageNumber,
                    TokenCount = EstimateTokenCount(text.Trim())
                });
                return results;
            }

            int startIndex = 0;
            int chunkIndex = 0;

            while (startIndex < text.Length)
            {
                int remaining = text.Length - startIndex;
                int currentLength = Math.Min(targetChars, remaining);

                // If not at end, try to find a natural break near targetChars
                int endIndex = startIndex + currentLength;
                if (endIndex < text.Length)
                {
                    // Look back up to 25% for a paragraph break, sentence break, or space
                    int lookbackWindow = (int)(currentLength * 0.25);
                    int breakPos = FindNaturalBreak(text, endIndex - lookbackWindow, endIndex);
                    if (breakPos > startIndex)
                    {
                        endIndex = breakPos;
                    }
                }

                var chunkContent = text.Substring(startIndex, endIndex - startIndex).Trim();
                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    results.Add(new TextChunk
                    {
                        ChunkIndex = chunkIndex++,
                        Content = chunkContent,
                        PageNumber = pageNumber,
                        TokenCount = EstimateTokenCount(chunkContent)
                    });
                }

                if (endIndex >= text.Length)
                    break;

                // Move forward by length minus overlap
                int actualChunkChars = endIndex - startIndex;
                int advanceBy = Math.Max(1, actualChunkChars - overlapChars);
                startIndex += advanceBy;
            }

            return results;
        }

        private int FindNaturalBreak(string text, int searchStart, int searchEnd)
        {
            if (searchStart < 0) searchStart = 0;
            if (searchEnd > text.Length) searchEnd = text.Length;

            var window = text.Substring(searchStart, searchEnd - searchStart);

            // 1. Double line break (paragraph)
            int pos = window.LastIndexOf("\n\n", StringComparison.Ordinal);
            if (pos >= 0) return searchStart + pos + 2;

            // 2. Single line break
            pos = window.LastIndexOf('\n');
            if (pos >= 0) return searchStart + pos + 1;

            // 3. Sentence end (. ! ?) followed by space
            for (int i = window.Length - 2; i >= 0; i--)
            {
                char c = window[i];
                if ((c == '.' || c == '!' || c == '?') && char.IsWhiteSpace(window[i + 1]))
                {
                    return searchStart + i + 2;
                }
            }

            // 4. Word break (space)
            pos = window.LastIndexOf(' ');
            if (pos >= 0) return searchStart + pos + 1;

            return searchEnd;
        }
    }
}
