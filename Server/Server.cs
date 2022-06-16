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
                    Task.Factory.StartNew(() => HandleNewConnection(_tcpListener.AcceptTcpClient()));
                }
                
                foreach (Client client in _clients.ToList())
                {
                    if (client.Connection.Client.Poll(10000, SelectMode.SelectRead))
                    {
                        _clients.Remove(_clients.Where(c => c.Id == client.Id).First());

                        _clients.ForEach(c => new Packet
                        {
                            Type = PacketType.ClientDisconnected,
                            Content = client.Id.ToString()
                        }.Send(c.Connection));
                    }

                    if (client.Connection.Available == 0)
                        continue;

                    Packet recvPacket = new Packet().Read(client.Connection);

                    switch (recvPacket.Type)
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
                                            ClientInfo = new ClientInfo
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

                                RoomInfo roomInfoDTO = new RoomInfo
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
                                    Content = JsonSerializer.Serialize<RoomInfo>(roomInfoDTO)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));

                                Packet clientInfoPacket = new Packet
                                {
                                    Type = PacketType.ClientInfo,
                                    Content = JsonSerializer.Serialize<ClientInfo>(new ClientInfo
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
                                JoinRoom joinRoomDTO = JsonSerializer.Deserialize<JoinRoom>(recvPacket.Content);
                                Room room = _rooms.Where(r => r.Id == joinRoomDTO.Room.Id).FirstOrDefault();

                                Packet joinRoomResponsePacket = new Packet
                                {
                                    Type = PacketType.JoinToRoomResponse
                                };

                                JoinRoomResponse joinRoomResponseDTO = new JoinRoomResponse();

                                if (room.Private && room.Password != joinRoomDTO.Password)
                                {
                                    joinRoomResponseDTO.Success = false;
                                    joinRoomResponseDTO.Content = "invalid password";

                                    joinRoomResponsePacket.Content = JsonSerializer.Serialize<JoinRoomResponse>(joinRoomResponseDTO);
                                    joinRoomResponsePacket.Send(client.Connection);
                                    break;
                                }

                                if (room.MaxPlayers <= room.Players.Count())
                                {
                                    joinRoomResponseDTO.Success = false;
                                    joinRoomResponseDTO.Content = "room is full";

                                    joinRoomResponsePacket.Content = JsonSerializer.Serialize<JoinRoomResponse>(joinRoomResponseDTO);
                                    joinRoomResponsePacket.Send(client.Connection);
                                    break;
                                }

                                client.RoomId = room.Id;

                                ClientInfo clientDTO = new ClientInfo
                                {
                                    Id = client.Id,
                                    RoomId = client.RoomId,
                                    Name = client.Name
                                };

                                _clients.ForEach(c => new Packet
                                {
                                    Type = PacketType.ClientInfo,
                                    Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
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
                                                ClientInfo = new ClientInfo
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
                                    ClientInfo = new ClientInfo
                                    {
                                        Id = client.Id,
                                        Name = client.Name,
                                        RoomId = client.RoomId
                                    }
                                };

                                joinRoomResponseDTO.Success = true;
                                joinRoomResponseDTO.Content = JsonSerializer.Serialize<RoomDTO>(roomDTO);
                                joinRoomResponsePacket.Content = JsonSerializer.Serialize<JoinRoomResponse>(joinRoomResponseDTO);
                                joinRoomResponsePacket.Send(client.Connection);

                                Packet playerJoinToRoomPacket = new Packet
                                {
                                    Type = PacketType.PlayerJoinToRoom,
                                    Content = JsonSerializer.Serialize<PlayerDTO>(playerDTO)
                                };
                                room.Players.Where(p => p.Client.Id != client.Id)
                                    .ToList().ForEach(p => playerJoinToRoomPacket.Send(p.Client.Connection));

                                RoomInfo roomInfoDTO = new RoomInfo
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
                                    Content = JsonSerializer.Serialize<RoomInfo>(roomInfoDTO)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));



                                break;
                            }
                        case PacketType.LeaveRoom:
                            {
                                Room room = _rooms.Where(r => r.Id == client.RoomId).FirstOrDefault();
                                if (room == null)
                                    break;
                                if (room.Owner == client.Id)
                                {
                                    foreach(var player in room.Players.ToList())
                                    {
                                        _clients.Where(c => c.Id == player.Client.Id).FirstOrDefault().RoomId = null;

                                        ClientInfo clientDTO = new ClientInfo
                                        {
                                            Id = player.Client.Id,
                                            RoomId = null,
                                            Name = player.Client.Name
                                        };

                                        _clients.ForEach(c => new Packet
                                        {
                                            Type = PacketType.ClientInfo,
                                            Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
                                        }.Send(c.Connection));

                                        if(player.Client.Id != client.Id)
                                        {
                                            Packet ownerLeftRoomPacket = new Packet
                                            {
                                                Type = PacketType.OwnerLeftRoom,
                                                Content = "kick"
                                            };
                                            ownerLeftRoomPacket.Send(player.Client.Connection);
                                        }
                                    }
                                    room.Players.Clear();

                                    Packet roomDeletedPacket = new Packet
                                    {
                                        Type = PacketType.RoomDeleted,
                                        Content = room.Id.ToString()
                                    };
                                    _rooms.Remove(room);
                                    _clients.ForEach(c => roomDeletedPacket.Send(c.Connection));

                                    Packet leaveRoomResponsePacket = new Packet
                                    {
                                        Type = PacketType.LeaveRoomResponse,
                                        Content = "leave"
                                    };
                                    leaveRoomResponsePacket.Send(client.Connection);
                                }
                                else
                                {
                                    room.Players.Remove(room.Players.Where(p => p.Client.Id == client.Id).FirstOrDefault());
                                    room.Players.ToList().ForEach(p =>
                                    {
                                        new Packet
                                        {
                                            Type = PacketType.ClientLeaveRoom,
                                            Content = client.Id.ToString()
                                        }.Send(p.Client.Connection);
                                    });
                                    RoomInfo roomInfoDTO = new RoomInfo
                                    {
                                        Id = room.Id,
                                        Name = room.Name,
                                        Private = room.Private,
                                        Games = room.Games,
                                        MaxPlayers = room.MaxPlayers,
                                        Players = room.Players.Count
                                    };

                                    Packet roomInfoPacket = new Packet
                                    {
                                        Type = PacketType.RoomInfo,
                                        Content = JsonSerializer.Serialize<RoomInfo>(roomInfoDTO)
                                    };
                                    _clients.ForEach(c => roomInfoPacket.Send(c.Connection));

                                    client.RoomId = null;

                                    ClientInfo clientDTO = new ClientInfo
                                    {
                                        Id = client.Id,
                                        RoomId = client.RoomId,
                                        Name = client.Name
                                    };

                                    _clients.ForEach(c => new Packet
                                    {
                                        Type = PacketType.ClientInfo,
                                        Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
                                    }.Send(c.Connection));

                                    Packet leaveRoomResponsePacket = new Packet
                                    {
                                        Type = PacketType.LeaveRoomResponse,
                                        Content = "leave"
                                    };
                                    leaveRoomResponsePacket.Send(client.Connection);
                                    client.RoomId = null;
                                } 
                                break;
                            }
                        case PacketType.EditRoom:
                            {
                                EditRoom editRoom = JsonSerializer.Deserialize<EditRoom>(recvPacket.Content);
                                EditRoomResponse editRoomResponse = new EditRoomResponse();
                                Packet editRoomResponsePacket = new Packet();
                                editRoomResponsePacket.Type = PacketType.EditRoomResponse;
                                Room room = _rooms.Where(r => r.Id == editRoom.Id).FirstOrDefault();

                                if (room == null) 
                                    break;

                                if (editRoom.MaxPlayers < room.Players.Count())
                                {
                                    editRoomResponse.Success = false;
                                    editRoomResponse.Content = "too many players in room to change the maximum number of players";
                                    editRoomResponsePacket.Content = JsonSerializer.Serialize<EditRoomResponse>(editRoomResponse);
                                    editRoomResponsePacket.Send(client.Connection);
                                    break;
                                }

                                room.Name = editRoom.Name;
                                room.Private = editRoom.Private;
                                room.Password = editRoom.Password;
                                room.Games = editRoom.Games;
                                room.MaxPlayers = editRoom.MaxPlayers;

                                RoomInfo roomInfoDTO = new RoomInfo
                                {
                                    Id = room.Id,
                                    Name = room.Name,
                                    Private = room.Private,
                                    Games = room.Games,
                                    MaxPlayers = room.MaxPlayers,
                                    Players = room.Players.Count
                                };

                                editRoomResponse.Success = true;
                                editRoomResponse.Content = recvPacket.Content;
                                editRoomResponsePacket.Content = JsonSerializer.Serialize<EditRoomResponse>(editRoomResponse);
                                editRoomResponsePacket.Send(client.Connection);

                                Packet roomInfoPacket = new Packet
                                {
                                    Type = PacketType.RoomInfo,
                                    Content = JsonSerializer.Serialize<RoomInfo>(roomInfoDTO)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));

                                room.Players.ToList().ForEach(p =>
                                {
                                    new Packet
                                    {
                                        Type = PacketType.RoomEdited,
                                        Content = recvPacket.Content
                                    }.Send(p.Client.Connection);
                                });

                                break;
                            }
                        case PacketType.KickPlayer:
                            {
                                int playerId = Convert.ToInt32(recvPacket.Content);
                                Client clientToKick = _clients.Where(c => c.Id == playerId).FirstOrDefault();

                                if (clientToKick == null) 
                                    break;

                                Room room = _rooms.Where(r => r.Id == clientToKick.RoomId).FirstOrDefault();

                                if (room == null)
                                    break;

                                Player player = room.Players.Where(p => p.Client.Id == playerId).FirstOrDefault();

                                if (player == null)
                                    break;

                                room.Players.Remove(player);

                                clientToKick.RoomId = null;
                                room.Players.ToList().ForEach(p =>
                                {
                                    new Packet
                                    {
                                        Type = PacketType.ClientLeaveRoom,
                                        Content = playerId.ToString()
                                    }.Send(p.Client.Connection);
                                });

                                RoomInfo roomInfoDTO = new RoomInfo
                                {
                                    Id = room.Id,
                                    Name = room.Name,
                                    Private = room.Private,
                                    Games = room.Games,
                                    MaxPlayers = room.MaxPlayers,
                                    Players = room.Players.Count
                                };

                                Packet roomInfoPacket = new Packet
                                {
                                    Type = PacketType.RoomInfo,
                                    Content = JsonSerializer.Serialize<RoomInfo>(roomInfoDTO)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));

                                ClientInfo clientDTO = new ClientInfo
                                {
                                    Id = clientToKick.Id,
                                    RoomId = clientToKick.RoomId,
                                    Name = clientToKick.Name
                                };

                                _clients.ForEach(c => new Packet
                                {
                                    Type = PacketType.ClientInfo,
                                    Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
                                }.Send(c.Connection));

                                Packet playerKickedPacket = new Packet
                                {
                                    Type = PacketType.PlayerKicked,
                                    Content = $"you have been kicked from {room.Name}"
                                };
                                playerKickedPacket.Send(clientToKick.Connection);

                                break;
                            }
                        case PacketType.InvitePlayer:
                            {
                                InvitePlayer invitePlayer = JsonSerializer.Deserialize<InvitePlayer>(recvPacket.Content);
                                Client player = _clients.Where(c => c.Id == invitePlayer.PlayerId).FirstOrDefault();

                                Packet invitePlayerResponsePacket = new Packet
                                {
                                    Type = PacketType.InvitePlayerResponse
                                };

                                InvitePlayerResponse invitePlayerResponse = new InvitePlayerResponse();

                                if (player == null || player.Name != invitePlayer.PlayerName)
                                {
                                    invitePlayerResponse.Success = false;
                                    invitePlayerResponse.Content = "player does not exist";
                                    invitePlayerResponsePacket.Content = JsonSerializer.Serialize<InvitePlayerResponse>(invitePlayerResponse);
                                    invitePlayerResponsePacket.Send(client.Connection);
                                }

                                if (player.RoomId != null)
                                {
                                    invitePlayerResponse.Success = false;
                                    invitePlayerResponse.Content = "player is in other room";
                                    invitePlayerResponsePacket.Content = JsonSerializer.Serialize<InvitePlayerResponse>(invitePlayerResponse);
                                    invitePlayerResponsePacket.Send(client.Connection);
                                }
                                else
                                {
                                    invitePlayerResponse.Success = true;
                                    invitePlayerResponse.Content = $"player {player.Name} invited";
                                    invitePlayerResponsePacket.Content = JsonSerializer.Serialize<InvitePlayerResponse>(invitePlayerResponse);
                                    invitePlayerResponsePacket.Send(client.Connection);

                                    Invitation invitation = new Invitation
                                    {
                                        RoomId = invitePlayer.RoomId,
                                        RoomName = _rooms.Where(r => r.Id == invitePlayer.RoomId).FirstOrDefault().Name
                                    };

                                    Packet invitationPacket = new Packet
                                    {
                                        Type = PacketType.Invitation,
                                        Content = JsonSerializer.Serialize<Invitation>(invitation)
                                    };
                                    invitationPacket.Send(player.Connection);
                                }


                                break;
                            }
                        case PacketType.InvitationAccept:
                            {
                                int roomId = Convert.ToInt32(recvPacket.Content);
                                Room room = _rooms.Where(r => r.Id == roomId).FirstOrDefault();

                                Packet invitationAcceptResponsePacket = new Packet
                                {
                                    Type = PacketType.InvitationAcceptResponse
                                };

                                InvitationAcceptResponse invitationAcceptResponse = new InvitationAcceptResponse();

                                if (room == null) {
                                    invitationAcceptResponse.Success = false;
                                    invitationAcceptResponse.Content = "room does not exist";
                                    invitationAcceptResponsePacket.Content = JsonSerializer.Serialize<InvitationAcceptResponse>(invitationAcceptResponse);
                                    invitationAcceptResponsePacket.Send(client.Connection);
                                    break;
                                }

                                if (room.MaxPlayers <= room.Players.Count())
                                {
                                    invitationAcceptResponse.Success = false;
                                    invitationAcceptResponse.Content = "room is full";
                                    invitationAcceptResponsePacket.Content = JsonSerializer.Serialize<InvitationAcceptResponse>(invitationAcceptResponse);
                                    invitationAcceptResponsePacket.Send(client.Connection);
                                    break;
                                }

                                client.RoomId = roomId;

                                ClientInfo clientDTO = new ClientInfo
                                {
                                    Id = client.Id,
                                    RoomId = client.RoomId,
                                    Name = client.Name
                                };

                                _clients.ForEach(c => new Packet
                                {
                                    Type = PacketType.ClientInfo,
                                    Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
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
                                                ClientInfo = new ClientInfo
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
                                    ClientInfo = new ClientInfo
                                    {
                                        Id = client.Id,
                                        Name = client.Name,
                                        RoomId = client.RoomId
                                    }
                                };
                                invitationAcceptResponse.Success = true;
                                invitationAcceptResponse.Content = JsonSerializer.Serialize<RoomDTO>(roomDTO);
                                invitationAcceptResponsePacket.Content = JsonSerializer.Serialize<InvitationAcceptResponse>(invitationAcceptResponse);
                                invitationAcceptResponsePacket.Send(client.Connection);

                                Packet playerJoinToRoomPacket = new Packet
                                {
                                    Type = PacketType.PlayerJoinToRoom,
                                    Content = JsonSerializer.Serialize<PlayerDTO>(playerDTO)
                                };
                                room.Players.Where(p => p.Client.Id != client.Id)
                                    .ToList().ForEach(p => playerJoinToRoomPacket.Send(p.Client.Connection));

                                RoomInfo roomInfoDTO = new RoomInfo
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
                                    Content = JsonSerializer.Serialize<RoomInfo>(roomInfoDTO)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));

                                break;
                            }
                        case PacketType.Disconnect:
                            {
                                _clients.Remove(_clients.Where(c => c.Id == client.Id).First());

                                _clients.ForEach(c => new Packet
                                {
                                    Type = PacketType.ClientDisconnected,
                                    Content = client.Id.ToString()
                                }.Send(c.Connection));
                                break;
                            }
                        default: Console.WriteLine($"{recvPacket.Type} {recvPacket.Content}"); break;
                    }
                }
            }
        }

        private void HandleNewConnection(TcpClient tcpClient)
        {
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

            ClientInfo clientDTO = new ClientInfo
            {
                Id = client.Id,
                Name = client.Name,
                RoomId = client.RoomId
            };

            Packet connectionResponsePacket = new Packet
            {
                Type = PacketType.ConnectionResponse,
                Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
            };
            connectionResponsePacket.Send(tcpClient);

            List<ClientInfo> clientsDTO = _clients
                .Select(c => new ClientInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    RoomId = c.RoomId
                }).ToList();

            foreach(ClientInfo c in clientsDTO)
            {
                Packet clientInfoPacket = new Packet
                {
                    Type = PacketType.ClientInfo,
                    Content = JsonSerializer.Serialize<ClientInfo>(c)
                };
                clientInfoPacket.Send(tcpClient);
            }

            _clients.Add(client);

            _clients.ForEach(c => new Packet
            {
                Type = PacketType.ClientInfo,
                Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
            }.Send(c.Connection));

            List<RoomInfo> roomInfoDTOs = _rooms.Select(r => new RoomInfo
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
                    Content = JsonSerializer.Serialize<RoomInfo>(r)
                };
                roomInfoPacket.Send(tcpClient);
            });
        }
    }
}
