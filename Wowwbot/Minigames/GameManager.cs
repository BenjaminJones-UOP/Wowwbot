using System;
using System.Collections.Generic;
using TwitchLib.Client;

namespace Wowwbot
{
    class GameManager
    {
        TwitchClient client;

        enum MiniGameType
        {
            TotalDmgDealt,
            LastHit,
            Heist,
            End
        };
        static MiniGameType mini_game_type;
        const int num_of_games = (int)MiniGameType.End;
        string[] mini_game_type_string = { "'Boss: Deal the most damage!'", "'Boss: Last hit the boss!'", "'Heist: Steal the most stuff!'" };
        string current_minigame_status;
        string[] game_info = {"/me Simply use !play to attack the boss. Winner is whoever deals the most damage by the time the boss' health reaches zero.", "/me Use !play to attack the boss. Get the last hit on the boss to win. Watch out though, the boss dodges attacks at low health.", "/me Use !play to steal gems, Chances are: 50% to steal small lootbag, 10% to steal a medium lootbag and 0.1% to steal a large lootbag!", "/me No game currently active. Ask Wowwyy or a mod to add one wowwyyP " };
        bool minigame_started;
        List<Player> player_list;
        Boss stream_boss;
        Player current_player;

        const int large_lootbag_chance = 100; //1 in 100
        const int medium_lootbag_chance = 90; //1 in 10
        const int small_lootbag_chance = 50; // 1 in 2
        const int large_lootbag_value_max = 3000;
        const int large_lootbag_value_min = 1000;
        const int medium_lootbag_value_max = 150;
        const int medium_lootbag_value_min = 50;
        const int small_lootbag_value_max = 20;
        const int small_lootbag_value_min = 1;
        string lootbag_stolen;

        TimeSpan play_cooldown = new TimeSpan(0, 5, 0); //Player's cooldown hh:mm:ss format
        const string boss_name = "WowwBoss";
        const int boss_health = 5000;
        const int boss_health_dodge_threshold = 750;
        const int boss_dodge_chance = 50;
        const int player_attack_min = -5;
        const int player_attack_max = 200;

        const int max_timeout = 300;
        const int min_timeout = 10;

        public GameManager(TwitchClient client)
        {
            player_list = new List<Player>();
            mini_game_type = MiniGameType.End;
            current_player = new Player();
            this.client = client;
            current_minigame_status = "";
            lootbag_stolen = "";
        }
        
