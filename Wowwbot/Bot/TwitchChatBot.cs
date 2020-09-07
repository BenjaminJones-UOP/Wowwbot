using System;
using System.Timers;
using System.IO;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Client.Enums;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace Wowwbot
{
    internal class TwitchChatBot 
    {
        GameManager game_manager;

        const int social_timer_minutes = 25;
        const int schedule_timer_minutes = 45;
        const int boss_stats_timer_minutes = 10;

        int wowwyyKcount = 0;
        string path = @"..\\..\\wowwyyKcount.txt";

        readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        public static TwitchClient client = new TwitchClient();
        //private static TwitchPubSub pubsub = new TwitchPubSub();
        string[] bannedWords = { "viewerlabs.com", "streambot.com", "feedpixel.com", "views.run", "phantom.bot", "stream-chaos.com", "views4twitch.com", "viewbotr.com", "addviewerz.com", "bigfollows.com" };
        string[] timedoutWords = { "nigger", "nigga", "niga", "n1gger", "n1gga", "n1gg3r", "nigg3r"};
        Timer social_timer;
        Timer schedule_timer;
        Timer minigame_timer;
        
        internal void Connect()
        {
            client.Initialize(credentials, TwitchInfo.ChannelName);
            social_timer = new Timer(minutes_to_milliseconds(social_timer_minutes));
            social_timer.Start();
            schedule_timer = new Timer(minutes_to_milliseconds(schedule_timer_minutes));
            schedule_timer.Start();
            minigame_timer = new Timer(minutes_to_milliseconds(boss_stats_timer_minutes));
            minigame_timer.Start();

            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += Client_OnReSubscriber;
            client.OnGiftedSubscription += Client_OnGiftedSubscription;
            client.OnUserBanned += Client_OnUserBanned;
            client.OnUserTimedout += Client_OnUserTimedout;
            social_timer.Elapsed += RepostSocials;
            schedule_timer.Elapsed += RepostSchedule;
            minigame_timer.Elapsed += RepostMinigameStats;

            //pubsub.Connect();
            //pubsub.OnLog += PubSub_OnLog;
            //pubsub.OnPubSubServiceError += PubSub_OnPubSubServiceError;
            //pubsub.OnPubSubServiceConnected += PubSub_OnPubSubServiceConnected;
            //pubsub.OnListenResponse += PubSub_OnListenResponse;
            //pubsub.OnStreamUp += PubSub_OnStreamUp;
            //pubsub.OnRewardRedeemed += PubSub_OnRewardRedeemed;
            //pubsub.ListenToVideoPlayback(TwitchInfo.ChannelName);

            StreamReader countfile = new StreamReader(path);
            wowwyyKcount = int.Parse(countfile.ReadLine());
            countfile.Close();

            game_manager = new GameManager();

            client.Connect();
        }

        internal void Disconnect()
        {
            social_timer.Stop();
            schedule_timer.Stop();
        }

        //private void PubSub_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        //{
        //    Console.WriteLine(e.Exception.ToString());
        //}
        //private void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        //{
        //    pubsub.SendTopics(TwitchInfo.BotToken);
        //}
        //private void PubSub_OnListenResponse(object sender, OnListenResponseArgs e)
        //{
        //    if (!e.Successful)
        //        throw new Exception($"Failed to listen! Response: {e.Response}");
        //}
        //private void PubSub_OnStreamUp(object sender, OnStreamUpArgs e)
        //{
        //    client.SendMessage(TwitchInfo.ChannelName, $"/me Wowwyy just went live!");
        //}
        //private void PubSub_OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        //{
        //    client.SendMessage(TwitchInfo.ChannelName, $"/me reward redeemed!?");
        //}

        public void RepostSocials(object sender, EventArgs e)
        {
            if(client.IsConnected)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Check out his twitter and make him use it more! https://twitter.com/wowwyytv Also has Instagram! https://www.instagram.com/wowwyytv All past broadcasts are available on YouTube https://www.youtube.com/channel/UCI9l7ije-iBVkmQ7BFHHKCg ");
            }
        }
        public void RepostSchedule(object sender, EventArgs e)
        {
            if (client.IsConnected)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Wowwyy's schedule is every Thursday and Friday 4pm GMT+1 (UK). Unscheduled streams may occur so please follow to catch these as well!");
            }
        }
        public void RepostMinigameStats(object sender, EventArgs e)
        {
            PostMinigameMsg();
        }

        //On message received
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            string roulette_reward = e.ChatMessage.CustomRewardId;
            //wowwyyK count logic
            if (e.ChatMessage.Message.Contains("wowwyyK"))
            {
                string temp = e.ChatMessage.Message;
                while(temp.IndexOf("wowwyyK")!=-1) //while wowwyyK is present, count it and then remove it
                {
                    wowwyyKcount++;
                    temp = temp.Remove(temp.IndexOf("wowwyyK"), 7);
                }
                File.WriteAllText(path, wowwyyKcount.ToString());
            }
            //Show list of commands
            if (e.ChatMessage.Message.Equals("!wowcommands"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me My current commands are:   !socials // !schedule // !wowwyyKcount // !wowcourt // !chance // !bin // !games // !gameinfo");
            }
            //Post wowwyyK count command
            if (e.ChatMessage.Message.Equals("!wowwyyKcount"))
            {
                client.SendMessage(TwitchInfo.ChannelName,  $"/me wowwyyK {wowwyyKcount}");
            }
            //Socials command
            if (e.ChatMessage.Message.Equals("!socials"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Check out his twitter and make him use it more! https://twitter.com/wowwyytv Also has Instagram! https://www.instagram.com/wowwyytv All past broadcasts are available on YouTube https://www.youtube.com/channel/UCI9l7ije-iBVkmQ7BFHHKCg ");
            }
            //Schedule command
            if (e.ChatMessage.Message.Equals("!schedule"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Wowwyy's schedule is every Thursday and Friday 4pm GMT+1 (UK). Unscheduled streams may occur so please follow to catch these as well!");
            }
            //Wowcourt command
            if (e.ChatMessage.Message.Contains("!wowcourt"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Mods have the power to judge you so be careful what you say. The judgement command, only issued by mods, will see the perpetrator face a 50/50 chance of a 10 minute timeout.");
            }
            //Bin command
            if (e.ChatMessage.Message.StartsWith("!bin"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me wowwyyT https://images-na.ssl-images-amazon.com/images/I/51w7Dz66ncL._AC_SL1000_.jpg wowwyyT");
            }
            //Minigames command
            if (e.ChatMessage.Message.Equals("!games"))
            {
                PostMinigameMsg();
            }
            //Games info command
            if (e.ChatMessage.Message.Equals("!gameinfo"))
            {
                client.SendMessage(TwitchInfo.ChannelName, game_manager.GameInfo);
            }
            //Player play minigame command
            if (e.ChatMessage.Message.Equals("!play"))
            {
                game_manager.Play(e.ChatMessage.Username);
            }
            //Roulette command
            if (e.ChatMessage.Message.Equals("!chance"))
            {
                if (e.ChatMessage.Username == "breadaddiction")
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Daddy Bread is always a winner <3 ");
                }
                else if (e.ChatMessage.Username == "ambiience")
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me How's your dad she alright ");
                }
                else if (e.ChatMessage.Username == "clumsyturtle")
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Clumsy's the mum wowwyyS");
                }
                else
                {
                    game_manager.ChanceGame(e.ChatMessage.Username);
                }
            }
            //Moderator Commands
            if (e.ChatMessage.IsModerator || e.ChatMessage.IsBroadcaster)
            {
                //Judgment
                if (e.ChatMessage.Message.StartsWith("!judgement @"))
                {
                    Random chance = new Random();
                    int chance_50 = chance.Next(1, 2);

                    string username = e.ChatMessage.Message.Remove(0, 12);
                    client.SendMessage(TwitchInfo.ChannelName, $"/me {username}: 3");
                    client.SendMessage(TwitchInfo.ChannelName, $"/me {username}: 2");
                    client.SendMessage(TwitchInfo.ChannelName, $"/me {username}: 1");
                    client.SendMessage(TwitchInfo.ChannelName, $"/me {username}: Verdict is:");

                    if (chance_50 == 0)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me {username}: NOT GUILTY!");
                    }
                    else if (chance_50 == 1)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me GUILTY! Enjoy your new home wowwyyT https://ensia.com/wp-content/uploads/2016/10/feature_landfill_methane_main-760x378.jpg wowwyyT");
                        client.SendMessage(TwitchInfo.ChannelName, $"/timeout {username} 600");
                    }
                }
                //Start minigame command
                if (e.ChatMessage.Message.StartsWith("!start"))
                {
                    game_manager.Start(client);
                }
                //Stop minigame command
                if (e.ChatMessage.Message.Equals("!stop"))
                {
                    game_manager.Stop();
                }
            }
            //When channel rewards come into it
            //Currently doing this manually
            //string channelptsBanWord = "dkfbasdfbjlabsdflajsbd";
            //bool active = true;
            //if (e.ChatMessage.Message.Contains(channelptsBanWord) && active == true)
            //{
            //    client.SendMessage(TwitchInfo.ChannelName, $"/timeout {e.ChatMessage.Username} 1");
            //    active = false;
            //}

            //Only route here when not mod or broadcaster - for extra "safety"
            if (!e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                //Banned words - currently for viewbot linkers
                foreach (string bannedWord in bannedWords)
                {
                    if (e.ChatMessage.Message.Contains(bannedWord))
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/ban {e.ChatMessage.Username} <- I'm the only bot around here");
                    }
                }
                //Timedout words - racism etc.
                foreach (string timedoutWord in timedoutWords)
                {
                    if (e.ChatMessage.Message.Contains(timedoutWord))
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/timeout {e.ChatMessage.Username} 600  <- No naughty words pls, 10 min in the bin");
                    }
                }
            }
        }

        //On new sub
        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
                client.SendMessage(e.Channel, $"/me Welcome {e.Subscriber.DisplayName} to the Brotherhood! Thank for using your twitch prime sub.");
            else
                client.SendMessage(e.Channel, $"/me Welcome {e.Subscriber.DisplayName} to the Brotherhood! Thanks for subscribing.");
        }

        //On resub
        private void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            client.SendMessage(e.Channel, $"/me Thank you {e.ReSubscriber.DisplayName} for your resub. Welcome back to the Brotherhood! Your sub streak is {e.ReSubscriber.MsgParamCumulativeMonths} months");
        }

        //On gifted sub
        private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            client.SendMessage(e.Channel, $"/me Thank you {e.GiftedSubscription.DisplayName} for gifting a sub to {e.GiftedSubscription.MsgParamRecipientDisplayName}! Welcome to the Brotherhood!");
        }

        //On user timed out
        private void Client_OnUserTimedout(object sender, OnUserTimedoutArgs e)
        {
            TimeSpan timeout_time = TimeSpan.FromSeconds(e.UserTimeout.TimeoutDuration);
            client.SendMessage(e.UserTimeout.Channel, $"/me {e.UserTimeout.Username} We'll leave the lid open for ya mate wowwyyT {timeout_time.Minutes}m{timeout_time.Seconds}s in the bin");
        }

        //On user banned
        private void Client_OnUserBanned(object sender, OnUserBannedArgs e)
        {
            client.SendMessage(e.UserBan.Channel, $"/me {e.UserBan.Username} Nah we shutting the lid on this one wowwyyT {e.UserBan.BanReason}");
        }

        //Logging for client
        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }

        //Connection error print to console
        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"Connection Error: {e.Error}");
        }

        //private void PubSub_OnLog(object sender, TwitchLib.PubSub.Events.OnLogArgs e)
        //{
        //    Console.WriteLine(e.Data);
        //}

        private void PostMinigameMsg()
        {
            if (game_manager.MiniGameStarted)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me This bot hosts minigames wowwyyPog {game_manager.CurrentMinigameStatusMessage}  Use the command !play to join.");
            }
            else
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me This bot hosts minigames wowwyyPog There is currently no game active. Ask either a mod or Wowwyy to get one started wowwyyPog");
            }
        }
        private double minutes_to_milliseconds(double minutes)
        {
            double milliseconds = minutes * 60000;
            return milliseconds;
        }
    }
}