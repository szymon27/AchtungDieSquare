using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Server
    {
        private static int MIN_LENGTH_NAME = 4;
        private static int MAX_LENGTH_NAME = 16;

        public int Port { get; private set; }

        private TcpListener _tcpListener;

        private int _newClientId;
        List<Client> _clients;

        private int _newRoomId;
        List<Room> _rooms;
        

        public Server(int port = 1234)
        {
            Port = port;
            _tcpListener = new TcpListener(Port);

            _newClientId = 1;
            _clients = new List<Client>();

            _newRoomId = 1;
            _rooms = new List<Room>();
        }

        public void Start()
        {
            if (_tcpListener == null)
                return;

            _tcpListener.Start();
            Console.WriteLine($"server is running on port {Port}");

            while(true)
            {
                if(_tcpListener.Pending())
                {
                    Task.Factory.StartNew(() => HandleNewConnection());
                    Thread.Sleep(200);
                }

                foreach(Client client in _clients)
                {
                    if (client.Connection.Available == 0)
                        continue;

                    Packet recvPacket = new Packet().Read(client.Connection);

                    switch(recvPacket.Type)
                    {
                        case PacketType.CreateRoom:
                            {
                                client.RoomId = _newRoomId;
                                NewRoom newRoom = JsonSerializer.Deserialize<NewRoom>(recvPacket.Content);
                                Room room = new Room
                                {
                                    Id = _newRoomId++,
                                    Name = newRoom.Name,
                                    Private = newRoom.Private,
                                    Password = newRoom.Password,
                                    Games = newRoom.Games,
                                    MaxPlayers = newRoom.MaxPlayers,
                                    Owner = newRoom.Owner,
                                    Players = new ObservableCollection<Player>() { new Player { Client = client } }
                                };
                                _rooms.Add(room);

                                RoomDTO roomDTO = new RoomDTO
                                {
                                    Id = room.Id,
                                    Name = room.Name,
                                    Private = room.Private,
                                    Password = room.Password,
                                    Games = room.Games,
                                    MaxPlayers = room.MaxPlayers,
                                    Owner = room.Owner,
                                    Players = new ObservableCollection<PlayerDTO>() 
                                    { 
                                        new PlayerDTO 
                                        { 
                                            ClientDTO = new ClientDTO
                                            { 
                                                Id = room.Players[0].Client.Id,
                                                Name = room.Players[0].Client.Name,
                                                RoomId = room.Players[0].Client.RoomId
                                            } 
                                        } 
                                    }
                                };

                                Packet createRoomResponsePacket = new Packet
                                {
                                    Type = PacketType.CreateRoomResponse,
                                    Content = JsonSerializer.Serialize<RoomDTO>(roomDTO)
                                };
                                createRoomResponsePacket.Send(client.Connection);

                                RoomInfoDTO roomInfoDTO = new RoomInfoDTO
                                {
                                    Id = roomDTO.Id,
                                    Name = roomDTO.Name,
                                    Private = roomDTO.Private,
                                    Games = roomDTO.Games,
                                    MaxPlayers = roomDTO.MaxPlayers,
                                    Players = roomDTO.Players.Count
                                };

                                Packet roomInfoPacket = new Packet
                                {
                                    Type = PacketType.RoomInfo,
                                    Content = JsonSerializer.Serialize<RoomInfoDTO>(roomInfoDTO)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));

                                Packet clientInfoPacket = new Packet
                                {
                                    Type = PacketType.ClientInfo,
                                    Content = JsonSerializer.Serialize<ClientDTO>(new ClientDTO
                                    {
                                        Id = client.Id,
                                        Name = client.Name,
                                        RoomId = client.RoomId
                                    })
                                };
                                _clients.ForEach(cl => clientInfoPacket.Send(cl.Connection));
                                break;
                            }
                        case PacketType.JoinToRoom:
                            {
                                JoinRoomDTO joinRoomDTO = JsonSerializer.Deserialize<JoinRoomDTO>(recvPacket.Content);
                                Room room = _rooms.Where(r => r.Id == joinRoomDTO.Room.Id).FirstOrDefault();

                                Packet joinRoomResponsePacket = new Packet
                                {
                                    Type = PacketType.JoinToRoomResponse
                                };

                                JoinRoomResponseDTO joinRoomResponseDTO = new JoinRoomResponseDTO();

                                if (room.Private && room.Password != joinRoomDTO.Password)
                                {
                                    joinRoomResponseDTO.Success = false;
                                    joinRoomResponseDTO.Content = "invalid password";

                                    joinRoomResponsePacket.Content = JsonSerializer.Serialize<JoinRoomResponseDTO>(joinRoomResponseDTO);
                                    joinRoomResponsePacket.Send(client.Connection);
                                    break;
                                }

                                if(room.MaxPlayers <= room.Players.Count())
                                {
                                    joinRoomResponseDTO.Success = false;
                                    joinRoomResponseDTO.Content = "room is full";

                                    joinRoomResponsePacket.Content = JsonSerializer.Serialize<JoinRoomResponseDTO>(joinRoomResponseDTO);
                                    joinRoomResponsePacket.Send(client.Connection);
                                    break;
                                }

                                client.RoomId = room.Id;

                                ClientDTO clientDTO = new ClientDTO
                                {
                                    Id = client.Id,
                                    RoomId = client.RoomId,
                                    Name = client.Name
                                };

                                _clients.ForEach(c => new Packet
                                {
                                    Type = PacketType.ClientInfo,
                                    Content = JsonSerializer.Serialize<ClientDTO>(clientDTO)
                                }.Send(c.Connection));

                                room.Players.Add(new Player { Client = client });

                                RoomDTO roomDTO = new RoomDTO
                                {
                                    Id = room.Id,
                                    Name = room.Name,
                                    Private = room.Private,
                                    Password = room.Password,
                                    Games = room.Games,
                                    MaxPlayers = room.MaxPlayers,
                                    Owner = room.Owner,
                                    Players = new ObservableCollection<PlayerDTO>(
                                        room.Players.Select(p =>
                                            new PlayerDTO()
                                            {
                                                ClientDTO = new ClientDTO
                                                {
                                                    Id = p.Client.Id,
                                                    Name = p.Client.Name,
                                                    RoomId = p.Client.RoomId
                                                }
                                            }
                                        )
                                    )
                                };

                                PlayerDTO playerDTO = new PlayerDTO
                                {
                                    ClientDTO = new ClientDTO
                                    {
                                        Id = client.Id,
                                        Name = client.Name,
                                        RoomId = client.RoomId
                                    }
                                };

                                joinRoomResponseDTO.Success = true;
                                joinRoomResponseDTO.Content = JsonSerializer.Serialize<RoomDTO>(roomDTO);
                                joinRoomResponsePacket.Content = JsonSerializer.Serialize<JoinRoomResponseDTO>(joinRoomResponseDTO);
                                joinRoomResponsePacket.Send(client.Connection);

                                Packet playerJoinToRoomPacket = new Packet
                                {
                                    Type = PacketType.PlayerJoinToRoom,
                                    Content = JsonSerializer.Serialize<PlayerDTO>(playerDTO)
                                };
                                room.Players.Where(p => p.Client.Id != client.Id)
                                    .ToList().ForEach(p => playerJoinToRoomPacket.Send(p.Client.Connection));

                                RoomInfoDTO roomInfoDTO = new RoomInfoDTO
                                {
                                    Id = roomDTO.Id,
                                    Name = roomDTO.Name,
                                    Private = roomDTO.Private,
                                    Games = roomDTO.Games,
                                    MaxPlayers = roomDTO.MaxPlayers,
                                    Players = roomDTO.Players.Count
                                };

                                Packet roomInfoPacket = new Packet
                                {
                                    Type = PacketType.RoomInfo,
                                    Content = JsonSerializer.Serialize<RoomInfoDTO>(roomInfoDTO)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));



                                break;
                            }
                        default: Console.WriteLine($"{recvPacket.Type} {recvPacket.Content}"); break;
                    }
                }
            }
        }

        private void HandleNewConnection()
        {
            TcpClient tcpClient = _tcpListener.AcceptTcpClient();

            string name = string.Empty;

            while (true)
            {
                if (tcpClient.Available > 0)
                {
                    Packet recvPacket = new Packet().Read(tcpClient);
                    if (recvPacket.Type == PacketType.Connection)
                        name = recvPacket.Content;
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(name) || name.Length < MIN_LENGTH_NAME || name.Length > MAX_LENGTH_NAME)
            {
                Packet disconnectPacket = new Packet
                {
                    Type = PacketType.Disconnect,
                    Content = string.Empty
                };
                disconnectPacket.Send(tcpClient);
                tcpClient.Close();
                return;
            }

            Client client = new Client
            {
                Id = _newClientId++,
                Name = name,
                RoomId = null,
                Connection = tcpClient
            };

            ClientDTO clientDTO = new ClientDTO
            {
                Id = client.Id,
                Name = client.Name,
                RoomId = client.RoomId
            };

            Packet connectionResponsePacket = new Packet
            {
                Type = PacketType.ConnectionResponse,
                Content = JsonSerializer.Serialize<ClientDTO>(clientDTO)
            };
            connectionResponsePacket.Send(tcpClient);

            List<ClientDTO> clientsDTO = _clients
                .Select(c => new ClientDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    RoomId = c.RoomId
                }).ToList();

            foreach(ClientDTO c in clientsDTO)
            {
                Packet clientInfoPacket = new Packet
                {
                    Type = PacketType.ClientInfo,
                    Content = JsonSerializer.Serialize<ClientDTO>(c)
                };
                clientInfoPacket.Send(tcpClient);
            }

            _clients.Add(client);

            _clients.ForEach(c => new Packet
            {
                Type = PacketType.ClientInfo,
                Content = JsonSerializer.Serialize<ClientDTO>(clientDTO)
            }.Send(c.Connection));

            List<RoomInfoDTO> roomInfoDTOs = _rooms.Select(r => new RoomInfoDTO
            {
                Id = r.Id,
                Name = r.Name,
                Private = r.Private,
                Games = r.Games,
                MaxPlayers = r.MaxPlayers,
                Players = r.Players.Count
            }).ToList();

            roomInfoDTOs.ForEach(r =>
            {
                Packet roomInfoPacket = new Packet
                {
                    Type = PacketType.RoomInfo,
                    Content = JsonSerializer.Serialize<RoomInfoDTO>(r)
                };
                roomInfoPacket.Send(tcpClient);
            });
        }
    }
}
