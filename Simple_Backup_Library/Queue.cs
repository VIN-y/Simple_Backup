using System.Collections.Generic;

namespace SimpleBackupLibrary
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
