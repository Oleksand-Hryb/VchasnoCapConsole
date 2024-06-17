using System;
using System.IO;
using System.Threading.Tasks;
using VchasnoCapConsole.VchasnoCapClient;

namespace VchasnoCapConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("usage: VchasnoCapConsole.exe clientId pathToFile");
                Console.ReadKey();
                return;
            }

            SignTestAsync(args[0], args[1]).Wait();
        }

        public static async Task SignTestAsync(string clientId, string path)
        {
            try
            {
                Console.Write("SIGNING...");

                var vchasnoClient = new VchasnoApiClient(clientId);

                var dataBytes = File.ReadAllBytes(path);
                var result = await vchasnoClient.SignAsync(dataBytes, Path.GetFileName(path));

                if (result.IsSuccessful)
                {
                    Console.Write("SIGNED!");
                    File.WriteAllBytes($"{path}.p7s", result.Value);
                    Console.ReadKey();
                }
                else
                {
                    Console.Write("ERROR:");
                    Console.WriteLine(result.StatusMessage);
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Console.Write("ERROR:");
                Console.WriteLine(e.Message);
                Console.ReadKey();
            }
        }
    }
}