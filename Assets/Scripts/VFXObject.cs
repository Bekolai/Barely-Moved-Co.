using UnityEngine;

public class VFXObject : MonoBehaviour
{
    public VFXType Type;
    [HideInInspector] public ParticleSystem Particle;
    
    private void Awake()
    {
        Particle = GetComponent<ParticleSystem>();
    }
}
