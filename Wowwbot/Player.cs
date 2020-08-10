using System;
using System.Timers;

namespace Wowwbot
{
    class Player
    {
        string username;
        Random rng;

        int attack_min;
        int attack_max;

        int last_damage_dealt;
        DateTime start_attack_time;
        TimeSpan current_cooldown;
        static bool can_attack;
        
        public Player(string init_name, int init_attack_min, int init_attack_max)
        {
            username = init_name;
            attack_min = init_attack_min;
            attack_max = init_attack_max;
            can_attack = true;
        }

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

        public bool getCanAttack()
        {
            return can_attack;
        }
        public int getLastDamageDealt()
        {
            return last_damage_dealt;
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
        public void setCanAttackTrue()
        {
            can_attack = true;
        }

        int minutes_to_milliseconds(int minutes)
        {
            int milliseconds = minutes * 60000;
            return milliseconds;
        }
    }
}
