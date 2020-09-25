using System;

namespace Wowwbot
{
    class Player : IComparable<Player>
    {
        string username;
        Random rng;

        int attack_min;
        int attack_max;
        int attack_chance;

        int last_damage_dealt;
        int total_damage_dealt;
        DateTime start_play_time;
        TimeSpan current_cooldown;
        bool can_play;
        bool attack_landed;

        int current_roulette_streak;

        int total_loot_stolen;
        
        public Player(string init_name, int init_attack_min, int init_attack_max)
        {
            username = init_name;
            attack_min = init_attack_min;
            attack_max = init_attack_max;
            attack_chance = 100;
            current_roulette_streak = 0;
            can_play = true;
            attack_landed = true;
            total_loot_stolen = 0;
        }
        public Player() { }

        public void Attack(Boss boss)
        {
            rng = new Random();
            last_damage_dealt = rng.Next(attack_min, attack_max);
            boss.Health -= last_damage_dealt;
            start_play_time = DateTime.Now;
            can_play = false;
        }

        public int CompareTo(Player other)
        {
            if (other == null)
                return 1;
            else
                return this.total_damage_dealt.CompareTo(other.total_damage_dealt);
        }

        public void ResetStats()
        {
            attack_chance = 100;
            total_damage_dealt = 0;
            can_play = true;
            total_loot_stolen = 0;
        }

        public bool CanPlay
        {
            get { return can_play; }
            set { can_play = value; }
        }
        public bool AttackLanded
        {
            get { return attack_landed; }
            set { attack_landed = value; }
        }
        public int LastDamageDealt
        {
            get { return last_damage_dealt; }
            set { last_damage_dealt = value; }
        }
        public int AttackChance
        {
            get { return attack_chance; }
            set { attack_chance = value; }
        }
        public string Username
        {
            get { return username; }
            //set { username = value; }
        }
        public DateTime StartPlayTime
        {
            get { return start_play_time; }
            set { start_play_time = value; }
        }
        public TimeSpan CurrentCooldown
        {
            get { return current_cooldown; }
            set { current_cooldown = value; }
        }
        public int CurrentRouletteStreak
        {
            get { return current_roulette_streak; }
            set { current_roulette_streak = value; }
        }
        public int TotalDamageDealt
        {
            get { return total_damage_dealt; }
            set { total_damage_dealt = value; }
        }
        public int TotalLootStolen
        {
            get { return total_loot_stolen; }
            set { total_loot_stolen = value; }
        }
    }
}