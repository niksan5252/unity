using UnityEngine;

// Prefab references filled by the editor setup menu (SubwayRun/Build Scene).
public class RunLibrary : MonoBehaviour
{
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
    [Header("FX")]
    public Texture2D spark;
    public Texture2D smoke;
}
