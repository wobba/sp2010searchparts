using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KQLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            string query = @"author:mikael";
            FqlHelper helper = new FqlHelper();
            string result = helper.GetFqlFromKql(query);
            Console.WriteLine(result);
        }
    }
}
