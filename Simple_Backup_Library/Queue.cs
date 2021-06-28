using System.Collections.Generic;

namespace Simple_Backup_Library
{
    public class Queue
    {
        public List<Source> Sources { get; set; }
        public Queue()
        {
            Sources = new List<Source>();
        }
    }
}
