using System;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: T5PyramidOCR <sourceDirectory> <destinationDirectory> [rePlaceFa]=0");
            return;
        }

        string sourceDirectory = args[0];
        string destinationDirectory = args[1];
        //string searchCodeOCR = args[2];
        string searchCodeOCR = "#11349;";
        string searchCodeFA = "#11304;";
        bool replaceFA = false;
        if (args.Length > 2)
        {
            replaceFA = args[2] == "1" ? true :false ;
        }

        Directory.CreateDirectory(destinationDirectory);

        Directory.EnumerateFiles(sourceDirectory, "*.txt").ToList().ForEach(filePath =>
        {
            string content = File.ReadAllText(filePath, Encoding.GetEncoding(28591));
            
            string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(searchCodeOCR))
                {
                    int index = lines[i].IndexOf(searchCodeOCR) + searchCodeOCR.Length;
                    string faNr = lines[i].Substring(index).Replace("-", "");
                    string len = (faNr.Length + 2).ToString();
                    if (len.Length > 1)
                        len = len.Substring(1);
                    var kk = LuhnDotNet.Luhn.ComputeLuhnCheckDigit(faNr + len);
                    string ocr = faNr + len + kk;
                    lines[i] = lines[i].Substring(0, index) + ocr; // replacementCode;
                }
                if (lines[i].Contains(searchCodeFA))
                {
                    if (replaceFA)
                    {
                        int index = lines[i].IndexOf(searchCodeFA) + searchCodeFA.Length;
                        string faNr = lines[i].Substring(index).Replace("-", "");
                        string len = (faNr.Length + 2).ToString();
                        if (len.Length > 1)
                            len = len.Substring(1);
                        var kk = LuhnDotNet.Luhn.ComputeLuhnCheckDigit(faNr + len);
                        string fa = faNr + len + kk;
                        lines[i] = lines[i].Substring(0, index) + fa; // replacementCode;
                    }
                }
            }
            string newContent = string.Join(Environment.NewLine, lines);
            string newFilePath = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
            File.WriteAllText(newFilePath, newContent, Encoding.GetEncoding(28591));

        });
    }
}