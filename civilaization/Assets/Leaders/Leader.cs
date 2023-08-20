using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Leader", menuName = "Leader")]
public class Leader : ScriptableObject
{
    public string name, status, message, action; 

    public int troops; 

    public Sprite sprite; 
}
