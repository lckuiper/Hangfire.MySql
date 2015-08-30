using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.MySql.src
{
   
    [Serializable]
    internal class MySqlDistributedLockException : Exception
    {
        public MySqlDistributedLockException(string message) : base(message)
        {

        }
    }
}
