using System;
using System.Collections.Generic;
using System.Text;

namespace MysteryMud.ConsoleApp3.Commands.v2;

//public class LiteralArgumentToken : ISyntaxToken
//{
//    private readonly string Word;
//    private readonly string Name; // name to store in arguments

//    public LiteralArgumentToken(string word, string name)
//    {
//        Word = word;
//        Name = name;
//    }

//    public bool Match(string token, Dictionary<string, object> args)
//    {
//        if (token.Equals(Word, StringComparison.OrdinalIgnoreCase))
//        {
//            args[Name] = Word; // store the literal word
//            return true;
//        }
//        return false;
//    }
//}

public class LiteralArgumentToken : ISyntaxToken
{
    private readonly string _word;
    private readonly string _name;
    public LiteralArgumentToken(string word, string name)
    {
        _word = word;
        _name = name;
    }

    public bool Match(ReadOnlySpan<char> input, Dictionary<string, object> args)
    {
        if (input.Equals(_word.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            args[_name] = _word;
            return true;
        }
        return false;
    }
}