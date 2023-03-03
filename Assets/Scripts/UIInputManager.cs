using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInputManager : MonoBehaviour{
    public void SendText(string message){
        LocalPlayerManager.LocalPlayer.SendText(message);
    }
}
