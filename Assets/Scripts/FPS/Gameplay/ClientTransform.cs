using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientTransform : NetworkTransform
{ 
    protected override bool OnIsServerAuthoritative(){ 
        return false;
    }
}
