using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Model.Config
{
    public class LoggerOptions
    {
        public string RepoName { set; get; } = "events_log";

        public string MessageTemplate { set; get; } = 
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    }
}
