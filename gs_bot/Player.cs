using System;
using System.Collections.Generic;
using System.Text;

namespace gs_bot
{
    class Player
    {
        public string FirstName { get; }

        public string LastName { get; }

        public int Id { get; set; }

        public string Party { get; set; }

        public string Role { get; set; }

        public Player(string firstName, string lastName, int id)
        {
            FirstName = firstName;
            LastName = lastName;
            Id = id;
            Party = null;
            Role = null;
        }
    }
}
