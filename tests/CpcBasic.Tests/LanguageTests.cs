// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core.Language;
using Xunit;

namespace CpcBasic.Tests;

public class LexerTests
{
    static List<Token> Lex(string source, out List<Diagnostic> diagnostics)
    {
        diagnostics = new List<Diagnostic>();
        return Lexer.Tokenize(source, diagnostics);
    }

    [Fact]
    public void Print_line_gives_expected_tokens()
    {
        List<Token> tokens = Lex("10 PRINT \"HELLO\"", out var diagnostics);

        Assert.Empty(diagnostics);
        TokenKind[] expected =
        [
            TokenKind.LineNumber, TokenKind.Word, TokenKind.String, TokenKind.EndOfLine, TokenKind.EndOfFile
        ];
        Assert.Equal(expected, tokens.Select(t => t.Kind).ToArray());
        Assert.Equal("10", tokens[0].Text);
        Assert.Equal("PRINT", tokens[1].Text);
        Assert.Equal("HELLO", tokens[2].Text);
    }

    [Fact]
    public void Words_are_converted_to_upper_case()
    {
        List<Token> tokens = Lex("10 print \"x\"", out _);

        Assert.Equal("PRINT", tokens[1].Text);
    }

    [Fact]
    public void Question_mark_is_a_shortcut_for_print()
    {
        List<Token> tokens = Lex("10 ?\"HELLO\"", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.Equal(TokenKind.Word, tokens[1].Kind);
        Assert.Equal("PRINT", tokens[1].Text);
        Assert.Equal(TokenKind.String, tokens[2].Kind);
    }

    [Fact]
    public void Text_between_quotes_keeps_its_case_and_spaces()
    {
        List<Token> tokens = Lex("10 PRINT \"Hello  World\"", out _);

        Assert.Equal("Hello  World", tokens[2].Text);
    }

    [Fact]
    public void Rem_and_apostrophe_comments_are_ignored()
    {
        List<Token> tokens = Lex("10 REM Mon jeu\n20 ' autre commentaire", out var diagnostics);

        Assert.Empty(diagnostics);
        TokenKind[] expected =
        [
            TokenKind.LineNumber, TokenKind.EndOfLine,
            TokenKind.LineNumber, TokenKind.EndOfLine,
            TokenKind.EndOfFile
        ];
        Assert.Equal(expected, tokens.Select(t => t.Kind).ToArray());
    }

    [Fact]
    public void Unterminated_string_is_reported()
    {
        _ = Lex("10 PRINT \"HELLO", out var diagnostics);

        Diagnostic d = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCode.UnterminatedString, d.Code);
        Assert.Equal(1, d.Line);
    }

    [Fact]
    public void Unexpected_character_is_reported()
    {
        _ = Lex("10 PRINT @", out var diagnostics);

        Diagnostic d = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCode.UnexpectedCharacter, d.Code);
        Assert.Equal("@", d.Argument);
    }

    [Fact]
    public void Line_numbers_in_tokens_follow_the_editor_lines()
    {
        List<Token> tokens = Lex("10 PRINT \"A\"\n20 PRINT \"B\"", out _);

        Token secondPrint = tokens.Where(t => t.Kind == TokenKind.Word).Skip(1).First();
        Assert.Equal(2, secondPrint.Line);
    }
}

public class ParserTests
{
    [Fact]
    public void Print_with_text_gives_a_print_statement()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"HELLO\"");

        Assert.False(result.HasErrors);
        PrintStatement print = Assert.IsType<PrintStatement>(Assert.Single(result.Program.Statements));
        Assert.Equal("HELLO", print.Text);
        Assert.True(print.NewLine);
        Assert.Equal(1, print.SourceLine);
    }

    [Fact]
    public void Trailing_semicolon_removes_the_line_break()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"HELLO\";");

        PrintStatement print = Assert.IsType<PrintStatement>(Assert.Single(result.Program.Statements));
        Assert.False(print.NewLine);
    }

    [Fact]
    public void Print_alone_gives_an_empty_line()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT");

        PrintStatement print = Assert.IsType<PrintStatement>(Assert.Single(result.Program.Statements));
        Assert.Equal("", print.Text);
        Assert.True(print.NewLine);
    }

    [Fact]
    public void Question_mark_works_like_print()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 ?\"HELLO\"\n20 ?\n30 ? \"A\";");

        Assert.False(result.HasErrors);
        Assert.Equal(3, result.Program.Statements.Count);
        PrintStatement first = Assert.IsType<PrintStatement>(result.Program.Statements[0]);
        Assert.Equal("HELLO", first.Text);
        PrintStatement third = Assert.IsType<PrintStatement>(result.Program.Statements[2]);
        Assert.False(third.NewLine);
    }

    [Fact]
    public void Colon_separates_statements_on_one_line()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"A\" : PRINT \"B\"");

        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Program.Statements.Count);
    }

    [Fact]
    public void End_gives_an_end_statement()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"A\"\n20 END");

        Assert.Equal(2, result.Program.Statements.Count);
        Assert.IsType<EndStatement>(result.Program.Statements[1]);
    }

    [Fact]
    public void Lines_without_numbers_are_accepted()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("PRINT \"A\"\nPRINT \"B\"");

        Assert.False(result.HasErrors);
        Assert.Equal(2, result.Program.Statements.Count);
    }

    [Fact]
    public void Empty_lines_and_comments_do_not_produce_statements()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 REM titre\n\n20 PRINT \"A\"\n30 ' fin");

        Assert.False(result.HasErrors);
        Assert.Single(result.Program.Statements);
    }

    [Fact]
    public void Unknown_command_is_reported_with_its_line()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"A\"\n20 PRNT \"B\"");

        Diagnostic d = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.UnknownCommand, d.Code);
        Assert.Equal(2, d.Line);
        Assert.Equal("PRNT", d.Argument);
        Assert.Single(result.Program.Statements);   // la première ligne est quand même reconnue
    }

    [Fact]
    public void Print_with_a_number_is_accepted()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT 5");

        Assert.False(result.HasErrors);
        Assert.IsType<PrintValueStatement>(Assert.Single(result.Program.Statements));
    }

    [Fact]
    public void Print_with_something_that_is_not_a_value_is_reported()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT )");

        Diagnostic d = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ExpectedExpression, d.Code);
        Assert.Equal(")", d.Argument);
    }

    [Fact]
    public void Extra_text_after_a_statement_is_reported()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"A\" \"B\"");

        Diagnostic d = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.UnexpectedToken, d.Code);
        Assert.Equal("B", d.Argument);
        Assert.Single(result.Program.Statements);   // PRINT "A" est conservé
    }

    [Fact]
    public void Several_errors_are_all_reported()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRNT \"A\"\n20 PRINT )\n30 PRINT \"C\"");

        Assert.Equal(2, result.Diagnostics.Count);
        Assert.Single(result.Program.Statements);   // seule la ligne 30 est correcte
    }

    [Fact]
    public void Error_message_names_the_line_and_the_cause()
    {
        AnalysisResult result = BasicAnalyzer.Analyze("10 PRINT \"A\"\n20 PRNT \"B\"");

        string message = Assert.Single(result.Diagnostics).Message;
        Assert.Contains("Ligne 2", message);   // la ligne affichée est celle de l'éditeur
        Assert.Contains("PRNT", message);
    }
}
