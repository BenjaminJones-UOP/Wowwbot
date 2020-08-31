﻿using System;
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
        enum BossGameType
        {
            TotalDmgDealt,
            LastHit,
            End
        };
        static BossGameType boss_game_type;
        string[] boss_game_type_string = {"Deal the most damage!", "Last hit the boss!" };
        const int boss_health = 4000;
        const int boss_health_dodge_threshold = 750;
        const int boss_dodge_chance = 50;
        const int player_attack_min = -5;
        const int player_attack_max = 200;

        const int max_sub_month = 11;
        const int social_timer_minutes = 25;
        const int schedule_timer_minutes = 45;
        const int boss_stats_timer_minutes = 10;
        const int max_timeout = 300;
        const int min_timeout = 10;
        TimeSpan attack_cooldown = new TimeSpan(0, 5, 0); //Player's attack cooldown hh:mm:ss format
        int wowwyyKcount = 0;
        string path = @"..\\..\\wowwyyKcount.txt";

        readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        private static TwitchClient client = new TwitchClient();
        //private static TwitchPubSub pubsub = new TwitchPubSub();
        string[] sub_titles = { "Private", "Private 2nd class", "Private 1st class", "Army Specialist", "Corporal", "Sergaent", "Staff Sergaent", "Sergaent 1st class", "Master Sergaent", "The 1st Sergaent", "Command Sergaent Major", "The Sergaent Major of the Army" };
        string[] bannedWords = { "viewerlabs.com", "streambot.com", "feedpixel.com", "views.run", "phantom.bot", "stream-chaos.com", "views4twitch.com", "viewbotr.com", "addviewerz.com", "bigfollows.com" };
        string[] timedoutWords = { "nigger", "nigga", "niga", "n1gger", "n1gga", "n1gg3r", "nigg3r"};
        Timer social_timer;
        Timer schedule_timer;
        Timer boss_stats_timer;
        List<Player> player_list;
        Boss stream_boss;
        
        internal void Connect()
        {
            client.Initialize(credentials, TwitchInfo.ChannelName);
            social_timer = new Timer(minutes_to_milliseconds(social_timer_minutes));
            social_timer.Start();
            schedule_timer = new Timer(minutes_to_milliseconds(schedule_timer_minutes));
            schedule_timer.Start();
            boss_stats_timer = new Timer(minutes_to_milliseconds(boss_stats_timer_minutes));
            boss_stats_timer.Start();

            //client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += Client_OnReSubscriber;
            client.OnGiftedSubscription += Client_OnGiftedSubscription;
            client.OnUserBanned += Client_OnUserBanned;
            client.OnUserTimedout += Client_OnUserTimedout;
            social_timer.Elapsed += RepostSocials;
            schedule_timer.Elapsed += RepostSchedule;
            boss_stats_timer.Elapsed += RepostBossStats;

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

            player_list = new List<Player>();

            boss_game_type = BossGameType.End;

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
        //    client.SendMessage(TwitchInfo.ChannelName, $"/me WowwyyTV just went live!");
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
                client.SendMessage(TwitchInfo.ChannelName, $"/me WowwyyTV's schedule is every Thursday and Friday 4pm GMT+1 (UK). Unscheduled streams may occur so please follow to catch these as well!");
            }
        }
        public void RepostBossStats(object sender, EventArgs e)
        {
            if (client.IsConnected && stream_boss != null)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me The current boss roaming the stream is '{stream_boss.getName()}' with {stream_boss.getHealth()} hit points left. Our fellow loungers and the pug army must unite to take them down! Use the command !attack to play. Current game type is {boss_game_type_string[(int)boss_game_type]}");
            }
        }

        //On message received
        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            //string roulette_reward = e.ChatMessage.CustomRewardId;
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
                client.SendMessage(TwitchInfo.ChannelName, $"/me My current commands are:  !myrank // !socials // !schedule // !wowwyyKcount // !wowcourt // !bin // !boss");
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
                client.SendMessage(TwitchInfo.ChannelName, $"/me WowwyyTV's schedule is every Thursday and Friday 4pm GMT+1 (UK). Unscheduled streams may occur so please follow to catch these as well!");
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
            //Boss command
            if (e.ChatMessage.Message.Equals("!boss"))
            {
                if (stream_boss != null)
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me A small minigame where twitch chatters can attack the current stream's boss! Use the !attack command to play. The current boss is called '{stream_boss.getName()}' and has {stream_boss.getHealth()} hit points left. The game type is {boss_game_type_string[(int)boss_game_type]}");
                }
                else
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me A small minigame where twitch chatters can attack the current stream's boss! Use the !attack command to play. There is currently no boss active! Message a mod to add one.");
                }
            }
            //Boss battle attack command
            if (e.ChatMessage.Message.Equals("!attack"))
            {
                Random rng_attack_chance = new Random();
                bool attack_landed = true;
                if(stream_boss != null)
                {
                    try
                    {
                        Player current_player = new Player();
                        Player new_player = new Player();
                        int hit_count = 0;
                        bool player_found = false;
                        for (int i = 0; i < player_list.Count; i++)
                        {
                            new_player = player_list[i];
                            if (e.ChatMessage.Username == new_player.getUsername())
                            {
                                player_found = true;
                                hit_count++;
                            }
                            else
                            {
                                player_found = false;
                            }
                            if (player_found)
                            {
                                current_player = player_list[i];
                            }
                            hit_count++;
                        }
                        if (hit_count == player_list.Count)
                        {
                            current_player = new Player(e.ChatMessage.Username, player_attack_min, player_attack_max);
                            player_list.Add(current_player);
                        }

                        current_player.setCurrentCooldown(attack_cooldown - (DateTime.Now - current_player.getStartAttackTime()));
                        if (current_player.getCurrentCooldown() <= TimeSpan.Zero) { current_player.setCanAttackTrue(); }
                        if (current_player.getCanAttack())
                        {
                            if (boss_game_type == BossGameType.TotalDmgDealt)
                            {
                                current_player.attack(stream_boss);
                                current_player.addToTotalDamageDealt(current_player.getLastDamageDealt());
                                attack_landed = true;
                            }
                            else if (boss_game_type == BossGameType.LastHit)
                            {
                                if (stream_boss.getHealth() < boss_health_dodge_threshold)
                                {
                                    current_player.setAttackChance(rng_attack_chance.Next(0, 100));
                                    if (current_player.getAttackChance() < boss_dodge_chance)
                                    {
                                        current_player.attack(stream_boss);
                                        attack_landed = true;
                                    }
                                    else
                                    {
                                        attack_landed = false;
                                    }
                                }
                                else
                                {
                                    current_player.attack(stream_boss);
                                }
                            }

                            current_player.setCurrentCooldown(attack_cooldown);
                            if (attack_landed)
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.getUsername()} dealt {current_player.getLastDamageDealt().ToString()} damage to {stream_boss.getName()}. Boss' health is now {stream_boss.getHealth()}. {e.ChatMessage.Username} your attack cooldown is {current_player.getCurrentCooldown().Minutes}m{current_player.getCurrentCooldown().Seconds}s");
                            }
                            else if (!attack_landed)
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.getUsername()} missed!!!!!");
                            }
                        }
                        else
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.getUsername()} can't attack now! Your attack cooldown is {current_player.getCurrentCooldown().Minutes}m{current_player.getCurrentCooldown().Seconds}s");
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString()} Exception in !attack command: { exception.ToString()}");
                        client.SendMessage(TwitchInfo.ChannelName, $"/me @breadaddiction Exception caught in !attack function.");
                    }
                    //Boss killed
                    if (stream_boss.getHealth() <= 0)
                    {
                        stream_boss = null;
                        if (boss_game_type == BossGameType.LastHit)
                            client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! {e.ChatMessage.Username} landed the final blow! The stream victor!");
                        else if(boss_game_type == BossGameType.TotalDmgDealt)
                        {
                            player_list.Sort();
                            client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! Winner for total damage dealt is {player_list[player_list.Count-1].getUsername()} with {player_list[player_list.Count-1].getTotalDamageDealt()} total damage.");
                        }

                        foreach(Player player in player_list)
                        {
                            player.ResetBossRelatedStats();
                        }
                    }
                }
                else
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Attack failed!! There currently isn't a boss active. Message a mod to add one.");
                }
            }
            //Roulette command
            if (e.ChatMessage.Message.Equals("!roulette"))
            {
                if (e.ChatMessage.Username == "breadaddiction")
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Daddy Bread is always a winner <3 ");
                }
                else if (e.ChatMessage.Username == "ambiience")
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me How's your dad she alright ");
                }
                else if (e.ChatMessage.IsModerator && e.ChatMessage.Username != "breadaddiction")
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Clumsy's the mum wowwyyS");
                }
                else
                {
                    Player current_player = new Player();
                    Player new_player = new Player();
                    int hit_count = 0;
                    bool player_found = false;
                    for (int i = 0; i < player_list.Count; i++)
                    {
                        new_player = player_list[i];
                        if (e.ChatMessage.Username == new_player.getUsername())
                        {
                            player_found = true;
                            hit_count++;
                        }
                        else
                        {
                            player_found = false;
                        }
                        if (player_found)
                        {
                            current_player = player_list[i];
                        }
                        hit_count++;
                    }
                    if (hit_count == player_list.Count)
                    {
                        current_player = new Player(e.ChatMessage.Username, 100, 200);
                        player_list.Add(current_player);
                    }

                    Random timeout_chance = new Random();
                    Random timeout_length = new Random();

                    int chance = timeout_chance.Next(1, 5);
                    if (chance == 1)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/timeout {e.ChatMessage.Username} {timeout_length.Next(min_timeout, max_timeout).ToString()}");
                        client.SendMessage(TwitchInfo.ChannelName, $"/me {e.ChatMessage.Username} Your streak was: {current_player.getCurrentRouletteStreak()}!");
                        current_player.resetCurrentRouletteStreak();
                    }
                    else
                    {
                        current_player.addOneCurrentRouletteStreak();
                        client.SendMessage(TwitchInfo.ChannelName, $"/me {e.ChatMessage.Username} You got lucky mate. Current streak: {current_player.getCurrentRouletteStreak()}");
                    }
                }
            }
            //Check rank of messager
            if (e.ChatMessage.Message.Equals("!myrank"))
            {
                if (e.ChatMessage.IsSubscriber)
                {
                    if (e.ChatMessage.SubscribedMonthCount <= 11)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me {e.ChatMessage.DisplayName} your rank is {sub_titles[e.ChatMessage.SubscribedMonthCount]}. Your sub month streak is {e.ChatMessage.SubscribedMonthCount} months.");
                    }
                    else
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me {e.ChatMessage.DisplayName} your rank is The Sergaent Major of the Pug Army wowwyyPog Only the best commanders of furry canines can achieve such a title. Your sub month streak is {e.ChatMessage.SubscribedMonthCount} months.");
                    }
                }
                else
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"/me {e.ChatMessage.DisplayName} you are not subscribed! Please sub to join the Pug Army! wowwyyPog");
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
                //Boss commands
                if (e.ChatMessage.Message.StartsWith("!addboss"))
                {
                    if (boss_game_type == BossGameType.End)
                    {
                        Random rng_gametype = new Random();

                        boss_game_type = (BossGameType)rng_gametype.Next(2);
                    }

                    string boss_name = e.ChatMessage.Message.Remove(0,9);
                    if (stream_boss == null)
                    {
                        stream_boss = new Boss(boss_health, boss_name);
                        client.SendMessage(TwitchInfo.ChannelName, $"/me Boss '{stream_boss.getName()}' has been added. The game type is: {boss_game_type_string[(int)boss_game_type]}");
                    }
                    else
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me A Boss named '{stream_boss.getName()}' exists, no more can be added! The game type is: {boss_game_type_string[(int)boss_game_type]}");
                    }
                }
                if (e.ChatMessage.Message.Equals("!removeboss"))
                {
                    boss_game_type = BossGameType.End;
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Boss '{stream_boss.getName()}' was removed");
                    stream_boss = null;
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
                client.SendMessage(e.Channel, $"/me Welcome {e.Subscriber.DisplayName} to the Pug Army! wowwyyPog You are now a Private, remember that morning rollcall is never! Thank for using your twitch prime sub");
            else
                client.SendMessage(e.Channel, $"/me Welcome {e.Subscriber.DisplayName} to the Pug Army! wowwyyPog You are now a Private, remember that morning rollcall is never!");
        }

        //On resub
        private void Client_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            int sub_month = int.Parse(e.ReSubscriber.MsgParamCumulativeMonths);

            if (sub_month < max_sub_month)
            {
                client.SendMessage(e.Channel, $"/me Thank you {e.ReSubscriber.DisplayName} for rejoining the fight for the Pug Army! wowwyyPog You're new title is {sub_titles[sub_month-1]}. Your sub streak is {e.ReSubscriber.Months} months.");
            }
            else
            {
                client.SendMessage(e.Channel, $"/me Thank you {e.ReSubscriber.DisplayName} for rejoining the fight for the Pug Army! wowwyyPog You're still The Sergaent Major of the Pug Army. Your sub streak is {e.ReSubscriber.Months} months.");
            }
        }

        //On gifted sub
        private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            int sub_month = int.Parse(e.GiftedSubscription.MsgParamMonths);

            client.SendMessage(e.Channel, $"/me Thank you {e.GiftedSubscription.DisplayName} for gifting a sub to {e.GiftedSubscription.MsgParamRecipientDisplayName}! Welcome to the Pug Army! Your new title is {sub_titles[max_sub_month-1]} wowwyyPog Remember that morning roll call is never! Your sub streak is {e.GiftedSubscription.MsgParamMonths} months.");
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

        private double minutes_to_milliseconds(double minutes)
        {
            double milliseconds = minutes * 60000;
            return milliseconds;
        }
    }
}