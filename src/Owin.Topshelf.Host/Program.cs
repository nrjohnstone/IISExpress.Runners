using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using TestWebApp.Owin.Startup;

namespace Owin.Topshelf.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:10008/";

            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine($"Running on {baseAddress}");
                Console.WriteLine("Hit enter to exit");
                Console.ReadLine();
            }
        }
    }
}
