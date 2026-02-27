using System.Collections.Generic;
using UnityEngine;

public class WinIntroPopupView : MonoBehaviour
{
    [Header("Required")]
    public RectTransform fireworksRoot;

    [Header("Optional: if empty, will spawn in center/random")]
    public List<RectTransform> spawnPoints = new();
}