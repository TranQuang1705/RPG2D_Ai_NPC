// using UnityEngine;

// [ExecuteAlways]
// public class ColorController : MonoBehaviour
// {
//     [Header("References")]
//     public Light directionalLight;
//     public SpriteRenderer skyOverlay;

//     [Header("Colors by Time of Day")]
//     public Gradient dayGradient;
//     public AnimationCurve intensityCurve;

//     [Header("Season Tint Multiplier")]
//     public Color springTint = new Color(1f, 1f, 1f, 1f);
//     public Color summerTint = new Color(1.1f, 1f, 0.9f, 1f);
//     public Color autumnTint = new Color(1f, 0.9f, 0.8f, 1f);
//     public Color winterTint = new Color(0.9f, 0.95f, 1.1f, 1f);

//     void Update()
//     {
//         if (TimeManager.Instance == null || directionalLight == null) return;

//         float t = Mathf.Repeat(TimeManager.Instance.GetDayProgress(), 1f);
//         Color baseColor = dayGradient.Evaluate(t);
//         directionalLight.color = baseColor;
//         directionalLight.intensity = intensityCurve.Evaluate(t);

//         Color seasonTint = GetSeasonTint(TimeManager.Instance.GetCurrentSeason());
//         Color finalColor = baseColor * seasonTint;

//         if (skyOverlay != null)
//             skyOverlay.color = new Color(finalColor.r, finalColor.g, finalColor.b, 0.4f);
//     }

//     Color GetSeasonTint(Season s)
//     {
//         switch (s)
//         {
//             case Season.Spring: return springTint;
//             case Season.Summer: return summerTint;
//             case Season.Autumn: return autumnTint;
//             case Season.Winter: return winterTint;
//             default: return Color.white;
//         }
//     }
// }
