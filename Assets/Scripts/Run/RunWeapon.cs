using UnityEngine;

// First-person arm holding a shovel (lower right of the screen). Swings into the monster on SMASH.
public class RunWeapon : MonoBehaviour
{
    RunDirector d;
    Transform pivot;
    static readonly Vector3 Rest = new Vector3(0.48f, -0.52f, 0.85f);

    public static RunWeapon Create(RunDirector d, GameObject shovel, Camera cam)
    {
        var go = new GameObject("Weapon");
        go.transform.SetParent(cam.transform, false);
        var w = go.AddComponent<RunWeapon>();
        w.d = d;
        w.pivot = new GameObject("Pivot").transform;
        w.pivot.SetParent(go.transform, false);

        var sleeve = WorldBuilder.Mat(new Color(0.25f, 0.12f, 0.08f));
        var skin = WorldBuilder.Mat(new Color(0.95f, 0.72f, 0.55f));
        Part(w.pivot, new Vector3(0.05f, -0.12f, -0.25f), new Vector3(0.13f, 0.13f, 0.5f), sleeve);
        Part(w.pivot, new Vector3(0.05f, -0.1f, 0.03f), new Vector3(0.11f, 0.12f, 0.13f), skin);
        if (shovel)
        {
            var s = Object.Instantiate(shovel, w.pivot);
            foreach (var c in s.GetComponentsInChildren<Collider>()) Destroy(c);
            var b = WorldBuilder.Bounds(s);
            s.transform.localScale *= 0.95f / Mathf.Max(0.01f, b.size.y);
            s.transform.localRotation = Quaternion.Euler(70f, 0, 0) * shovel.transform.rotation;
            s.transform.localPosition = new Vector3(0.05f, -0.05f, 0.05f);
            foreach (var r in s.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        go.transform.localPosition = Rest;
        return w;
    }

    static void Part(Transform p, Vector3 pos, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        g.transform.localScale = size;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void LateUpdate()
    {
        if (d.CurrentLevel < 0) return;
        float t = d.SongTime;
        float bob = Mathf.Sin(t * Mathf.PI * 2f * 152f / 60f);
        Vector3 pos = Rest + new Vector3(bob * 0.015f, Mathf.Abs(bob) * 0.025f, 0);
        Quaternion rot = Quaternion.Euler(-10f + bob * 2f, -12f, 8f);

        foreach (var c in d.CurrentCues)
        {
            if (c.move != RunMove.Smash) continue;
            float u = t - c.time;
            if (u < -0.4f || u > 0.45f) continue;
            if (u < -0.12f)         // wind up: shovel goes up and back
            {
                float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-0.4f, -0.12f, u));
                rot = Quaternion.Slerp(rot, Quaternion.Euler(-75f, -20f, 25f), k);
                pos += new Vector3(0.05f, 0.2f, -0.1f) * k;
            }
            else if (u < 0.05f)     // strike: big swing across toward the center
            {
                float k = Mathf.InverseLerp(-0.12f, 0.05f, u);
                rot = Quaternion.Slerp(Quaternion.Euler(-75f, -20f, 25f), Quaternion.Euler(55f, 35f, -40f), k);
                pos += Vector3.Lerp(new Vector3(0.05f, 0.2f, -0.1f), new Vector3(-0.35f, -0.05f, 0.35f), k);
            }
            else                    // return to rest
            {
                float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.05f, 0.45f, u));
                rot = Quaternion.Slerp(Quaternion.Euler(55f, 35f, -40f), rot, k);
                pos += Vector3.Lerp(new Vector3(-0.35f, -0.05f, 0.35f), Vector3.zero, k);
            }
        }
        transform.localPosition = pos;
        pivot.localRotation = rot;
    }
}
