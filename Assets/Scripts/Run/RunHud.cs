using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Screen overlay built from code: speed lines, action icon, progress bar, cards, flash.
public class RunHud : MonoBehaviour
{
    Font font;
    RectTransform canvasRt;
    RawImage speedLines;
    RectTransform icon; Text iconGlyph, iconLabel;
    Image progressFill; RectTransform progressDot; CanvasGroup progressGroup;
    CanvasGroup card; Text cardTitle, cardSub; RectTransform cardMoves; RawImage cardLines;
    Image flash;
    float lineTimer;

    public static RunHud Create()
    {
        var go = new GameObject("RunHud");
        var hud = go.AddComponent<RunHud>();
        hud.Build();
        return hud;
    }

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        canvasRt = (RectTransform)transform;

        speedLines = Raw("SpeedLines", canvasRt, RunTextures.SpeedLines(512, 110, 0.42f));
        Anchor(speedLines.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2600, 2600));
        speedLines.color = new Color(1, 1, 1, 0.55f);

        // progress bar
        var pg = new GameObject("Progress", typeof(RectTransform), typeof(CanvasGroup));
        pg.transform.SetParent(canvasRt, false);
        progressGroup = pg.GetComponent<CanvasGroup>();
        var prt = (RectTransform)pg.transform;
        Anchor(prt, new Vector2(0.5f, 1f), new Vector2(0, -38), new Vector2(1560, 26));
        var bg = Img("Bg", prt, new Color(0.08f, 0.08f, 0.12f, 0.75f)); Stretch(bg.rectTransform);
        progressFill = Img("Fill", prt, new Color(1f, 0.82f, 0.15f));
        Stretch(progressFill.rectTransform, 4);
        progressFill.sprite = RunTextures.WhiteSprite();
        progressFill.type = Image.Type.Filled; progressFill.fillMethod = Image.FillMethod.Horizontal; progressFill.fillAmount = 0;
        progressDot = Img("Dot", prt, Color.white).rectTransform;
        ((Image)progressDot.GetComponent<Image>()).sprite = RunTextures.CircleSprite(Color.white, new Color(0.1f, 0.15f, 0.4f), 0f);
        progressDot.sizeDelta = new Vector2(46, 46);
        progressDot.anchorMin = progressDot.anchorMax = new Vector2(0, 0.5f);

        // action icon
        var ic = Img("Icon", canvasRt, Color.white);
        ic.sprite = RunTextures.CircleSprite(new Color(0.07f, 0.12f, 0.35f), Color.white, 0.09f);
        icon = ic.rectTransform;
        Anchor(icon, new Vector2(0.5f, 1f), new Vector2(0, -155), new Vector2(190, 190));
        iconGlyph = Txt("Glyph", icon, "", 96, Color.white); iconGlyph.rectTransform.anchoredPosition = new Vector2(0, 16);
        iconLabel = Txt("Label", icon, "", 30, Color.white); iconLabel.rectTransform.anchoredPosition = new Vector2(0, -52);
        icon.localScale = Vector3.zero;

        flash = Img("Flash", canvasRt, new Color(1, 1, 1, 0)); Stretch(flash.rectTransform);
        flash.raycastTarget = false;

        // card overlay
        var cg = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup));
        cg.transform.SetParent(canvasRt, false);
        card = cg.GetComponent<CanvasGroup>();
        var crt = (RectTransform)cg.transform; Stretch(crt);
        var black = Img("Black", crt, new Color(0.03f, 0.03f, 0.05f, 1f)); Stretch(black.rectTransform);
        cardLines = Raw("Lines", crt, RunTextures.SpeedLines(512, 80, 0.5f));
        Anchor(cardLines.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2600, 2600));
        cardLines.color = new Color(1, 1, 1, 0.35f);
        cardTitle = Txt("Title", crt, "", 150, Color.white); cardTitle.rectTransform.anchoredPosition = new Vector2(0, 260);
        cardSub = Txt("Sub", crt, "", 70, new Color(1f, 0.85f, 0.2f)); cardSub.rectTransform.anchoredPosition = new Vector2(0, 120);
        var mv = new GameObject("Moves", typeof(RectTransform)); mv.transform.SetParent(crt, false);
        cardMoves = (RectTransform)mv.transform; cardMoves.anchoredPosition = new Vector2(0, -160);
        card.alpha = 0;
    }

    void Update()
    {
        lineTimer -= Time.deltaTime;
        if (lineTimer <= 0f)
        {
            lineTimer = 0.05f;
            float a = Random.Range(0f, 360f);
            speedLines.rectTransform.localRotation = Quaternion.Euler(0, 0, a);
            cardLines.rectTransform.localRotation = Quaternion.Euler(0, 0, -a);
        }
    }

    // ---------- API ----------
    public void SetProgress(float k, bool visible)
    {
        progressGroup.alpha = visible ? 1 : 0;
        progressFill.fillAmount = Mathf.Clamp01(k);
        progressDot.anchoredPosition = new Vector2(1560 * Mathf.Clamp01(k), 0);
    }

    public void ShowIcon(RunMove m)
    {
        iconGlyph.text = RunMoveInfo.Glyph(m);
        iconLabel.text = RunMoveInfo.Label(m);
        icon.DOKill();
        icon.localScale = Vector3.zero;
        icon.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
    }

    public void HideIcon()
    {
        icon.DOKill();
        icon.DOScale(0f, 0.15f).SetEase(Ease.InBack);
    }

    public void Flash()
    {
        flash.DOKill();
        flash.color = new Color(1, 1, 1, 0.6f);
        flash.DOFade(0f, 0.25f);
    }

    public IEnumerator Card(string title, string sub, RunMove[] moves, RunMove[] newMoves, float hold)
    {
        cardTitle.text = title; cardSub.text = sub ?? "";
        cardTitle.rectTransform.anchoredPosition = new Vector2(0, moves == null ? 60 : 260);
        cardSub.rectTransform.anchoredPosition = new Vector2(0, moves == null ? -90 : 120);
        foreach (Transform c in cardMoves) Destroy(c.gameObject);
        if (moves != null)
        {
            float w = 300f, x0 = -(moves.Length - 1) * w * 0.5f;
            for (int i = 0; i < moves.Length; i++)
            {
                var slot = new GameObject("M", typeof(RectTransform)).GetComponent<RectTransform>();
                slot.SetParent(cardMoves, false);
                slot.anchoredPosition = new Vector2(x0 + i * w, 0);
                var circ = Img("C", slot, Color.white);
                circ.sprite = RunTextures.CircleSprite(new Color(0.07f, 0.12f, 0.35f), Color.white, 0.09f);
                circ.rectTransform.sizeDelta = new Vector2(200, 200);
                Txt("G", slot, RunMoveInfo.Glyph(moves[i]), 110, Color.white).rectTransform.anchoredPosition = new Vector2(0, 10);
                Txt("L", slot, RunMoveInfo.Label(moves[i]), 46, Color.white).rectTransform.anchoredPosition = new Vector2(0, -150);
                if (newMoves != null && System.Array.IndexOf(newMoves, moves[i]) >= 0)
                {
                    var n = Txt("N", slot, "NEW", 40, new Color(0.75f, 1f, 0.2f));
                    n.rectTransform.anchoredPosition = new Vector2(0, 135);
                    n.rectTransform.DOScale(1.2f, 0.35f).SetLoops(-1, LoopType.Yoyo).SetLink(n.gameObject);
                }
            }
        }
        card.DOKill();
        card.alpha = 1;
        cardTitle.rectTransform.localScale = Vector3.one * 1.6f;
        cardTitle.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        cardSub.rectTransform.localScale = Vector3.zero;
        cardSub.rectTransform.DOScale(1f, 0.3f).SetDelay(0.15f).SetEase(Ease.OutBack);
        cardMoves.localScale = Vector3.zero;
        cardMoves.DOScale(1f, 0.35f).SetDelay(0.25f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(hold);
    }

    public void HideCard(float fade = 0.25f)
    {
        card.DOKill();
        card.DOFade(0f, fade);
    }

    // ---------- ui helpers ----------
    Image Img(string n, Transform p, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p, false);
        var i = go.GetComponent<Image>(); i.color = c; i.raycastTarget = false;
        return i;
    }

    RawImage Raw(string n, Transform p, Texture t)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(p, false);
        var i = go.GetComponent<RawImage>(); i.texture = t; i.raycastTarget = false;
        return i;
    }

    Text Txt(string n, Transform p, string s, int size, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(p, false);
        var t = go.GetComponent<Text>();
        t.font = font; t.fontSize = size; t.fontStyle = FontStyle.Bold; t.color = c; t.text = s;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        var o = go.GetComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.8f); o.effectDistance = new Vector2(3, -3);
        ((RectTransform)go.transform).sizeDelta = new Vector2(1600, size * 1.3f);
        return t;
    }

    static void Stretch(RectTransform rt, float inset = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one * inset; rt.offsetMax = -Vector2.one * inset;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
    }
}

