using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using PostServiceFormsApp.Forms;

namespace PostServiceFormsApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Реєстрація глобального обробника необроблених винятків перед стартом інтерфейсу
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        public static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception? ex = e.ExceptionObject as Exception;

            // Винятки з асинхронних викликів WinForms можуть надходити загорнутими в TargetInvocationException
            if (ex is TargetInvocationException && ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            // 1. Подробиці для розробника виводимо в Debug-консоль
            System.Diagnostics.Debug.WriteLine($"================== [UNHANDLED EXCEPTION] ==================");
            System.Diagnostics.Debug.WriteLine($"Час: {DateTime.Now}");
            System.Diagnostics.Debug.WriteLine($"Тип: {ex?.GetType().FullName}");
            System.Diagnostics.Debug.WriteLine($"Повідомлення: {ex?.Message}");
            System.Diagnostics.Debug.WriteLine($"Стек викликів: {ex?.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"===========================================================");

            // 2. Фіксуємо технічні деталі в журналі помилок (errors.log)
            try
            {
                File.AppendAllText("errors.log", $"{DateTime.Now} [{ex?.GetType().Name}]: {ex}{Environment.NewLine}");
            }
            catch
            {
                // Запобігаємо збою під час самого логування
            }

            // 3. Користувачу показуємо лаконічне і зрозуміле вікно без лякаючих технічних стек-трейсів
            MessageBox.Show(
                "Сталась непередбачена помилка. Подробиці збережено в журналі.",
                "Помилка застосунку",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}