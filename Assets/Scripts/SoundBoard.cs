using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum TetrisSound
{
    Rotate,
    Drop,
    ClearLine,
    Tetris
}

public class SoundBoard : MonoBehaviour {

    [SerializeField]
    private AudioClip m_dropSound;


    [SerializeField]
    private AudioClip m_clearLineSound;

    [SerializeField]
    private AudioClip m_rotateSound;

    private AudioSource m_source;

    private void Start()
    {
        this.m_source = GetComponent<AudioSource>();
    }

    public void PlaySound(TetrisSound sound)
    {
        switch (sound)
        {
            case TetrisSound.Rotate:
                this.m_source.PlayOneShot(this.m_rotateSound);
                break;
            case TetrisSound.Drop:
                this.m_source.PlayOneShot(this.m_dropSound);
                break;
            case TetrisSound.ClearLine:
                this.m_source.PlayOneShot(this.m_clearLineSound);
                break;
            case TetrisSound.Tetris:
                break;
            default:
                break;
        }
    }

}
