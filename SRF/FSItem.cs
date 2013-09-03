namespace SRF
{

    public class FSItem
    {

        private string path;
        private FSItemType type;

        public FSItem(string path, FSItemType type)
        {
            this.path = path;
            this.type = type;
        }

        public string Path
        {
            get
            {
                return path;
            }
        }

        public FSItemType Type
        {
            get
            {
                return type;
            }
        }

    }

    public enum FSItemType
    {

        Folder,
        File

    }

}