namespace PDML.Compiler.Lexing;

public enum TokenType
{
    Error,
    Eof,
    Whitespace,
    Identifier,

    TypeKeyword,

    Ampersand,
    Colon,
    Semicolon,
    Comma,
    Equals,
    LBrace,
    RBrace,
    LAngle,
    RAngle
}