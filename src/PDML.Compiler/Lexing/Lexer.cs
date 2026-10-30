using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;

namespace PDML.Compiler.Lexing;

public sealed class Lexer
{
    private readonly DiagnosticBag _diagnostics;
    private readonly SourceFile _sourceFile;

    private Coordinate _data;

    private char _current
    {
        get
        {
            if (_data.Position >= _sourceFile.Length)
            {
                return '\0';
            }

            return _sourceFile[_data.Position];
        }
    }

    private char _next
    {
        get
        {
            if (_data.Position + 1 >= _sourceFile.Length)
            {
                return '\0';
            }

            return _sourceFile[_data.Position + 1];
        }
    }

    private bool _isEof => _data.Position >= _sourceFile.Length;

    public Lexer(DiagnosticBag diagnostics, SourceFile sourceFile)
    {
        _diagnostics = diagnostics;
        _sourceFile = sourceFile;

        _data = new Coordinate
        {
            Position = 0,
            Line = 1,
            Column = 1
        };
    }

    public Token NextToken()
    {
        Coordinate start = _data;
        if (_isEof)
        {
            return CreateToken(start, TokenType.Eof);
        }

        if (char.IsWhiteSpace(_current))
        {
            return NextWhitespace(start);
        }

        if (char.IsLetter(_current))
        {
            return NextIdentifier(start);
        }

        if (_current == '"')
        {
            return NextString(start);
        }
        
        if (_current == '/' && _next == '/')
        {
            return NextComment(start);
        }

        char current = _current;
        Skip();

        switch (current)
        {
            case '&':
                return CreateToken(start, TokenType.Ampersand);
            case ':':
                return CreateToken(start, TokenType.Colon);
            case ';':
                return CreateToken(start, TokenType.Semicolon);
            case ',':
                return CreateToken(start, TokenType.Comma);
            case '=':
                return CreateToken(start, TokenType.Equals);
            case '{':
                return CreateToken(start, TokenType.LBrace);
            case '}':
                return CreateToken(start, TokenType.RBrace);
            case '<':
                return CreateToken(start, TokenType.LAngle);
            case '>':
                return CreateToken(start, TokenType.RAngle);
        }

        Location location = new Location
        {
            SourceFile = _sourceFile,
            Start = start,
            End = _data
        };

        _diagnostics.Report(LexingError.UnexpectedCharacter, location, current);
        return CreateToken(start, TokenType.Error);
    }

    private Token NextWhitespace(Coordinate start)
    {
        while (!_isEof && char.IsWhiteSpace(_current))
        {
            Skip();
        }

        return CreateToken(start, TokenType.Whitespace);
    }

    private Token NextIdentifier(Coordinate start)
    {
        while (!_isEof && char.IsLetterOrDigit(_current))
        {
            Skip();
        }

        string content = _sourceFile.Content.Substring(start.Position, _data.Position - start.Position);
        Location location = new Location
        {
            SourceFile = _sourceFile,
            Start = start,
            End = _data
        };
        TokenType type = content switch
        {
            "type" => TokenType.TypeKeyword,
            "virtual" => TokenType.VirtualKeyword,
            "import" => TokenType.ImportKeyword,
            "from" => TokenType.FromKeyword,
            _ => TokenType.Identifier,
        };

        return CreateToken(location, type, content);
    }
    
    private Token NextString(Coordinate start)
    {
        Skip();

        while (!_isEof && _current != '"')
        {
            if (_current == '\n')
            {
                Location location = new Location
                {
                    SourceFile = _sourceFile,
                    Start = start,
                    End = _data
                };
                _diagnostics.Report(LexingError.UnterminatedString, location);
                return CreateToken(location, TokenType.Error);
            }

            if (_current == '\\')
            {
                Skip();
            }

            Skip();
        }

        if (_current != '"')
        {
            Location location = new Location
            {
                SourceFile = _sourceFile,
                Start = start,
                End = _data
            };
            _diagnostics.Report(LexingError.UnterminatedString, location);
            return CreateToken(location, TokenType.Error);
        }
        Skip();

        return CreateToken(start, TokenType.String);
    }

    private Token NextComment(Coordinate start)
    {
        while (!_isEof && _current != '\n')
        {
            Skip();
        }

        return CreateToken(start, TokenType.Comment);
    }

    private Token CreateToken(Coordinate start, TokenType type, string? value = null)
    {
        Location location = new Location
        {
            SourceFile = _sourceFile,
            Start = start,
            End = _data
        };

        return CreateToken(location, type, value);
    }

    private Token CreateToken(Location location, TokenType type, string? value = null)
    {
        int start = location.Start.Position;
        int length = location.End.Position - start;
        value ??= _sourceFile.Content.Substring(start, length);

        return new Token
        {
            Location = location,
            Type = type,
            Value = value
        };

    }

    private void Skip()
    {
        if (_current == '\n')
        {
            _data.Line++;
            _data.Column = 0;
        }

        _data.Column++;
        _data.Position++;
    }

    public static List<Token> TokenizeAll(DiagnosticBag diagnostics, SourceFile sourceFile)
    {
        Lexer lexer = new Lexer(diagnostics, sourceFile);
        List<Token> tokens = new List<Token>();
        Token token;

        do
        {
            token = lexer.NextToken();
            if (token.Type is TokenType.Error or TokenType.Whitespace or TokenType.Comment)
            {
                continue;
            }

            tokens.Add(token);
        } while (token.Type != TokenType.Eof);

        return tokens;
    }
}