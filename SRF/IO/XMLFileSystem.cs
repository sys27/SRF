using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace SRF.IO
{

    public class XMLFileSystem
    {

        private XDocument doc;

        private Random r = new Random();
        private string cut = string.Empty;

        public XMLFileSystem()
        {
            doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), new XElement("fs", new XAttribute("filesCount", 0), new XAttribute("fullSize", 0), new XElement("folders"), new XElement("files")));
        }

        public XMLFileSystem(string path)
            : this()
        {
            Load(path, true);
        }

        public bool CheckID(XElement root, uint id)
        {
            foreach (XElement el in root.Elements("file"))
            {
                if (el.HasElements)
                    if (CheckID(el, id))
                        return true;

                if (uint.Parse(el.Attribute("id").Value) == id)
                    return true;
            }

            return false;
        }

        public uint GenerateID()
        {
            uint id = 0;
            byte[] buf = new byte[4];

            do
            {
                r.NextBytes(buf);
                id = BitConverter.ToUInt32(buf, 0);
            } while (CheckID(doc.Root, id));

            return id;
        }

        public void Load(string path)
        {
            Load(path, false);
        }

        public void Load(string path, bool decompress)
        {
            lock (doc)
            {
                if (path == null)
                    throw new ArgumentNullException("path");
                if (path.Length <= 0)
                    throw new ArgumentException();

                if (decompress)
                    DecompressFS(path);

                doc = XDocument.Load(path);
            }
        }

        public void Save(string path)
        {
            Save(path, false);
        }

        public void Save(string path, bool compress)
        {
            lock (doc)
            {
                if (path == null)
                    throw new ArgumentNullException("path");
                if (path.Length <= 0)
                    throw new ArgumentException();

                doc.Save(path);

                foreach (XElement el in Files.Elements("file"))
                {
                    el.Attribute("cut").Remove();
                }

                doc.Save(path + ".out");

                if (compress)
                {
                    CompressFS(path + ".out");
                    File.Delete(path + ".out");
                }
            }

            Load(path, false);
        }

        public void CompressFS(string path)
        {
            FileStream fsIn = new FileStream(path, FileMode.Open, FileAccess.Read);
            BufferedStream bsIn = new BufferedStream(fsIn);

            FileStream fsOut = new FileStream(path + ".gz", FileMode.Create, FileAccess.Write);
            BufferedStream bsOut = new BufferedStream(fsOut);
            GZipStream gzipStream = new GZipStream(bsOut, CompressionMode.Compress);

            byte[] buffer = new byte[8192];
            int count;

            while ((count = bsIn.Read(buffer, 0, buffer.Length)) > 0)
            {
                gzipStream.Write(buffer, 0, count);
            }

            gzipStream.Close();
            bsOut.Close();
            fsOut.Close();

            bsIn.Close();
            fsIn.Close();
        }

        public void DecompressFS(string path)
        {
            FileStream fsIn = new FileStream(path + ".gz", FileMode.Open, FileAccess.Read);
            BufferedStream bsIn = new BufferedStream(fsIn);
            GZipStream gzipStream = new GZipStream(bsIn, CompressionMode.Decompress);

            FileStream fsOut = new FileStream(path, FileMode.Create, FileAccess.Write);
            BufferedStream bsOut = new BufferedStream(fsOut);

            byte[] buffer = new byte[8192];
            int count;

            while ((count = gzipStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                bsOut.Write(buffer, 0, count);
            }

            bsOut.Close();
            fsOut.Close();

            gzipStream.Close();
            bsIn.Close();
            fsIn.Close();
        }

        //public string GetPath(XElement el)
        //{
        //    if (el.Parent == doc.Root)
        //        return el.Attribute("name").Value;
        //    else
        //        return GetPath(el.Parent) + "\\" + el.Attribute("name").Value;
        //}

        public void AddFile(string path)
        {
            cut = path.Substring(0, path.LastIndexOf('\\') + 1);
            AddFile(Files, path);
        }

        private void AddFile(XElement parent, string path)
        {
            FileInfo info = new FileInfo(path);
            uint id = GenerateID();
            parent.Add(new XElement("file", new XAttribute("id", id),
                                            new XAttribute("name", path.Substring(path.LastIndexOf('\\') + 1)),
                                            new XAttribute("size", info.Length),
                                            new XAttribute("cut", cut),
                                            new XAttribute("path", path.Substring(cut.Length))));

            FilesCount++;
            FullSize += info.Length;
        }

        public void AddFolder(string path)
        {
            cut = path.Substring(0, path.LastIndexOf('\\') + 1);
            AddFolder(Folders, path);
        }

        private void AddFolder(XElement parent, string path)
        {
            XElement parentFolder = new XElement("folder", new XAttribute("name", path.Substring(path.LastIndexOf('\\') + 1)));
            parent.Add(parentFolder);

            foreach (string folder in Directory.GetDirectories(path))
            {
                AddFolder(parentFolder, folder);
            }

            if (Directory.GetFiles(path).Length > 0)
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    AddFile(Files, file);
                }
            }
        }

        public void PrepareFolders(string path)
        {
            foreach (XElement item in Folders.Elements("folder"))
            {
                PrepareFolders(item, path);
            }
        }

        private void PrepareFolders(XElement el, string path)
        {
            path += "\\" + el.Attribute("name").Value;
            Directory.CreateDirectory(path);

            foreach (XElement item in el.Elements("folder"))
            {
                PrepareFolders(item, path);
            }
        }

        public XElement SearchFile(uint id)
        {
            foreach (XElement item in Files.Elements("file"))
            {
                if (uint.Parse(item.Attribute("id").Value) == id)
                    return item;
            }

            return null;
        }

        public XElement Root
        {
            get
            {
                return doc.Root;
            }
        }

        public XElement Folders
        {
            get
            {
                return doc.Root.Element("folders");
            }
        }

        public XElement Files
        {
            get
            {
                return doc.Root.Element("files");
            }
        }

        public int FilesCount
        {
            get
            {
                return int.Parse(doc.Root.Attribute("filesCount").Value);
            }
            set
            {
                doc.Root.Attribute("filesCount").Value = value.ToString();
            }
        }

        public long FullSize
        {
            get
            {
                return long.Parse(Root.Attribute("fullSize").Value);
            }
            set
            {
                Root.Attribute("fullSize").Value = value.ToString();
            }
        }

    }

}