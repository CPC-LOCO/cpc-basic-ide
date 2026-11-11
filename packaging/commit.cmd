@echo off
rem ------------------------------------------------------------------
rem  commit.cmd : lance les tests, montre ce qui change, puis enregistre
rem  (commit) et, si vous le voulez, envoie sur GitHub (push).
rem  A lancer par un double-clic. Il ne fait rien sans vos reponses.
rem ------------------------------------------------------------------
cd /d "%~dp0.."
set CHANGED=

rem Dossier "essais" : pour vos programmes d'essai, jamais enregistres dans git
if not exist essais mkdir essais
findstr /x /c:"essais/" .gitignore >nul 2>&1
if errorlevel 1 (
    echo.>>.gitignore
    echo essais/>>.gitignore
)

echo.
echo === 1. Lancement des tests ===
echo.
dotnet test --nologo
if errorlevel 1 (
    echo.
    echo LES TESTS ECHOUENT : rien n'a ete enregistre.
    echo Copiez l'ecran ci-dessus dans la conversation.
    pause
    exit /b 1
)

echo.
echo === 2. Fichiers qui vont etre enregistres ===
echo.
git status --short
for /f %%i in ('git status --short') do set CHANGED=1
if not defined CHANGED (
    echo Rien a enregistrer : tout est deja a jour.
    pause
    exit /b 0
)

echo.
echo M = fichier modifie    ?? = nouveau fichier    D = fichier supprime
echo.
set /p OK=Enregistrer tous ces fichiers ? (o/n) : 
if /i not "%OK%"=="o" (
    echo Rien n'a ete enregistre.
    pause
    exit /b 0
)

echo.
set /p MSG=Message du commit (une phrase, sans guillemets) : 
if "%MSG%"=="" (
    echo Message vide : rien n'a ete enregistre.
    pause
    exit /b 1
)

git add -A
git commit -m "%MSG%"
if errorlevel 1 (
    echo.
    echo Le commit n'a pas abouti : copiez l'ecran dans la conversation.
    pause
    exit /b 1
)

echo.
set /p PUSH=Envoyer sur GitHub maintenant ? (o/n) : 
if /i "%PUSH%"=="o" git push

echo.
echo === Etat final ===
git status
echo.
pause
