using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeacherScheduler
{
    class Logger
    {        
        private StringBuilder log = new StringBuilder();
        private string logFileName;

        public Logger(string logFileName)
        {
            this.logFileName = logFileName;
        }

        public void clear()
        {
            log.Clear();
        }

        public void appendLine(string line)
        {
            log.AppendLine(line);
        }
        
        public void write()
        {
            using (System.IO.StreamWriter file = System.IO.File.CreateText(logFileName))
            {
                file.WriteLine(log.ToString());
            }
        }        
    }
}
