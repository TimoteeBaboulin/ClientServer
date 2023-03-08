using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class PlayerCard : MonoBehaviour{
    [SerializeField] private TextMeshProUGUI _playerNameTmp;
    [SerializeField] private TextMeshProUGUI _playerIdTmp;

    public Player Player => _player;
    private Player _player;

    private void OnEnable(){
        if (_player == null) return;

        if (!_player.Data.TryGetValue("Name", out var playerName))
            playerName = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "Invalid Player Name");
        _playerNameTmp.text = playerName.Value;

        _playerIdTmp.text = _player.Id;
    }

    public void SetPlayer(Player player){
        if (player == null) return;

        _player = player;
        Debug.Log(player.Data);
        if (!_player.Data.TryGetValue("Name", out var playerName))
            playerName = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "Invalid Player Name");
        _playerNameTmp.text = playerName.Value;

        _playerIdTmp.text = _player.Id;
    }
}