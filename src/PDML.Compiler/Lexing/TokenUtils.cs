using System.Text;

namespace PDML.Compiler.Lexing;

public static class TokenUtils
{
    public static string NormalizeString(Token token)
    {
        if (token.Type is not TokenType.String)
        {
            throw new ArgumentException("Token is not of type String", nameof(token));
        }

        string value = token.Value.Substring(1, token.Value.Length - 2);
        return NormalizeString(value);
    }

    private static string NormalizeString(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 >= value.Length)
            {
                builder.Append(value[i]);
                continue;
            }

            char escape = value[++i];
            builder.Append(escape switch
            {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'b' => '\b',
                'f' => '\f',
                'v' => '\v',
                '0' => '\0',
                '\\' => '\\',
                '"' => '"',
                '\'' => '\'',
                _ => throw new InvalidOperationException($"Unknown escape sequence '\\{escape}'")
            });
        }

        return builder.ToString();
    }
}