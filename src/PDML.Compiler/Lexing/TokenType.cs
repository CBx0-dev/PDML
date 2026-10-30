namespace PDML.Compiler.Lexing;

public enum TokenType
{
    Error,
    Eof,
    Whitespace,
    Identifier,
    Comment,
    String,

    TypeKeyword,
    VirtualKeyword,
    ImportKeyword,
    FromKeyword,

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