@echo off
rem Rassemble dans un seul dossier les fichiers a envoyer dans une nouvelle conversation.
rem A lancer par double-clic ou depuis l'invite de commandes. Le dossier cree est E:\transfert
setlocal
set RACINE=E:\projets amstrad ide\cpc-basic-ide
set DEST=E:\transfert

if exist "%DEST%" rmdir /s /q "%DEST%"
mkdir "%DEST%"

for %%F in (
    "docs\REPRISE.md"
    "docs\ARCHITECTURE.md"
    "docs\JOURNAL-DECISIONS.md"
    "docs\IDEES.md"
    "src\CpcBasic.Core\Language\Lexer.cs"
    "src\CpcBasic.Core\Language\Parser.cs"
    "src\CpcBasic.Core\Language\Ast.cs"
    "src\CpcBasic.Core\Language\Diagnostic.cs"
    "src\CpcBasic.Core\Language\BasicAnalyzer.cs"
    "src\CpcBasic.Core\Codegen\Z80Emitter.cs"
    "src\CpcBasic.Core\Codegen\CodeGenerator.cs"
    "src\CpcBasic.Core\Codegen\RuntimeLibrary.cs"
    "src\CpcBasic.Core\Compiler.cs"
    "src\CpcIde\MainWindow.axaml"
    "src\CpcIde\MainWindow.axaml.cs"
    "src\CpcIde\BasicColorizer.cs"
    "tests\CpcBasic.Tests\MiniZ80.cs"
) do (
    copy "%RACINE%\%%~F" "%DEST%\" >nul
    if errorlevel 1 echo MANQUANT : %%~F
)

echo.
echo Termine. Les fichiers sont dans %DEST%
echo Si un fichier est indique MANQUANT, verifiez son emplacement dans le depot.
echo.
dir /b "%DEST%"
pause
endlocal
