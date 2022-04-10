using UnityEditor;
using UnityEngine;

public class TerrainLoadEditor : Editor {
    [MenuItem("Terrain/Load")]
    private static void Load() {
        int XMax = TerrainConfig.Slice, YMax = TerrainConfig.Slice;
        Vector3 offset = Vector3.zero;
        Terrain terr = GameObject.FindObjectOfType<Terrain>();
        if (terr != null) {
            offset = terr.transform.position;
            terr.gameObject.SetActive(false);
        } else {
            Debug.LogError("not found terrain");
            return;
        }
        string terrainRoot = "TerrainRoot";
        string colliderRoot = "ColliderRoot";
        string partsRoot = "Parts";
        GameObject terrainRootGo = GameObject.Find(terrainRoot);
        GameObject colliderRootGo = GameObject.Find(colliderRoot);
        GameObject partRootGo = GameObject.Find(partsRoot);
        if (terrainRootGo == null) terrainRootGo = AttachGameObject(terrainRoot);
        if (colliderRootGo == null) colliderRootGo = AttachGameObject(colliderRoot);
        TerrainLoadManager.Instance.SetRoot(terrainRootGo.transform, colliderRootGo.transform, terr.name);
        for (int x = 0; x < XMax; ++x) {
            for (int y = 0; y < YMax; ++y) {
                TerrainLoadManager.Instance.LoadItem(x, y);
                TerrainLoadManager.Instance.LoadCollider(x, y);
            }
        }
        Debug.Log(partRootGo);
        if (partRootGo != null) partRootGo.SetActive(false);
        TerrainLoadManager.Instance.ResetRootPos();
    }
    private static GameObject AttachGameObject(string name) {
        GameObject go = new GameObject(name);
        var root = GameObject.Find("Root");
        go.transform.SetParent(root.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }
}