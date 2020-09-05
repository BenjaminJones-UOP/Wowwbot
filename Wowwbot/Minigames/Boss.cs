using System;

namespace Wowwbot
{
    class Boss
    {
        string name;
        int health; 

        public Boss(int init_health, string init_name)
        {
            health = init_health;
            name = init_name;
        }

        public int GetHealth()
        {
            return health;
        }
        public void SetHealth(int new_health)
        {
            health = new_health;
        }
        public string GetName()
        {
            return name;
        }
    }
}
