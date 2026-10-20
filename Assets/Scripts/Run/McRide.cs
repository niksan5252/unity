using DG.Tweening;
using UnityEngine;

// A short minecart ride in the caves: in a quiet gap the player hops into a cart for ~4.5 s
// (cart rim at the bottom of the screen, bumpy rails), then jumps back out.
public class McRide : MonoBehaviour
{
    RunDirector d;
    Transform cart;
    int level = -2;
    float at = -1;
    bool said;
    const float Len = 4.5f;

    public static McRide Create(RunDirector d)
    {
        var r = d.gameObject.AddComponent<McRide>();
        r.d = d;
        return r;
    }

    void BuildCart()
    {
        var cam = Camera.main;
        cart = new GameObject("RideCart").transform;
        cart.SetParent(cam.transform, false);
        var iron = WorldBuilder.Mat(new Color(0.5f, 0.52f, 0.56f)); iron.SetFloat("_Metallic", 0.3f);
        var rim = WorldBuilder.Mat(new Color(0.32f, 0.33f, 0.36f));
        void B(Vector3 p, Vector3 s, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(cart, false); g.transform.localPosition = p; g.transform.localScale = s;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        B(new Vector3(0, -1.15f, 0.2f), new Vector3(1.6f, 0.08f, 2.2f), iron);
        B(new Vector3(0, -0.8f, 1.25f), new Vector3(1.6f, 0.7f, 0.08f), iron);
        B(new Vector3(-0.8f, -0.8f, 0.2f), new Vector3(0.08f, 0.7f, 2.2f), iron);
        B(new Vector3(0.8f, -0.8f, 0.2f), new Vector3(0.08f, 0.7f, 2.2f), iron);
        B(new Vector3(0, -0.44f, 1.25f), new Vector3(1.7f, 0.06f, 0.12f), rim);
        B(new Vector3(-0.8f, -0.44f, 0.2f), new Vector3(0.12f, 0.06f, 2.2f), rim);
        B(new Vector3(0.8f, -0.44f, 0.2f), new Vector3(0.12f, 0.06f, 2.2f), rim);
        cart.localScale = Vector3.zero;
    }

    void PickTime()
    {
        at = -1; said = false;
        if (WorldBuilder.Theme != RunTheme.McCaves) return;
        foreach (var t0 in new[] { 24f, 30f, 36f, 42f, 18f })
            for (float t = t0; t < t0 + 6f; t += 0.25f)
            {
                bool clear = true;
                foreach (var c in d.CurrentCues) if (c.time > t - 1.2f && c.time < t + Len + 1f) { clear = false; break; }
                if (clear) { at = t; return; }
            }
    }

    void LateUpdate()
    {
        if (d.CurrentLevel < 0) return;
        if (d.CurrentLevel != level) { level = d.CurrentLevel; PickTime(); if (cart) cart.localScale = Vector3.zero; }
        if (at < 0) { d.RideDip = 0; return; }
        if (!cart) BuildCart();
        float u = d.SongTime - at;
        bool inside = u > 0 && u < Len;
        float target = inside ? 1f : 0f;
        cart.localScale = Vector3.one * Mathf.MoveTowards(cart.localScale.x, target, Time.deltaTime * 6f);
        // sit lower in the cart and feel the bumps of the rails
        d.RideDip = inside ? -0.35f + Mathf.Sin(u * 11f) * 0.05f + Mathf.Max(0, Mathf.Sin(u * 2.3f)) * 0.25f : Mathf.MoveTowards(d.RideDip, 0, Time.deltaTime * 2f);
        if (u > 0 && !said) { said = true; d.Say("wheee"); }
    }
}
