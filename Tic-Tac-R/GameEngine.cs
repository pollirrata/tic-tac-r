using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SignalR.Hubs;
using Tic_Tac_R.Models;
using System.Threading.Tasks;
using SignalR;
using System.Text;

namespace Tic_Tac_R
{
    public class GameEngine : Hub, IConnected, IDisconnect
    {
        private static List<Room> _rooms;
        private static List<Player> _players;
        private bool _win;


        //Shows the play again button for both players
        public Task NobodyWins(string groupName)
        {
            return Clients[groupName].showPlayAgain();
        }

        //Change the status text for both players
        public Task SetGameStatus(string groupName, string status)
        {
            return Clients[groupName].setStatus(status);
        }

        public Task Connect()
        {
            if (_players == null)
            {
                _players = new List<Player>();
            }
            if (_rooms == null)
            {
                _rooms = new List<Room>();
            }
            return null;
        }

        private void UpdateRooms()
        {
            Clients["game"].updateRooms(_rooms.Where(r => r.Players.Count < 2).Select(r => r.Name)
                                        , _rooms.Where(r => r.Players.Count == 2).Select(r => r.Name)
                                        , _players.Count);
        }


        public void CreatePlayer(string nick, string guid)
        {
            if (_players.Any(p => p.ConnectionId == Context.ConnectionId))
            {
                var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
                _player.Nick = nick;
                _player.Uid = guid;
            }
            else
            {
                _players.Add(new Player { ConnectionId = Context.ConnectionId, Uid = guid, Nick = nick });
                Groups.Add(Context.ConnectionId, "game");
            }
            Caller.ack();
            UpdateRooms();
        }

        //Create a new room
        public Task CreateRoom(string room)
        {
            //Look for the joining player
            var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
            var status = "";
            var success = false;

            if (_rooms.Any(r => r.Name == room))
            {
                //if the room already exist warn the user
                status = "The room name already exists";
            }
            else
            {
                if (_player != null)
                {
                    _player.RoomName = room;
                    //Create the room in order to make it available for other user
                    _rooms.Add(new Room
                    {
                        Name = room,
                        Players = new List<Player>
                        {
                            {_player}   
                        }
                    });

                    //Add the user to the group (room)
                    Groups.Add(Context.ConnectionId, room);
                    success = true;
                    status = "Waiting for an oponent";
                    UpdateRooms();
                }
            }
            Caller.ack();
            return Caller.roomCreationStatus(status, success);
        }

        //Join to an existing room
        public Task JoinRoom(string room)
        {
            //Look for the joining player
            var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
            var status = "Waiting for the oponent to start";
            var success = false;

            if (_rooms != null && _rooms.Any(r => r.Name == room))//check if the room exists
            {
                var _room = _rooms.Single(r => r.Name == room);
                switch (_room.Players.Count)
                {
                    case 1://already have a player
                        //add the new player
                        _room.Players.Add(_player);
                        _player.RoomName = room;
                        Groups.Add(_player.ConnectionId, room);

                        var _firstPlayer = _room.Players.First();
                        var _lastPlayer = _room.Players.Last();

                        if ((_firstPlayer.Marks == null || _firstPlayer.Marks.Count == 0)
                            && (_lastPlayer.Marks == null || _lastPlayer.Marks.Count == 0))
                        {
                            //First joining player plays X's
                            var connId = _firstPlayer.NowPlaying = "X";
                            _firstPlayer.Marks = new List<Mark>();
                            //Groups.Add(_firstPlayer.ConnectionId, room);

                            //Last joining player plays O's
                            _lastPlayer.NowPlaying = "O";
                            _lastPlayer.Marks = new List<Mark>();
                            Clients[room].startGame(_firstPlayer.Uid, _lastPlayer.Uid, _firstPlayer.Nick, _lastPlayer.Nick, room);
                            success = true;
                        }
                        status = _firstPlayer.Nick + "'s turn";
                        break;
                    case 0: //room was empty
                        _room.Players.Add(_player);
                        _player.RoomName = room;
                        status = "Waiting for another player to join";
                        success = true;
                        break;
                    default: //room was full
                        status = "The room is already full, please select another";
                        break;
                }

                UpdateRooms();
            }
            Caller.ack();
            return Caller.roomJoiningStatus(status, success);

        }

        public void Move(int x, int y)
        {
            var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
            var _room = _rooms.Single(r => r.Name == _player.RoomName);
            var _nextPlayer = _room.Players.First(p => p.Uid != _player.Uid);
            Clients[_room.Name].switchTurn(_nextPlayer.Uid, _nextPlayer.Nick);
            _player.Marks.Add(new Mark { X = x, Y = y });
            //check only if the user has at least 3 marks
            if (!(_player.Marks.Count >= 3 && ValidateWin()))
            {
                //switch turn
                var _oponent = _players.Single(p => p.ConnectionId != Context.ConnectionId && p.RoomName == _room.Name);
                Clients[_player.RoomName].switchTurn(_oponent.Uid, _oponent.Nick);
            }
            Caller.ack();
            Clients[_room.Name].addMark(_player.NowPlaying, x, y, _win);

        }
        private bool ValidateWin()
        {
            var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
            var _verticalCombinations = new List<String>
            {
                {"0,0-0,1-0,2"},
                {"1,0-1,1-1,2"},
                {"2,0-2,1-2,2"}
            };
            var _horizontalCombinations = new List<String>
            {
                {"0,0-1,0-2,0"},
                {"0,1-1,1-2,1"},
                {"0,2-1,2-2,2"}
            };
            var _diagonalCombinations = new List<String>
            {
                {"0,0-1,1-2,2"},
                {"0,2-1,1-2,0"}
            };
            if (_player != null)
            {
                var _vertical = new StringBuilder();
                var _horizontal = new StringBuilder();
                var _diagonal = new StringBuilder();
                //vertical
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (_player.Marks.Any(m => m.X == x && m.Y == y))
                        {
                            _vertical.Append(_player.Marks.First(m => m.X == x && m.Y == y).XY);
                            _vertical.Append("-");
                        }
                    }
                    _win = _verticalCombinations.Any(c => _vertical.ToString().Contains(c));
                    if (_win)
                    {
                        Clients[_player.RoomName].endGame(_player.Uid,
                            _verticalCombinations.First(
                            c => _vertical.ToString().Contains(c))
                            );
                        return _win;
                    }
                }


