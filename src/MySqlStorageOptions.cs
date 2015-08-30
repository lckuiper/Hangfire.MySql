using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.MySql.src
{
    public class MySqlStorageOptions
    {
        public MySqlStorageOptions()
        {
            MyFirstOption = TimeSpan.FromMinutes(5);

        }

        public TimeSpan MyFirstOption { get; set; }
    }

}