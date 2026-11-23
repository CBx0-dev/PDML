namespace PDML.Compiler.Lexing;

public enum TokenType
{
    Error,
    Eof,
    Whitespace,
    Identifier,
    Comment,
    String,
    Int,

    TypeKeyword,
    VirtualKeyword,
    AliasKeyword,
    ImportKeyword,
    FromKeyword,
    EnumKeyword,
    
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