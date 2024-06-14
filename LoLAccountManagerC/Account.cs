using System;

namespace LoLAccountManagerC
{
    internal class Account
    {
        private string accountName;
        private string password;
        private string riotID;
        public string AccountName
        {
            get { return accountName; }
            set
            {
                accountName = value;
            }
        }
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
            }
        }
        public string RiotID
        {
            get { return riotID; }
            set
            {
                riotID = value;
            }
        }


        public Account(string accountName, string password, string riotID)
        {
            AccountName = accountName;
            Password = password;
            RiotID = riotID;
        }

        public void DisplayAccount()
        {
            Console.WriteLine($"Name {AccountName} Password {Password} RiotID {RiotID}");
        }
    }
}
