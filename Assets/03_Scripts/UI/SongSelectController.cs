using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SongSelectController : MonoBehaviour
{
    [SerializeField] private SongCard cardPrefab;
    [SerializeField] private Transform cardParent;
    [SerializeField] private List<SongEntry> songs;

    void Start()
    {
        foreach (var song in songs)
        {
            SongCard card = Instantiate(cardPrefab, cardParent);
            card.Setup(song, () => SceneManager.LoadScene(song.sceneToLoad));
        }
    }
}