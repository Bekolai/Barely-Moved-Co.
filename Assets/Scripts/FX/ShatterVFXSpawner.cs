using System.Collections.Generic;
using UnityEngine;

namespace BarelyMoved
{
	/// <summary>
	/// Global pooled spawner for ShatterVFX. Keeps allocations and GameObjects minimal.
	/// </summary>
	public sealed class ShatterVFXSpawner : MonoBehaviour
	{
		[SerializeField] private ShatterVFX m_Prefab;
		[SerializeField] private int m_InitialPoolSize = 8;

		private readonly Queue<ShatterVFX> m_Available = new Queue<ShatterVFX>();
		private readonly HashSet<ShatterVFX> m_InUse = new HashSet<ShatterVFX>();

		private static ShatterVFXSpawner s_Instance;
		public static ShatterVFXSpawner Instance
		{
			get
			{
				if (s_Instance == null)
				{
					s_Instance = FindAnyObjectByType<ShatterVFXSpawner>();
					if (s_Instance == null)
					{
						var go = new GameObject("ShatterVFXSpawner");
						s_Instance = go.AddComponent<ShatterVFXSpawner>();
						DontDestroyOnLoad(go);
					}
				}
				return s_Instance;
			}
		}

		void Awake()
		{
			if (s_Instance != null && s_Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			s_Instance = this;
			DontDestroyOnLoad(gameObject);
			EnsurePrefab();
			WarmPool();
		}

		private void EnsurePrefab()
		{
			if (m_Prefab == null)
			{
				var go = new GameObject("ShatterVFXPrefab");
				go.SetActive(false);
				m_Prefab = go.AddComponent<ShatterVFX>();
			}
		}

		private void WarmPool()
		{
			for (int i = 0; i < Mathf.Max(1, m_InitialPoolSize); i++)
			{
				CreateOne();
			}
		}

		private ShatterVFX CreateOne()
		{
			ShatterVFX entry = Instantiate(m_Prefab, transform);
			entry.gameObject.name = "ShatterVFX";
			entry.ResetForPool();
			m_Available.Enqueue(entry);
			return entry;
		}

		private ShatterVFX Rent()
		{
			if (m_Available.Count == 0)
			{
				CreateOne();
			}
			var entry = m_Available.Dequeue();
			m_InUse.Add(entry);
			return entry;
		}

		private void Return(ShatterVFX entry)
		{
			if (entry == null) return;
			entry.ResetForPool();
			if (m_InUse.Remove(entry))
			{
				m_Available.Enqueue(entry);
			}
		}

		public static void SpawnShatter(Vector3 position, Vector3 normal, int shards, float speed, float lifetime, Color tint)
		{
			var inst = Instance;
			if (inst == null) return;
			inst.InternalSpawn(position, normal, shards, speed, lifetime, tint);
		}

		private void InternalSpawn(Vector3 position, Vector3 normal, int shards, float speed, float lifetime, Color tint)
		{
			var entry = Rent();
			entry.Configure(shards, speed, lifetime);
			entry.Play(position, normal, tint, OnEntryComplete);
		}

		private void OnEntryComplete(ShatterVFX entry)
		{
			Return(entry);
		}
	}
}




