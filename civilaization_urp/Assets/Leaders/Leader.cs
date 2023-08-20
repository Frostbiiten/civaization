using UnityEngine;

[CreateAssetMenu(fileName = "New Leader", menuName = "Leader")]
public class Leader : ScriptableObject
{
    public string name, status, message, action; 
    public int troops; 
    public Sprite sprite;
    public Material material;
    public string isoID;
}
