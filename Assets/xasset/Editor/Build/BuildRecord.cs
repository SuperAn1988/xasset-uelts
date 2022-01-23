using System;
using System.Collections.Generic;

namespace xasset.editor
{
    [Serializable]
    public class BuildRecord
    {
        public string file;
        public long size;
        public long timestamp;
        public string date;
        public string platform;
        public float elapsed;
        public List<string> changes = new List<string>();
        public string build;
    }
}