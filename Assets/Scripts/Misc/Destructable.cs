using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    [SerializeField] private GameObject destroyVFX; // Hiệu ứng phá hủy
    [SerializeField] private AudioClip defaultDestroySound; // Âm thanh mặc định
    [SerializeField] private List<DestructibleSound> destructibleSounds; // Các âm thanh khác nhau

    private AudioSource audioSource;

    private void Awake()
    {
        // Kiểm tra hoặc thêm AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<DamageSource>() || other.gameObject.GetComponent<ProjectTile>())
        {
            // Lấy âm thanh phù hợp
            AudioClip destroySound = GetSoundForTag(gameObject.tag);

            // Phát âm thanh
            if (destroySound != null)
            {
                audioSource.PlayOneShot(destroySound);
            }
            else if (defaultDestroySound != null) // Âm thanh mặc định
            {
                audioSource.PlayOneShot(defaultDestroySound);
            }

            // Gọi hiệu ứng VFX và phá hủy vật thể
            GetComponent<PickUpSpawner>().DropItems();
            Instantiate(destroyVFX, transform.position, Quaternion.identity);

            // Phá hủy vật thể
            Destroy(gameObject);
        }
    }

    private AudioClip GetSoundForTag(string tag)
    {
        // Tìm âm thanh theo tag
        foreach (var destructibleSound in destructibleSounds)
        {
            if (destructibleSound.tag == tag)
            {
                return destructibleSound.audioClip;
            }
        }
        return null;
    }

    [System.Serializable]
    public class DestructibleSound
    {
        public string tag; // Tag của vật thể
        public AudioClip audioClip; // Âm thanh tương ứng
    }
}