        public void Start()
        {
            if (minigame_started)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Minigame already exists: {CurrentMinigameStatusMessage}");
                return;
            }
            //Determine minigame type
            if (mini_game_type == MiniGameType.End && !minigame_started)
            {
                Random rng_gametype = new Random();
                mini_game_type = (MiniGameType)rng_gametype.Next(num_of_games);
                //mini_game_type = MiniGameType.Heist; //This is for setting the game type for testing

                while (mini_game_type == MiniGameType.TotalDmgDealt)
                {
                    mini_game_type = (MiniGameType)rng_gametype.Next(num_of_games); //This should bypass last damage hit, quick fix 
                }

                minigame_started = true;
            }
            //Boss related messages and set up
            if (mini_game_type == MiniGameType.LastHit || mini_game_type == MiniGameType.TotalDmgDealt)
            {
                if (stream_boss == null)
                {
                    stream_boss = new Boss(boss_health, boss_name);
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Boss '{stream_boss.Name}' has been added. The game type is: {mini_game_type_string[(int)mini_game_type]} and Boss' health is {stream_boss.Health}.");
                    current_minigame_status = $"The game type is: {mini_game_type_string[(int)mini_game_type]} and Boss' health is {stream_boss.Health}.";
                }
            }
            if (mini_game_type == MiniGameType.Heist && minigame_started)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Starting heist!! Steal the most gems by the end of the stream to win! Each !play will gain you a random amount of gems (or none!)");
                current_minigame_status = $"The game type is: {mini_game_type_string[(int)mini_game_type]}. Each !play will gain you a random amount of gems (or none!) Steal the most by the end of stream to win!";
            }
        }
        public void Stop()
        {
            if (mini_game_type == MiniGameType.Heist)
            {
                StopHeist();
            }
            client.SendMessage(TwitchInfo.ChannelName, $"/me Minigame {mini_game_type_string[(int)mini_game_type]} stopped.");
            ResetGamesAndPlayer();
        }
        public void Play(string input_username)
        {
            if (minigame_started)
            {
                try
                {
                    //Find or add player
                    FindOrAddPlayer(input_username);

                    //Specific play logic here
                    current_player.CurrentCooldown = play_cooldown - (DateTime.Now - current_player.StartPlayTime);
                    if (current_player.CurrentCooldown <= TimeSpan.Zero) { current_player.CanPlay = true; }
                    if (current_player.CanPlay)
                    {
                        current_player.CurrentCooldown = play_cooldown;
                        if (mini_game_type == MiniGameType.TotalDmgDealt)
                        {
                            TotalDmgDealtPlay();
                            DidAttackLand();
                            current_player.CanPlay = false;
                        }
                        else if (mini_game_type == MiniGameType.LastHit)
                        {
                            LastHitPlay();
                            DidAttackLand();
                            current_player.CanPlay = false;
                        }
                        else if (mini_game_type == MiniGameType.Heist)
                        {
                            current_player.StartPlayTime = DateTime.Now;
                            current_player.CanPlay = false;
                            HeistPlay();
                            client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.Username} stole {lootbag_stolen} Their total stolen is {current_player.TotalLootStolen} gems. Cooldown until next heist: {current_player.CurrentCooldown.Minutes}m{current_player.CurrentCooldown.Seconds}s");
                        }
                        else if (mini_game_type == MiniGameType.End)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.Username} there currently isn't a game active! Message a mod or Wowwyy to see if we should add one :) ");
                        }
                    }
                    else
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.Username} can't play now! Your cooldown is {current_player.CurrentCooldown.Minutes}m{current_player.CurrentCooldown.Seconds}s");
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{DateTime.Now} Exception in !play command: {exception}");
                    client.SendMessage(TwitchInfo.ChannelName, $"/me @breadaddiction Exception caught in !play function.");
                }
            }
        }
        public void FindOrAddPlayer(string username)
        {
            Player new_player = new Player();
            int hit_count = 0;
            bool player_found = false;
            for (int i = 0; i < player_list.Count; i++)
            {
                new_player = player_list[i];
                if (username == new_player.Username)
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
                current_player = new Player(username, player_attack_min, player_attack_max);
                player_list.Add(current_player);
            }
        }
        public void ChanceGame(string input_username)
        {
            FindOrAddPlayer(input_username);
            Random timeout_chance = new Random();
            Random timeout_length = new Random();

            int chance = timeout_chance.Next(1, 5);
            if (chance == 1)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/timeout {current_player.Username} {timeout_length.Next(min_timeout, max_timeout)}");
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.Username} Your streak was: {current_player.CurrentRouletteStreak}!");
                current_player.CurrentRouletteStreak = 0;
            }
            else
            {
                current_player.CurrentRouletteStreak += 1;
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.Username} You got lucky mate. Current streak: {current_player.CurrentRouletteStreak}");
            }
        }
        private void TotalDmgDealtPlay()
        {
            current_player.Attack(stream_boss);
            current_player.TotalDamageDealt += current_player.LastDamageDealt;
            current_player.AttackLanded = true;
        }
        private void LastHitPlay()
        {
            Random rng_attack_chance = new Random();
            if (stream_boss.Health < boss_health_dodge_threshold)
            {
                current_player.AttackChance = rng_attack_chance.Next(0, 100);
                if (current_player.AttackChance < boss_dodge_chance)
                {
                    current_player.Attack(stream_boss);
                    current_player.AttackLanded = true;
                }
                else
                {
                    current_player.StartPlayTime = DateTime.Now;
                    current_player.AttackLanded = false;
                }
            }
            else
            {
                current_player.Attack(stream_boss);
            }
        }
        private void HeistPlay()
        {
            Random loot_bag_chance = new Random();
            int loot_bag_roll = loot_bag_chance.Next(0, large_lootbag_chance);
            if (loot_bag_roll == large_lootbag_chance)
            {
                Random large_lootbag_value = new Random();
                int loot_stolen = large_lootbag_value.Next(large_lootbag_value_min, large_lootbag_value_max);
                current_player.TotalLootStolen += loot_stolen;
                lootbag_stolen = $"a large lootbag worth {loot_stolen} gems!!!!!!!";
            }
            else if (loot_bag_roll > medium_lootbag_chance)
            {
                Random medium_lootbag_value = new Random();
                int loot_stolen = medium_lootbag_value.Next(medium_lootbag_value_min, medium_lootbag_value_max);
                current_player.TotalLootStolen += loot_stolen;
                lootbag_stolen = $"a medium lootbag worth {loot_stolen} gems!";
            }
            else if (loot_bag_roll > small_lootbag_chance)
            {
                Random small_lootbag_value = new Random();
                int loot_stolen = small_lootbag_value.Next(small_lootbag_value_min, small_lootbag_value_max); ;
                current_player.TotalLootStolen += loot_stolen;
                lootbag_stolen = $"a small lootbag worth just {loot_stolen} gems";
            }
            else
            {
                lootbag_stolen = "nothing wowwyyT";
            }
        }

        private void DidAttackLand()
        {
            if (current_player.AttackLanded)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.Username} dealt {current_player.LastDamageDealt} damage to {stream_boss.Name}. Boss' health is now {stream_boss.Health}. {current_player.Username} your attack cooldown is {current_player.CurrentCooldown.Minutes}m{current_player.CurrentCooldown.Seconds}s");
                CheckBossDead();
            }
            else
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.Username} missed!!!!!");
            }
        }
        private void CheckBossDead()
        {
            if (stream_boss.Health <= 0)
            {
                if (mini_game_type == MiniGameType.LastHit)
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! {current_player.Username} landed the final blow! The stream victor!");
                else if (mini_game_type == MiniGameType.TotalDmgDealt)
                {
                    player_list.Sort();
                    if (player_list.Count > 2)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! Winner for total damage dealt is {player_list[player_list.Count - 1].Username} with {player_list[player_list.Count - 1].TotalDamageDealt} total damage. 2nd was {player_list[player_list.Count - 2].Username} with {player_list[player_list.Count - 2].TotalDamageDealt} damage. 3rd was {player_list[player_list.Count - 3].Username} with {player_list[player_list.Count - 3].TotalDamageDealt} damage");
                    }
                    else
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! Winner for total damage dealt is {player_list[player_list.Count - 1].Username} with {player_list[player_list.Count - 1].TotalDamageDealt} total damage.");
                    }
                }
                ResetGamesAndPlayer();
            }
        }
        private void StopHeist()
        {
            player_list.Sort((p1, p2) => p1.TotalLootStolen.CompareTo(p2.TotalLootStolen));
            if (player_list.Count > 2)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me End of the heist! Winner for total stolen is {player_list[player_list.Count - 1].Username} with {player_list[player_list.Count - 1].TotalLootStolen} gems. 2nd was {player_list[player_list.Count - 2].Username} with {player_list[player_list.Count - 2].TotalLootStolen} gems. 3rd was {player_list[player_list.Count - 3].Username} with {player_list[player_list.Count - 3].TotalLootStolen} gems");
            }
            else
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me End of the heist! Winner for total stolen is {player_list[player_list.Count - 1].Username} with {player_list[player_list.Count - 1].TotalLootStolen} gems.");
            }
        }
        private void ResetGamesAndPlayer()
        {
            minigame_started = false;
            foreach (Player player in player_list)
            {
                player.ResetStats();
            }
            mini_game_type = MiniGameType.End;

            if (stream_boss != null) stream_boss = null;
            //if (client != null) client = null;
        }

        public bool MiniGameStarted
        {
            get { return minigame_started; }
            set { minigame_started = value; }
        }
        public string CurrentMinigameStatusMessage
        {
            get
            {
                if (stream_boss != null) //Resets string with most current boss health
                {
                    current_minigame_status = $"The game type is: {mini_game_type_string[(int)mini_game_type]} and Boss' health is {stream_boss.Health}.";
                }
                return current_minigame_status;
            }
            set { current_minigame_status = value; }
        }
        public string GameInfo
        {
            get { return game_info[(int)mini_game_type]; }
        }
    }
}