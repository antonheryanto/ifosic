using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic.Logging;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MMU.Ifosic;

public partial class OpticalSwitch : ObservableObject
{
    [ObservableProperty] private string _address = "192.168.1.1";
    [ObservableProperty] private string _ports = "1,2,3";

    public IPAddress IpAddress { get; set; } = new(new byte[] { 192, 168, 1, 1 });
    public int Port { get; set; } = 3082;
    public int IncomingPort { get; set; } = 9;
    public int MinPort { get; set; } = 1;
    public int MaxPort { get; set; } = 8;
    public List<int> OutgoingPorts { get; set;} = new () {  1, 2, 3 };
    public List<string> Logs { get; set; } = new();

    const string AUTH = "ACT-USER::root:1::root;";
    const string PATCH_LIST = "RTRV-PATCH:::123:;";
    const string PATCH_EDIT = "ENT-PATCH::{0},{1}:123:;";
    const string PATCH_CLEAR = "DLT-PATCH::ALL:123:;";

    partial void OnAddressChanged(string value)
    {
        var c = value.Split('.');
        if (c.Length < 4)
            return;
        var ip = new byte[c.Length];
        byte empty = 0;
        for (int i = 0; i < ip.Length; i++)
        {
            ip[i] = byte.TryParse(c[i], out var v) ? v : empty;
        }
        IpAddress = new IPAddress(ip);
    }

    partial void OnPortsChanged(string value)
    {
        var c = value.Split(',');
        if (c.Length == 0)
            return;
        OutgoingPorts = new();
        for (int i = 0;i < c.Length;i++)
        {
            if (!int.TryParse(c[i], out var v))
                continue;
            if (v < MinPort || v > MaxPort)
                continue;
            OutgoingPorts.Add(v);
        }
    }

    IPEndPoint GetEndPoint () => new (IpAddress, Port);

    string Connect(int i = 0) => string.Format(PATCH_EDIT, IncomingPort, OutgoingPorts[i]);

    public async Task RunSerial(TimeSpan duration, int count = 1)
    {
        var ipEndPoint = GetEndPoint();
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        await SendMessage(client, AUTH);
        var sw = new Stopwatch();

        for (int j = 0; j < count; j++)
        {
            for (int i = 0; i < OutgoingPorts.Count; i++)
            {
                sw.Start();
                var r = await SendMessage(client, Connect());
                if (r == "FAIL")
                    continue;
                sw.Stop();
                var timeLeft = duration - sw.Elapsed * 2;
                await Task.Delay(timeLeft);
            } 
        }
       
        client.Shutdown(SocketShutdown.Both);
    }

    public async Task RunParallel(TimeSpan duration, int count = 1)
    {
        var ipEndPoint = GetEndPoint();
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        await client.ConnectAsync(ipEndPoint);
        await SendMessage(client, AUTH);
        var sw = new Stopwatch();

        for (int j = 0; j < count; j++)
        {
            for (int i = 0; i < OutgoingPorts.Count; i++)
            {
                sw.Start();
                var r = await SendMessage(client, Connect());
                if (r == "FAIL")
                    continue;
                sw.Stop();
                var timeLeft = duration - sw.Elapsed * 2;
                await Task.Delay(timeLeft);
            }
        }

        client.Shutdown(SocketShutdown.Both);
    }

    async Task Test()
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
        Debug.WriteLine($"Elapsed={sw.Elapsed}");
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
