using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace CpcIde;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;

            // Problème imprévu (D119) : au lieu de fermer l'éditeur (et de perdre le programme), on note le détail
            // dans erreurs.txt et on l'affiche dans le panneau des messages. Le programme reste à l'écran.
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                string? log = WriteErrorLog(e.Exception);
                e.Handled = true;
                window.ReportInternalError(e.Exception, log);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    WriteErrorLog(ex);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Fichier des problèmes imprévus : %APPDATA%\CpcBasicIde\erreurs.txt (à envoyer pour corriger).</summary>
    public static string ErrorLogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CpcBasicIde", "erreurs.txt");

    static string? WriteErrorLog(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ErrorLogPath)!);
            File.AppendAllText(ErrorLogPath, $"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====\r\n{ex}\r\n\r\n");
            return ErrorLogPath;
        }
        catch (Exception)
        {
            return null;   // le fichier ne peut pas être écrit : on n'ajoute pas un deuxième problème
        }
    }
}