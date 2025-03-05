using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Mongo;
using Beamable.Server;
using MongoDB.Driver;

namespace Beamable.TunaTacoGull
{
	[Microservice("TunaTacoGull")]
	public partial class TunaTacoGull : Microservice
	{
		[ClientCallable]
		public async Promise<GameState> CreateGame(long hostPlayerId, long joinPlayerId)
		{
			// TODO: remove lobbyId
			var collection = await Storage.DatabaseCollection<GameState>();
			var gameState = new GameState
			{
				players = new List<PlayerState>
				{
					new PlayerState
					{
						playerId = hostPlayerId,
						hint = UserSelection.None,
						actual = UserSelection.None
					},
					new PlayerState
					{
						playerId = joinPlayerId,
						hint = UserSelection.None,
						actual = UserSelection.None
					}
				}
			};
			await collection.InsertOneAsync(gameState);

			await Services.Notifications.NotifyPlayer(gameState.PlayerIds, "game", gameState);
			
			return gameState;
		}

		[ClientCallable]
		public async Promise SubmitActual(string matchId, UserSelection selection)
		{
			if (selection == UserSelection.None)
			{
				// it is not valid to submit "None" as an actual turn 
				return;
			}

			var (gameState, collection) = await GetExistingGame(matchId);

			gameState.GetPlayers(Context.UserId, out var selfPlayer, out var otherPlayer);

			if (selfPlayer.actual != UserSelection.None)
			{
				// the users turn has already been submitted! 
				//  it is not allowed to change your move once its been submitted!
				return;
			}

			selfPlayer.actual = selection;

			// save the move
			await collection.ReplaceOneAsync(gs => gs.Id == matchId, gameState);


			if (otherPlayer.actual == UserSelection.None)
			{

				// if the other player hasn't done anything yet, then there is nothing to do.
				return;
			}


			// both players have valid moves now, so we can submit the result
			switch (selfPlayer.actual)
			{
				// tie-case
				//  the tie goes to the first person who selectedlo
				case var _ when selfPlayer.actual == otherPlayer.actual:
					gameState.winningPlayer = otherPlayer.playerId;
					break;
				
				// winning states for the current player...
				//  all of these states are winning states for the current player

				// gull snatches tuna
				case UserSelection.Gull when otherPlayer.actual == UserSelection.Tuna:

				// taco gives gull heart attack
				case UserSelection.Taco when otherPlayer.actual == UserSelection.Gull:

				// tuna eats taco
				case UserSelection.Tuna when otherPlayer.actual == UserSelection.Taco:

					// mark the current player as the winner!
					gameState.winningPlayer = selfPlayer.playerId;
					break;

				// all other cases mean the other player won. 
				default:
					gameState.winningPlayer = otherPlayer.playerId;
					break;
			}

			// save the winning state!
			await collection.ReplaceOneAsync(gs => gs.Id == matchId, gameState);
			
			// notify both players that the game is over!
			await Services.Notifications.NotifyPlayer(gameState.PlayerIds, "result", gameState);
		}

		[ClientCallable]
		public async Promise ShowHint(string matchId, UserSelection selection)
		{
			var (gameState, collection) = await GetExistingGame(matchId);
			
			gameState.GetPlayers(Context.UserId, out var selfPlayer, out var otherPlayer);
			
			selfPlayer.hint = selection;

			var updateCall = collection.ReplaceOneAsync(gs => gs.Id == matchId, gameState);
			var notificationCall = Services.Notifications.NotifyPlayer(otherPlayer.playerId, "hint", new HintData
			{
				hint = selection
			});

			await updateCall;
			await notificationCall;
		}

		async Task<(GameState, IMongoCollection<GameState>)> GetExistingGame(string matchId)
		{
			var collection = await Storage.DatabaseCollection<GameState>();
			var gameState = await collection.Get(matchId);
			return (gameState, collection);
		}
	}
}
