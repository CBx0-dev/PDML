namespace PDML.Compiler.Lexing;

public enum TokenType
{
    Error,
    Eof,
    Whitespace,
    Identifier,
    Keyword,

    Ampersand,
    Colon,
    Semicolon,
    LBrace,
    RBrace,
    LAngle,
    RAngle
}