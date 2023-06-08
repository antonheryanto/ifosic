using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MMU.Ifosic;

public partial class OpticalSwitch : ObservableObject
{
    [ObservableProperty] private string _address = "192.168.3.1";
    [ObservableProperty] private string _ports = "1,2,3,4";
    [ObservableProperty] private int _repetition = 1;
    [ObservableProperty] private int _port = 3082;
    [ObservableProperty] private int _incomingPort = 9;
    [ObservableProperty] private int _minPort = 1;
    [ObservableProperty] private int _maxPort = 8;
    [ObservableProperty] private TimeSpan _duration = new (0, 0, 1);
    [ObservableProperty] private ObservableCollection<string> _logs = new();

    private const string AUTH = "ACT-USER::root:1::root;";
    private const string PATCH_LIST = "RTRV-PATCH:::123:;";
    private const string PATCH_EDIT = "ENT-PATCH::{0},{1}:123:;";
    private const string PATCH_CLEAR = "DLT-PATCH::ALL:123:;";

    private static IPAddress ToIPAddress(string value)
    {
        var c = value.Split('.');
        if (c.Length < 4)
            return new(new byte[] { 127, 0, 0, 1 });
        var ip = new byte[c.Length];
        byte empty = 0;
        for (int i = 0; i < ip.Length; i++)
        {
            ip[i] = byte.TryParse(c[i], out var v) ? v : empty;
        }
        return new IPAddress(ip);
    }

    private List<int> GetPorts()
    {
        var c = Ports.Split(',');
        var o = new List<int>();
        if (c.Length == 0)
            return o;        
        for (int i = 0; i < c.Length; i++)
        {
            if (!int.TryParse(c[i], out var v))
                continue;
            if (v < MinPort || v > MaxPort)
                continue;
            o.Add(v);
        }
        return o;
    }

    private IPEndPoint GetEndPoint () => new (ToIPAddress(Address), Port);

    private string Connect(int port = 0) => string.Format(PATCH_EDIT, port, IncomingPort);

    public async Task RunSerial()
    {
        var ipEndPoint = GetEndPoint();
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        await SendMessage(client, AUTH);
        var sw = new Stopwatch();
        var ports = GetPorts();
        Logs.Add($"Process start at {DateTime.Now}");
        for (int j = 0; j < Repetition; j++)
        {
            for (int i = 0; i < ports.Count; i++)
            {
                sw.Start();
                var r = await SendMessage(client, Connect(ports[i]));
                if (r == "FAIL")
                    continue;
                sw.Stop();
                var timeLeft = Duration - sw.Elapsed * 3;
                await Task.Delay(timeLeft);
            } 
        }
        Logs.Add($"Process end at {DateTime.Now}");
        client.Shutdown(SocketShutdown.Both);
    }

    /// <summary>
    /// parallel the frequency scanning process try to cover all with multiple running steps
    /// </summary>
    /// <returns></returns>
    public async Task RunParallel()
    {
        var ipEndPoint = GetEndPoint();
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        await SendMessage(client, AUTH);
        var sw = new Stopwatch();
        var ports = GetPorts();
        for (int j = 0; j < Repetition; j++)
        {
            for (int i = 0; i < ports.Count; i++)
            {
                sw.Start();
                var r = await SendMessage(client, Connect(ports[i]));
                if (r == "FAIL")
                    continue;
                sw.Stop();
                var timeLeft = Duration - sw.Elapsed * 2;
                await Task.Delay(timeLeft);
            }
        }

        client.Shutdown(SocketShutdown.Both);
    }

    public async Task Test()
    {
        IPEndPoint ipEndPoint = GetEndPoint();
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        var auth = await SendMessage(client, AUTH);
        await SendMessage(client, PATCH_CLEAR);
        var list = await SendMessage(client, PATCH_LIST);
        var sw = new Stopwatch();
        sw.Start();
        var add = await SendMessage(client, Connect());
        sw.Stop();
        Logs.Add($"Elapsed={sw.Elapsed}");
        var list2 = await SendMessage(client, PATCH_LIST);
        client.Shutdown(SocketShutdown.Both);
    }

    private async Task<string> SendMessage(Socket client, string message)
    {
        Logs.Add($"sending: {message}");
        var authMessage = Encoding.UTF8.GetBytes(message);
        _ = await client.SendAsync(authMessage, SocketFlags.None);
        // Receive ack.
        var buffer = new byte[1_024];
        var received = await client.ReceiveAsync(buffer, SocketFlags.None);
        var response = Encoding.UTF8.GetString(buffer, 0, received);
        Logs.Add($"receive: {response}");
        if (!response.Contains("COMPLD"))
            return "FAIL";
        var ress = response.Trim('\r', '\n', ';', ' ').Split("\r\n");
        return ress.Length < 3 ? "" : ress[2].Trim(' ', '"');
    }
}
