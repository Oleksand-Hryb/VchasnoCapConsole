using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VchasnoCapConsole.Data;
using VchasnoCapConsole.VchasnoCap;

namespace VchasnoCapConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                PrintUsage();
                return;
            }

            switch (args[0])
            {
                case "single":
                    if (args.Length != 3)
                    {
                        PrintUsage();
                        return;
                    }
                    SignTestSingleAsync(args[1], args[2]).Wait();
                    break;

                case "extended":
                    if (args.Length < 3)
                    {
                        PrintUsage();
                        return;
                    }
                    var paths1 = args.Select((e, i) => (e, i)).Where(a => a.i > 1).Select(a => a.e).ToArray();
                    SignTestExtendedAsync(args[1], paths1).Wait();
                    break;

                case "session":
                    if (args.Length < 5)
                    {
                        PrintUsage();
                        return;
                    }
                    var paths2 = args.Select((e, i) => (e, i)).Where(a => a.i > 3).Select(a => a.e).ToArray();
                    SignTestSessionAsync(args[1], args[2], args[3], paths2).Wait();
                    break;
                default:
                    PrintUsage();
                    break;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("usage:");
            Console.WriteLine("  1. Sign single file without session");
            Console.WriteLine("  VchasnoCapConsole.exe single clientId filePathToSign");
            Console.WriteLine("  2. Sign list of files without session");
            Console.WriteLine("  VchasnoCapConsole.exe extended clientId [filePathToSign..]");
            Console.WriteLine("  3. Sign list of files with session");
            Console.WriteLine("  VchasnoCapConsole.exe session apikey clientId password [filePathToSign..]");
            Console.ReadKey();
        }

        static async Task SignTestSingleAsync(string clientId, string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"File was not found: {path}.");
                    Console.WriteLine($"Operation cancelled.");
                    return;
                }

                var dataBytes = File.ReadAllBytes(path);
                var request = new SignContentRequest { ContentToSign = dataBytes, Description = Path.GetFileName(path) };

                Console.WriteLine($"Content to sign: {request.ToLogString(addContent: false)}");

                Console.WriteLine("Signing... Please confirm in application!");
                var vchasnoClient = new VchasnoApiClient(clientId);
                var result = await vchasnoClient.SignAsync(request);

                Console.WriteLine($"Sign result: {result.ToLogString(addSignedData: false)}");

                if (result.IsSuccess)
                {
                    var pathToSave = $"{path}.p7s";
                    Console.Write($"Result saved to file: {pathToSave}");
                    File.WriteAllBytes(pathToSave, result.SignedData);
                }
            }
            catch (Exception e)
            {
                Console.Write("ERROR:");
                Console.WriteLine(e.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        static async Task SignTestExtendedAsync(string clientId, params string[] paths)
        {
            try
            {
                var requests = new List<SignContentRequest>();
                var filesDictionary = new Dictionary<int, string>();
                foreach(var path in paths)
                {
                    if (!File.Exists(path))
                    {
                        Console.WriteLine($"File was not found: {path}.");
                    }
                    else
                    {
                        filesDictionary[requests.Count] = path;
                        var dataBytes = File.ReadAllBytes(path);
                        var request = new SignContentRequest { ContentToSign = dataBytes, Description = Path.GetFileName(path) };
                        requests.Add(request);
                    }
                }

                if (requests.Count == 0)
                {
                    Console.WriteLine("No files to process. Operation cancelled.");
                }
                
                Console.WriteLine($"Contents to sign:");
                foreach(var request in requests)
                {
                    Console.WriteLine($"   {request.ToLogString(addContent: false)}");
                }

                Console.WriteLine("Signing... Please confirm in application!");

                var vchasnoClient = new VchasnoApiClient(clientId);
                var results = await vchasnoClient.SignAsync(requests.ToArray());
                Console.WriteLine($"Sign results:");

                for (var i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    Console.WriteLine($"   {result.ToLogString(addSignedData: false)}");
                    if (result.IsSuccess)
                    {
                        var pathToSave = $"{filesDictionary[i]}.p7s";
                        Console.Write($"   Result saved to file: {pathToSave}");
                        File.WriteAllBytes(pathToSave, result.SignedData);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("ERROR:");
                Console.WriteLine(e.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }

        static async Task SignTestSessionAsync(string apikey, string clientId, string password, params string[] paths)
        {
            try
            {
                var requests = new List<SignContentRequest>();
                var filesDictionary = new Dictionary<int, string>();
                foreach (var path in paths)
                {
                    if (!File.Exists(path))
                    {
                        Console.WriteLine($"File was not found: {path}.");
                    }
                    else
                    {
                        filesDictionary[requests.Count] = path;
                        var dataBytes = File.ReadAllBytes(path);
                        var request = new SignContentRequest { ContentToSign = dataBytes, Description = Path.GetFileName(path) };
                        requests.Add(request);
                    }
                }

                if (requests.Count == 0)
                {
                    Console.WriteLine($"Operation cancelled.");
                }

                Console.WriteLine($"Contents to sign:");
                foreach (var request in requests)
                {
                    Console.WriteLine($"   {request.ToLogString(addContent: false)}");
                }

                Console.WriteLine("Creating session... Please confirm in application!");

                var vchasnoClient = new VchasnoApiClientWithSession(apikey, clientId, password);
                var authResult = await vchasnoClient.AuthorizeAsync();
                if (!authResult.IsSuccessful)
                {
                    Console.WriteLine($"Auth error: {authResult.StatusMessage}");
                    return;
                }

                Console.WriteLine("Singing...");

                var results = await vchasnoClient.SignAsync(requests.ToArray());

                Console.WriteLine($"Sign results:");

                for (var i = 0; i < results.Length; i++)
                {
                    var result = results[i];
                    Console.WriteLine($"   {result.ToLogString(addSignedData: false)}");
                    if (result.IsSuccess)
                    {
                        var pathToSave = $"{filesDictionary[i]}.p7s";
                        Console.Write($"   Result saved to file: {pathToSave}");
                        File.WriteAllBytes(pathToSave, result.SignedData);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("ERROR:");
                Console.WriteLine(e.Message);
            }
            finally
            {
                Console.ReadKey();
            }
        }
    }
}