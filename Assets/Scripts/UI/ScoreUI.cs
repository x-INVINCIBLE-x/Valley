using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Valley.Scoring;

namespace Valley.UI
{
    public class ScoreUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DistanceScoreTracker scoreTracker;

        [SerializeField] private TextMeshProUGUI score;
        [SerializeField] private TextMeshProUGUI multiplier;

        [SerializeField] private Transform[] multiplierContainers;

        [Header("Appearance")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color[] levelColors;

        [Header("Animation")]
        [SerializeField] private float iconDelay = 0.08f;
        [SerializeField] private float colorChangeDelay = 0.05f;

        private Image[][] _containerIcons;

        private Coroutine _animationRoutine;

        private int _currentProgress;
        private int _currentCycle;

        private void Awake()
        {
            CacheIcons();
        }

        private void Start()
        {
            scoreTracker.OnMultiplierUpdated += HandleMultiplierUpdated;

            Initialize();
        }

        private void OnDestroy()
        {
            scoreTracker.OnMultiplierUpdated -= HandleMultiplierUpdated;
        }

        private void Update()
        {
            score.text = $"{scoreTracker.Score:F0}";
        }

        private void Initialize()
        {
            score.text = $"{scoreTracker.Score:F0}";
            multiplier.text = $"x{scoreTracker.CurrentMultiplier:F2}";

            _currentProgress = Mathf.Max(0, Mathf.FloorToInt(scoreTracker.CurrentMultiplier) - 1);
            _currentCycle = Mathf.Max(0, (_currentProgress - 1) / _containerIcons[0].Length);

            ApplyInstant(_currentProgress);
        }

        private void CacheIcons()
        {
            _containerIcons = new Image[multiplierContainers.Length][];

            for (int i = 0; i < multiplierContainers.Length; i++)
            {
                int childCount = multiplierContainers[i].childCount;
                _containerIcons[i] = new Image[childCount];

                for (int j = 0; j < childCount; j++)
                {
                    _containerIcons[i][j] = multiplierContainers[i]
                        .GetChild(j)
                        .GetComponent<Image>();
                }
            }
        }

        private void HandleMultiplierUpdated(float previousMultiplier, float newMultiplier)
        {
            multiplier.text = $"x{newMultiplier:F2}";

            int targetProgress = Mathf.Max(0, Mathf.FloorToInt(newMultiplier) - 1);

            if (_animationRoutine != null)
                StopCoroutine(_animationRoutine);

            _animationRoutine = StartCoroutine(AnimateToProgress(targetProgress));
        }

        private IEnumerator AnimateToProgress(int targetProgress)
        {
            int iconCount = _containerIcons[0].Length;

            while (_currentProgress != targetProgress)
            {
                bool increasing = targetProgress > _currentProgress;

                if (increasing)
                    _currentProgress++;
                else
                    _currentProgress--;

                int cycle = Mathf.Max(0, (_currentProgress - 1) / iconCount);

                if (cycle != _currentCycle)
                {
                    yield return AnimateColorChange(cycle);
                    _currentCycle = cycle;
                }

                ApplyInstant(_currentProgress);

                yield return new WaitForSeconds(iconDelay);
            }

            _animationRoutine = null;
        }

        private IEnumerator AnimateColorChange(int cycle)
        {
            Color color = cycle > 0
                ? levelColors[Mathf.Min(cycle - 1, levelColors.Length - 1)]
                : defaultColor;

            int iconCount = _containerIcons[0].Length;

            for (int i = 0; i < iconCount; i++)
            {
                foreach (var container in _containerIcons)
                    container[i].gameObject.SetActive(false);

                yield return new WaitForSeconds(colorChangeDelay);

                foreach (var container in _containerIcons)
                {
                    container[i].color = color;
                    container[i].gameObject.SetActive(true);
                }

                yield return new WaitForSeconds(colorChangeDelay);
            }
        }

        private void ApplyInstant(int progress)
        {
            int iconCount = _containerIcons[0].Length;

            int activeIcons = Mathf.Clamp(progress, 0, iconCount);

            int cycle = Mathf.Max(0, (progress - 1) / iconCount);

            int upgradedIcons = progress <= iconCount
                ? 0
                : (progress - 1) % iconCount + 1;

            Color upgradeColor = cycle > 0
                ? levelColors[Mathf.Min(cycle - 1, levelColors.Length - 1)]
                : defaultColor;

            foreach (var container in _containerIcons)
            {
                for (int i = 0; i < container.Length; i++)
                {
                    bool active = i < activeIcons;
                    container[i].gameObject.SetActive(active);

                    if (active)
                    {
                        container[i].color = (cycle > 0 && i < upgradedIcons)
                            ? upgradeColor
                            : defaultColor;
                    }
                    else
                    {
                        container[i].color = defaultColor;
                    }
                }
            }
        }
    }
}