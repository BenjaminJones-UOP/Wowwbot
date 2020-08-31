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
        static bool can_attack;

        int current_roulette_streak;
        
        public Player(string init_name, int init_attack_min, int init_attack_max)
        {
            username = init_name;
            attack_min = init_attack_min;
            attack_max = init_attack_max;
            attack_chance = 100;
            can_attack = true;
            current_roulette_streak = 0;
        }
        public Player() { }

        public void attack(Boss boss)
        {
            rng = new Random();
            int boss_health = boss.getHealth();
            last_damage_dealt = rng.Next(attack_min, attack_max);
            boss_health -= last_damage_dealt;
            boss.setHealth(boss_health);
            start_attack_time = DateTime.Now;
            can_attack = false;
        }

        public int CompareTo(Player other)
        {
            if (other == null)
                return 1;
            else
                return this.total_damage_dealt.CompareTo(other.total_damage_dealt);
        }

        public void addOneCurrentRouletteStreak()
        {
            current_roulette_streak += 1;
        }
        public void resetCurrentRouletteStreak()
        {
            current_roulette_streak = 0;
        }
        public void ResetBossRelatedStats()
        {
            attack_chance = 100;
            total_damage_dealt = 0;
            can_attack = true;
        }

        public bool getCanAttack()
        {
            return can_attack;
        }
        public void setCanAttackTrue()
        {
            can_attack = true;
        }
        public int getLastDamageDealt()
        {
            return last_damage_dealt;
        }
        public int getAttackChance()
        {
            return attack_chance;
        }
        public void setAttackChance(int new_attack_chance)
        {
            attack_chance = new_attack_chance;
        }
        public string getUsername()
        {
            return username;
        }
        public DateTime getStartAttackTime()
        {
            return start_attack_time;
        }
        public TimeSpan getCurrentCooldown()
        {
            return current_cooldown;
        }
        public void setCurrentCooldown(TimeSpan new_current_cooldown)
        {
            current_cooldown = new_current_cooldown;
        }
        public int getCurrentRouletteStreak()
        {
            return current_roulette_streak;
        }
        public int getTotalDamageDealt()
        {
            return total_damage_dealt;
        }
        public void addToTotalDamageDealt(int damage)
        {
            total_damage_dealt += damage;
        }

    }
}