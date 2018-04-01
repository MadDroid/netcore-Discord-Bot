using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Models
{
    public class Team
    {
        #region Fields
        private string fullName;
        private HashSet<string> aliases = new HashSet<string>(); 
        #endregion

        #region Properties
        public string FullName => fullName;
        public string[] Aliases => aliases.ToArray(); 
        #endregion

        #region Constructors
        public Team(string fullName) : this(fullName, new string[0]) { }

        public Team(string fullName, params string[] aliases)
        {
            this.fullName = fullName;
            foreach (string aliase in aliases)
            {
                this.aliases.Add(aliase);
            }
        }
        #endregion

        #region Methods
        public bool AddAliase(string aliase) => aliases.Add(aliase);

        public bool RemoveAliase(string aliase) => aliases.Remove(aliase); 
        #endregion
    }
}
