using System;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Runtime.InteropServices;
using System.Threading;

namespace DeleteDuplicatesSurveyTexts
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var menu = new Menu();
                menu.Render();
            }
            catch (Exception e)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
                Console.ForegroundColor = color;
                throw;
            }
        }
    }
}
