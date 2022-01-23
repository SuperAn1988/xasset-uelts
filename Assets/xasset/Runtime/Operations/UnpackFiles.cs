using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace xasset
{
    public sealed class UnpackFiles : Operation
    {
        public static long MaxBandwidth = 1024 * 1024 * 10; // 10 MB 
        private readonly byte[] _readBuffer = new byte[4096];
        private long _bandWidth;
        private string _message;
        public string name;
        public Action<string, float> updated;
        private bool dirty { get; set; }

        public override void Start()
        {
            base.Start();
            var thread = new Thread(Run);
            thread.Start();
        }

        private void UpdateBandwidth(ref DateTime startTime)
        {
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            while (MaxBandwidth > 0 && status == OperationStatus.Processing && _bandWidth >= MaxBandwidth &&
                   elapsed < 1000)
            {
                var wait = Mathf.Clamp((int)(1000 - elapsed), 1, 33);
                Thread.Sleep(wait);
                elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            }

            if (!(elapsed >= 1000))
            {
                return;
            }

            startTime = DateTime.Now;
            _bandWidth = 0L;
        }

        protected override void Update()
        {
            if (status != OperationStatus.Processing)
            {
                return;
            }

            if (!dirty)
            {
                return;
            }

            updated?.Invoke(_message, progress);
            dirty = false;
        }

        private void Run()
        {
            if (!File.Exists(name))
            {
                error = "File not found.";
                status = OperationStatus.Failed;
                return;
            }

            try
            {
                Unpacking();
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogException(e);
                status = OperationStatus.Failed;
            }
        }

        private void Unpacking()
        {
            var startTime = DateTime.Now;
            _bandWidth = 0;
            using (var reader = new BinaryReader(File.OpenRead(name)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var bundle = reader.ReadString();
                    var nameWithAppendHash = reader.ReadString();
                    var hash = reader.ReadString();
                    var size = reader.ReadInt64();
                    var getBundle = Versions.GetBundle(bundle);
                    if (getBundle != null)
                    {
                        if (getBundle.size != size)
                        {
                            error = "getBundle.size != size";
                            break;
                        }

                        if (getBundle.hash != hash)
                        {
                            error = "getBundle.crc != crc";
                            break;
                        }
                    }

                    var file = Downloader.GetDownloadDataPath(nameWithAppendHash);
                    var info = new FileInfo(file);
                    dirty = true;
                    _message = nameWithAppendHash;
                    if (info.Exists && info.Length == size && Utility.ComputeHash(file) == hash)
                    {
                        reader.BaseStream.Seek(size, SeekOrigin.Current);
                    }
                    else
                    {
                        using (var fs = File.OpenWrite(file))
                        {
                            var len = 0;
                            while (len < (int)size)
                            {
                                var read = reader.Read(_readBuffer, 0, Math.Min(_readBuffer.Length, (int)size - len));
                                if (read > 0)
                                {
                                    fs.Write(_readBuffer, 0, read);
                                    len += read;
                                    _bandWidth += read;
                                }

                                progress = reader.BaseStream.Position * 1f / reader.BaseStream.Length;
                                UpdateBandwidth(ref startTime);
                            }
                        }
                    }

                    progress = reader.BaseStream.Position * 1f / reader.BaseStream.Length;
                }

                status = string.IsNullOrEmpty(error) ? OperationStatus.Success : OperationStatus.Failed;
            }

            File.Delete(name);
        }
    }
}