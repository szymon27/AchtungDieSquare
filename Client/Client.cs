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
        public event Action BackToLobby;
        public event Action<string> EditRoomFailed;
        public event Action RoomEdited;
        public event Action<string> Kicked;
        public event Action<string> InviteSuccess;
        public event Action<string> InviteFailed;
        public event Action<Invitation> InvitationDialog;
        public event Action StartGameEvent;
        public event Action<string> CountDownEvent;
        public event Action StartRoundEvent;
        public event Action<WormMove> NextMoveEvent;
        public event Action ClearBoardEvent;
        public event Action ChangeColorSuccess;
        public event Action<string> ChangeColorFailed;

        public int Port { get; private set; }
        public bool Connected { get; private set; }
        public ClientInfo CurrentClient { get; private set; }
        public ObservableCollection<ClientInfo> Clients { get; private set; }
        public RoomDTO CurrentRoom { get; private set; }
        public ObservableCollection<RoomInfo> Rooms { get; private set; }

        private TcpClient _tcpClient;

        public Client(int port = 1234)
        {
            Port = port;
            Connected = false;
            CurrentClient = new ClientInfo
            {
                Id = -1,
                Name = String.Empty,
                RoomId = null
            };
            Clients = new ObservableCollection<ClientInfo>();
            BindingOperations.EnableCollectionSynchronization(Clients, new object());

            Rooms = new ObservableCollection<RoomInfo>();           
            BindingOperations.EnableCollectionSynchronization(Rooms, new object());

            CurrentRoom = new RoomDTO();
            CurrentRoom.Id = -1;
            CurrentRoom.Name = string.Empty;
            CurrentRoom.Private = false;
            CurrentRoom.Password = string.Empty;
            CurrentRoom.Games = -1;
            CurrentRoom.MaxPlayers = -1;
            CurrentRoom.Owner = -1;
            CurrentRoom.Players = new ObservableCollection<PlayerDTO>();
            BindingOperations.EnableCollectionSynchronization(CurrentRoom.Players, new object());
        }

        public void StartGame()
        {
            Packet startGamePacket = new Packet
            {
                Type = PacketType.StartGame,
                Content = "start game"
            };
            startGamePacket.Send(_tcpClient);
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

        public void EditRoom(EditRoom room)
        {
            Packet editRoomPacket = new Packet
            {
                Type = PacketType.EditRoom,
                Content = JsonSerializer.Serialize<EditRoom>(room)
            };
            editRoomPacket.Send(_tcpClient);
        }

        public void Join(RoomInfo room, string password)
        {
            JoinRoom joinRoom = new JoinRoom
            {
                Room = room,
                Password = password
            };

            Packet joinToRoomPacket = new Packet
            {
                Type = PacketType.JoinToRoom,
                Content = JsonSerializer.Serialize<JoinRoom>(joinRoom)
            };
            joinToRoomPacket.Send(_tcpClient);
        }

        public void Leave()
        {
            Packet leaveRoomPacket = new Packet
            {
                Type = PacketType.LeaveRoom,
                Content = "leave"
            };
            leaveRoomPacket.Send(_tcpClient);
        }

        public void Kick(PlayerDTO player)
        {
            Packet kickPlayerPacket = new Packet
            {
                Type = PacketType.KickPlayer,
                Content = player.ClientInfo.Id.ToString()
            };
            kickPlayerPacket.Send(_tcpClient);
        }

        public void Invite(InvitePlayer invitePlayer) 
        {
            Packet invitePlayerPacket = new Packet
            {
                Type = PacketType.InvitePlayer,
                Content = JsonSerializer.Serialize<InvitePlayer>(invitePlayer)
            };
            invitePlayerPacket.Send(_tcpClient);
        }

        public void InviteAccept(int roomId)
        {
            Packet inviteAcceptPacket = new Packet
            {
                Type = PacketType.InvitationAccept,
                Content = roomId.ToString()
            };
            inviteAcceptPacket.Send(_tcpClient);
        }

        public void ChangeColor(Color color)
        {
            Packet changeColorPacket = new Packet
            {
                Type = PacketType.ChangeColor,
                Content = JsonSerializer.Serialize<Color>(color)
            };
            changeColorPacket.Send(_tcpClient);
        }

        public void ChangeDirection(Direction direction)
        {
            Packet changeDirectionPacket = new Packet
            {
                Type = PacketType.ChangeDirection,
                Content = ((int)direction).ToString()
            };
            changeDirectionPacket.Send(_tcpClient);
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
                        CurrentClient.Id = JsonSerializer.Deserialize<ClientInfo>(recvPacket.Content).Id;
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
                    case PacketType.RoomInfo:{
                            RoomInfo roomInfoDTO = JsonSerializer.Deserialize<RoomInfo>(recvPacket.Content);
                            RoomInfo room = Rooms.Where(r => r.Id == roomInfoDTO.Id).FirstOrDefault();
                            if (room == null) Rooms.Add(roomInfoDTO);
                            else
                            { 
                                room.Name = roomInfoDTO.Name;
                                room.Private = roomInfoDTO.Private;
                                room.Games = roomInfoDTO.Games;
                                room.MaxPlayers = roomInfoDTO.MaxPlayers;
                                room.Players = roomInfoDTO.Players;
                                room.GameIsRunning = roomInfoDTO.GameIsRunning;
                            }
                            break;
                        }
                    case PacketType.ClientInfo: {
                            ClientInfo clientDTO = JsonSerializer.Deserialize<ClientInfo>(recvPacket.Content);
                            ClientInfo client = Clients.Where(c => c.Id == clientDTO.Id).FirstOrDefault();
                            if (client == null) Clients.Add(clientDTO);
                            else
                            {
                                client.Name = clientDTO.Name;
                                client.RoomId = clientDTO.RoomId;
                            }
                            break;
                        }
                    case PacketType.JoinToRoomResponse:{
                            JoinRoomResponse joinRoomResponseDTO = JsonSerializer.Deserialize<JoinRoomResponse>(recvPacket.Content);
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
                    case PacketType.LeaveRoomResponse:
                        {
                            BackToLobby?.Invoke();
                            CurrentRoom.Id = -1;
                            CurrentRoom.Name = string.Empty;
                            CurrentRoom.Private = false;
                            CurrentRoom.Password = string.Empty;
                            CurrentRoom.Games = -1;
                            CurrentRoom.MaxPlayers = -1;
                            CurrentRoom.Owner = -1;
                            CurrentRoom.Players.Clear();
                            break;
                        }
                    case PacketType.ClientLeaveRoom:
                        {
                            int playerId = Convert.ToInt32(recvPacket.Content);
                            CurrentRoom.Players.Remove(CurrentRoom.Players.Where(p => p.ClientInfo.Id == playerId).FirstOrDefault());
                            break;
                        }
                    case PacketType.OwnerLeftRoom:
                        {
                            BackToLobby?.Invoke();
                            CurrentRoom.Id = -1;
                            CurrentRoom.Name = string.Empty;
                            CurrentRoom.Private = false;
                            CurrentRoom.Password = string.Empty;
                            CurrentRoom.Games = -1;
                            CurrentRoom.MaxPlayers = -1;
                            CurrentRoom.Owner = -1;
                            CurrentRoom.Players.Clear();
                            break;
                        }
                    case PacketType.RoomDeleted:
                        {
                            int roomId = Convert.ToInt32(recvPacket.Content);
                            Rooms.Remove(Rooms.Where(r => r.Id == roomId).FirstOrDefault());
                            break;
                        }
                    case PacketType.EditRoomResponse:
                        {
                            EditRoomResponse editRoomResponse = JsonSerializer.Deserialize<EditRoomResponse>(recvPacket.Content);
                            if (editRoomResponse.Success)
                            {
                                EditRoom room = JsonSerializer.Deserialize<EditRoom>(editRoomResponse.Content);
                                CurrentRoom.Id = room.Id;
                                CurrentRoom.Name = room.Name;
                                CurrentRoom.Private = room.Private;
                                CurrentRoom.Password = room.Password;
                                CurrentRoom.Games = room.Games;
                                CurrentRoom.MaxPlayers = room.MaxPlayers;
                            }
                            else EditRoomFailed?.Invoke(editRoomResponse.Content);

                            break;
                        }
                    case PacketType.RoomEdited:
                        {
                            EditRoom room = JsonSerializer.Deserialize<EditRoom>(recvPacket.Content);
                            CurrentRoom.Id = room.Id;
                            CurrentRoom.Name = room.Name;
                            CurrentRoom.Private = room.Private;
                            CurrentRoom.Password = room.Password;
                            CurrentRoom.Games = room.Games;
                            CurrentRoom.MaxPlayers = room.MaxPlayers;
                            RoomEdited?.Invoke();
                            break;
                        }
                    case PacketType.PlayerKicked:
                        {
                            CurrentRoom.Id = -1;
                            CurrentRoom.Name = string.Empty;
                            CurrentRoom.Private = false;
                            CurrentRoom.Password = string.Empty;
                            CurrentRoom.Games = -1;
                            CurrentRoom.MaxPlayers = -1;
                            CurrentRoom.Owner = -1;
                            CurrentRoom.Players.Clear();
                            Kicked?.Invoke(recvPacket.Content);
                            break;
                        }
                    case PacketType.InvitePlayerResponse:
                        {
                            InvitePlayerResponse invitePlayerResponse = JsonSerializer.Deserialize<InvitePlayerResponse>(recvPacket.Content);
                            if (invitePlayerResponse.Success) InviteSuccess?.Invoke(invitePlayerResponse.Content);
                            else InviteFailed?.Invoke(invitePlayerResponse.Content);
                            break;
                        }
                    case PacketType.Invitation:
                        {
                            Invitation invitation = JsonSerializer.Deserialize<Invitation>(recvPacket.Content);
                            InvitationDialog?.Invoke(invitation);
                            break;
                        }
                    case PacketType.InvitationAcceptResponse:
                        {
                            InvitationAcceptResponse invitationAcceptResponse = JsonSerializer.Deserialize<InvitationAcceptResponse>(recvPacket.Content);
                            if (invitationAcceptResponse.Success)
                            {
                                RoomDTO room = JsonSerializer.Deserialize<RoomDTO>(invitationAcceptResponse.Content);
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
                            else JoinToRoomFailed?.Invoke(invitationAcceptResponse.Content);

                            break;
                        }
                    case PacketType.ClientDisconnected:
                        {
                            Clients.Remove(Clients.Where(c => c.Id == Convert.ToInt32(recvPacket.Content)).FirstOrDefault());
                            break;
                        }
                    case PacketType.StartGame:
                        {
                            StartGameEvent?.Invoke();
                            CurrentRoom.GameIsRunning = true;
                            break;
                        }
                    case PacketType.ChangeColorResponse:
                        {
                            ChangeColorResponse changeColorResponse = JsonSerializer.Deserialize<ChangeColorResponse>(recvPacket.Content);
                            if(changeColorResponse.Success)
                            {
                                ChangeColorSuccess?.Invoke();
                                CurrentRoom.Players.Where(p => p.ClientInfo.Id == CurrentClient.Id).First().Color = JsonSerializer.Deserialize<Color>(changeColorResponse.Content);
                            }
                            else
                            {
                                ChangeColorFailed?.Invoke(changeColorResponse.Content);
                            }
                            break;
                        }
                    case PacketType.ChangeColor:
                        {
                            ChangeColor changeColor = JsonSerializer.Deserialize<ChangeColor>(recvPacket.Content);
                            CurrentRoom.Players.Where(p => p.ClientInfo.Id == changeColor.Id).First().Color = changeColor.Color;
                            break;
                        }
                    case PacketType.CountDown:
                        {
                            CountDownEvent?.Invoke(recvPacket.Content);
                            break;
                        }
                    case PacketType.StartRound:
                        {
                            StartRoundEvent?.Invoke();
                            break;
                        }
                    case PacketType.NextMove:
                        {
                            WormMove wm = JsonSerializer.Deserialize<WormMove>(recvPacket.Content);
                            NextMoveEvent?.Invoke(wm);
                            break;
                        }
                    case PacketType.Points:
                        {
                            PointsDTO pointsDTO = JsonSerializer.Deserialize<PointsDTO>(recvPacket.Content);
                            CurrentRoom.Players.Where(p => p.ClientInfo.Id == pointsDTO.PlayerId).First().Points = pointsDTO.Points;
                            break;
                        }
                    case PacketType.ClearBoard:
                        {
                            ClearBoardEvent?.Invoke();
                            break;
                        }
                    default: Console.WriteLine($"{recvPacket.Type} {recvPacket.Content}"); break;
                }
            }
        }
    }
}
