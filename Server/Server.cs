using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

        private static Color generateColor()
        {
            Random random = new Random();
            Color color = new Color();
            color.A = 255;
            color.R = (byte)random.Next(0, 256);
            color.G = (byte)random.Next(0, 256);
            color.B = (byte)random.Next(0, 256);
            return color;
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
                    if (client.Connection.Client.Poll(1, SelectMode.SelectRead) && client.Connection.Available == 0)
                    {
                        if (client.RoomId != null)
                        {
                            Room room = _rooms.Where(r => r.Id == client.RoomId).First();
                            if (room.Owner == client.Id)
                            {
                                foreach (var player in room.Players.ToList())
                                {
                                    if (player.Client.Id != client.Id)
                                    {
                                        Packet ownerLeftRoomPacket = new Packet
                                        {
                                            Type = PacketType.OwnerLeftRoom,
                                            Content = "kick"
                                        };
                                        ownerLeftRoomPacket.Send(player.Client.Connection);

                                        _clients.Where(c => c.Id == player.Client.Id).First().RoomId = null;

                                        ClientInfo clientDTO = new ClientInfo
                                        {
                                            Id = player.Client.Id,
                                            RoomId = null,
                                            Name = player.Client.Name
                                        };

                                        _clients.Where(c => c.Id != client.Id).ToList().ForEach(c => new Packet
                                        {
                                            Type = PacketType.ClientInfo,
                                            Content = JsonSerializer.Serialize<ClientInfo>(clientDTO)
                                        }.Send(c.Connection));
                                    }
                                }

                                room.Players.Clear();

                                Packet roomDeletedPacket = new Packet
                                {
                                    Type = PacketType.RoomDeleted,
                                    Content = room.Id.ToString()
                                };
                                _rooms.Remove(room);
                                _clients.Where(c => c.Id != client.Id).ToList().ForEach(c => roomDeletedPacket.Send(c.Connection));
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
                                _clients.Where(c => c.Id != client.Id).ToList().ForEach(c => roomInfoPacket.Send(c.Connection));
                            }
                        }

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
                                    Players = new ObservableCollection<Player>() { new Player() { Client = client, Color = generateColor() }}
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
                                            },
                                            Color = room.Players[0].Color
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

                                var colors = room.Players.Select(p => p.Color).ToList();
                                Color color;
                                do
                                {
                                    color = generateColor();
                                } while (colors.Contains(color));

                                room.Players.Add(new Player { Client = client, Color = color });

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
                                                },
                                                Color = p.Color
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
                                    },
                                    Color = color
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
                                    break;
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

                                var colors = room.Players.Select(p => p.Color).ToList();
                                Color color;
                                do
                                {
                                    color = generateColor();
                                } while (colors.Contains(color));

                                room.Players.Add(new Player { Client = client, Color = color });

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
                                                },
                                                Color = p.Color
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
                                    },
                                    Color = color
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
                        case PacketType.StartGame:
                            {
                                int roomId = client.RoomId.Value;
                                Room room = _rooms.Where(r => r.Id == roomId).First();
                                room.GameIsRunning = true;
                                RoomInfo roomInfo = new RoomInfo
                                {
                                    Id = room.Id,
                                    Name = room.Name,
                                    Private = room.Private,
                                    Games = room.Games,
                                    MaxPlayers = room.MaxPlayers,
                                    Players = room.Players.Count,
                                    GameIsRunning = room.GameIsRunning,
                                };

                                Packet roomInfoPacket = new Packet
                                {
                                    Type = PacketType.RoomInfo,
                                    Content = JsonSerializer.Serialize<RoomInfo>(roomInfo)
                                };
                                _clients.ForEach(c => roomInfoPacket.Send(c.Connection));

                                Packet startGamePacket = new Packet
                                {
                                    Type = PacketType.StartGame,
                                    Content = "start game"
                                };
                                _rooms.Where(r => r.Id == roomId).First().Players.ToList().ForEach(p => startGamePacket.Send(p.Client.Connection));
                                
                                List<Player> players = room.Players.ToList();
                                Packet startingCoordinatesPacket = new Packet
                                {
                                    Type = PacketType.StartingCoordinates
                                };
                                List<WormPrep> wp = StartingCoordinates(players);

                                foreach (Player p in players)
                                    foreach(WormPrep w in wp)
                                    {
                                        startingCoordinatesPacket.Content = JsonSerializer.Serialize<WormPrep>(w);
                                        startingCoordinatesPacket.Send(p.Client.Connection);
                                    }

                                Thread countDownThread = new Thread(() => CountDown(players));
                                countDownThread.Start();
                                break;
                            }
                        case PacketType.ChangeColor:
                            {
                                Color color = JsonSerializer.Deserialize<Color>(recvPacket.Content);
                                var colors = _rooms.Where(r => r.Id == client.RoomId.Value).First().Players.Select(p => p.Color);
                                bool check = false;
                                for(int i = 0; i < colors.Count() && !check; i++)
                                {
                                    Color temp = colors.ElementAt(i);
                                    if (temp.A == color.A && temp.R == color.R && temp.G == color.G && temp.B == color.B)
                                        check = true;
                                }

                                Packet changeColorResponsePacket = new Packet
                                {
                                    Type = PacketType.ChangeColorResponse
                                };

                                ChangeColorResponse changeColorResponse = new ChangeColorResponse();

                                if(check)
                                {
                                    changeColorResponse.Success = false;
                                    changeColorResponse.Content = "choose other color";
                                    changeColorResponsePacket.Content = JsonSerializer.Serialize<ChangeColorResponse>(changeColorResponse);
                                    changeColorResponsePacket.Send(client.Connection);
                                }
                                else
                                {
                                    Player player = _rooms.Where(r => r.Id == client.RoomId.Value).First().Players.Where(p => p.Client.Id == client.Id).First();
                                    player.Color = color;
                                    changeColorResponse.Success = true;
                                    changeColorResponse.Content = JsonSerializer.Serialize<Color>(color);
                                    changeColorResponsePacket.Content = JsonSerializer.Serialize<ChangeColorResponse>(changeColorResponse);

                                    ChangeColor changeColor = new ChangeColor
                                    {
                                        Id = client.Id,
                                        Color = color,
                                    };

                                    Packet changeColorPacket = new Packet
                                    {
                                        Type = PacketType.ChangeColor,
                                        Content = JsonSerializer.Serialize<ChangeColor>(changeColor)
                                    };
                                    _rooms.Where(r => r.Id == client.RoomId.Value).First().Players.ToList().ForEach(p => changeColorPacket.Send(p.Client.Connection));
                                }
                                
                                break;
                            }
                        case PacketType.ChangeDirection:
                            {
                                Direction direction = (Direction)Int32.Parse(recvPacket.Content);
                                var player = _rooms.Where(r => r.Id == client.RoomId.Value).First().Players.Where(p => p.Client.Id == client.Id).First();
                                Direction currentDirection = player.Worm.Direction;
                                switch (direction)
                                {
                                    case Direction.Up:
                                        if (currentDirection != Direction.Down)
                                            player.Worm.Direction = currentDirection;
                                    break;
                                    case Direction.Down:
                                        if (currentDirection != Direction.Up)
                                            player.Worm.Direction = currentDirection;
                                        break;
                                    case Direction.Left:
                                        if (currentDirection != Direction.Right)
                                            player.Worm.Direction = currentDirection;
                                        break;
                                    case Direction.Right:
                                        if (currentDirection != Direction.Left)
                                            player.Worm.Direction = currentDirection;
                                        break;
                                }
                            break;
                            }
                        default: Console.WriteLine($"{recvPacket.Type} {recvPacket.Content}"); break;
                    }
                }
            }
        }

        private List<WormPrep> StartingCoordinates(List<Player> players)
        {
            List<WormPrep> wormPreps = new List<WormPrep>();
            Random r = new Random();
            int min = 4;
            int step = 57 / players.Count;
            int max = step;
            foreach(Player p in players)
            {
                WormPrep wp = new WormPrep
                {
                    PlayerId = p.Client.Id,
                    X = r.Next(min, max) * 5,
                    Y = r.Next(4, 57) * 5,
                    Color = p.Color
                };
                wormPreps.Add(wp);
                p.Worm = new Worm(wp);
                min += step;
                max += step;
            }
            return wormPreps;
        }

        private void CountDown(List<Player> players)
        {
            Packet Packet = new Packet()
            {
                Type = PacketType.CountDown,
            };
            for (int i = 3; i > 0; i--)
            {
                Packet.Content = i.ToString();
                foreach(Player p in players)
                {
                    Packet.Send(p.Client.Connection);
                }
                Thread.Sleep(1000);
            }
            Packet.Type = PacketType.StartRound;
            Packet.Content = "Start!";
            foreach (Player p in players)
            {
                Packet.Send(p.Client.Connection);
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
