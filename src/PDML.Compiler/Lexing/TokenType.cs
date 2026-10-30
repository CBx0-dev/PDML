namespace PDML.Compiler.Lexing;

public enum TokenType
{
    Error,
    Eof,
    Whitespace,
    Identifier,

    TypeKeyword,
    VirtualKeyword,

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