public static class RunTextures
{
    public static Texture2D SpeedLines(int size, int count, float clearRadius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        var rnd = new System.Random(7);
        var angles = new float[count]; var widths = new float[count]; var starts = new float[count];
        for (int i = 0; i < count; i++)
        {
            angles[i] = (float)(rnd.NextDouble() * Mathf.PI * 2);
            widths[i] = 0.002f + (float)rnd.NextDouble() * 0.006f;
            starts[i] = clearRadius + (float)rnd.NextDouble() * 0.25f;
        }
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2 - 1, v = (y + 0.5f) / size * 2 - 1;
                float r = Mathf.Sqrt(u * u + v * v), a = Mathf.Atan2(v, u);
                float best = 0;
                if (r > clearRadius)
                    for (int i = 0; i < count; i++)
                    {
                        if (r < starts[i]) continue;
                        float d = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, angles[i] * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                        float w = widths[i] * (r - starts[i] + 0.1f) * 3f;
                        if (d < w) best = Mathf.Max(best, 1f - d / w);
                    }
                byte al = (byte)(Mathf.Clamp01(best * Mathf.Clamp01((r - clearRadius) * 3f)) * 255);
                px[y * size + x] = new Color32(255, 255, 255, al);
            }
        tex.SetPixels32(px); tex.Apply();
        return tex;
    }

    static Sprite white;
    public static Sprite WhiteSprite()
    {
        if (white) return white;
        var t = new Texture2D(4, 4); var c = new Color[16]; for (int i = 0; i < 16; i++) c[i] = Color.white;
        t.SetPixels(c); t.Apply();
        return white = Sprite.Create(t, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
    }

    public static Sprite CircleSprite(Color fill, Color ring, float ringWidth)
    {
        const int S = 256;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2 - 1, v = (y + 0.5f) / S * 2 - 1, r = Mathf.Sqrt(u * u + v * v);
                float edge = Mathf.Clamp01((1f - r) * S * 0.5f);
                Color c = r > 1f - ringWidth ? ring : fill;
                c.a *= edge;
                px[y * S + x] = c;
            }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, S, S), Vector2.one * 0.5f);
    }
}
