using System;

namespace Tests
{
    public class TimekeeperCtor
    {
        public TimekeeperCtor(string name, int age
            //DateTime? dateOfBirth, MatterNumber matterNumber
            )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Age = age;
            //DateOfBirth = dateOfBirth;
            //MatterNumber = matterNumber;
        }

        public string Name { get; }

        public int Age { get; }

        //public DateTime? DateOfBirth { get; }

        //public MatterNumber MatterNumber { get; }

        //public override string ToString() =>
        //    $"Name; {Name}, " +
        //    $"Age: {Age}, " +
        //    $"Date of birth: {DateOfBirth}, " +
        //    $"Matter number: {MatterNumber}";
    }
}
