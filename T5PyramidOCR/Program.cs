using System;
using AccountNumberValidator.Core;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        string sourceDirectory = "";
        string destinationDirectory = "";
        string processedSubfolder = "processad";
        string searchCodeOCR = "#11349;";
        string searchCodeFA = "#11304;";
        string searchClearing = "#12259;";
        string searchAccount = "#12260;";
        bool replaceFA = false;
        bool replaceOCR = false;
        bool validateBankAccount = false;

        Dictionary<string, string> parameters = new Dictionary<string, string>();
        foreach (string arg in args)
        {
            string[] parts = arg.Split(' ');
            foreach (string part in parts)
            {
                string[] keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].ToLower()] = keyValue[1];
                }
            }
        }

        if (parameters.ContainsKey("from")) sourceDirectory = parameters["from"];
        if (parameters.ContainsKey("to")) destinationDirectory = parameters["to"];
        if (parameters.ContainsKey("replacefa")) replaceFA = parameters["replacefa"] == "1";
        if (parameters.ContainsKey("replaceocr")) replaceOCR = parameters["replaceocr"] == "1";
        if (parameters.ContainsKey("validatebank")) validateBankAccount = parameters["validatebank"] == "1";

        if (string.IsNullOrEmpty(sourceDirectory) || string.IsNullOrEmpty(destinationDirectory))
        {
            PrintHelp();
            return;
        }

        Console.WriteLine($"Processing from: {sourceDirectory} to: {destinationDirectory}");
        Console.WriteLine($"Replace FA: {replaceFA}, Replace OCR: {replaceOCR}, Validate Bank: {validateBankAccount}");

        Directory.CreateDirectory(destinationDirectory);

        Directory.EnumerateFiles(sourceDirectory, "*.txt").ToList().ForEach(filePath =>
        {
            string content = File.ReadAllText(filePath, Encoding.GetEncoding(28591));

            string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            List<string> processedLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("01"))
                {
                    List<string> invoiceLines = new List<string>();
                    invoiceLines.Add(lines[i]);
                    i++;

                    while (i < lines.Length && !lines[i].StartsWith("01"))
                    {
                        invoiceLines.Add(lines[i]);
                        i++;
                    }
                    i--; // Adjust for the outer loop increment

                    bool overallValid = true;
                    string firstError = "";
                    for (int j = 0; j < invoiceLines.Count; j++)
                    {
                        if (replaceFA)
                        {
                            invoiceLines[j] = ReplaceFA(invoiceLines[j], searchCodeFA);
                        }
                        if (replaceOCR)
                        {
                            invoiceLines[j] = ReplaceOCR(invoiceLines[j], searchCodeOCR);
                        }
                        if (validateBankAccount && j < invoiceLines.Count - 1)
                        {
                            bool valid = true;
                            string error = "";
                            string validatedLine = ValidateBankAccount(invoiceLines[j], invoiceLines[j + 1], searchClearing, searchAccount, out valid, out error);
                            invoiceLines[j] = validatedLine.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];
                            invoiceLines[j + 1] = validatedLine.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[1];
                            j++; // Skip the next line as it has been processed
                            if (!valid)
                            {
                                overallValid = false;
                                if (string.IsNullOrEmpty(firstError))
                                {
                                    firstError = error;
                                }
                            }
                        }
                    }

                    if (overallValid)
                    {
                        processedLines.AddRange(invoiceLines);
                    }
                    else
                    {
                        string originalFileName = Path.GetFileNameWithoutExtension(filePath);
                        string invalidFileName = $"{originalFileName}_fel_{firstError.Replace(" ", "_")}.txt";
                        string invalidDirectory = Path.Combine(destinationDirectory, "fel");
                        string invalidFilePath = Path.Combine(invalidDirectory, invalidFileName);
                        string invalidContent = string.Join(Environment.NewLine, invoiceLines);

                        if (!Directory.Exists(invalidDirectory))
                        {
                            Directory.CreateDirectory(invalidDirectory);
                        }
                        File.WriteAllText(invalidFilePath, invalidContent, Encoding.GetEncoding(28591));
                    }
                }
                else
                {
                    processedLines.Add(lines[i]);
                }
            }

            string newContent = string.Join(Environment.NewLine, processedLines);
            string newFilePath = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
            File.WriteAllText(newFilePath, newContent, Encoding.GetEncoding(28591));

            // Move the processed file to the subfolder
            string processedDirectory = Path.Combine(sourceDirectory, processedSubfolder);
            if (!Directory.Exists(processedDirectory))
            {
                Directory.CreateDirectory(processedDirectory);
            }
            string processedFilePath = Path.Combine(processedDirectory, Path.GetFileName(filePath));
            File.Move(filePath, processedFilePath);
        });
    }

    static string ReplaceFA(string line, string searchCodeFA)
    {
        if (line.Contains(searchCodeFA))
        {
            int index = line.IndexOf(searchCodeFA) + searchCodeFA.Length;
            string faNr = line.Substring(index).Replace("-", "");
            string len = (faNr.Length + 2).ToString();
            if (len.Length > 1)
                len = len.Substring(1);
            var kk = LuhnDotNet.Luhn.ComputeLuhnCheckDigit(faNr + len);
            string fa = faNr + len + kk;
            line = line.Substring(0, index) + fa; // replacementCode;
        }
        return line;
    }

    static string ReplaceOCR(string line, string searchCodeOCR)
    {
        if (line.Contains(searchCodeOCR))
        {
            int index = line.IndexOf(searchCodeOCR) + searchCodeOCR.Length;
            string faNr = line.Substring(index).Replace("-", "");
            string len = (faNr.Length + 2).ToString();
            if (len.Length > 1)
                len = len.Substring(1);
            var kk = LuhnDotNet.Luhn.ComputeLuhnCheckDigit(faNr + len);
            string ocr = faNr + len + kk;
            line = line.Substring(0, index) + ocr; // replacementCode;
        }
        return line;
    }

    static string ValidateBankAccount(string line, string line2, string searchClearing, string searchAccount, out bool valid, out string error)
    {
        valid = true;
        error = "";

        if (line.Contains(searchClearing) || line2.Contains(searchAccount))
        {
            int index = line.IndexOf(searchClearing) + searchClearing.Length;
            string clearing = line.Substring(index).Replace("-", "");

            index = line2.IndexOf(searchAccount) + searchAccount.Length;
            string account = line2.Substring(index).Replace("-", "");

            string digits = clearing + account;
            try
            {
                var bankAccount = BankAccount.Parse(digits);
                bankAccount = BankAccount.ExtraCheck(bankAccount);
                if (bankAccount.Valid)
                {
                    line = line.Substring(0, line.IndexOf(searchClearing) + searchClearing.Length) + bankAccount.SortingCode;
                    line2 = line2.Substring(0, line2.IndexOf(searchAccount) + searchAccount.Length) + bankAccount.AccountNumber;
                }
                else
                {
                    error = bankAccount.Note;
                    valid = false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                valid = false;
            }
        }

        return line + Environment.NewLine + line2;
    }

    static void PrintHelp()
    {
        Console.WriteLine("Usage: T5PyramidOCR [from=<sourceDirectory>] [to=<destinationDirectory>] [replaceFA=0|1] [replaceOCR=0|1] [validateBank=0|1]");
        Console.WriteLine();
        Console.WriteLine("Parameters:");
        Console.WriteLine("  from=<sourceDirectory>     Path to the source directory");
        Console.WriteLine("  to=<destinationDirectory>   Path to the destination directory");
        Console.WriteLine("  replaceFA=0|1              Replace OCR on invoice (0 = No, 1 = Yes)");
        Console.WriteLine("  replaceOCR=0|1             Replace OCR on OCR (0 = No, 1 = Yes)");
        Console.WriteLine("  validateBank=0|1           Perform bank account validation (0 = No, 1 = Yes)");
    }
}
