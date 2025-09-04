using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoundryOcr;

namespace FoundryOcr.Cli;

internal static class Program
{
    [STAThread]
    static async Task<int> Main(string[] args)
    {
        if (args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase)) || args.Length == 0)
        {
            PrintUsage();
            return 0;
        }

        bool useStdin = args.Contains("--stdin", StringComparer.OrdinalIgnoreCase);
        bool pretty = args.Contains("--pretty", StringComparer.OrdinalIgnoreCase);
        bool isBase64 = args.Contains("--base64", StringComparer.OrdinalIgnoreCase);

        string? pathArg = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
        string? outFile = GetOptionValue(args, "--out");
        string? langCode = GetOptionValue(args, "--lang"); // reserved for future use

        if (!isBase64 && !useStdin && string.IsNullOrWhiteSpace(pathArg))
        {
            Console.Error.WriteLine("Missing image path.");
            PrintUsage();
            return 2;
        }

        if (isBase64 && !useStdin)
        {
            pathArg = GetOptionValue(args, "--base64");
            if (string.IsNullOrWhiteSpace(pathArg))
            {
                Console.Error.WriteLine("Missing base64 string after --base64.");
                return 2;
            }
        }

        if (!string.IsNullOrWhiteSpace(pathArg) && (useStdin && isBase64))
        {
            Console.Error.WriteLine("When using --stdin --base64, do not also pass a file path.");
            return 2;
        }

        try
        {
            string json;

            if (isBase64)
            {
                string base64 = useStdin
                    ? await new StreamReader(Console.OpenStandardInput(), Encoding.UTF8).ReadToEndAsync()
                    : pathArg!;

                byte[] bytes = DecodeBase64(base64);
                json = await OcrService.RecognizeAsJsonFromBytesAsync(bytes, indented: pretty);
            }
            else if (useStdin)
            {
                using var stdin = Console.OpenStandardInput();
                json = await OcrService.RecognizeAsJsonFromStreamAsync(stdin, indented: pretty);
            }
            else
            {
                json = await OcrService.RecognizeAsJsonAsync(pathArg!, indented: pretty);
            }

            if (!string.IsNullOrWhiteSpace(outFile))
                await File.WriteAllTextAsync(outFile, json);
            else
                Console.Out.WriteLine(json);

            return 0;
        }
        catch (FormatException fex)
        {
            Console.Error.WriteLine("Base64 decode failed: " + fex.Message);
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static string? GetOptionValue(string[] args, string optionName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    return args[i + 1];
                return null;
            }
        }
        return null;
    }

    private static byte[] DecodeBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new FormatException("Empty base64 string.");

        base64 = base64.Trim();
        return Convert.FromBase64String(base64);
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
@"Usage:
  FoundryOcr.exe <imagePath> [--pretty] [--out <file>] [--lang <code>]
  FoundryOcr.exe --stdin [--pretty] [--out <file>] [--lang <code>]
  FoundryOcr.exe --base64 <base64String> [--pretty] [--out <file>] [--lang <code>]
  FoundryOcr.exe --stdin --base64 [--pretty] [--out <file>] [--lang <code>]
  FoundryOcr.exe --help");
    }
}
