using UnityEngine;

public class TerrainNode : MonoBehaviour {
    public int x = 0;
    public int y = 0;
    public Collider box;
    private void OnTriggerEnter(Collider other) {
        TerrainLoadManager.Instance.LoadItem(x, y);
    }
    private void OnTriggerExit(Collider other) {
        TerrainLoadManager.Instance.UnloadItem(x, y);
    }
    private void OnTriggerStay(Collider other) {
        //Debug.Log("OnTriggerStay: " + other.name);
    }
}