using UnityEngine;

namespace BarelyMoved
{
	/// <summary>
	/// Individual fractured mesh piece with physics.
	/// Auto-destroys after a set lifetime.
	/// </summary>
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(MeshCollider))]
	public class FracturedPiece : MonoBehaviour
	{
		private float m_Lifetime = 5f;
		private float m_FadeStartTime = 4f;
		private float m_TimeAlive = 0f;
		
		private MeshRenderer m_Renderer;
		private MaterialPropertyBlock m_PropBlock;
		private static readonly int s_AlphaId = Shader.PropertyToID("_Alpha");
		private Color m_OriginalColor;
		
		public void Initialize(Mesh _mesh, Material _material, Vector3 _initialVelocity, float _lifetime = 5f)
		{
			// Setup mesh
			MeshFilter meshFilter = GetComponent<MeshFilter>();
			meshFilter.mesh = _mesh;
			
			// Setup renderer
			m_Renderer = GetComponent<MeshRenderer>();
			m_Renderer.material = _material;
			m_OriginalColor = _material.color;
			
			// Setup collider
			MeshCollider collider = GetComponent<MeshCollider>();
			collider.sharedMesh = _mesh;
			collider.convex = true;
			
			// Setup physics
			Rigidbody rb = GetComponent<Rigidbody>();
			rb.linearVelocity = _initialVelocity;
			rb.angularVelocity = Random.insideUnitSphere * Random.Range(2f, 8f);
			
			// Calculate mass based on volume approximation
			float volume = _mesh.bounds.size.x * _mesh.bounds.size.y * _mesh.bounds.size.z;
			rb.mass = Mathf.Max(0.1f, volume * 100f); // Scale factor for reasonable mass
			rb.linearDamping = 0.5f;
			rb.angularDamping = 0.5f;
			
			m_Lifetime = _lifetime;
			m_FadeStartTime = _lifetime * 0.8f;
			m_PropBlock = new MaterialPropertyBlock();
		}
		
		private void Update()
		{
			m_TimeAlive += Time.deltaTime;
			
			// Start fading near end of life
			if (m_TimeAlive >= m_FadeStartTime)
			{
				float fadeProgress = (m_TimeAlive - m_FadeStartTime) / (m_Lifetime - m_FadeStartTime);
				float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
				
				// Try to fade using material property block (works if shader supports transparency)
				if (m_Renderer != null)
				{
					m_Renderer.GetPropertyBlock(m_PropBlock);
					m_PropBlock.SetFloat(s_AlphaId, alpha);
					
					// Also try setting color alpha
					Color fadedColor = m_OriginalColor;
					fadedColor.a = alpha;
					m_PropBlock.SetColor("_Color", fadedColor);
					m_PropBlock.SetColor("_BaseColor", fadedColor);
					
					m_Renderer.SetPropertyBlock(m_PropBlock);
				}
			}
			
			// Destroy after lifetime
			if (m_TimeAlive >= m_Lifetime)
			{
				Destroy(gameObject);
			}
		}
		
		/// <summary>
		/// Apply an explosive force to this piece from a point
		/// </summary>
		public void ApplyExplosiveForce(Vector3 _explosionPoint, float _force, float _radius)
		{
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb != null)
			{
				Vector3 direction = (transform.position - _explosionPoint).normalized;
				float distance = Vector3.Distance(transform.position, _explosionPoint);
				float falloff = Mathf.Clamp01(1f - (distance / _radius));
				
				Vector3 force = direction * _force * falloff;
				rb.AddForce(force, ForceMode.Impulse);
			}
		}
	}
}

