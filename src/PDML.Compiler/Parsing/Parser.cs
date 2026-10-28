using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Lexing;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Compiler.Parsing;

public sealed class Parser
{
    private readonly DiagnosticBag _diagnostics;
    private readonly List<Token> _tokens;

    private bool _lastMatchIsError;
    private int _position;

    private Token _current
    {
        get
        {
            if (_position >= _tokens.Count)
            {
                return _tokens[_tokens.Count - 1];
            }

            return _tokens[_position];
        }
    }

    private bool _isEof => _position >= _tokens.Count;

    public Parser(DiagnosticBag diagnostics, List<Token> tokens)
    {
        _diagnostics = diagnostics;
        _tokens = tokens;
        _lastMatchIsError = false;
        _position = 0;
    }

    public Module ParseModule()
    {
        List<Member> members = new List<Member>();

        while (!_isEof && _current.Type != TokenType.Eof)
        {
            Member? member = ParseMember();
            if (member is not null)
            {
                members.Add(member);
            }
        }

        Token eof = Match(TokenType.Eof);

        return new Module
        {
            Location = Location.Merge(
                members.Count > 0
                    ? members[0].Location
                    : eof.Location,
                eof.Location
            ),
            Members = members,
            Eof = eof
        };
    }

    private Member? ParseMember()
    {
        if (_current.Type is TokenType.TypeKeyword)
        {
            return ParseTypeDefinitionMember();
        }

        _diagnostics.Report(ParsingError.UnexpectedToken, _current.Location, _current.Type.GetDisplayString());
        _position++;
        return null;
    }

    private Member ParseTypeDefinitionMember()
    {
        Token typeKeyword = Match(TokenType.TypeKeyword);
        Token name = Match(TokenType.Identifier);
        Token? colon = null;
        TypeSignature? inheritingType = null;
        if (_current.Type is TokenType.Colon)
        {
            colon = Match(TokenType.Colon);
            inheritingType = ParseTypeSignature();
        }

        Token equal = Match(TokenType.Equals);
        DelimitedList<TypeDefinitionItem, Token> items = new DelimitedList<TypeDefinitionItem, Token>();
        items.AddItem(ParseTypeDefinitionItem());
        while (!_isEof && _current.Type is not TokenType.Semicolon)
        {
            items.AddDelimiter(Match(TokenType.Ampersand));
            items.AddItem(ParseTypeDefinitionItem());
        }

        Token semicolon = Match(TokenType.Semicolon);

        return new TypeDefinitionMember
        {
            Location = Location.Merge(typeKeyword.Location, semicolon.Location),
            TypeKeyword = typeKeyword,
            Name = name,
            Colon = colon,
            InheritingType = inheritingType,
            Equal = equal,
            Items = items,
            Semicolon = semicolon
        };
    }

    private TypeDefinitionItem ParseTypeDefinitionItem()
    {
        if (_current.Type is TokenType.LBrace)
        {
            return ParseTypeDefinitionItemBody();
        }

        return ParseTypeDefinitionItemTarget();
    }

    private TypeDefinitionItem ParseTypeDefinitionItemBody()
    {
        Token lBrace = Match(TokenType.LBrace);
        List<TypeDefinitionBodyProperty> properties = new List<TypeDefinitionBodyProperty>();
        while (!_isEof && _current.Type is not TokenType.RBrace)
        {
            properties.Add(ParseTypeDefinitionProperty());
        }

        Token rBrace = Match(TokenType.RBrace);

        return new TypeDefinitionItemBody
        {
            Location = Location.Merge(lBrace.Location, rBrace.Location),
            LBrace = lBrace,
            Properties = properties,
            RBrace = rBrace,
        };
    }

    private TypeDefinitionBodyProperty ParseTypeDefinitionProperty()
    {
        Token name = Match(TokenType.Identifier);
        TypeClause typeClause = ParseTypeClause();
        Token semicolon = Match(TokenType.Semicolon);
        return new TypeDefinitionBodyProperty
        {
            Location = Location.Merge(name.Location, semicolon.Location),
            Name = name,
            TypeClause = typeClause,
            Semicolon = semicolon
        };
    }

    private TypeDefinitionItem ParseTypeDefinitionItemTarget()
    {
        TypeSignature type = ParseTypeSignature();

        return new TypeDefinitionItemTarget
        {
            Location = type.Location,
            Type = type
        };
    }

    private TypeClause ParseTypeClause()
    {
        Token colon = Match(TokenType.Colon);
        TypeSignature typeSignature = ParseTypeSignature();
        return new TypeClause
        {
            Location = Location.Merge(colon.Location, typeSignature.Location),
            Colon = colon,
            TypeSignature = typeSignature
        };
    }

    private TypeSignature ParseTypeSignature()
    {
        Token name = Match(TokenType.Identifier);
        Token? lAngle = null;
        DelimitedList<TypeSignature, Token>? generics = null;
        Token? rAngle = null;

        if (_current.Type is TokenType.LAngle)
        {
            lAngle = Match(TokenType.LAngle);
            generics = new DelimitedList<TypeSignature, Token>();
            generics.AddItem(ParseTypeSignature());
            while (!_isEof && _current.Type is not TokenType.RAngle)
            {
                generics.AddDelimiter(Match(TokenType.Comma));
                generics.AddItem(ParseTypeSignature());
            }

            rAngle = Match(TokenType.RAngle);
        }

        return new TypeSignature
        {
            Location = Location.Merge(
                name.Location,
                rAngle is null
                    ? name.Location
                    : rAngle.Location
            ),
            Name = name,
            LAngle = lAngle,
            Generics = generics,
            RAngle = rAngle
        };
    }

    private Token Match(TokenType type)
    {
        Token current = _current;
        _position++;

        if (current.Type == type)
        {
            _lastMatchIsError = false;
            return current;
        }

        if (!_lastMatchIsError)
        {
            _diagnostics.Report(ParsingError.ExpectedToken, current.Location, type.GetDisplayString(), current.Type.GetDisplayString());
        }

        _lastMatchIsError = true;
        return new Token
        {
            Location = current.Location,
            Type = type,
            Value = current.Value
        };
    }
}