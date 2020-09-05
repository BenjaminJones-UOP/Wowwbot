using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        bool minigame_started;
        List<Player> player_list;
        Boss stream_boss;
        Player current_player;

        const int large_lootbag_chance = 1000; //1 in 1000
        const int medium_lootbag_chance = 900; //1 in 10
        const int small_lootbag_chance = 500; // 1 in 2
        const int large_lootbag_value = 1000;
        const int medium_lootbag_value = 10;
        const int small_lootbag_value = 1;
        string lootbag_stolen;

        TimeSpan play_cooldown = new TimeSpan(0, 5, 0); //Player's cooldown hh:mm:ss format
        const string boss_name = "WowwBoss";
        const int boss_health = 8000;
        const int boss_health_dodge_threshold = 750;
        const int boss_dodge_chance = 50;
        const int player_attack_min = -5;
        const int player_attack_max = 200;

        const int max_timeout = 300;
        const int min_timeout = 10;

        public GameManager()
        {
            player_list = new List<Player>();
            mini_game_type = MiniGameType.End;
            current_player = new Player();
            client = new TwitchClient();
            current_minigame_status = "";
            lootbag_stolen = "";
        }

        public void Start(TwitchClient client)
        {
            this.client = client;
            if (minigame_started)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me Minigame already exists: {GetCurrentMinigameStatusMessage()}");
                return;
            }
            //Determine minigame type
            if (mini_game_type == MiniGameType.End && !minigame_started)
            {
                Random rng_gametype = new Random();
                mini_game_type = (MiniGameType)rng_gametype.Next(num_of_games);

                minigame_started = true;
            }
            //Boss related messages and set up
            if (mini_game_type == MiniGameType.LastHit || mini_game_type == MiniGameType.TotalDmgDealt)
            {
                if (stream_boss == null)
                {
                    stream_boss = new Boss(boss_health, boss_name);
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Boss '{stream_boss.GetName()}' has been added. The game type is: {mini_game_type_string[(int)mini_game_type]} and Boss' health is {stream_boss.GetHealth()}.");
                    current_minigame_status = $"The game type is: {mini_game_type_string[(int)mini_game_type]} and Boss' health is {stream_boss.GetHealth()}.";
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
                HeistWinner();
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
                    current_player.SetCurrentCooldown(play_cooldown - (DateTime.Now - current_player.GetStartAttackTime()));
                    if (current_player.GetCurrentCooldown() <= TimeSpan.Zero) { current_player.SetCanPlayTrue(); }
                    if (current_player.GetCanPlay())
                    {
                        current_player.SetCurrentCooldown(play_cooldown);
                        if (mini_game_type == MiniGameType.TotalDmgDealt)
                        {
                            TotalDmgDealtPlay();
                            DidAttackLand();
                        }
                        else if (mini_game_type == MiniGameType.LastHit)
                        {
                            LastHitPlay();
                            DidAttackLand();
                        }
                        else if (mini_game_type == MiniGameType.Heist)
                        {
                            HeistPlay();
                            client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.GetUsername()} stole {lootbag_stolen} Their total stolen is {current_player.GetTotalLootStolen()} gems. Cooldown until next heist: {current_player.GetCurrentCooldown().Minutes}m{current_player.GetCurrentCooldown().Seconds}s");
                        }
                    }
                    else
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.GetUsername()} can't play now! Your cooldown is {current_player.GetCurrentCooldown().Minutes}m{current_player.GetCurrentCooldown().Seconds}s");
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
                if (username == new_player.GetUsername())
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
                client.SendMessage(TwitchInfo.ChannelName, $"/timeout {current_player.GetUsername()} {timeout_length.Next(min_timeout, max_timeout)}");
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.GetUsername()} Your streak was: {current_player.GetCurrentRouletteStreak()}!");
                current_player.ResetCurrentRouletteStreak();
            }
            else
            {
                current_player.AddOneCurrentRouletteStreak();
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.GetUsername()} You got lucky mate. Current streak: {current_player.GetCurrentRouletteStreak()}");
            }
        }
        private void TotalDmgDealtPlay()
        {
            current_player.Attack(stream_boss);
            current_player.AddToTotalDamageDealt(current_player.GetLastDamageDealt());
            current_player.SetAttackLanded(true);
        }
        private void LastHitPlay()
        {
            Random rng_attack_chance = new Random();
            if (stream_boss.GetHealth() < boss_health_dodge_threshold)
            {
                current_player.SetAttackChance(rng_attack_chance.Next(0, 100));
                if (current_player.GetAttackChance() < boss_dodge_chance)
                {
                    current_player.Attack(stream_boss);
                    current_player.SetAttackLanded(true);
                }
                else
                {
                    current_player.SetAttackLanded(false);
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
                current_player.AddToLootStolen(large_lootbag_value);
                lootbag_stolen = "a large lootbag worth 1000 gems!!!!!!!";
            }
            else if (loot_bag_roll > medium_lootbag_chance)
            {
                current_player.AddToLootStolen(medium_lootbag_value);
                lootbag_stolen = "a medium lootbag worth 10 gems!";
            }
            else if (loot_bag_roll > small_lootbag_chance)
            {
                current_player.AddToLootStolen(small_lootbag_value);
                lootbag_stolen = "a small lootbag worth just 1 gem";
            }
            else
            {
                lootbag_stolen = "nothing wowwyyT";
            }
        }

        private void DidAttackLand()
        {
            if (current_player.GetAttackLanded())
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.GetUsername()} dealt {current_player.GetLastDamageDealt()} damage to {stream_boss.GetName()}. Boss' health is now {stream_boss.GetHealth()}. {current_player.GetUsername()} your attack cooldown is {current_player.GetCurrentCooldown().Minutes}m{current_player.GetCurrentCooldown().Seconds}s");
                CheckBossDead();
            }
            else
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me {current_player.GetUsername()} missed!!!!!");
            }
        }
        private void CheckBossDead()
        {
            if (stream_boss.GetHealth() <= 0)
            {
                if (mini_game_type == MiniGameType.LastHit)
                    client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! {current_player.GetUsername()} landed the final blow! The stream victor!");
                else if (mini_game_type == MiniGameType.TotalDmgDealt)
                {
                    player_list.Sort();
                    if (player_list.Count > 2)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! Winner for total damage dealt is {player_list[player_list.Count - 1].GetUsername()} with {player_list[player_list.Count - 1].GetTotalDamageDealt()} total damage. 2nd was {player_list[player_list.Count - 2].GetUsername()} with {player_list[player_list.Count - 2].GetTotalDamageDealt()} damage. 3rd was {player_list[player_list.Count - 3].GetUsername()} with {player_list[player_list.Count - 3].GetTotalDamageDealt()} damage");
                    }
                    else
                    {
                        client.SendMessage(TwitchInfo.ChannelName, $"/me Boss destroyed! Winner for total damage dealt is {player_list[player_list.Count - 1].GetUsername()} with {player_list[player_list.Count - 1].GetTotalDamageDealt()} total damage.");
                    }
                }
                ResetGamesAndPlayer();
            }
        }
        private void HeistWinner()
        {
            player_list.Sort();
            if (player_list.Count > 2)
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me End of the heist! Winner for total stolen is {player_list[player_list.Count - 1].GetUsername()} with {player_list[player_list.Count - 1].GetTotalLootStolen()} gems. 2nd was {player_list[player_list.Count - 2].GetUsername()} with {player_list[player_list.Count - 2].GetTotalLootStolen()} gems. 3rd was {player_list[player_list.Count - 3].GetUsername()} with {player_list[player_list.Count - 3].GetTotalLootStolen()} gems");
            }
            else
            {
                client.SendMessage(TwitchInfo.ChannelName, $"/me End of the heist! Winner for total stolen is {player_list[player_list.Count - 1].GetUsername()} with {player_list[player_list.Count - 1].GetTotalLootStolen()} gems.");
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
            if (client != null) client = null;
        }

        public bool GetMiniGameStarted()
        {
            return minigame_started;
        }
        public string GetCurrentMinigameStatusMessage()
        {
            if(stream_boss != null) //Resets string with most current boss health
            {
                current_minigame_status = $"The game type is: {mini_game_type_string[(int)mini_game_type]} and Boss' health is {stream_boss.GetHealth()}.";
            }
            return current_minigame_status;
        }
    }
}