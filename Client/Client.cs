using Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Client
{
    public class Client
    {
        public event Action ConnectionSuccess;
        public event Action ConnectionFailed;
        public event Action Disconnect;
        public event Action RoomCreated;
        public event Action JoinToRoomSuccess;
        public event Action<string> JoinToRoomFailed;

        public int Port { get; private set; }
        public bool Connected { get; private set; }
        public ClientDTO CurrentClient { get; private set; }
        public ObservableCollection<ClientDTO> Clients { get; private set; }
        public RoomDTO CurrentRoom { get; private set; }
        public ObservableCollection<RoomInfoDTO> Rooms { get; private set; }

        private TcpClient _tcpClient;

        public Client(int port = 1234)
        {
            Port = port;
            Connected = false;
            CurrentClient = new ClientDTO
            {
                Id = -1,
                Name = String.Empty,
                RoomId = null
            };
            Clients = new ObservableCollection<ClientDTO>();
            BindingOperations.EnableCollectionSynchronization(Clients, new object());

            Rooms = new ObservableCollection<RoomInfoDTO>();           
            BindingOperations.EnableCollectionSynchronization(Rooms, new object());

            CurrentRoom = new RoomDTO();
            CurrentRoom.Players = new ObservableCollection<PlayerDTO>();
            BindingOperations.EnableCollectionSynchronization(CurrentRoom.Players, new object());
        }

        public void Connect(string name)
        {
            if (Connected)
                return;

            CurrentClient.Name = name;
            Task.Factory.StartNew(() => Start());
        }

        public void CreateRoom(NewRoom newRoom)
        {
            Packet createRoomPacket = new Packet
            {
                Type = PacketType.CreateRoom,
                Content = JsonSerializer.Serialize<NewRoom>(newRoom)
            };
            createRoomPacket.Send(_tcpClient);
        }

        public void Join(RoomInfoDTO room, string password)
        {
            Console.WriteLine($"{room.Id} {password}");
            JoinRoomDTO joinRoom = new JoinRoomDTO
            {
                Room = room,
                Password = password
            };

            Packet joinToRoomPacket = new Packet
            {
                Type = PacketType.JoinToRoom,
                Content = JsonSerializer.Serialize<JoinRoomDTO>(joinRoom)
            };
            joinToRoomPacket.Send(_tcpClient);
        }

        private void Start()
        {
            try
            {
                _tcpClient = new TcpClient("localhost", Port);
                Connected = true;
            }
            catch (SocketException ex)
            {
                ConnectionFailed?.Invoke();
                return;
            }

            Packet connectionPacket = new Packet
            {
                Type = PacketType.Connection,
                Content = CurrentClient.Name
            };
            connectionPacket.Send(_tcpClient);

            bool isFirstPacket = true;

            while(Connected)
            {
                if (_tcpClient.Available == 0)
                    continue;

                Packet recvPacket = new Packet().Read(_tcpClient);

                if(isFirstPacket)
                {
                    if (recvPacket.Type == PacketType.ConnectionResponse)
                    {
                        CurrentClient.Id = JsonSerializer.Deserialize<ClientDTO>(recvPacket.Content).Id;
                        ConnectionSuccess?.Invoke();
                    }
                    else {
                        _tcpClient.Close();
                        Connected = false;
                        ConnectionFailed?.Invoke();               
                    }

                    isFirstPacket = false;
                    continue;
                }

                switch(recvPacket.Type)
                {
                    case PacketType.Disconnect: {
                            _tcpClient.Close();
                            Connected = false;
                            Disconnect?.Invoke();
                            break;
                        }
                    case PacketType.CreateRoomResponse: {
                            //CurrentRoom = JsonSerializer.Deserialize<RoomDTO>(recvPacket.Content);
                            //BindingOperations.EnableCollectionSynchronization(CurrentRoom.Players, new object());
                            RoomDTO room = JsonSerializer.Deserialize<RoomDTO>(recvPacket.Content);
                            CurrentRoom.Id = room.Id;
                            CurrentRoom.Name = room.Name;
                            CurrentRoom.Private = room.Private;
                            CurrentRoom.Password = room.Password;
                            CurrentRoom.Games = room.Games;
                            CurrentRoom.MaxPlayers = room.MaxPlayers;
                            CurrentRoom.Owner = room.Owner;
                            foreach (PlayerDTO player in room.Players)
                                CurrentRoom.Players.Add(player);
                            RoomCreated?.Invoke();
                            break;
                        }
                    case PacketType.RoomInfo:
                        {
                            RoomInfoDTO roomInfoDTO = JsonSerializer.Deserialize<RoomInfoDTO>(recvPacket.Content);
                            RoomInfoDTO room = Rooms.Where(r => r.Id == roomInfoDTO.Id).FirstOrDefault();
                            if (room == null) Rooms.Add(roomInfoDTO);
                            else
                            { 
                                room.Name = roomInfoDTO.Name;
                                room.Private = roomInfoDTO.Private;
                                room.Games = roomInfoDTO.Games;
                                room.MaxPlayers = roomInfoDTO.MaxPlayers;
                                room.Players = roomInfoDTO.Players;
                            }
                            break;
                        }
                    case PacketType.ClientInfo: {
                            ClientDTO clientDTO = JsonSerializer.Deserialize<ClientDTO>(recvPacket.Content);
                            ClientDTO client = Clients.Where(c => c.Id == clientDTO.Id).FirstOrDefault();
                            if (client == null) Clients.Add(clientDTO);
                            else
                            {
                                client.Name = clientDTO.Name;
                                client.RoomId = clientDTO.RoomId;
                            }
                            break;
                        }
                    case PacketType.JoinToRoomResponse:
                        {
                            JoinRoomResponseDTO joinRoomResponseDTO = JsonSerializer.Deserialize<JoinRoomResponseDTO>(recvPacket.Content);
                            if (joinRoomResponseDTO.Success)
                            {
                                RoomDTO room = JsonSerializer.Deserialize<RoomDTO>(joinRoomResponseDTO.Content);
                                CurrentRoom.Id = room.Id;
                                CurrentRoom.Name = room.Name;
                                CurrentRoom.Private = room.Private;
                                CurrentRoom.Password = room.Password;
                                CurrentRoom.Games = room.Games;
                                CurrentRoom.MaxPlayers = room.MaxPlayers;
                                CurrentRoom.Owner = room.Owner;
                                foreach (PlayerDTO player in room.Players)
                                    CurrentRoom.Players.Add(player);
                                JoinToRoomSuccess?.Invoke();
                            }
                            else JoinToRoomFailed?.Invoke(joinRoomResponseDTO.Content);

                            break;
                        }
                    case PacketType.PlayerJoinToRoom:
                        {
                            PlayerDTO playerDTO = JsonSerializer.Deserialize<PlayerDTO>(recvPacket.Content);
                            CurrentRoom.Players.Add(playerDTO);
                            break;
                        }
                    default: Console.WriteLine($"{recvPacket.Type} {recvPacket.Content}"); break;
                }
            }
        }
    }
}
