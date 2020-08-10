using System;

namespace Wowwbot
{
    class Boss
    {
        string name;
        int health; //between 5000-10000

        public Boss(int init_health, string init_name)
        {
            health = init_health;
            name = init_name;
        }

        public int getHealth()
        {
            return health;
        }
        public void setHealth(int new_health)
        {
            health = new_health;
        }
        public string getName()
        {
            return name;
        }
    }
}
