using UnityEngine;

public class MoveScript : MonoBehaviour
{
    public int Radius;
    
    void Update()
    {
        transform.position = Radius * new Vector3(Mathf.Cos(Time.time), 0, Mathf.Sign(Time.time));
    }
}
