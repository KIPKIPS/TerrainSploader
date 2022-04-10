using System.IO;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class TerrainLoadManager {
    private static TerrainLoadManager instance = null;
    public static TerrainLoadManager Instance {
        get {
            if (instance == null) instance = new TerrainLoadManager();
            return instance;
        }
    }
    private Transform terrainRoot;
    private Transform colliderRoot;
    private Transform partsRoot;
    private TerrainPart[] parts;
    private string terrainName;
    private TerrainDataStruct terrainInfo;
    private DyncRenderInfo[] dyncRenderInfo;
    private TerrainInfo[] terrainInfoArr;
    private Dictionary<int, Terrain> map;
    private TerrainNode[] bxs;
    LightingMapData lightingMapData;
    LightingMapData LightingMapData {
        get {
            if (lightingMapData == null) {
                lightingMapData = GameObject.FindObjectOfType<LightingMapData>();
            }
            return lightingMapData;
        }
    }
    public Transform PartsRoot {
        get {
            return partsRoot;
        }
    }
    public void SetRoot(Transform terrain, Transform collider, string name) {
        terrainRoot = terrain;
        colliderRoot = collider;
        partsRoot = GameObject.Find("RuntimeParts").transform;
        terrainName = name;
        string path = "Assets/Resources/" + name + "/" + name;
        terrainInfo = LoadTerrainInfo(path + ".bytes");
        if (map == null) {
            map = new Dictionary<int, Terrain>();
        } else {
            map.Clear();
        }
        bxs = colliderRoot.GetComponentsInChildren<TerrainNode>();
    }
    public void ResetRootPos() {
        if (terrainRoot != null) {
            terrainRoot.position = terrainInfo.pos;
            terrainRoot.localScale = Vector3.one;
        }
        if (colliderRoot != null) {
            colliderRoot.position = terrainInfo.pos;
            colliderRoot.localScale = Vector3.one;
        }
    }
    public TerrainDataStruct LoadTerrainInfo(string path) {
        FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);
        BinaryReader reader = new BinaryReader(fs);
        TerrainDataStruct info = new TerrainDataStruct();
        try {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            info.pos = new Vector3(x, y, z);
            info.sliceSize = reader.ReadInt32();
            info.treeDistance = reader.ReadSingle();
            info.treeBillboardDistance = reader.ReadSingle();
            info.treeCrossFadeLength = reader.ReadInt32();
            info.treeMaximumFullLODCount = reader.ReadInt32();
            info.detailObjectDistance = reader.ReadSingle();
            info.detailObjectDensity = reader.ReadSingle();
            info.heightmapPixelError = reader.ReadSingle();
            info.heightmapMaximumLOD = reader.ReadInt32();
            info.basemapDistance = reader.ReadSingle();
            info.lightingMapIndex = reader.ReadInt32();
            info.castShadows = reader.ReadBoolean();
            LoadPartsInfo(reader);
        } catch (Exception e) {
            Debug.LogError(e.Message + " \n" + e.StackTrace);
        } finally {
            reader.Close();
            fs.Close();
        }
        return info;
    }
    private void LoadPartsInfo(BinaryReader reader) {
        int cnt = reader.ReadInt32();
        parts = new TerrainPart[cnt];
        Debug.Log(cnt);
        for (int i = 0; i < cnt; i++) {
            parts[i] = new TerrainPart();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            parts[i].pos = new Vector3(x, y, z);
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            parts[i].rot = Quaternion.Euler(x, y, z);
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            parts[i].scale = new Vector3(x, y, z);
            parts[i].lightingMapIndex = reader.ReadInt32();
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            float w = reader.ReadSingle();
            parts[i].lightingMapOffsetScale = new Vector4(x, y, z, w);
            parts[i].path = reader.ReadString();
        }
    }
    public void LoadItem(int x, int y) {
        GameObject go = new GameObject(string.Format("{2}_{0}_{1}", y, x, terrainName));
        string path = terrainName + "/" + terrainName;
        go.transform.SetParent(terrainRoot);
        go.transform.localPosition = new Vector3(x * terrainInfo.sliceSize, 0, y * terrainInfo.sliceSize);
        Terrain terrain = go.AddComponent<Terrain>();
        terrain.terrainData = Resources.Load<TerrainData>(string.Format(path + "_{0}_{1}", y, x));
        var collider = go.AddComponent<TerrainCollider>();
        collider.terrainData = terrain.terrainData;
        terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        terrain.treeDistance = terrainInfo.treeDistance;
        terrain.treeBillboardDistance = terrainInfo.treeBillboardDistance;
        terrain.treeCrossFadeLength = terrainInfo.treeCrossFadeLength;
        terrain.treeMaximumFullLODCount = terrainInfo.treeMaximumFullLODCount;
        terrain.detailObjectDistance = terrainInfo.detailObjectDistance;
        terrain.detailObjectDensity = terrainInfo.detailObjectDensity;
        // terrain.materialTemplate = terrainInfo.materialTemplate;
        terrain.heightmapPixelError = terrainInfo.heightmapPixelError;
        terrain.heightmapMaximumLOD = terrainInfo.heightmapMaximumLOD;
        terrain.basemapDistance = terrainInfo.basemapDistance;
        terrain.castShadows = terrainInfo.castShadows;
        terrain.gameObject.isStatic = true;
        if (terrainInfoArr != null) {
            int index = 4 * x + y;
            terrain.lightmapIndex = terrainInfoArr[index].lightmapIndex;
            terrain.lightmapScaleOffset = terrainInfoArr[index].lightmapOffsetScale;
        } else {
            terrain.lightmapIndex = terrainInfo.lightingMapIndex;
        }
        int key = (y << 4) + x;
        if (!map.ContainsKey(key)) {
            map.Add(key, terrain);
        }
        LoadPart(x, y);
    }
    public void LoadCollider(int x, int y) {
        GameObject go = new GameObject(string.Format("box_{0}_{1}", y, x));
        go.transform.SetParent(colliderRoot);
        go.transform.position = new Vector3(x * terrainInfo.sliceSize, 0, y * terrainInfo.sliceSize);
        var bx = go.AddComponent<BoxCollider>();
        bx.isTrigger = true;
        bx.center = new Vector3(80, 0, 80);
        bx.size = new Vector3(400, 100, 400);
        bx.enabled = false;
        var node = go.AddComponent<TerrainNode>();
        node.y = y;
        node.x = x;
        node.box = bx;
    }
    public void EnableBox(bool enabled) {
        int cnt = bxs.Length;
        for (int i = 0; i < cnt; i++) {
            bxs[i].box.enabled = enabled;
        }
    }
    public void UnloadAll() {
        if (map != null) {
            foreach (var item in map) {
                int y = item.Key >> 4;
                int x = item.Key & 0xf;
                UnloadItem(x, y);
            }
            map.Clear();
        }
        if (terrainRoot != null) {
            GameObject.Destroy(terrainRoot.gameObject);
        }
        if (colliderRoot != null) {
            GameObject.Destroy(colliderRoot.gameObject);
        }
    }
    public bool UnloadItem(int x, int y) {
        int key = (y << 4) + x;
        UnloadPart(x, y);
        return Unload(key);
    }
    private bool Unload(int indx) {
        if (map.ContainsKey(indx)) {
            if (map[indx] != null) {
                Terrain.Destroy(map[indx], 0.4f);
                GameObject.Destroy(map[indx].gameObject, 0.4f);
            }
            map.Remove(indx);
            return true;
        }
        return false;
    }
    private void LoadPart(int x, int y) {
        Bounds bounds = new Bounds();
        Vector3 pos = new Vector3((x + 0.5f) * terrainInfo.sliceSize, 0, (y + 0.5f) * terrainInfo.sliceSize);
        bounds.center = terrainInfo.pos + pos;
        bounds.size = terrainInfo.sliceSize * Vector3.one;
        for (int i = 0; i < parts.Length; i++) {
            if (parts[i].InRange(bounds)) {
                parts[i].Load();
            }
        }
    }
    private void UnloadPart(int x, int y) {
        Bounds bounds = new Bounds();
        Vector3 pos = new Vector3((x + 0.5f) * terrainInfo.sliceSize, 0, (y + 0.5f) * terrainInfo.sliceSize);
        bounds.center = terrainInfo.pos + pos;
        bounds.size = terrainInfo.sliceSize * Vector3.one;
        for (int i = 0; i < parts.Length; i++) {
            if (parts[i].InRange(bounds)) {
                parts[i].Unload();
            }
        }
    }
    public IEnumerator LoadLM(Action finish) {
        string scene = SceneManager.GetActiveScene().name;
        string path = Path.Combine(Application.streamingAssetsPath, scene + "_lightmap.ab");
        WWW www = new WWW(path);
        yield return www;
        AssetBundle curBundleObj = www.assetBundle;
        TextAsset text = curBundleObj.LoadAsset<TextAsset>(scene);
        MemoryStream ms = new MemoryStream(text.bytes);
        ms.Position = 0;
        BinaryReader reader = new BinaryReader(ms);
        int cnt = reader.ReadInt32();
        string[] lmcolors = new string[cnt];
        string[] lmdirs = new string[cnt];
        LightmapData[] datas = new LightmapData[cnt];
        for (int i = 0; i < cnt; i++) {
            lmcolors[i] = reader.ReadString();
            lmdirs[i] = reader.ReadString();
            LightmapData data = new LightmapData();
            if (!string.IsNullOrEmpty(lmcolors[i])) {
                data.lightmapColor = curBundleObj.LoadAsset<Texture2D>(lmcolors[i]);
            }
            if (!string.IsNullOrEmpty(lmdirs[i])) {
                data.lightmapDir = curBundleObj.LoadAsset<Texture2D>(lmdirs[i]);
            }
            datas[i] = data;
        }
        lightingMapData.SetUp();
        LightmapSettings.lightmaps = datas;
        LoadLightmapOffsetInfo(reader);
        reader.Close();
        ms.Close();
        www.Dispose();
        if (finish != null) finish();
    }
    private void LoadLightmapOffsetInfo(BinaryReader reader) {
        int cnt = reader.ReadInt32();
        dyncRenderInfo = new DyncRenderInfo[cnt];
        for (int i = 0; i < cnt; i++) {
            DyncRenderInfo info = new DyncRenderInfo();
            info.lightIndex = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = reader.ReadSingle();
            info.lightOffsetScale = new Vector4(w, y, z, w);
            info.hash = reader.ReadInt32();
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            info.pos = new Vector3(x, y, z);
            dyncRenderInfo[i] = info;
        }
        cnt = reader.ReadInt32();
        terrainInfoArr = new TerrainInfo[cnt];
        for (int i = 0; i < cnt; i++) {
            TerrainInfo info = new TerrainInfo();
            info.lightmapIndex = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = reader.ReadSingle();
            info.lightmapOffsetScale = new Vector4(x, y, z, w);
            terrainInfoArr[i] = info;
        }
    }
}