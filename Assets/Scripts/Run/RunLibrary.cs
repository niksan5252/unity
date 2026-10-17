using UnityEngine;

// Prefab references filled by the editor setup menu (SubwayRun/Build Scene).
public class RunLibrary : MonoBehaviour
{
    System.Collections.Generic.Dictionary<string, GameObject> kitMap;
    public GameObject Kit(string name)
    {
        if (kitMap == null) { kitMap = new System.Collections.Generic.Dictionary<string, GameObject>(); if (kit != null) foreach (var k in kit) if (k) kitMap[k.name] = k; }
        return kitMap.TryGetValue(name, out var g) ? g : null;
    }

    [Header("Track")]
    public GameObject track;
    [Header("City")]
    public GameObject[] commercial;
    public GameObject[] skyscrapers;
    public GameObject[] suburban;
    public GameObject[] trees;
    public GameObject[] streetProps;
    public GameObject pole;
    public GameObject pillar;
    [Header("Obstacles")]
    public GameObject barrier;
    public GameObject cone;
    public GameObject gantry;
    public GameObject[] trains;
    public GameObject[] monsters;
    [Header("Block world (used when blockMode)")]
    public bool blockMode;
    public Texture2D grassTop, dirtGrass, dirt, gravel, stone, planks, trunk, leaves, lava, netherrack, sand;
    public Texture2D[] ores;
    public Texture2D atlas;
    public GameObject buddy;
    public float buddyYaw = 180f;
    public GameObject[] flowers, fencePiece;
    public GameObject[] blockTrees, animals, mineProps, netherProps;
    [Header("Obby")]
    public bool obbyMode;
    public Texture2D studs, arrows;
    [Header("Halloween")]
    public bool halloweenMode;
    public GameObject[] kit;          // Kenney Graveyard Kit, looked up by name
    public Texture2D wallpaper, darkWood, cobweb, moon, rain, bat;
    public Texture2D[] portraits;
    public GameObject weapon;
    [Header("FX")]
    public Texture2D spark;
    public Texture2D smoke;
}
