namespace Simple_Backup_Library
{
    public class Source
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Queue { get; set; }
        public string QueueStat
        {
            get
            {
                if (Queue == true)
                {
                    return "Yes";
                }
                else
                {
                    return "---";
                }
            }
        }
        public Source()
        {
            Queue = false;
        }
    }
}
