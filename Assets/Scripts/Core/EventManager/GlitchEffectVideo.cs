using UnityEngine;
using UnityEngine.Video;

public class GlitchEffectVideo : MonoBehaviour
{
    public VideoPlayer video;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        video.isLooping = true; // video chạy vòng lặp
        video.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
