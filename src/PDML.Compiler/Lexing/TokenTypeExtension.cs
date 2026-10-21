namespace PDML.Compiler.Lexing;

public static class TokenTypeExtension
{
    public static string GetDisplayString(this TokenType type)
    {
        switch (type)
        {
            case TokenType.Error:
                return "?";
            case TokenType.Eof:
                return "<EOF>";
            case TokenType.Whitespace:
                return "<WHITESPACE>";
            case TokenType.Identifier:
                return "<IDENTIFIER>";
            case TokenType.TypeKeyword:
                return "@type";
            case TokenType.Ampersand:
                return "&";
            case TokenType.Colon:
                return ":";
            case TokenType.Semicolon:
                return ";";
            case TokenType.Comma:
                return ",";
            case TokenType.Equals:
                return "=";
            case TokenType.LBrace:
                return "{";
            case TokenType.RBrace:
                return "}";
            case TokenType.LAngle:
                return "<";
            case TokenType.RAngle:
                return ">";
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}