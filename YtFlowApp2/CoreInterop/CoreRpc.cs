using Newtonsoft.Json.Linq;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;


namespace YtFlowApp2.CoreInterop
{
    public sealed class CoreRpc : IDisposable
    {
        private StreamSocket m_socket;
        private bool disposed = false;

        private readonly SemaphoreSlim ioSemaphore = new SemaphoreSlim(1, 1);

        public CoreRpc() { }

        public CoreRpc(StreamSocket socket)
        {
            m_socket = socket;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (m_socket != null)
                {
                    m_socket.Dispose();
                    m_socket = null;
                }
                disposed = true;
            }
        }

        public void Close()
        {
            Dispose();
        }

        public static async Task<CoreRpc> ConnectAsync()
        {
            StreamSocket socket = new StreamSocket();
            socket.Control.NoDelay = true;

            var localSettings = ApplicationData.Current.LocalSettings;
            var port = localSettings.Values["YTFLOW_CORE_RPC_PORT"] as string ?? "9097";

            await socket.ConnectAsync(new HostName("127.0.0.1"), port);
            Console.WriteLine("port:" + port);

            return new CoreRpc(socket);
        }

        public async Task<IReadOnlyList<YtFlowApp2.CoreInterop.RpcPluginInfo>> CollectAllPluginInfoAsync(IDictionary<uint, uint> hashcodes)
        {
            var sharedMap = new Dictionary<uint, uint>();
            foreach (var pair in hashcodes)
            {
                sharedMap[pair.Key] = pair.Value;
            }

            var writeStream = m_socket.OutputStream;
            var readStream = m_socket.InputStream;

            await ioSemaphore.WaitAsync();
            try
            {
                {

                    var cborWriter = new System.Formats.Cbor.CborWriter();
                    cborWriter.WriteStartMap(1);
                    {
                        cborWriter.WriteTextString("c");
                        cborWriter.WriteStartMap(1);
                        {
                            cborWriter.WriteTextString("h");
                            cborWriter.WriteStartMap(sharedMap.Count);
                            foreach (var kvp in sharedMap)
                            {
                                cborWriter.WriteUInt32(kvp.Key);
                                cborWriter.WriteUInt32(kvp.Value);
                            }
                            cborWriter.WriteEndMap();
                        }
                        cborWriter.WriteEndMap();
                    }
                    cborWriter.WriteEndMap();

                    var reqData = cborWriter.Encode();
                    var reqDataLen = reqData.Length;


                    var reqDataWithPrefix = new byte[reqDataLen + 4];
                    reqDataWithPrefix[0] = (byte)(reqDataLen >> 24);
                    reqDataWithPrefix[1] = (byte)(reqDataLen >> 16);
                    reqDataWithPrefix[2] = (byte)(reqDataLen >> 8);
                    reqDataWithPrefix[3] = (byte)reqDataLen;
                    Array.Copy(reqData, 0, reqDataWithPrefix, 4, reqDataLen);

                    var reqBuf = new Windows.Storage.Streams.Buffer((uint)(reqDataLen + 4));
                    reqBuf.Length = (uint)(reqDataLen + 4);
                    reqBuf.AsStream().Write(reqDataWithPrefix, 0, reqDataWithPrefix.Length);

                    await writeStream.WriteAsync(reqBuf);
                    await writeStream.FlushAsync();
                }

                var resData = await ReadChunk(readStream);
                var res = BridgeExtensions.FromCBORBytes(resData.ToArray());

                if (res["c"].AsString() != "Ok")
                {
                    throw new RpcException(res["e"].ToString());
                }

                var ret = res["d"].ToObject<List<YtFlowApp2.CoreInterop.RpcPluginInfo>>();
                return ret;
            }
            finally
            {
                ioSemaphore.Release();
            }
        }

        public async Task<byte[]> SendRequestToPluginAsync(uint pluginId, string func, byte[] parameters)
        {
            var writeStream = m_socket.OutputStream;
            var readStream = m_socket.InputStream;

            await ioSemaphore.WaitAsync();
            try
            {
                {
                    var reqDoc = CBORObject.NewMap();
                    {
                        reqDoc["p"] = CBORObject.NewMap();
                        {
                            reqDoc["p"]["id"] = CBORObject.FromObject(pluginId);
                            reqDoc["p"]["fn"] = CBORObject.FromObject(func);
                            reqDoc["p"]["p"] = CBORObject.FromObject(parameters.ToArray());
                        }
                    };

                    var reqData = reqDoc.EncodeToBytes();
                    var reqDataLen = reqData.Length;

                    reqData = new byte[] { (byte)(reqDataLen >> 24), (byte)(reqDataLen >> 16), (byte)(reqDataLen >> 8), (byte)reqDataLen }.Concat(reqData).ToArray();

                    var reqBuf = new Windows.Storage.Streams.Buffer((uint)(reqDataLen + 4));
                    reqBuf.Length = (uint)(reqDataLen + 4);
                    reqBuf.AsStream().Write(reqData, 0, reqData.Length);

                    await writeStream.WriteAsync(reqBuf);
                    await writeStream.FlushAsync();
                }

                var resData = await ReadChunk(readStream);
                var res = BridgeExtensions.FromCBORBytes(resData.ToArray());

                if (res["c"].AsString() != "Ok")
                {
                    throw new RpcException(res["e"].ToString());
                }

                return res["d"].ToObject<byte[]>();
            }
            finally
            {
                ioSemaphore.Release();
            }
        }

        private async Task<IBuffer> ReadChunk(IInputStream stream)
        {
            var reader = new DataReader(stream);
            reader.ByteOrder = ByteOrder.BigEndian;

            uint readLen = 4;
            while (readLen > 0)
            {
                var len = await reader.LoadAsync(readLen);
                if (len == 0)
                {
                    throw new RpcException("RPC EOF");
                }
                readLen -= len;
            }

            uint chunkSize = reader.ReadUInt32();
            while (readLen < chunkSize)
            {
                var len = await reader.LoadAsync(chunkSize - readLen);
                if (len == 0)
                {
                    throw new RpcException("RPC EOF");
                }
                readLen += len;
            }

            var ret = new byte[chunkSize];
            reader.ReadBytes(ret);
            reader.DetachStream();
            reader.Dispose();

            return ret.AsBuffer();
        }



    }

    public class RpcException : Exception
    {
        public string Msg { get; }

        public RpcException(string msg) : base(msg)
        {
            Msg = msg;
        }
    }


}
