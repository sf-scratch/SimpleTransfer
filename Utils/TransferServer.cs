using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net.NetworkInformation;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Diagnostics;
using ImTools;
using System.Reflection;
using System.Net.Http;

namespace SimpleTransfer.Utils
{
    public class TransferServer
    {
        public static readonly int BufferSize = 131072;//建议的缓冲区大小是不小于4KB且不大于2MB的任何2的幂
        private readonly int UdpClientToReceiveIP_Port = 31000;
        private readonly int ListenReceiveFile_Port = 11001;
        public event Action<SendFileProgress> AddSendFileProgressEvent;
        public event Action<ReceiveFileProgress> AddReceiveFileProgressEvent;
        public string SaveFolder { get; set; }
        public string IdCode { get; set; }
        public bool IsNotTransferLocal { get; set; } = true;//是否不能发送到本地
        private ConcurrentQueue<string> TransferFilePathQueue {  get; set; }
        private ConcurrentQueue<string> ConnectedIPQueue {  get; set; }
        private UdpClient _udpClientListenReceiveIP;
        private TcpListener _tcpListener;
        private CancellationTokenSource _ctsSendLocalHostIP;

        public TransferServer(string saveFolder, string idCode)
        {
            SaveFolder = saveFolder;
            IdCode = idCode;
            TransferFilePathQueue = new ConcurrentQueue<string>();
            ConnectedIPQueue = new ConcurrentQueue<string>();
        }

        public void Start()
        {
            StartReceiveIPAddress();
            StartListenReceiveFileConnecttion();
        }

        private void Stop()
        {
            _ctsSendLocalHostIP.Cancel();
        }

        public async Task SendFiles(string[] files)
        {
            foreach (var file in files)
            {
                TransferFilePathQueue.Enqueue(file);
                await SendLocalHostIP();
                await Task.Delay(1000);
                TransferFilePathQueue.TryDequeue(out string outTimeFile);
                if (ConnectedIPQueue.Count == 0)
                {
                    MessageBox.Show($"没有主机接收文件: {outTimeFile}");
                }
                else
                {
                    while (ConnectedIPQueue.TryDequeue(out string ipAddress)) { }
                }
            }
        }

        private async Task SendFileByTCP(string filePath, TcpClient tcpClient, CancellationToken token)
        {
            //回收TcpClient的资源
            try
            {
                NetworkStream stream = tcpClient.GetStream();
                //发送文件前，必须先接收idCode
                int idCodeBytesLength = 4;
                byte[] buffer = new byte[BufferSize];
                if (await stream.ReadAsync(buffer, 0, idCodeBytesLength, token) != idCodeBytesLength)
                {
                    return;
                }
                string receiveIdCode = Encoding.UTF8.GetString(buffer, 0, idCodeBytesLength);
                //判断接收的idCode和本地的是否相等
                if (receiveIdCode != IdCode)
                {
                    return;
                }

                //开始发送文件
                #region 校验信息
                FileInfo fileInfo = new FileInfo(filePath);
                long fileLength = fileInfo.Length;
                string fileName = fileInfo.Name;
                byte[] fileNameData = Encoding.UTF8.GetBytes(fileName);
                long totalBytes = sizeof(long) + sizeof(long) + fileNameData.Length + fileLength;
                long fileNameBytes = fileNameData.Length;

                byte[] totalBytesData = BitConverter.GetBytes(totalBytes);
                await stream.WriteAsync(totalBytesData, 0, totalBytesData.Length, token);//总字节数

                byte[] fileNameBytesData = BitConverter.GetBytes(fileNameBytes);
                await stream.WriteAsync(fileNameBytesData, 0, fileNameBytesData.Length, token);//文件名字节数

                await stream.WriteAsync(fileNameData, 0, fileNameData.Length, token);//文件名
                #endregion

                SendFileProgress progress = new SendFileProgress(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString(), fileName)
                {
                    TotalBytesLength = fileLength
                };
                AddSendFileProgressEvent?.Invoke(progress);

                long sendTotalBytes = 0;
                using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open))
                {
                    int numBytesRead;
                    while ((numBytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, numBytesRead, token);
                        sendTotalBytes += numBytesRead;
                        progress.TransferredBytesLength = sendTotalBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                tcpClient?.Close();
            }
        }