                //horizontal
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        if (_player.Marks.Any(m => m.X == x && m.Y == y))
                        {
                            _horizontal.Append(_player.Marks.First(m => m.X == x && m.Y == y).XY);
                            _horizontal.Append("-");
                        }
                    }

                    _win = _horizontalCombinations.Any(c => _horizontal.ToString().Contains(c));
                    if (_win)
                    {
                        Clients[_player.RoomName].endGame(_player.Uid,
                            _horizontalCombinations.First(
                            c => _horizontal.ToString().Contains(c))
                            );
                        return _win;
                    }
                }

                //diagonal 1
                for (int x = 0; x < 3; x++)
                {
                    if (_player.Marks.Any(m => m.X == x && m.Y == x))
                    {
                        _diagonal.Append(_player.Marks.First(m => m.X == x && m.Y == x).XY);
                        _diagonal.Append("-");
                    }
                }

                _win = _diagonalCombinations.Any(c => _diagonal.ToString().Contains(c));
                if (_win)
                {
                    Clients[_player.RoomName].endGame(_player.Uid,
                        _diagonalCombinations.First<String>(
                        c => _diagonal.ToString().Contains(c))
                        );
                    return _win;
                }
                //diagonal 2
                var yIndex = 2;
                for (int x = 0; x < 3; x++)
                {
                    if (_player.Marks.Any(m => m.X == x && m.Y == yIndex))
                    {
                        _diagonal.Append(_player.Marks.First(m => m.X == x && m.Y == yIndex).XY);
                        _diagonal.Append("-");
                    }
                    yIndex--;
                }

                _win = _diagonalCombinations.Any(c => _diagonal.ToString().Contains(c));
                if (_win)
                {
                    Clients[_player.RoomName].endGame(_player.Uid,
                        _diagonalCombinations.First<String>(
                        c => _diagonal.ToString().Contains(c))
                        );
                    return _win;
                }

            }
            return _win;
        }


        public void PlayAgain()
        {
            var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
            var _room = _rooms.Single(r => r.Name == _player.RoomName);

            var _xPlayer = _room.Players.First(p => p.NowPlaying == "O");
            var _oPlayer = _room.Players.First(p => p.NowPlaying == "X");
            _xPlayer.NowPlaying = "X";
            _oPlayer.NowPlaying = "O";

            _xPlayer.Marks = new List<Mark>();
            _oPlayer.Marks = new List<Mark>();
            Caller.ack();
            Clients[_room.Name].startGame(_xPlayer.Uid, _oPlayer.Uid, _xPlayer.Nick, _oPlayer.Nick, _room.Name);

        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            return null;
        }

        public Task Disconnect()
        {
            if (_players != null && _players.Any(p => p.ConnectionId == Context.ConnectionId))
            {
                var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
                if (_rooms.Any(r => r.Name == _player.RoomName))
                {

                    var _room = _rooms.Single(r => r.Name == _player.RoomName);

                    //when disconnected we kick the user out from the room
                    _room.Players.Remove(_room.Players.Single(p => p.ConnectionId == Context.ConnectionId));
                    _players.Remove(_player);
                    Groups.Remove(Context.ConnectionId, _player.RoomName);
                    Groups.Remove(Context.ConnectionId, "game");


                    if (!_room.Players.Any())
                    {
                        //if the room is empty we remove it from the list
                        _rooms.Remove(_room);
                    }
                    else
                    {
                        //if there remain a player we clean the value games
                        var _remainingPlayer = _room.Players.First();
                        _remainingPlayer.Marks = null;
                        _remainingPlayer.NowPlaying = "";
                    }
                    UpdateRooms();
                    return Clients[_room.Name].oponentLeft();
                }
            }
            return null;
        }

        public Task LeavingRoom()
        {
            var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
            var _room = _rooms.Single(r => r.Name == _player.RoomName);

            //remove the user from the room from the room
            _room.Players.Remove(_room.Players.Single(p => p.ConnectionId == Context.ConnectionId));
            Groups.Remove(Context.ConnectionId, _player.RoomName);

            if (!_room.Players.Any())
            {
                //if the room is empty we remove it from the list
                _rooms.Remove(_room);
            }
            else
            {
                //if there remain a player we clean the value games
                var _remainingPlayer = _room.Players.First();
                _remainingPlayer.Marks = null;
                _remainingPlayer.NowPlaying = "";

                _player.Marks = null;
                _player.NowPlaying = "";

                Clients[_room.Name].oponentLeft();
            }
            UpdateRooms();
            Caller.ack();
            return Caller.leaveRoom();
        }

        #region Chat
        public Task SendMessage(string message)
        {
            var _player = _players.Single(p => p.ConnectionId == Context.ConnectionId);
            Caller.ack();
            return Clients[_player.RoomName].broadcastMessage(String.Format("<strong>{0}[{1}]:</strong><br/> {2}"
                , _player.Nick
                , DateTime.Now.ToShortTimeString()
                , System.Web.HttpUtility.HtmlEncode(message)));//prevent xss
        }
        #endregion
    }
}