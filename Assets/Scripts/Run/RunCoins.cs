using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Rows of spinning coins along the camera's path; collected as the camera passes them.
public class RunCoins : MonoBehaviour
{
    RunDirector d;
    readonly List<Transform> coins = new List<Transform>();
    int next;
    static Mesh coinMesh;
    static Material gold, xpMat;
    static Mesh orbMesh;
    bool xp;
    readonly List<float> baseY = new List<float>();

    public static RunCoins Build(RunDirector d, List<RunCue> cues, float start, float end, Transform parent)
    {
        var go = new GameObject("Coins");
        go.transform.SetParent(parent);
        var rc = go.AddComponent<RunCoins>();
        rc.d = d;
        rc.xp = d.lib && d.lib.mcMode;
        bool pinkCoins = d.lib && d.lib.kpopMode;
        if (!coinMesh || !gold || !xpMat)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coinMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tmp);
            gold = WorldBuilder.Emissive(new Color(1f, 0.62f, 0.02f), 0.55f);
            xpMat = WorldBuilder.Emissive(new Color(0.55f, 1f, 0.2f), 1.6f);
            var tmp2 = GameObject.CreatePrimitive(PrimitiveType.Sphere); orbMesh = tmp2.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(tmp2);
            gold.SetFloat("_Metallic", 0.35f);
            gold.SetFloat("_Smoothness", 0.8f);
        }
        const float spacing = 2.2f;
        const int rowLen = 12;
        float z = d.Dist(start);
        int inRow = 0;
        while (z < d.Dist(end))
        {
            float t = d.InvDist(z);
            if (Blocked(t, cues)) { z += spacing; inRow = 0; continue; }
            var off = d.PathOffset(t, cues);
            var c = new GameObject("Coin", typeof(MeshFilter), typeof(MeshRenderer));
            c.transform.SetParent(go.transform);
            c.transform.position = new Vector3(off.x, Mathf.Max(0.45f, 0.75f + off.y), z);
            c.transform.localScale = rc.xp ? Vector3.one * 0.32f : new Vector3(0.48f, 0.05f, 0.48f);
            c.transform.rotation = Quaternion.Euler(90, 0, 0);
            c.GetComponent<MeshFilter>().sharedMesh = rc.xp ? orbMesh : coinMesh;
            var mr = c.GetComponent<MeshRenderer>();
            mr.sharedMaterial = rc.xp ? xpMat : pinkCoins ? Pink() : gold;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rc.coins.Add(c.transform);
            rc.baseY.Add(c.transform.position.y);
            z += spacing;
            if (++inRow >= rowLen) { inRow = 0; z += 18f; }
        }
        return rc;
    }

    static Material pink;
    static Material Pink() { if (!pink) { pink = WorldBuilder.Emissive(new Color(1f, 0.3f, 0.8f), 1.2f); pink.SetFloat("_Smoothness", 0.8f); } return pink; }

    // no coins inside crate walls or monsters
    static bool Blocked(float t, List<RunCue> cues)
    {
        foreach (var c in cues)
            if ((c.move == RunMove.Dash || c.move == RunMove.Smash || c.move == RunMove.Mirror) && Mathf.Abs(t - c.time) < 0.5f) return true;
        return false;
    }

    void Update()
    {
        float spin = Time.time * 240f;
        float camZ = d.Dist(d.SongTime);
        for (int i = next; i < coins.Count; i++)
        {
            var c = coins[i];
            if (!c) continue;
            if (c.position.z - camZ > 120f) break;
            if (xp) c.position = new Vector3(c.position.x, baseY[i] + Mathf.Sin(Time.time * 4f + i) * 0.12f, c.position.z);
            else c.rotation = Quaternion.AngleAxis(spin, Vector3.up) * Quaternion.Euler(90, 0, 0);
        }
        while (next < coins.Count && coins[next].position.z < camZ + 0.6f)
        {
            var c = coins[next++];
            d.CollectCoin();
            c.DOScale(c.localScale * 1.8f, 0.12f).SetLink(c.gameObject);
            c.DOMoveY(c.position.y + 0.8f, 0.15f).SetLink(c.gameObject).OnComplete(() => { if (c) c.gameObject.SetActive(false); });
        }
    }
}
