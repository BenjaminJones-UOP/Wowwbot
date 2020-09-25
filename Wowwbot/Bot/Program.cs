using System;
using TwitchLib;
using TwitchLib.Client;

namespace Wowwbot
{
    class Program
    {
        static void Main(string[] args)
        {
            TwitchChatBot bot = new TwitchChatBot();
            bot.Connect();

            try
            {
                while (true)
                {
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch { bot.Disconnect(); }
        }
    }
}
