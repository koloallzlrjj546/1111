using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;

class Program
{
    static void Main()
    {
        Console.WriteLine("C# + Python через JSON-файлы");
        Console.WriteLine("Введите числа через пробел (пример: 1 2 3.5 -4)");
        Console.WriteLine("Чтобы выйти: просто нажмите Enter на пустой строке.");

        while (true)
        {
            Console.WriteLine();
            Console.Write("Числа: ");

            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                Console.WriteLine("Выход.");
                return;
            }

            string[] parts = line.Split(new[] { ' ', '\t', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            double[] numbers;

            try
            {
                numbers = ParseNumbers(parts);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
                continue;
            }

            string inputPath = "input.json";
            string outputPath = "output.json";

            try
            {
                WriteInputJson(inputPath, numbers);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка записи input.json: " + ex.Message);
                continue;
            }

            int exitCode;
            string pythonOutput;

            try
            {
                exitCode = RunPython("analyzer.py", inputPath, outputPath, out pythonOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось запустить Python: " + ex.Message);
                Console.WriteLine("Проверьте, что Python установлен и команда 'python' работает в терминале.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(pythonOutput))
            {
                Console.WriteLine("Сообщение Python:");
                Console.WriteLine(pythonOutput.Trim());
            }

            if (exitCode != 0)
            {
                Console.WriteLine("Python завершился с ошибкой (exit code " + exitCode + ").");
            }

            try
            {
                ReadAndPrintResult(outputPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка чтения output.json: " + ex.Message);
            }
        }
    }

    static double[] ParseNumbers(string[] parts)
    {
        var list = new double[parts.Length];
        var ci = CultureInfo.InvariantCulture;

        for (int i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, ci, out double value))
                throw new Exception("Не число: '" + parts[i] + "'");
            list[i] = value;
        }

        if (list.Length == 0)
            throw new Exception("Не найдено ни одного числа.");

        return list;
    }

    static void WriteInputJson(string path, double[] numbers)
    {
        var obj = new
        {
            numbers = numbers
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(obj, options);
        File.WriteAllText(path, json);
    }

    static int RunPython(string script, string inputPath, string outputPath, out string stdoutAndStderr)
    {
        var psi = new ProcessStartInfo();
        psi.FileName = "python";
        psi.Arguments = script + " " + inputPath + " " + outputPath;
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;

        using var p = Process.Start(psi);
        if (p == null)
            throw new Exception("Process.Start вернул null.");

        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();

        stdoutAndStderr = (stdout + "\n" + stderr).Trim();
        return p.ExitCode;
    }

    static void ReadAndPrintResult(string outputPath)
    {
        if (!File.Exists(outputPath))
            throw new Exception("Файл не найден: " + outputPath);

        string json = File.ReadAllText(outputPath);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        bool ok = root.GetProperty("ok").GetBoolean();
        if (!ok)
        {
            string error = root.TryGetProperty("error", out var errEl) ? errEl.GetString() ?? "Неизвестная ошибка" : "Неизвестная ошибка";
            Console.WriteLine("Python вернул ошибку: " + error);
            return;
        }

        JsonElement stats = root.GetProperty("stats");

        Console.WriteLine();
        Console.WriteLine("Результат:");
        Console.WriteLine("Количество: " + stats.GetProperty("count").GetInt32());
        Console.WriteLine("Сумма:      " + stats.GetProperty("sum").GetDouble());
        Console.WriteLine("Среднее:    " + stats.GetProperty("avg").GetDouble());
        Console.WriteLine("Минимум:    " + stats.GetProperty("min").GetDouble());
        Console.WriteLine("Максимум:   " + stats.GetProperty("max").GetDouble());
    }
}

