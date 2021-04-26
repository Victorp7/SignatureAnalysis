using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SignatureAnalysis
{
    class Analyze
    {

        static readonly Byte[] jpg = new byte[] { 0xFF, 0xD8 };
        static readonly Byte[] pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        static void Main(string[] args)
        {
            Console.Title = "Signature Analysis";
            AnalyzeDir();
        }

        static void AnalyzeDir()
        {
            string srcDir, outFile;
            bool incSubDir = false;
            List<string> errorList = new List<string>();
            try
            {
                //Loop to ask for Directory until a valid input is provided
                while (true)
                {
                    Console.WriteLine("Path of the directory to be analyzed:");
                    srcDir = Console.ReadLine();
                    if (Directory.Exists(srcDir))
                        break;
                    else
                        Console.WriteLine("Directory is invalid.");
                }

                //Loop to ask for output file until a valid input is provided
                while (true)
                {
                    Console.WriteLine("Path for the output file (include file name and extension, i.e. C:\\test.csv):");

                    outFile = Console.ReadLine();
                    //Validate if the directory exists and valid name is provided
                    if (string.IsNullOrWhiteSpace(Path.GetFileName(outFile)) || !Path.GetExtension(outFile).Equals(".csv", StringComparison.InvariantCultureIgnoreCase) || !Directory.Exists(Path.GetDirectoryName(outFile)))
                    {
                        Console.WriteLine("Path for output file is invalid.");
                        continue;
                    }

                    if (!File.Exists(outFile))
                        break;
                    //In case the file already exists, ask to the user if want to replace it or go back and chage the path 
                    if (GetFlagResponse("Output file already exists. Do you want to replace it?"))
                        break;
                }

                //Flag to know if subdirectories should be included or not
                incSubDir = GetFlagResponse("Do you want to include subdirectories?");

                List<FileDetails> fDtlList = new List<FileDetails>();
                DirectoryInfo d = new DirectoryInfo(srcDir);

                //Process all the files in the directory and subdirectories depending of the flag
                string[] files = Directory.GetFiles(srcDir, "*.*", incSubDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (string f in files)
                {
                    CalculateMD5(f, fDtlList, errorList);
                }

                //Write the file details list into the csv
                if (fDtlList.Count > 0)
                {
                    CsvConfiguration config = new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ";", HasHeaderRecord = true };
                    using (var mem = new MemoryStream())
                    using (var writer = new StreamWriter(mem))
                    using (var csvWriter = new CsvWriter(writer, config))
                    {

                        csvWriter.WriteHeader<FileDetails>();
                        csvWriter.NextRecord();
                        csvWriter.WriteRecords(fDtlList);

                        writer.Flush();
                        var result = Encoding.UTF8.GetString(mem.ToArray());
                        File.WriteAllText(outFile, result, Encoding.Default);
                    }
                }

                Console.WriteLine($"Directory { srcDir  } has been analyzed and { fDtlList.Count } files have been found.");
                if (errorList.Count > 0)
                {
                    Console.WriteLine("Error(s) ocurred, please check below: ");
                    Console.WriteLine(string.Join(System.Environment.NewLine, errorList));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An error has occurred: " + ex.Message);
            }
            finally
            {
                if (GetFlagResponse("Do you want to analyze another directory?"))
                    AnalyzeDir();
            }
        }

        private class FileDetails
        {
            public string Path { get; set; }
            public string Type { get; set; }
            public string Hash { get; set; }
        }

        /// <summary>
        /// Method to obtain the MD5 hash algorithm for JPG and PDF files and add the info to the list of fileDetails.
        /// </summary>
        /// <param name="filePath">Path of the File to anlayse</param>
        /// <param name="fileDetails">List of Details for JPG and PDF files</param>
        static void CalculateMD5(string fPath, List<FileDetails> fDetList, List<string> errorList)
        {
            using (var md5 = MD5.Create())
            {
                try
                {
                    using (var fStream = File.OpenRead(fPath))
                    {

                        Byte[] b = new byte[4];
                        fStream.Read(b, 0, 4);
                        if (jpg.SequenceEqual(b.Take(jpg.Length)))
                            fDetList.Add(new FileDetails { Path = fPath, Type = "JPG", Hash = BitConverter.ToString(md5.ComputeHash(fStream)) });
                        if (pdf.SequenceEqual(b.Take(pdf.Length)))
                            fDetList.Add(new FileDetails { Path = fPath, Type = "PDF", Hash = BitConverter.ToString(md5.ComputeHash(fStream)) });
                    }
                }
                catch (Exception ex)
                {
                    errorList.Add(ex.Message);
                }
            }
        }

        /// <summary>
        /// Method used for get a response to Yes or No questions
        /// </summary>
        /// <param name="message">Message will be displayed to the user</param>
        /// <returns></returns>
        static bool GetFlagResponse(string message)
        {
            ConsoleKey response;
            //Loop to validate the user type the right answer (Y or N Key)
            do
            {
                Console.Write($"{ message } [Y/N] ");
                if ((response = Console.ReadKey(false).Key) != ConsoleKey.Enter)
                    Console.WriteLine();
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return (response == ConsoleKey.Y);
        }

    }
}
