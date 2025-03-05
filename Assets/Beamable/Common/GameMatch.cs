using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server;

namespace Beamable.Common
{
    [Serializable]
    public class GameState : StorageDocument
    {
        public List<PlayerState> players;
        public long winningPlayer;

        public List<long> PlayerIds => players.Select(x => x.playerId).ToList();
        
        public void GetPlayers(long playerId, out PlayerState currentPlayer, out PlayerState otherPlayer)
        {
            currentPlayer = otherPlayer = null;
            foreach (var player in players)
            {
                if (playerId == player.playerId)
                {
                    currentPlayer = player;
                }
                else
                {
                    otherPlayer = player;
                }
            }
        }
    }

    [Serializable]
    public class PlayerState
    {
        public long playerId;
        public UserSelection hint;
        public UserSelection actual;
    }
}