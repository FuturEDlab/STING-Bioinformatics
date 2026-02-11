using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CaptionManager : MonoBehaviour
{
    public static CaptionManager Instance;

    [Header("Assign in Inspector")]
    public TMP_Text captionText;
    public GameObject captionBar;

    [Header("Timing")]
    public float minPageTime = 1.5f;
    public float secondsPerWord = 0.25f;

    private Queue<string> queue = new Queue<string>();
    private Coroutine routine;

    private void Awake()
    {
        Instance = this;

        captionText.text = "";
        captionBar.SetActive(true);
    }

    public void ShowCaption(string text)
    {
        queue.Clear();
        queue.Enqueue(text);

        if (routine == null)
        {
            routine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        captionBar.SetActive(true);

        while (queue.Count > 0)
        {
            string full = queue.Dequeue();
            List<string> pages = SplitIntoPagesThatFit(full);

            foreach (string page in pages)
            {
                captionText.text = page;

                float duration =
                    Mathf.Max(minPageTime, CountWords(page) * secondsPerWord);

                yield return new WaitForSeconds(duration);
            }
        }

        captionText.text = "";
        captionBar.SetActive(true);
        routine = null;
    }

    private List<string> SplitIntoPagesThatFit(string text)
    {
        List<string> pages = new List<string>();

        string[] words = text.Split(' ');
        string current = "";

        foreach (string word in words)
        {
            string test = string.IsNullOrEmpty(current)
                ? word
                : current + " " + word;

            captionText.text = test;
            captionText.ForceMeshUpdate();

            if (captionText.preferredHeight >
                captionText.rectTransform.rect.height)
            {
                pages.Add(current);
                current = word;
            }
            else
            {
                current = test;
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            pages.Add(current);
        }

        return pages;
    }

    private int CountWords(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        return s.Split(' ').Length;
    }
}
