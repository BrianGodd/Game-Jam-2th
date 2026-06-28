using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ButtonHintUI : MonoBehaviour
{
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private TMP_Text hintLabel;
    [SerializeField] private float verticalSpacing = 12f;

    public static ButtonHintUI Instance { get; private set; }

    private readonly List<GameObject> hintObjects = new();
    private readonly List<TMP_Text> hintLabels = new();

    private void Awake()
    {
        Instance = this;

        hintObjects.Add(hintRoot);
        hintLabels.Add(hintLabel);
    }

    private void Start()
    {
        Hide();
    }

    public void Show(IReadOnlyList<string> messages)
    {
        EnsureHintCount(messages.Count);

        for (int i = 0; i < hintObjects.Count; i++)
        {
            bool shouldShow = i < messages.Count;
            hintObjects[i].SetActive(shouldShow);

            if (shouldShow)
            {
                hintLabels[i].text = messages[i];
            }
        }
    }

    public void Hide()
    {
        for (int i = 0; i < hintObjects.Count; i++)
        {
            hintObjects[i].SetActive(false);
        }
    }

    private void EnsureHintCount(int requiredCount)
    {
        while (hintObjects.Count < requiredCount)
        {
            GameObject hintClone = Instantiate(hintRoot, hintRoot.transform.parent);
            hintClone.name = $"Hint{hintObjects.Count + 1}";

            RectTransform sourceRect = (RectTransform)hintRoot.transform;
            RectTransform cloneRect = (RectTransform)hintClone.transform;
            float offsetY = (sourceRect.sizeDelta.y + verticalSpacing) * hintObjects.Count;
            cloneRect.anchoredPosition = sourceRect.anchoredPosition + Vector2.down * offsetY;

            TMP_Text cloneLabel = hintClone.GetComponentInChildren<TMP_Text>(true);

            hintObjects.Add(hintClone);
            hintLabels.Add(cloneLabel);
        }
    }
}
