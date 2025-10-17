using System;
using UnityEngine;

namespace BarelyMoved
{
	/// <summary>
	/// Lightweight pooled shatter effect using instanced meshes or particles.
	/// Designed to be cheap: no runtime mesh fracturing, only prebuilt shards.
	/// </summary>
	public sealed class ShatterVFX : MonoBehaviour
	{
		[SerializeField] private Mesh[] m_Shards;
		[SerializeField] private Material m_SharedMaterial;
		[SerializeField] private int m_ShardsToSpawn = 8;
		[SerializeField] private float m_InitialSpeed = 4f;
		[SerializeField] private float m_Gravity = 9.81f;
		[SerializeField] private float m_Lifetime = 1.2f;
		[SerializeField] private Vector3 m_ScaleRange = new Vector3(0.3f, 0.5f, 0.3f);

		// Internal state (per play)
		private float m_TimeRemaining;
		private Vector3[] m_Positions;
		private Vector3[] m_Velocities;
		private Quaternion[] m_Rotations;
		private Vector3[] m_AngularVelocities;
		private Vector3[] m_Scales;

		private Transform m_CachedTransform;
		private Action<ShatterVFX> m_OnComplete;

		void Awake()
		{
			m_CachedTransform = transform;
		}

		public void Configure(int shardsToSpawn, float initialSpeed, float lifetime)
		{
			m_ShardsToSpawn = Mathf.Max(1, shardsToSpawn);
			m_InitialSpeed = Mathf.Max(0.1f, initialSpeed);
			m_Lifetime = Mathf.Max(0.1f, lifetime);
		}

		public void Play(Vector3 position, Vector3 normal, Color tint, Action<ShatterVFX> onComplete)
		{
			m_OnComplete = onComplete;
			m_TimeRemaining = m_Lifetime;
			m_CachedTransform.position = position;

			EnsureArrays();
			for (int i = 0; i < m_ShardsToSpawn; i++)
			{
				// Randomize initial direction biased by impact normal
				Vector3 rand = UnityEngine.Random.onUnitSphere;
				rand = Vector3.Lerp(rand, normal.normalized, 0.6f).normalized;
				m_Positions[i] = position + rand * 0.02f;
				m_Velocities[i] = rand * (m_InitialSpeed * UnityEngine.Random.Range(0.6f, 1.2f));
				m_Rotations[i] = UnityEngine.Random.rotationUniform;
				m_AngularVelocities[i] = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(3f, 10f);
				m_Scales[i] = new Vector3(
					UnityEngine.Random.Range(m_ScaleRange.x, m_ScaleRange.y),
					UnityEngine.Random.Range(m_ScaleRange.x, m_ScaleRange.y),
					UnityEngine.Random.Range(m_ScaleRange.x, m_ScaleRange.y));
			}

			// Optional: apply tint via material property block
			if (m_SharedMaterial != null)
			{
				var block = s_Mpb ?? (s_Mpb = new MaterialPropertyBlock());
				block.SetColor(_TintColorId, tint);
			}

			gameObject.SetActive(true);
		}

		private void EnsureArrays()
		{
			int n = Mathf.Max(1, m_ShardsToSpawn);
			if (m_Positions == null || m_Positions.Length != n)
			{
				m_Positions = new Vector3[n];
				m_Velocities = new Vector3[n];
				m_Rotations = new Quaternion[n];
				m_AngularVelocities = new Vector3[n];
				m_Scales = new Vector3[n];
			}
		}

		void LateUpdate()
		{
			if (m_TimeRemaining <= 0f) return;

			float dt = Time.deltaTime;
			m_TimeRemaining -= dt;
			Vector3 gravity = Vector3.down * m_Gravity;

			for (int i = 0; i < m_ShardsToSpawn; i++)
			{
				m_Velocities[i] += gravity * dt;
				m_Positions[i] += m_Velocities[i] * dt;
				m_Rotations[i] = Quaternion.Euler(m_AngularVelocities[i] * dt) * m_Rotations[i];
			}

			// Draw with instanced calls; choose a random shard mesh each instance
			if (m_Shards != null && m_Shards.Length > 0 && m_SharedMaterial != null)
			{
				for (int i = 0; i < m_ShardsToSpawn; i++)
				{
					var mesh = m_Shards[(i + 17) % m_Shards.Length];
					Matrix4x4 matrix = Matrix4x4.TRS(m_Positions[i], m_Rotations[i], m_Scales[i]);
					Graphics.DrawMesh(mesh, matrix, m_SharedMaterial, gameObject.layer, null, 0, s_Mpb);
				}
			}

			if (m_TimeRemaining <= 0f)
			{
				Complete();
			}
		}

		private void Complete()
		{
			m_OnComplete?.Invoke(this);
			gameObject.SetActive(false);
		}

		public void ResetForPool()
		{
			m_TimeRemaining = 0f;
			m_OnComplete = null;
			gameObject.SetActive(false);
		}

		private static MaterialPropertyBlock s_Mpb;
		private static readonly int _TintColorId = Shader.PropertyToID("_BaseColor");
	}
}




