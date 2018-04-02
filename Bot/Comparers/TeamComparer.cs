using System;
using System.Collections.Generic;
using System.Text;
using Bot.Models;

namespace Bot.Comparers
{
    public class TeamComparer : IEqualityComparer<Team>
    {
        public bool Equals(Team x, Team y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(Team obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
