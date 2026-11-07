@echo off
rem Prepare la version a publier : dossier autonome + archive .zip + somme de controle.
rem A lancer depuis la racine du depot : packaging\build-release.cmd
setlocal

set VERSION=0.1.0
set NAME=CpcBasic-%VERSION%-win-x64
set OUT=release\%NAME%

echo.
echo === Nettoyage ===
if exist release rmdir /s /q release
mkdir "%OUT%"

echo.
echo === Publication de l'editeur (peut prendre une minute) ===
dotnet publish src\CpcIde -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o "%OUT%"
if errorlevel 1 goto erreur

echo.
echo === Ajout des fichiers d'accompagnement ===
mkdir "%OUT%\examples"
copy examples\hello.bas "%OUT%\examples\" >nul
copy packaging\LISEZMOI.txt "%OUT%\" >nul
copy LICENSE "%OUT%\" >nul

echo.
echo === Creation de l'archive .zip ===
powershell -NoProfile -Command "Compress-Archive -Path '%OUT%' -DestinationPath 'release\%NAME%.zip' -Force"
if errorlevel 1 goto erreur

echo.
echo === Somme de controle SHA256 (a copier dans la description de la version) ===
certutil -hashfile "release\%NAME%.zip" SHA256

echo.
echo Termine : release\%NAME%.zip
goto fin

:erreur
echo.
echo ERREUR : la preparation a echoue. Copiez les messages ci-dessus.
exit /b 1

:fin
endlocal
