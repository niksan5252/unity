using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Minecraft-style HUD: hotbar (the slot for the current move lights up), hearts, XP bar with level,
// and "Advancement" toasts sliding in from the top right.
public class McHud : MonoBehaviour
{
    RunDirector d;
    Font font;
    RectTransform root, sel, xpFill, toast, levelPop;
    Text levelText, toastTitle, toastText;
    RawImage toastIcon;
    CanvasGroup group;
    int xp, level;
    const int XpPerLevel = 18;
    readonly System.Collections.Generic.HashSet<RunMove> praised = new System.Collections.Generic.HashSet<RunMove>();
    // slots: 0 pickaxe 1 sword 2 bow 3 grass 4 torch 5 apple 6 bread 7 diamond 8 heart(food)
    static readonly string[] SlotIcons = { "pickaxe", "sword", "bow", "grass", "torch", "apple", "bread", "diamond", "" };
    Texture2D Icon(string n) => n != "" && BlockFigure.Tex.TryGetValue("icon_" + n, out var t) ? t : null;

    public static McHud Create(RunDirector d)
    {
        var go = new GameObject("McHud");
        var h = go.AddComponent<McHud>();
        h.d = d;
        h.Build();
        return h;
    }

    Image Img(string n, Transform p, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p, false);
        var i = go.GetComponent<Image>(); i.color = c; i.raycastTarget = false;
        var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        return i;
    }

    RawImage Raw(string n, Transform p, Texture t, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(p, false);
        var i = go.GetComponent<RawImage>(); i.texture = t; i.raycastTarget = false;
        var rt = (RectTransform)go.transform; rt.anchoredPosition = pos; rt.sizeDelta = size;
        return i;
    }

    Text Txt(string n, Transform p, string s, int size, Color c, Vector2 pos, TextAnchor a = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(p, false);
        var t = go.GetComponent<Text>(); t.font = font; t.fontSize = size; t.fontStyle = FontStyle.Bold; t.color = c; t.text = s; t.alignment = a;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        var sh = go.GetComponent<Shadow>(); sh.effectColor = new Color(0.15f, 0.15f, 0.15f, 1f); sh.effectDistance = new Vector2(4, -4);
        var rt = (RectTransform)go.transform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(900, size * 1.4f);
        return t;
    }

    void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 5;
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 1f;
        group = gameObject.AddComponent<CanvasGroup>();
        root = (RectTransform)transform;

        // hotbar
        const float slot = 84f;
        var bar = Img("Hotbar", root, RunHud.Quest ? new Color(0.22f, 0.08f, 0.42f, 0.8f) : new Color(0.1f, 0.1f, 0.1f, 0.72f), new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(slot * 9 + 8, slot + 8));
        for (int i = 0; i < 9; i++)
        {
            var cell = Img("Slot" + i, bar.transform, new Color(0.55f, 0.55f, 0.55f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2((i - 4) * slot, 0), new Vector2(slot - 8, slot - 8));
            var ic = Icon(SlotIcons[i]);
            if (ic) Raw("I", cell.transform, ic, Vector2.zero, new Vector2(slot - 22, slot - 22));
        }
        var s = Img("Selected", bar.transform, new Color(1, 1, 1, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(slot + 6, slot + 6));
        var outline = s.gameObject.AddComponent<Outline>(); outline.effectColor = Color.white; outline.effectDistance = new Vector2(5, -5);
        var frame = Img("Frame", s.transform, new Color(1, 1, 1, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(slot + 6, slot + 6));
        frame.sprite = RingSprite();
        sel = s.rectTransform;

        // xp bar + level
        Img("XpBack", root, new Color(0.05f, 0.05f, 0.05f, 0.8f), new Vector2(0.5f, 0), new Vector2(0, 124), new Vector2(slot * 9, 16));
        var fill = Img("XpFill", root, new Color(0.5f, 1f, 0.25f), new Vector2(0.5f, 0), new Vector2(-slot * 4.5f, 124), new Vector2(0, 12));
        xpFill = fill.rectTransform; xpFill.pivot = new Vector2(0, 0.5f);
        levelText = Txt("Level", root, "0", 46, new Color(0.5f, 1f, 0.25f), Vector2.zero);
        var lrt = levelText.rectTransform; lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0); lrt.anchoredPosition = new Vector2(0, 152);

        // hearts
        var heart = Icon("heart");
        for (int i = 0; i < 10; i++)
        {
            var h = Raw("Heart" + i, root, heart, Vector2.zero, new Vector2(38, 38));
            var rt = h.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0); rt.anchoredPosition = new Vector2(-slot * 4.5f + 20 + i * 40, 160);
        }

        // advancement toast
        var tb = Img("Toast", root, new Color(0.13f, 0.13f, 0.14f, 0.95f), new Vector2(1, 1), new Vector2(330, -80), new Vector2(560, 120));
        var tOut = tb.gameObject.AddComponent<Outline>(); tOut.effectColor = new Color(0.55f, 0.55f, 0.6f); tOut.effectDistance = new Vector2(4, -4);
        toast = tb.rectTransform;
        toastIcon = Raw("Icon", toast, null, new Vector2(-210, 0), new Vector2(76, 76));
        toastTitle = Txt("Title", toast, "Advancement Made!", 30, new Color(1f, 0.92f, 0.2f), new Vector2(-150 + 450, 22), TextAnchor.MiddleLeft);
        toastTitle.rectTransform.anchoredPosition = new Vector2(250, 24);
        toastText = Txt("Text", toast, "", 34, Color.white, new Vector2(250, -22), TextAnchor.MiddleLeft);
        toastTitle.rectTransform.sizeDelta = toastText.rectTransform.sizeDelta = new Vector2(420, 50);
        toastTitle.rectTransform.anchoredPosition = new Vector2(55, 24);
        toastText.rectTransform.anchoredPosition = new Vector2(55, -22);

        levelPop = Txt("LevelUp", root, "LEVEL UP!", 64, new Color(0.5f, 1f, 0.25f), new Vector2(0, 260)).rectTransform;
        levelPop.anchorMin = levelPop.anchorMax = new Vector2(0.5f, 0);
        levelPop.localScale = Vector3.zero;
        Select(-1);
    }

    static Sprite ring;
    static Sprite RingSprite()
    {
        if (ring) return ring;
        const int S = 64; var t = new Texture2D(S, S); var px = new Color[S * S];
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) px[y * S + x] = (x < 5 || y < 5 || x >= S - 5 || y >= S - 5) ? Color.white : Color.clear;
        t.SetPixels(px); t.Apply(); t.filterMode = FilterMode.Point;
        return ring = Sprite.Create(t, new Rect(0, 0, S, S), Vector2.one * 0.5f);
    }

    public void Show(bool on) { group.alpha = on ? 1 : 0; }

    public void Select(int slotIndex)
    {
        sel.gameObject.SetActive(slotIndex >= 0);
        if (slotIndex < 0) return;
        sel.anchoredPosition = new Vector2((slotIndex - 4) * 84f, 0);
        sel.DOKill(true); sel.DOPunchScale(Vector3.one * 0.15f, 0.2f, 6, 0.5f);
    }

    public void SelectFor(RunMove m) => Select(m switch
    {
        RunMove.Dash => 0, RunMove.Smash => 1, RunMove.Left => 2, RunMove.Right => 2, RunMove.Jump => 3, RunMove.Duck => 4, _ => 7,
    });

    public void AddXp()
    {
        xp++;
        float k = (xp % XpPerLevel) / (float)XpPerLevel;
        xpFill.sizeDelta = new Vector2(84f * 9 * k, 12);
        if (xp % XpPerLevel == 0)
        {
            level++;
            levelText.text = level.ToString();
            levelText.rectTransform.DOKill(true); levelText.rectTransform.DOPunchScale(Vector3.one * 0.5f, 0.35f, 6, 0.5f);
            levelPop.DOKill(); levelPop.localScale = Vector3.zero;
            levelPop.DOScale(1f, 0.25f).SetEase(Ease.OutBack).OnComplete(() => levelPop.DOScale(0f, 0.2f).SetDelay(0.6f));
            d.PlaySfx(d.lib.sfxLevel, 0.6f);
            if (level == 5) Toast("Level 5 Reached!", "diamond");
            if (level == 15) Toast("XP Master!", "diamond");
        }
    }

    // first time each move is done: an advancement
    public void Praise(RunMove m)
    {
        if (!praised.Add(m)) return;
        switch (m)
        {
            case RunMove.Jump: Toast("Hop Hop!", "grass"); break;
            case RunMove.Duck: Toast("Sneaky Runner!", "torch"); break;
            case RunMove.Dash: Toast("Block Breaker!", "pickaxe"); break;
            case RunMove.Smash: Toast("Mob Hunter!", "sword"); break;
            case RunMove.Left: case RunMove.Right: Toast("Dodge Master!", "bow"); break;
            case RunMove.Mirror: Toast("Pose Master!", "diamond"); break;
        }
    }

    public void Toast(string text, string icon)
    {
        toastText.text = text;
        toastIcon.texture = Icon(icon);
        toast.DOKill();
        toast.anchoredPosition = new Vector2(330, -80);
        toast.DOAnchorPosX(-300, 0.35f).SetEase(Ease.OutCubic);
        toast.DOAnchorPosX(330, 0.35f).SetEase(Ease.InCubic).SetDelay(2.6f);
        d.PlaySfx(d.lib.sfxToast, 0.7f);
    }
}
