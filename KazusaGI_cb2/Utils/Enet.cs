using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Utils;

public class ENet
{
    //  [StructLayout(LayoutKind.Explicit, Size = 18)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ENetAddress
    {
        public uint host;
        public ushort port;
        // [FieldOffset(16)]
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ENetPacket
    {
        public IntPtr referenceCount;
        public uint flags;
        public IntPtr data;
        public uint dataLength;
        public IntPtr freeCallback;
        public IntPtr userData;
    }
    [Flags]
    public enum PacketFlags
    {
        None = 0,
        Reliable = 1 << 0,
        Unsequenced = 1 << 1,
        NoAllocate = 1 << 2,
        UnreliableFragment = 1 << 3
    }

    public enum EventType
    {
        None = 0,
        Connect = 1,
        Disconnect = 2,
        Receive = 3

    }
    public class ENetListNode
    {
        public ENetListNode Next { get; set; }
        public ENetListNode Previous { get; set; }
    }

    public enum ENetPeerState
    {
        Disconnected = 0,
        Connecting = 1,
        AcknowledgingConnect = 2,
        ConnectionPending = 3,
        ConnectionSucceeded = 4,
        Connected = 5,
        DisconnectLater = 6,
        Disconnecting = 7,
        AcknowledgingDisconnect = 8,
        Zombie = 9
    }
    public class ENetList
    {
        public ENetListNode Sentinel { get; set; } = new ENetListNode();
    }
    [StructLayout(LayoutKind.Sequential)]
    public class ENetPeer
    {
        public IntPtr DispatchList;
        public IntPtr Host; // struct _ENetHost* in C, represented as IntPtr
        public ushort OutgoingPeerID;
        public ushort IncomingPeerID;
        public uint ConnectID;
        public byte OutgoingSessionID;
        public byte IncomingSessionID;
        public IntPtr Address;
        public IntPtr Data; // void* in C, represented as IntPtr
        public ENetPeerState State;
        public IntPtr Channels; // ENetChannel* in C, represented as IntPtr
        public IntPtr ChannelCount;
        public uint IncomingBandwidth;
        public uint OutgoingBandwidth;
        public uint IncomingBandwidthThrottleEpoch;
        public uint OutgoingBandwidthThrottleEpoch;
        public uint IncomingDataTotal;
        public uint OutgoingDataTotal;
        public uint LastSendTime;
        public uint LastReceiveTime;
        public uint NextTimeout;
        public uint EarliestTimeout;
        public uint PacketLossEpoch;
        public uint PacketsSent;
        public uint PacketsLost;
        public uint PacketLoss;
        public uint PacketLossVariance;
        public uint PacketThrottle;
        public uint PacketThrottleLimit;
        public uint PacketThrottleCounter;
        public uint PacketThrottleEpoch;
        public uint PacketThrottleAcceleration;
        public uint PacketThrottleDeceleration;
        public uint PacketThrottleInterval;
        public uint PingInterval;
        public uint TimeoutLimit;
        public uint TimeoutMinimum;
        public uint TimeoutMaximum;
        public uint LastRoundTripTime;
        public uint LowestRoundTripTime;
        public uint LastRoundTripTimeVariance;
        public uint HighestRoundTripTimeVariance;
        public uint RoundTripTime;
        public uint RoundTripTimeVariance;
        public uint Mtu;
        public uint WindowSize;
        public uint ReliableDataInTransit;
        public ushort OutgoingReliableSequenceNumber;
        public IntPtr Acknowledgements;
        public IntPtr SentReliableCommands;
        public IntPtr SentUnreliableCommands;
        public IntPtr OutgoingCommands;
        public IntPtr DispatchedCommands;
        public ushort Flags;
        public ushort Reserved;
        public ushort IncomingUnsequencedGroup;
        public ushort OutgoingUnsequencedGroup;
        public uint UnsequencedWindow;
        public uint EventData;
        public IntPtr TotalWaitingData;

    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ENetEvent
    {
        public EventType type;
        public IntPtr peer;
        public byte channelID;
        public uint data;
        public IntPtr packet;

    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint ENetChecksumCallback(IntPtr data, IntPtr dataLength);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int enet_address_set_host(ref ENetAddress address, string hostName);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int enet_address_set_host_ip(ref ENetAddress address, string hostName);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int enet_initialize();

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void enet_deinitialize();

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr enet_host_create(ref ENetAddress address, int peerCount, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr enet_host_create(IntPtr address, int peerCount, int channelLimit, uint incomingBandwidth, uint outgoingBandwidth, int bufferSize);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void enet_host_destroy(IntPtr host);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr enet_host_connect(IntPtr host, ref ENetAddress address, IntPtr channelCount, uint data);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int enet_host_service(IntPtr host, out ENetEvent @event, uint timeout);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int enet_host_compress_with_range_coder(IntPtr host);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void enet_host_set_checksum(IntPtr host, ENetChecksumCallback callback);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint enet_crc32(IntPtr buffers, IntPtr bufferCount);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void enet_host_flush(IntPtr host);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr enet_packet_get_data(IntPtr packet);
    //  [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    // public static extern ENetPacket enet_packet_create(IntPtr data, uint dataLength, uint flags);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr enet_packet_create(IntPtr data, uint dataLength, uint flags);

    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern ENetPacket enet_packet_create(byte[] data, uint dataLength, PacketFlags flags);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int enet_peer_send(IntPtr peer, byte channelID, ENetPacket packet);
    [DllImport("enet.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int enet_peer_send(IntPtr peer, byte channelID, IntPtr packet);

    public static IntPtr StructToPtr<T>(T structure) where T : struct
    {
        // Allocate unmanaged memory for the structure
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
        // Copy the structure to unmanaged memory
        Marshal.StructureToPtr(structure, ptr, false);
        return ptr;
    }
}