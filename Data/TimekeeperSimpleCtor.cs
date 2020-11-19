using System;

namespace Data
{
    public class TimekeeperSimpleCtor
    {
        public TimekeeperSimpleCtor(string name, int age)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Age = age;
        }

        public string Name { get; }

        public int Age { get; }
    }
}
