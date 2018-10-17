using System;
using System.Linq;
using System.Reflection;

using SteamKit2.GC.Dota.Internal; 

namespace DotaMatchRequest
{
    class Program
    {
        static void Main(string[] args)
        {


            Console.WriteLine("Username:");
            string username = Console.ReadLine();

            Console.WriteLine("Password:");
            string password = Console.ReadLine();

            uint matchId;
            if (!uint.TryParse(args[2], out matchId))
            {
                Console.WriteLine("Invalid Match ID!");
                return;
            }

            DotaClient client = new DotaClient(username, password, matchId);


            client.Connect();


            client.Wait();

            PrintMatchDetails(client.Match);
        }


        static void PrintMatchDetails(CMsgDOTAMatch match)
        {
            if (match == null)
            {
                Console.WriteLine("No match details to display");
                return;
            }

            var fields = typeof(CMsgDOTAMatch).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields.OrderBy(f => f.Name))
            {
                var value = field.GetValue(match, null);

                Console.WriteLine("{0}: {1}", field.Name, value);
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("DotaMatchRequest <username> <password> <matchid>");
        }
    }
}
