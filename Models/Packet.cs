using System;
using System.Net.Sockets;
using System.Text;

namespace Models
{
    public enum PacketType
    {
        Disconnect = 0,
        Connection,
        ConnectionResponse,
        ClientInfo,
        CreateRoom,
        CreateRoomResponse,
        RoomInfo,
        JoinToRoom,
        JoinToRoomResponse,
        PlayerJoinToRoom,
        LeaveRoom,
        LeaveRoomResponse,
        ClientLeaveRoom,
        OwnerLeftRoom,
        RoomDeleted,
        EditRoom,
        EditRoomResponse,
        RoomEdited,
        KickPlayer,
        PlayerKicked
    }

    public class Packet
    {
        public PacketType Type { get; set; }
        public string Content { get; set; }
    }

    public static class PacketExtensions
    {
        public static void Send(this Packet packet, TcpClient  tcpClient)
        {
            byte[] typeBuffer = BitConverter.GetBytes((ushort)packet.Type);
            byte[] contentBuffer = Encoding.UTF8.GetBytes(packet.Content);
            byte[] lengthBuffer = BitConverter.GetBytes((ushort)contentBuffer.Length);

            int bufferLength = lengthBuffer.Length + typeBuffer.Length + contentBuffer.Length;
            byte[] buffer = new byte[bufferLength];
            lengthBuffer.CopyTo(buffer, 0);
            typeBuffer.CopyTo(buffer, lengthBuffer.Length);
            contentBuffer.CopyTo(buffer, lengthBuffer.Length + typeBuffer.Length);

            tcpClient.GetStream().Write(buffer, 0, buffer.Length);
        }

        public static Packet Read(this Packet packet, TcpClient tcpClient)
        {
            byte[] lengthBuffer = new byte[2];
            tcpClient.GetStream().Read(lengthBuffer, 0, lengthBuffer.Length);
            ushort length = BitConverter.ToUInt16(lengthBuffer);

            byte[] typeBuffer = new byte[2];
            tcpClient.GetStream().Read(typeBuffer, 0, typeBuffer.Length);
            PacketType packetType = (PacketType)BitConverter.ToUInt16(typeBuffer);

            byte[] contentBuffer = new byte[length];
            tcpClient.GetStream().Read(contentBuffer, 0, contentBuffer.Length);
            string content = Encoding.UTF8.GetString(contentBuffer);

            return new Packet
            {
                Type = packetType,
                Content = content
            };
        }
    }
}
