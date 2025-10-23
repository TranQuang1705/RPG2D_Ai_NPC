using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    private AudioSource audioSource;
    public AudioClip defaultMusic;   
    public AudioClip scene3Music;
    public AudioClip scene4Music;
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        PlayMusic(defaultMusic);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.buildIndex)
        {
            case 3: 
                PlayMusic(scene3Music);
                break;
            case 4: 
                PlayMusic(scene4Music);
                break;
            default:
                PlayMusic(defaultMusic);
                break;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip) return; 
        audioSource.clip = clip;

        if (clip == scene3Music)
        {
            audioSource.volume = 0.3f; 
        }
        else
        {
            audioSource.volume = 1.0f; 
        }

        audioSource.Play();
    }

}
