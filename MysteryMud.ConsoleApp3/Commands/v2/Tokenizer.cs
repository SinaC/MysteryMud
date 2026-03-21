namespace MysteryMud.ConsoleApp3.Commands.v2
{
    public static class Tokenizer
    {
        public static int Tokenize(ReadOnlySpan<char> input, Span<Token> tokens) 
        {
            int i = 0;
            int tokenIndex = 0;

            while (i < input.Length)
            {
                // skip whitespace
                while (i < input.Length && char.IsWhiteSpace(input[i]))
                    i++;
                if (i >= input.Length)
                    break;

                int start = i;

                if (input[i] == '\'' || input[i] == '"') // quoted token
                {
                    char quote = input[i++];
                    start = i;
                    while (i < input.Length && input[i] != quote) i++;
                    int length = i - start;
                    tokens[tokenIndex] = new Token(start, length);
                    tokenIndex++;
                    i++; // skip closing quote
                }
                else
                {
                    while (i < input.Length && !char.IsWhiteSpace(input[i])) i++;
                    int length = i - start;
                    tokens[tokenIndex] = new Token(start, length);
                    tokenIndex++;
                }
            }

            return tokenIndex;
        }
    }
}
