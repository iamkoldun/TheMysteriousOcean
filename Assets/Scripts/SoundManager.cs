using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private AudioClip _click;
    private AudioClip _generator;
    private AudioClip _object;
    private AudioClip _pump;
    private AudioSource _source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;

        _click = Resources.Load<AudioClip>("Sounds/click");
        _generator = Resources.Load<AudioClip>("Sounds/generator");
        _object = Resources.Load<AudioClip>("Sounds/object");
        _pump = Resources.Load<AudioClip>("Sounds/pump");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            var go = new GameObject("SoundManager");
            go.AddComponent<SoundManager>();
        }
    }

    public void PlayClick() => Play(_click);
    public void PlayGenerator() => Play(_generator);
    public void PlayObject() => Play(_object);
    public void PlayPump() => Play(_pump);

    private void Play(AudioClip clip)
    {
        if (clip != null && _source != null)
            _source.PlayOneShot(clip);
    }
}
