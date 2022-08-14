using FreeDiscussions.Plugin;
using SevenZip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Usenet.Yenc;

namespace FreeDiscussions.Plugins.Download
{

    [Serializable]
    public class Download :  ISerializable, INotifyPropertyChanged
    {
        const int MAX_FAIL_COUNT = 5;
        
        public event PropertyChangedEventHandler PropertyChanged;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        protected static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);

        private string _name;
        public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }

        private DownloadStatus _status;
        public DownloadStatus Status { get { return _status; } set { _status = value; OnPropertyChanged("Status"); } }

        public int EncodingProgress { get; set; }

        private long _sizeLoaded;
        public long SizeLoaded { get { return _sizeLoaded; } set { _sizeLoaded = value; OnPropertyChanged("SizeLoaded"); } }

        private long _sizeTotal;
        public long SizeTotal { get { return _sizeTotal; } set { _sizeTotal = value; OnPropertyChanged("SizeTotal"); } }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string GenerateFilePath(string messageId, string folder)
        {
            string downloads;
            SHGetKnownFolderPath(KnownFolder.Downloads, 0, IntPtr.Zero, out downloads);

            var destinationFolder = System.IO.Path.GetFullPath(downloads + "/FreeDiscussions/" + folder + "/");
            if (!System.IO.Directory.Exists(destinationFolder))
            {
                System.IO.Directory.CreateDirectory(destinationFolder);
            }

            var fileName = messageId;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            var destinationFile = System.IO.Path.GetFullPath(destinationFolder + fileName + ".txt");
            return destinationFile;
        }

        protected List<DownloadSegment> Segments { get; set; }
        protected string Folder { get; set; }

        Action<Download> OnUpdate;

        DateTime LastUpdate = DateTime.MinValue;

        public List<string> FilesWritten { get; set; } = new List<string>();
        public ICollection<string> Password { get; set; }

        private readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();

        private readonly ManualResetEventSlim PauseHandle = new ManualResetEventSlim(true);

        public Download(string name, string folder, List<DownloadSegment> segments, Action<Download> onUpdate)
        {
            this.Name = name;
            this.Folder = folder;
            this.Segments = segments;
            this.OnUpdate = onUpdate;

            var chars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars());
            foreach (var c in chars)
            {
                this.Folder = this.Folder.Replace(c, '_');
            }
        }

        public Download(SerializationInfo info, StreamingContext context)
        {
            try
            {
                this.Segments = (List<DownloadSegment>)info.GetValue("Segments", typeof(List<DownloadSegment>));
                this.Name = (string)info.GetValue("Name", typeof(string));
                this.Folder = (string)info.GetValue("Folder", typeof(string));
                this.Status = (DownloadStatus)info.GetValue("Status", typeof(DownloadStatus));
                this.SizeTotal = (long)info.GetValue("SizeTotal", typeof(long));
                this.SizeLoaded = (long)info.GetValue("SizeLoaded", typeof(long));
                this.Password = (ICollection<string>)info.GetValue("Password", typeof(ICollection<string>));
            } catch { }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Segments", this.Segments.ToList(), typeof(List<DownloadSegment>));
            info.AddValue("Name", this.Name, typeof(string));
            info.AddValue("Folder", this.Folder, typeof(string));
            info.AddValue("Status", this.Status, typeof(DownloadStatus));
            info.AddValue("SizeTotal", this.SizeTotal, typeof(long));
            info.AddValue("SizeLoaded", this.SizeLoaded, typeof(long));
            info.AddValue("Password", this.Password, typeof(ICollection<string>));
        }

        private void Update(Action ac, bool throttle = false)
        {
            if (throttle)
            {
                if (TimeSpan.FromTicks(DateTime.Now.Ticks - LastUpdate.Ticks).TotalMilliseconds < 500)
                    return;
            }


            LastUpdate = DateTime.Now;
            

            if (Panel.Instance == null)
            {
                ac();
            } else
            {
                Panel.Instance?.Dispatcher.Invoke(ac);
            }

            if (this.OnUpdate != null)
            {
                this.OnUpdate(this);
               
            }
        }

        private async Task<bool> downloadSegment(DownloadSegment segment, string folder, Client client, Throttled th)
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        this.Update(() =>
                        {
                            this.Status = DownloadStatus.Cancelled;
                        });
                        return false;
                    }

                    PauseHandle.Wait();

                    if (segment.IsComplete) return true;

                    List<string> content = new List<string>();

                    var article = client.Nntp.Body(segment.MessageId);
                    if (!article.Success)
                    {
                        segment.HasFailed = true;
                        continue;
                    }

                    // download throttled
                    long size;
                    long begin;
                    long end;

                    var segmentFile = new FileInfo(Path.Combine(folder, "cache", segment.UUID));
                    segment.CacheFile = segmentFile.FullName;

                    if (!segmentFile.Directory.Exists)
                    {
                        segmentFile.Directory.Create();
                    }


                    var x = article.Article.Body.ToList();
                    var body = String.Join(Environment.NewLine,x);
                    File.WriteAllText(segmentFile.FullName, body);

                    segment.IsComplete = true;
                }
            }
            catch (Exception ex)
            {
                segment.HasFailed = true;
                Console.WriteLine(ex.Message);
            }
            
            return segment.IsComplete;
        }

        public async Task Start()
        {
            this.Update(() =>
            {
                if (this.Status == DownloadStatus.Idle)
                    this.Status = DownloadStatus.Downloading;
            });

            Console.WriteLine("Start");
            var settings = SettingsModel.Read();
                var folder = "";
            SHGetKnownFolderPath(KnownFolder.Downloads, 0, IntPtr.Zero, out folder);
                folder = System.IO.Path.GetFullPath(folder + "/FreeDiscussions/" + this.Folder + "/");
            Func<long> alreadyLoaded = () => this.Segments.Where(x => x.IsComplete).Sum(x => x.SizeTotal);
            var th = new Throttled() { MaxBytesPerSecond = settings.HasDownloadLimit ? settings.MaxBytesPerSecond * 131072 : long.MaxValue };
              

            if (this.Segments.Count == 1 && this.Segments[0].FileName == null)
            {
                // probably an article
            } else
            {
                var files = this.Segments.GroupBy(x => x.FileName);
                Console.WriteLine(files);
            }

            await Task.WhenAll(new List<Task> {
                Task.Run(async () =>
                {
                    // download
                    if (this.Status == DownloadStatus.Downloading) {
                        DownloadSegments s = new DownloadSegments(this.Segments);
                        await s.Start(async (seg, client) =>
                        {

                            await downloadSegment(seg, folder, client, th);
                            return true;
                        });


                        this.Update(() => {
                            if (this.Status == DownloadStatus.Downloading) {
                                this.Status = DownloadStatus.Decoding;
                                this.EncodingProgress = 0;
                            }
                        });
                    };

                    // decoding
                    if (this.Segments.Where(x => x.IsComplete).Count() == this.Segments.Count)
                    {
                        if (this.Status == DownloadStatus.Decoding) {
                            var done = 0;
                            foreach (var segment in Segments)
                            {
                                if (!File.Exists(segment.CacheFile))
                                {
                                    //segment.HasFailed = true;
                                    continue;
                                }
                                
                                var content = File.ReadLines(segment.CacheFile).ToList();
                                if (content.Count == 0) continue;

                                bool isYenc = String.Join("\n", content).IndexOf("=ybegin") != -1;

                                if (isYenc) {
                                    using (YencStream yencStream = YencStreamDecoder.Decode(content))
                                    {
                                        try
                                        {
                                            YencHeader header = yencStream.Header;

                                            var destinationFolder = folder;
                                            var fileName = System.IO.Path.Combine(destinationFolder, header.FileName);

                                            if (!File.Exists(fileName))
                                            {
                                                // create file and pre-allocate disk space for it
                                                using (FileStream stream = File.Create(fileName))
                                                {
                                                    stream.SetLength(header.FileSize);
                                                }
                                            }

                                            // write to file
                                            using (FileStream stream = File.Open(
                                                fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                                            {
                                                // copy incoming parts to file
                                                stream.Position = header.PartOffset;
                                                yencStream.CopyTo(stream);
                                            }


                                            if (!FilesWritten.Contains(fileName))
                                            {
                                                FilesWritten.Add(fileName);
                                            }

                                            segment.LocalFile = fileName;
                                        }
                                        catch (Exception ex)
                                        {
                                            // used by another process ?? which 
                                            Console.WriteLine(ex);
                                        }
                                    }
                                }
                                else
                                {
                                    // text article 
                                    var destinationFolder = folder;

                                    var fileName = segment.MessageId;
                                    foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                                    {
                                        fileName = fileName.Replace(c, '_');
                                    }

                                    fileName = System.IO.Path.Combine(destinationFolder, fileName + ".txt");

                                    segment.LocalFile = fileName;

                                    File.WriteAllText(fileName, String.Join("\n", content));
                                    FilesWritten.Add(fileName);

                                }

                                try
                                {
                                    File.Delete(segment.CacheFile);
                                }
                                catch (Exception ex)
                                {
                                    //Console.WriteLine(ex);
                                }

                                done++;

                                this.Update(() => {
                                    var p = (100.0 / this.Segments.Count) * done;
                                    this.EncodingProgress = (int) p;
                                });
                            }


                            this.Update(() => {
                                var failedCount = this.Segments.Where(x => x.HasFailed).Count();
                                if (failedCount < MAX_FAIL_COUNT) {
                                    this.Status = DownloadStatus.Verifying;
                                } else
                                {
                                    this.Status = DownloadStatus.Failed;
                                }
                            });
                        }

                        if (this.Status == DownloadStatus.Verifying || this.Status == DownloadStatus.Repairing) {
                        
                            // Verifying
                            var par2File = this.Segments.Where(x => x.LocalFile != default(string) && x.LocalFile.ToLower().EndsWith(".par2")).ToList();
                            if (par2File.Count != 0)
                            {
                                // verify
                                var exitCode = 0;
                                string localPath = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
                                var par2ExePath = Path.Combine(localPath, "tools", "par2.exe");
                                var success = false;
                                string output = "";
                                string err = "";

                                Serilog.Log.Logger.Information("Verifying:");

                                try
                                {
                                    Serilog.Log.Logger.Information("Par2 Exe:" + par2ExePath);
                                    Serilog.Log.Logger.Information("Args" + "verify \"" + par2File[0].LocalFile + "\"");
                                    var process = new Process
                                    {
                                        StartInfo =
                                        {
                                        FileName = par2ExePath,
                                        Arguments = "verify \"" + par2File[0].LocalFile + "\"",
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardOutput = true,
                                        WorkingDirectory = new FileInfo(par2ExePath).Directory.FullName,
                                        RedirectStandardError = true,
                                        }
                                    };

                                    Serilog.Log.Logger.Information("Start...");
                                    success = process.Start();
                                    output = process.StandardOutput.ReadToEnd();
                                    err = process.StandardError.ReadToEnd();
                                    exitCode = process.ExitCode;
                                    Serilog.Log.Logger.Information("Wait for exit");
                                    process.WaitForExit();
                                    Serilog.Log.Logger.Information("done. Exitcode:" + process.ExitCode);

                                } catch (Exception ex)
                                {
                                    Serilog.Log.Logger.Error(ex.Message + "\n" + ex.StackTrace);
                                }

                                if (exitCode == 0)
                                {
                                    // nothing to do
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Extracting;
                                    });

                                }
                                else if (exitCode == 1)
                                {
                                    // Data files are damaged and there is enough recovery data available to repair them.
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Repairing;
                                    });

                                   var  process = new Process
                                    {
                                        StartInfo =
                                            {
                                                FileName = par2ExePath,
                                                Arguments = "repair \"" + par2File[0].LocalFile + "\"",
                                                UseShellExecute = false,
                                                CreateNoWindow = true,
                                                RedirectStandardOutput = true,
                                                WorkingDirectory = new FileInfo(par2ExePath).Directory.FullName,
                                                RedirectStandardError = true,
                                            }
                                    };
                                    success = process.Start();
                                    output = process.StandardOutput.ReadToEnd();
                                    err = process.StandardError.ReadToEnd();
                                    exitCode = process.ExitCode;

                                    if (exitCode == 0)
                                    {
                                        this.Update(() =>
                                        {
                                            this.Status = DownloadStatus.Extracting;
                                        });
                                    } else
                                    {
                                        this.Update(() =>
                                        {
                                            this.Status = DownloadStatus.Damaged;
                                        });
                                    }

                                }
                                else if (exitCode == 2)
                                {
                                    // Data files are damaged and there is insufficient recovery data available to be able to repair them.
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Damaged;
                                    });
                                }
                                else if (exitCode == 3)
                                {
                                    //  There was something wrong with the command line arguments
                                    throw new InvalidOperationException("par2 args wrong");
                                }
                                else if (exitCode == 4)
                                {
                                    // The PAR2 files did not contain sufficient information about the data files to be able to verify them.
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Damaged;
                                    });
                                }
                                else if (exitCode == 5)
                                {
                                    // Repair completed but the data files still appear to be damaged.
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Damaged;
                                    });
                                }
                                else if (exitCode == 6)
                                {
                                    // An error occured when accessing files
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Damaged;
                                    });
                                }
                                else if (exitCode == 7)
                                {
                                    // In internal error occurred
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Damaged;
                                    });
                                }
                                else if (exitCode == 8)
                                {
                                    // Out of memory
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.Damaged;
                                    });
                                }
                                else
                                {
                                    throw new InvalidOperationException("unknow par2.exe exitcode " + exitCode);
                                }
                            } else
                            {
                                this.Update(() =>
                                {
                                    this.Status = DownloadStatus.Extracting;
                                });
                            }
                        }

                        if (this.Status == DownloadStatus.Extracting) {

                            var rarFile = this.Segments.FirstOrDefault(x => x.LocalFile != default(string) && x.LocalFile.ToLower().EndsWith(".rar"));
                            if (rarFile != default(DownloadSegment))
                            {
                                try
                                {
                                    // Toggle between the x86 and x64 bit dll
                                    //var path = Path.Combine(new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName, Environment.Is64BitProcess ? "x64" : "x86", "7z.dll");
                                    //SevenZip.SevenZipBase.SetLibraryPath(path);

                                    //var extractor = new SevenZipExtractor(rarFile.LocalFile, this.Password != null && this.Password.Count != 0 ? this.Password.First() : null);
                                    //var lnfo = new FileInfo(rarFile.LocalFile);
                                    //var targetPath = Path.Combine(lnfo.Directory.FullName);
                                    //extractor.ExtractArchive(targetPath);
                                    //extractor.Dispose();
                                    string localPath = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
                                    var zipPath = Path.Combine(localPath, "tools", "unrar.exe");

                                    var destination = new FileInfo(rarFile.LocalFile).Directory.FullName;

                                    var args = $"e \"{rarFile.LocalFile}\" \"{destination}\"";
                                    if (this.Password != null && this.Password.Count != 0)
                                    {
                                        args += $" -p\"{this.Password.First()}\"";
                                    }


                                    var timeout = 10 * 1000;
                                    var lastOutput = DateTime.Now;
                                    var exitCode = -1;
                                    using (Process process = new Process())
                                    {
                                        process.StartInfo.FileName = zipPath;
                                        process.StartInfo.Arguments = args;
                                        process.StartInfo.UseShellExecute = false;

                                        process.StartInfo.RedirectStandardOutput = true;
                                        process.StartInfo.RedirectStandardError = true;
                                        process.StartInfo.CreateNoWindow = true;

                                        StringBuilder output = new StringBuilder();
                                        StringBuilder error = new StringBuilder();


                                        process.OutputDataReceived += (sender, e) => {
                                            output.AppendLine(e.Data);
                                            lastOutput = DateTime.Now;   
                                        };
                                        process.ErrorDataReceived += (sender, e) =>
                                        {
                                            error.AppendLine(e.Data);
                                            if (!String.IsNullOrEmpty(e.Data))
                                            {
                                                process.Kill(true);
                                                        
                                            }              
                                        };

                                        process.Start();

                                        process.BeginOutputReadLine();
                                        process.BeginErrorReadLine();
                                            
                                        while (!process.HasExited)
                                        {
                                            if (DateTime.Now - lastOutput > TimeSpan.FromMilliseconds(timeout))
                                            {
                                                process.Kill(true);
                                            }
  
                                            Thread.Sleep(1000);
                                        }
                                            
                                        exitCode = process.ExitCode;
                                    }
                                    

                                    if (exitCode == 0) {
                                        // delete segment files
                                        var files = this.Segments.Select(x => x.LocalFile).Distinct();
                                        foreach (var file in files)
                                        {
                                            try
                                            {
                                                File.Delete(file);
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.Message);
                                            }
                                        }

                                        this.Update(() =>
                                        {
                                            this.Status = DownloadStatus.Finished;
                                            Panel.Instance.Refresh();
                                        });
                                    } else
                                    {
                                        this.Update(() =>
                                        {
                                            this.Status = DownloadStatus.ExtractingFailed;
                                            Panel.Instance.Refresh();
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this.Update(() =>
                                    {
                                        this.Status = DownloadStatus.ExtractingFailed;
                                    });
                                }
                            }
                        }
                    }
                }),
                Task.Run(async () =>
                {
                    // update status
                    while (this.Status == DownloadStatus.Downloading)
                    {
                        this.Update(() => {
                            this.SizeLoaded = alreadyLoaded();
                            this.SizeTotal = this.Segments.Sum(x => x.SizeTotal);
                        });

                        var failedParts = this.Segments.Where(x => x.HasFailed).Count();
                        if (failedParts >= MAX_FAIL_COUNT)
                        {
                             this.Update(() => {
                                this.Status = DownloadStatus.Failed;
                                this.SizeLoaded = alreadyLoaded();
                                this.SizeTotal = this.Segments.Sum(x => x.SizeTotal);
                            });
                            CancellationToken.Cancel();
                        }
        
                        Thread.Sleep(1000);
                    }
                })
            });
            

           

            
            //try
            //{
            //    var settings = SettingsModel.Read();
            //    var folder = "";
            //    var failedParts = 0;
            //    Func<long> alreadyLoaded = () => this.Segments.Where(x => x.IsComplete).Sum(x => x.SizeTotal);
            //    SHGetKnownFolderPath(KnownFolder.Downloads, 0, IntPtr.Zero, out folder);
            //    folder = System.IO.Path.GetFullPath(folder + "/FreeDiscussions/" + this.Folder + "/");
            //    if (!System.IO.Directory.Exists(folder))
            //    {
            //        System.IO.Directory.CreateDirectory(folder);
            //    }

            //    using (var _client = await ConnectionManager.GetClient())
            //    {
            //        var client = _client.Nntp;

            //        if (client == null)
            //        {
            //            this.Update(() =>
            //            {
            //                this.Status = DownloadStatus.Failed;
            //                this.SizeTotal = this.Segments.Sum(x => x.SizeTotal);
            //            });
            //            return;
            //        }

            //        try
            //        {

            //            this.Update(() =>
            //            {
            //                this.Status = DownloadStatus.Running;
            //                this.SizeTotal = this.Segments.Sum(x => x.SizeTotal);
            //                this.SizeLoaded = alreadyLoaded();
            //            });

            //            
            // später

            //foreach (var line in article.Article.Body)
            //{
            //    if (line.Contains("=ybegin"))
            //    {
            //        // get total size
            //        // >>   
            //        Regex rg = new Regex(@"size=(?<size>[^\s]*)\s");
            //        MatchCollection matches = rg.Matches(line);
            //        if (matches.Count != 0)
            //        {
            //            size = long.Parse(matches[0].Groups["size"].Value);

            //        }
            //    }
            //    else if (line.Contains("=ypart"))
            //    {
            //        Regex rg = new Regex(@"begin=(?<begin>[^\s]*)\send=(?<end>[^\s]*)");
            //        MatchCollection matches = rg.Matches(line);
            //        if (matches.Count != 0)
            //        {
            //            begin = long.Parse(matches[0].Groups["begin"].Value);
            //            end = long.Parse(matches[0].Groups["end"].Value);

            //            this.Update(() =>
            //            {
            //                if (this.SizeTotal == 0)
            //                {
            //                    this.SizeTotal = end - begin;
            //                }
            //            });

            //        }
            //        isYenc = true;
            //    }

            //    content.Add(line);


            //    th.Tick(client.Nntp.BytesRead);


            //    this.Update(() =>
            //    {
            //        this.SizeLoaded = alreadyLoaded();
            //    }, true);
            //}



            //if (isYenc)
            //{
            //// yenc
            //using (YencStream yencStream = YencStreamDecoder.Decode(content))
            //{
            //    try
            //    {
            //        YencHeader header = yencStream.Header;

            //        var destinationFolder = folder;
            //        var fileName = System.IO.Path.Combine(destinationFolder, header.FileName);

            //        if (!File.Exists(fileName))
            //        {
            //            // create file and pre-allocate disk space for it
            //            using (FileStream stream = File.Create(fileName))
            //            {
            //                stream.SetLength(header.FileSize);
            //            }
            //        }

            //        while (true)
            //        {
            //            try
            //            {
            //                using (FileStream stream = File.Open(
            //                    fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            //                {
            //                    // copy incoming parts to file
            //                    stream.Position = header.PartOffset;
            //                    yencStream.CopyTo(stream);
            //                }
            //                break;
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex);
            //            }
            //        }

            //        if (!FilesWritten.Contains(fileName))
            //        {
            //            FilesWritten.Add(fileName);
            //        }

            //        Segment.LocalFile = fileName;
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex);
            //    }
            //}
            //}
            //else
            //{
            //    // text article 
            //    var destinationFolder = folder;

            //    var fileName = Segment.MessageId;
            //    foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            //    {
            //        fileName = fileName.Replace(c, '_');
            //    }

            //    fileName = System.IO.Path.Combine(destinationFolder, fileName + ".txt");

            //    Segment.LocalFile = fileName;

            //    File.WriteAllText(fileName, String.Join("\n", content));
            //    FilesWritten.Add(fileName);

            //}

            //                        Segment.IsComplete = true;
            //                    }
            //                    return true;
            //                }
            //                catch (Exception ex)
            //                {
            //                    return false;
            //                }
            //            };

            //            using (var th = new Throttled() { MaxBytesPerSecond = settings.HasDownloadLimit ? settings.MaxBytesPerSecond * 131072 : long.MaxValue })
            //            {

            //                // downloadFile => downloadSegment
            //                var dSegments = new DownloadSegments(this.Segments.Where(x => !x.IsComplete).ToList());
            //                await dSegments.Start((segment, client) =>
            //                {
            //                    Console.WriteLine("s");
            //                    return downloadFile(new List<DownloadSegment> { segment }, client, th);
            //                });

            //            }

            //            if (this.Status == DownloadStatus.Failed || this.Status == DownloadStatus.Cancelled)
            //            {
            //                return;
            //            }

            //            this.Update(() =>
            //            {
            //                this.Status = DownloadStatus.Finished;
            //            });



            //            this.Update(() =>
            //            {
            //                this.Status = DownloadStatus.Finished;
            //            });

            //            return; // den rest später


            //            try
            //            {





            //                var par2File = this.Segments.Where(x => x.FileName != default(string) && x.FileName.ToLower().EndsWith(".par2")).ToList();
            //                if (par2File.Count != 0)
            //                {
            //                    // verify
            //                    this.Update(() =>
            //                    {
            //                        this.Status = DownloadStatus.Verifying;
            //                    });

            //                    var exitCode = 0;
            //                    string localPath = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
            //                    var par2ExePath = Path.Combine(localPath, "tools", "par2.exe");
            //                    var success = false;
            //                    string output = "";
            //                    string err = "";

            //                    Serilog.Log.Logger.Information("Verifying:");

            //                    try
            //                    {
            //                        Serilog.Log.Logger.Information("Par2 Exe:" + par2ExePath);
            //                        Serilog.Log.Logger.Information("Args" + "verify \"" + par2File[0].LocalFile + "\"");
            //                        var process = new Process
            //                        {
            //                            StartInfo =
            //                            {
            //                            FileName = par2ExePath,
            //                            Arguments = "verify \"" + par2File[0].LocalFile + "\"",
            //                            UseShellExecute = false,
            //                            CreateNoWindow = true,
            //                            RedirectStandardOutput = true,
            //                            WorkingDirectory = new FileInfo(par2ExePath).Directory.FullName,
            //                            RedirectStandardError = true,
            //                            }
            //                        };

            //                        Serilog.Log.Logger.Information("Start...");
            //                        success = process.Start();
            //                        output = process.StandardOutput.ReadToEnd();
            //                        err = process.StandardError.ReadToEnd();
            //                        exitCode = process.ExitCode;
            //                        Serilog.Log.Logger.Information("Wait for exit");
            //                        process.WaitForExit();
            //                        Serilog.Log.Logger.Information("done. Exitcode:" + process.ExitCode);

            //                    } catch (Exception ex)
            //                    {
            //                        Serilog.Log.Logger.Error(ex.Message + "\n" + ex.StackTrace);
            //                    }

            //                    if (exitCode == 0)
            //                    {
            //                        // nothing to do
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Verified;
            //                        });

            //                    }
            //                    else if (exitCode == 1)
            //                    {
            //                        // Data files are damaged and there is enough recovery data available to repair them.
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Repairing;
            //                        });

            //                       var  process = new Process
            //                        {
            //                            StartInfo =
            //                                {
            //                                    FileName = par2ExePath,
            //                                    Arguments = "repair \"" + par2File[0].LocalFile + "\"",
            //                                    UseShellExecute = false,
            //                                    CreateNoWindow = true,
            //                                    RedirectStandardOutput = true,
            //                                    WorkingDirectory = new FileInfo(par2ExePath).Directory.FullName,
            //                                    RedirectStandardError = true,
            //                                }
            //                        };
            //                        success = process.Start();
            //                        output = process.StandardOutput.ReadToEnd();
            //                        err = process.StandardError.ReadToEnd();
            //                        exitCode = process.ExitCode;

            //                        if (exitCode == 0)
            //                        {
            //                            this.Update(() =>
            //                            {
            //                                this.Status = DownloadStatus.Verified;
            //                            });
            //                        } else
            //                        {
            //                            this.Update(() =>
            //                            {
            //                                this.Status = DownloadStatus.Damaged;
            //                            });
            //                        }

            //                    }
            //                    else if (exitCode == 2)
            //                    {
            //                        // Data files are damaged and there is insufficient recovery data available to be able to repair them.
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Damaged;
            //                        });
            //                    }
            //                    else if (exitCode == 3)
            //                    {
            //                        //  There was something wrong with the command line arguments
            //                        throw new InvalidOperationException("par2 args wrong");
            //                    }
            //                    else if (exitCode == 4)
            //                    {
            //                        // The PAR2 files did not contain sufficient information about the data files to be able to verify them.
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Damaged;
            //                        });
            //                    }
            //                    else if (exitCode == 5)
            //                    {
            //                        // Repair completed but the data files still appear to be damaged.
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Damaged;
            //                        });
            //                    }
            //                    else if (exitCode == 6)
            //                    {
            //                        // An error occured when accessing files
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Damaged;
            //                        });
            //                    }
            //                    else if (exitCode == 7)
            //                    {
            //                        // In internal error occurred
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Damaged;
            //                        });
            //                    }
            //                    else if (exitCode == 8)
            //                    {
            //                        // Out of memory
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Damaged;
            //                        });
            //                    }
            //                    else
            //                    {
            //                        throw new InvalidOperationException("unknow par2.exe exitcode " + exitCode);
            //                    }
            //                }

            //                // entpacken
            //                if (this.Status == DownloadStatus.Finished || this.Status == DownloadStatus.Verified)
            //                {
            //                    var rarFile = this.Segments.FirstOrDefault(x => x.LocalFile != default(string) && x.LocalFile.ToLower().EndsWith(".rar"));
            //                    if (rarFile != default(DownloadSegment))
            //                    {
            //                        this.Update(() =>
            //                        {
            //                            this.Status = DownloadStatus.Extracting;
            //                        });

            //                        try
            //                        {
            //                            // Toggle between the x86 and x64 bit dll
            //                            var path = Path.Combine(new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName, Environment.Is64BitProcess ? "x64" : "x86", "7z.dll");
            //                            SevenZip.SevenZipBase.SetLibraryPath(path);

            //                            var extractor = new SevenZipExtractor(rarFile.LocalFile, "password_here");
            //                            var lnfo = new FileInfo(rarFile.LocalFile);
            //                            var targetPath = Path.Combine(lnfo.Directory.FullName);
            //                            extractor.ExtractArchive(targetPath);
            //                            extractor.Dispose();

            //                            // delete segment files
            //                            foreach (var segment in this.Segments)
            //                            {
            //                                try
            //                                {
            //                                    File.Delete(segment.LocalFile);
            //                                }
            //                                catch
            //                                {
            //                                }
            //                            }

            //                            this.Update(() =>
            //                            {
            //                                this.Status = DownloadStatus.Extracted;
            //                            });
            //                        }
            //                        catch (Exception ex)
            //                        {
            //                            this.Update(() =>
            //                            {
            //                                this.Status = DownloadStatus.ExtratingFailind;
            //                            });
            //                        }
            //                    }
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine(ex);
            //            }
            //        }
            //        finally
            //        {
            //            this.Update(() =>
            //            {
            //                this.SizeLoaded = alreadyLoaded();

            //                Panel.Instance.Refresh();
            //            });
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    if (ex is AuthException)
            //    {
            //        this.Update(() =>
            //        {
            //            this.Status = DownloadStatus.AuthError;
            //            DownloadController.Instance.Pause();
            //            Panel.Instance.Refresh();
            //        });
            //    }
            //    Console.WriteLine(ex);
            //}
        }

        private static void WaitForFile(string filename)
        {
            //This will lock the execution until the file is ready
            //TODO: Add some logic to make it async and cancelable
            while (!IsFileReady(filename)) { }
        }

        private  static bool IsFileReady(string filename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void Pause()
        {
            PauseHandle.Reset();
            this.Update(() =>
            {
                this.Status = DownloadStatus.Paused;
            });
        }

        internal void Resume()
        {
            PauseHandle.Set();
            this.Update(() =>
            {
                this.Status = DownloadStatus.Downloading;
            });
            Task.Run(() => Start());
        }

        internal void Delete()
        { 
            this.CancellationToken.Cancel();
            foreach (var file in FilesWritten)
            {
                try
                {
                    new FileInfo(file).Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        internal void Cancel()
        {
            this.CancellationToken.Cancel();

            // delete all files
            foreach (var file in FilesWritten)
            {
                try
                {
                    new FileInfo(file).Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }

    internal class DownloadSegments
    {
        private List<DownloadSegment> Segments;
        private int ThreadCount = 10;

        public DownloadSegments(List<DownloadSegment> segments, int threadCount = 5)
        {
            this.Segments = segments;
            this.ThreadCount = threadCount;
        }

        internal async Task Start(Func<DownloadSegment, Client, Task<bool>> download)
        {

            // create and fill queue
            var queue = new ConcurrentQueue<DownloadSegment>();
            foreach (var seg in this.Segments) {
                queue.Enqueue(seg);
            }

            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(async () =>
            {
                DownloadSegment seg;
                using (var client = await ConnectionManager.GetClient())
                {
                    while (queue.TryDequeue(out seg))
                    {
                        await download(seg, client);
                    }
                }
            }));
            await Task.WhenAll(tasks);
        }
    }
}
