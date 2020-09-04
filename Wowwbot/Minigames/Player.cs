using System;
using System.Timers;

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
        DateTime start_attack_time;
        TimeSpan current_cooldown;
        static bool can_play;
        bool attack_landed;

        int current_roulette_streak;
        
        public Player(string init_name, int init_attack_min, int init_attack_max)
        {
            username = init_name;
            attack_min = init_attack_min;
            attack_max = init_attack_max;
            attack_chance = 100;
            current_roulette_streak = 0;
            can_play = true;
            attack_landed = true;
        }
        public Player() { }

        public void Attack(Boss boss)
        {
            rng = new Random();
            int boss_health = boss.GetHealth();
            last_damage_dealt = rng.Next(attack_min, attack_max);
            boss_health -= last_damage_dealt;
            boss.SetHealth(boss_health);
            start_attack_time = DateTime.Now;
            can_play = false;
        }

        public int CompareTo(Player other)
        {
            if (other == null)
                return 1;
            else
                return this.total_damage_dealt.CompareTo(other.total_damage_dealt);
        }

        public void AddOneCurrentRouletteStreak()
        {
            current_roulette_streak += 1;
        }
        public void ResetCurrentRouletteStreak()
        {
            current_roulette_streak = 0;
        }
        public void ResetBossRelatedStats()
        {
            attack_chance = 100;
            total_damage_dealt = 0;
            can_play = true;
        }

        public bool GetCanPlay()
        {
            return can_play;
        }
        public void SetCanPlayTrue()
        {
            can_play = true;
        }
        public bool GetAttackLanded()
        {
            return attack_landed;
        }
        public void SetAttackLanded(bool landed)
        {
            attack_landed = landed;
        }
        public int GetLastDamageDealt()
        {
            return last_damage_dealt;
        }
        public int GetAttackChance()
        {
            return attack_chance;
        }
        public void SetAttackChance(int new_attack_chance)
        {
            attack_chance = new_attack_chance;
        }
        public string GetUsername()
        {
            return username;
        }
        public DateTime GetStartAttackTime()
        {
            return start_attack_time;
        }
        public TimeSpan GetCurrentCooldown()
        {
            return current_cooldown;
        }
        public void SetCurrentCooldown(TimeSpan new_current_cooldown)
        {
            current_cooldown = new_current_cooldown;
        }
        public int GetCurrentRouletteStreak()
        {
            return current_roulette_streak;
        }
        public int GetTotalDamageDealt()
        {
            return total_damage_dealt;
        }
        public void AddToTotalDamageDealt(int damage)
        {
            total_damage_dealt += damage;
        }
    }
}