        private async Task ReceiveFileByTCP(string saveFolder, TcpClient tcpClient, CancellationToken token)
        {
            NetworkStream stream = tcpClient.GetStream();
            //发送本地idCode给发送端验证身份
            byte[] idCodeBytes = Encoding.UTF8.GetBytes(IdCode);
            stream.Write(idCodeBytes, 0, idCodeBytes.Length);

            #region 校验
            byte[] buffer = new byte[BufferSize];
            int longSize = sizeof(long);
            long totalBytes;
            long fileNameBytes;
            string fileName;
            if (await stream.ReadAsync(buffer, 0, longSize, token) == longSize)
            {
                totalBytes = BitConverter.ToInt64(buffer, 0);
            }
            else
            {
                MessageBox.Show("读取totalBytes错误");
                return;
            }
            if (await stream.ReadAsync(buffer, 0, longSize, token) == longSize)
            {
                fileNameBytes = BitConverter.ToInt64(buffer, 0);
            }
            else
            {
                MessageBox.Show("读取fileNameBytes错误");
                return;
            }
            if (await stream.ReadAsync(buffer, 0, (int)fileNameBytes, token) == (int)fileNameBytes)
            {
                fileName = Encoding.UTF8.GetString(buffer, 0, (int)fileNameBytes);
            }
            else
            {
                MessageBox.Show("读取fileName错误");
                return;
            }
            #endregion

            long fileLength = totalBytes - (sizeof(long) + sizeof(long) + fileNameBytes);
            ReceiveFileProgress progress = new ReceiveFileProgress(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString(), fileName)
            {
                TotalBytesLength = fileLength
            };
            AddReceiveFileProgressEvent?.Invoke(progress);

            //先存到临时文件
            string tempFileName = string.Format("{0}\\{1}{2}", saveFolder, Guid.NewGuid().ToString(), ".tmp");
            long acceptTotalBytes = 0;
            using (FileStream fileStream = new FileStream(tempFileName, FileMode.Create))
            {
                int numBytesRead;
                while ((numBytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    fileStream.Write(buffer, 0, numBytesRead);
                    acceptTotalBytes += numBytesRead;
                    progress.TransferredBytesLength = acceptTotalBytes;
                }
            }

            string newFileName = string.Format("{0}\\{1}", saveFolder, fileName);
            // 确保目标文件名不存在
            if (File.Exists(newFileName))
            {
                FileInfo fileInfo = new FileInfo(fileName);
                string prefix = fileName.Replace(fileInfo.Extension, "");
                string suffix = fileInfo.Extension;
                newFileName = string.Format("{0}\\{1}({2}){3}", saveFolder, prefix, DateTime.Now.ToString("yyyyMMdd_HHmmss"), suffix);
            }
            File.Move(tempFileName, newFileName);
        }

        private Task StartListenReceiveFileConnecttion()
        {
            _tcpListener = new TcpListener(IPAddress.Any, ListenReceiveFile_Port);
            _tcpListener.Start();
            CancellationToken token = _ctsSendLocalHostIP.Token;
            Task task = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                        //检查等待发送的文件集合
                        if (TransferFilePathQueue.TryPeek(out string filePath))
                        {
                            ConnectedIPQueue.Enqueue(((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
                            SendFileByTCP(filePath, tcpClient, token);
                        }
                    }
                }
                catch(Exception ex) 
                {
                    MessageBox.Show(ex.Message);
                }
            });
            return task;
        }

        private Task SendLocalHostIP()
        {
            // 设置广播组地址
            IPAddress broadcastAddress = IPAddress.Parse("255.255.255.255");
            // 发送数据到广播地址和端口,选择一个端口号
            int port = UdpClientToReceiveIP_Port;
            // 创建UdpClient实例，使用本地端口号
            UdpClient udpClientToSendIP = new UdpClient()
            {
                // 设置UdpClient为广播模式
                EnableBroadcast = true
            };
            //获取本地ip地址
            string localHostIP = GetLocalHostIP();
            // 创建一个数据字节数组
            byte[] data = Encoding.UTF8.GetBytes(localHostIP);
            IPEndPoint broadcastEndpoint = new IPEndPoint(broadcastAddress, port);
            Task task = Task.Run(async () =>
            {
                try
                {
                    await udpClientToSendIP.SendAsync(data, data.Length, broadcastEndpoint);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    udpClientToSendIP?.Close();
                }
            });
            return task;
        }

        private Task StartReceiveIPAddress()
        {
            _ctsSendLocalHostIP = new CancellationTokenSource();
            _udpClientListenReceiveIP = new UdpClient(UdpClientToReceiveIP_Port); // 创建UdpClient实例，监听11000端口
            Task task = Task.Run(async () =>
            {
                CancellationToken token = _ctsSendLocalHostIP.Token;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // 接收数据包
                        UdpReceiveResult receivedBytes = await _udpClientListenReceiveIP.ReceiveAsync();
                        string receivedString = Encoding.UTF8.GetString(receivedBytes.Buffer);
                        //判断是不是本地ip
                        bool isCreateConnect = GetLocalHostIP() != receivedString || !IsNotTransferLocal;
                        if (isCreateConnect && IPAddress.TryParse(receivedString, out IPAddress receivedIpAddress))
                        {
                            CreateConnectToTransfer(receivedIpAddress, token);
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    _udpClientListenReceiveIP?.Close();
                }
            });
            return task;
        }

        private async Task CreateConnectToTransfer(IPAddress ipAddress, CancellationToken token)
        {
            TcpClient client = new TcpClient();
            try
            {
                client.Connect(ipAddress, ListenReceiveFile_Port);
                if (client.Connected)
                {
                    await ReceiveFileByTCP(SaveFolder, client, token);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                client?.Close();
            }
        }

        private string GetLocalHostIP()
        {
            string ip = string.Empty;
            // 获取本地计算机上的所有网络接口
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                // 过滤掉非活动的接口和回环接口
                if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    // 获取网络接口上的IP属性
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

                    // 获取该接口上的所有IP地址
                    foreach (UnicastIPAddressInformation ipAddressInfo in ipProperties.UnicastAddresses)
                    {
                        // 过滤掉IPv6地址和非本地链接地址
                        if (ipAddressInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                            && !System.Net.IPAddress.IsLoopback(ipAddressInfo.Address)
                            && (networkInterface.Name.Contains("WLAN") || networkInterface.Name.Contains("以太网")))
                        {
                            ip = ipAddressInfo.Address.ToString();
                        }
                    }
                }
            }
            return ip;
        }
    }
}
