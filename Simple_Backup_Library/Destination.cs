namespace Simple_Backup_Library
{
    public class Destination
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Display
        {
            get
            {
                return string.Format("{0} \t({1})", Path, Name);
            }
        }
    }
}
