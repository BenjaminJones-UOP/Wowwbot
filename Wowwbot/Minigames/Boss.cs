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

        public int Health
        {
            get { return health; }
            set { health = value; }
        }
        public string Name
        {
            get { return name; }
            //set { name = value; }
        }
    }
}
