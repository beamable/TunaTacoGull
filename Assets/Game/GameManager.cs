using System;
using System.Collections.Generic;
using System.Linq;
using Beamable;
using Beamable.Common;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Player;
using Beamable.Server.Clients;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Config Data")]
    public string player;
    
    [Header("Scene References")]
    public Button hostGameButton;
    public Button joinGameButton;
    public Button replayButton;
    public TMP_Text statusText;
    public TMP_Text otherPlayerText;
    public TMP_Text moveLabelText;
    public TMP_Text winLoseText;
    public TMP_Text winLoseDescriptionText;
    public TMP_Text winHistoryText;
    public TMP_InputField joinCodeInput;
    public TMP_InputField aliasInput;

    public ButtonRow hintSelection;
    public ButtonRow actualSelection;
    public ButtonRow otherPlayerHintSelection;

    public GameObject menuGameObject;
    public GameObject gameGameObject;
    public GameObject gameOverObject;

    public RectTransform otherPlayerHintPreview;
    
    [Header("Instance Data")]
    public BeamContext _context;
    public PlayerLobby _lobby;
    public GameState _gameState;
    private TunaTacoGullClient _client;
    private string _aliasValue;

    public void Start()
    {
        Init().Error(ex =>
        {
            Debug.LogError("Game failed to init");
            Debug.LogException(ex);
        });
    }

    private async Promise Init()
    {
        menuGameObject.SetActive(true);
        gameGameObject.SetActive(false);
        gameOverObject.SetActive(false);
        
        joinCodeInput.gameObject.SetActive(false);
        hostGameButton.gameObject.SetActive(true);
        joinGameButton.gameObject.SetActive(true);
        aliasInput.gameObject.SetActive(true);
        
        otherPlayerHintPreview.gameObject.SetActive(false);

        statusText.text = "";
        winHistoryText.text = "";
        
        hostGameButton.interactable = false;
        joinGameButton.interactable = false;
        aliasInput.interactable = false;

        _context = BeamContext.ForPlayer(player);

        await _context.OnReady;
        await _context.Accounts.Refresh();
        await _context.Lobby.Refresh();
        
        _client = _context.Microservices().TunaTacoGull();

        var winCount = await _client.GetWinCount();
        winHistoryText.text = $"wins: {winCount}";
        
        
        _lobby = _context.Lobby;
        _lobby.OnUpdated += () =>
        {
            var otherLobbyPlayer =
                _context.Lobby.Players.FirstOrDefault(x => long.Parse(x.playerId) != _context.PlayerId);
            if (otherLobbyPlayer != null)
            {
                var aliasTag = otherLobbyPlayer.tags.FirstOrDefault(t => t.name == "alias")?.value;
                otherPlayerText.text = $"{aliasTag}'s preview hint";
            }
        };
        
        _aliasValue = aliasInput.text = _context.Accounts.Current.Alias;
        aliasInput.interactable = true;
        aliasInput.onEndEdit.AddListener(OnSubmitAlias);
        if (!string.IsNullOrEmpty(_aliasValue))
        {
            OnSubmitAlias(_aliasValue);
        }


        hostGameButton.onClick.AddListener(HostGame);
        joinGameButton.onClick.AddListener(JoinGame);
        replayButton.onClick.AddListener(Replay);
        
        hintSelection.selectionChanged += HintChanged;
        actualSelection.selectionChanged += SelectActual;

        _context.Api.NotificationService.Subscribe("move", _ =>
        {
            otherPlayerHintPreview.DOPunchScale(Vector3.one, .25f);
        });
        
        _context.Api.NotificationService.Subscribe<GameState>("game", game =>
        {
            _gameState = game;
        });

        _context.Api.NotificationService.Subscribe<GameState>("result", game =>
        {
            menuGameObject.SetActive(false);
            gameGameObject.SetActive(false);
            gameOverObject.SetActive(true);
            
            var didWin = game.winningPlayer == _context.PlayerId;
            game.GetPlayers(_context.PlayerId, out var currentPlayer, out var otherPlayer);

            var verbTable = new Dictionary<UserSelection, string>
            {
                [UserSelection.Gull] = "snatched",
                [UserSelection.Tuna] = "ate",
                [UserSelection.Taco] = "sickened",
            };
            
            // TODO: change based tie verb
            
            if (didWin)
            {
                winLoseText.text = "YOU WIN!";
                winLoseDescriptionText.text = $"Your {currentPlayer.actual} {verbTable[currentPlayer.actual]} their {otherPlayer.actual}";
            }
            else
            {
                winLoseText.text = "You lost.";
                winLoseDescriptionText.text = $"Their {otherPlayer.actual} {verbTable[otherPlayer.actual]} your {currentPlayer.actual}";
            }
        });
        
        _context.Api.NotificationService.Subscribe<HintData>("hint", data =>
        {
            Debug.Log("the other player picked " + data.hint);
            var offset = Vector3.zero;
            otherPlayerHintPreview.gameObject.SetActive(true);
            switch (data.hint)
            {
                case UserSelection.Gull:
                    otherPlayerHintPreview.localPosition = otherPlayerHintSelection.gull.transform.localPosition + offset;
                    break;
                
                case UserSelection.Tuna:
                    otherPlayerHintPreview.localPosition = otherPlayerHintSelection.tuna.transform.localPosition + offset;
                    break;
                
                case UserSelection.Taco:
                    otherPlayerHintPreview.localPosition = otherPlayerHintSelection.taco.transform.localPosition + offset;
                    break;
            }
        });
    }

    private void Replay()
    {
        Beam.StopAllContexts();
        SceneManager.LoadScene("Game");
    }

    private void OnSubmitAlias(string aliasValue)
    {
        _aliasValue = aliasValue;
        hostGameButton.interactable = true;
        joinGameButton.interactable = true;

        _context.Accounts.Current.SetAlias(aliasValue);
    }

    private void SelectActual(UserSelection actual)
    {
        actualSelection.Lock();
        moveLabelText.text = $"You picked {actual}";
        _client.SubmitActual(_gameState.Id, actual);
    }

    private void HintChanged(UserSelection hint)
    {
        _client.ShowHint(_gameState.Id, hint);
    }

    public void HostGame()
    {
        aliasInput.interactable = false;
        statusText.text = "Hosting...";
        string gameTypeId = null; // TODO: WHY; switch in content def. 
        var promise = _context.Lobby.Create(Guid.NewGuid().ToString(), LobbyRestriction.Open, gameTypeId, playerTags: new List<Tag>
        {
            new Tag("alias", _aliasValue)
        });
        promise.Then(_ =>
        {
            statusText.text = "Waiting for player...";
            
            hostGameButton.gameObject.SetActive(false);
            joinGameButton.gameObject.SetActive(false);
            
            joinCodeInput.gameObject.SetActive(true);
            joinCodeInput.text = _lobby.Passcode;
            // joinCodeInput.interactable = false; // TODO: how to stop the player from erasing the code by accident?
        });

        _lobby.OnUpdated += () =>
        {
            Debug.Log($"HOST UPDATE: count-{_lobby.Players.Count} ids-[{string.Join(",", _lobby.Players.Select(x => x.playerId))}] " );
            if (_lobby.Players.Count == 2) 
            {
                menuGameObject.SetActive(false);
                gameGameObject.SetActive(true);

                _client.CreateGame(long.Parse(_lobby.Players[0].playerId), long.Parse(_lobby.Players[1].playerId));
            }
        };
    }

    public void JoinGame()
    {
        aliasInput.interactable = false;
        var passCode = joinCodeInput.text;
        var hasPassCode = !string.IsNullOrEmpty(passCode);

        if (!hasPassCode)
        {
            hostGameButton.gameObject.SetActive(false);
            joinCodeInput.gameObject.SetActive(true);
            return;
        }
        
        // do beamable jazz
        statusText.text = "joining...";
        var promise = _lobby.JoinByPasscode(passCode, playerTags: new List<Tag>
        {
            new Tag("alias", _aliasValue)
        });

        promise.Then(_ =>
        {
            menuGameObject.SetActive(false);
            gameGameObject.SetActive(true);
        });
    }

}
