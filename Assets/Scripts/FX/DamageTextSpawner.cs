using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace BarelyMoved
{
	/// <summary>
	/// Global pooled spawner for floating damage texts.
	/// Add one to the scene (auto-creates at runtime if none exists).
	/// </summary>
	public sealed class DamageTextSpawner : MonoBehaviour
	{
		[SerializeField] private DamageText m_Prefab;
		[SerializeField] private int m_InitialPoolSize = 16;
		[SerializeField] private float m_DefaultLifetime = 1.25f;
		[SerializeField] private float m_DefaultFloatSpeed = 1.5f;
		[SerializeField] private float m_SpawnJitterRadius = 0.05f;
		[SerializeField] private TMP_FontAsset m_Font;
		[SerializeField] private float m_FontSize = 2.5f;

		private readonly Queue<DamageText> m_Available = new Queue<DamageText>();
		private readonly HashSet<DamageText> m_InUse = new HashSet<DamageText>();

		private static DamageTextSpawner s_Instance;
		public static DamageTextSpawner Instance
		{
			get
			{
				if (s_Instance == null)
				{
					s_Instance = FindAnyObjectByType<DamageTextSpawner>();
					if (s_Instance == null)
					{
						var go = new GameObject("DamageTextSpawner");
						s_Instance = go.AddComponent<DamageTextSpawner>();
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
				var go = new GameObject("DamageTextPrefab");
				go.SetActive(false);
				m_Prefab = go.AddComponent<DamageText>();
			}
			m_Prefab.ConfigureAppearance(m_Font, m_FontSize);
		}

		private void WarmPool()
		{
			for (int i = 0; i < Mathf.Max(1, m_InitialPoolSize); i++)
			{
				CreateOne();
			}
		}

		private DamageText CreateOne()
		{
			DamageText instance = Instantiate(m_Prefab, transform);
			instance.gameObject.name = "DamageText";
			instance.ResetForPool();
			m_Available.Enqueue(instance);
			return instance;
		}

		private DamageText Rent()
		{
			if (m_Available.Count == 0)
			{
				CreateOne();
			}
			var entry = m_Available.Dequeue();
			m_InUse.Add(entry);
			return entry;
		}

		private void Return(DamageText entry)
		{
			if (entry == null) return;
			entry.ResetForPool();
			if (m_InUse.Remove(entry))
			{
				m_Available.Enqueue(entry);
			}
		}

		[SerializeField] private Gradient m_SeverityGradient;

		public static void SpawnDamageText(string text, Vector3 worldPosition, float severityRatio)
		{
			var inst = Instance;
			if (inst == null) return;
			Color color = inst.GetColorForSeverity(severityRatio);
			inst.InternalSpawn(text, worldPosition, color, inst.m_DefaultLifetime, inst.m_DefaultFloatSpeed);
		}

		private Color GetColorForSeverity(float severity)
		{
			severity = Mathf.Clamp01(severity);
			if (m_SeverityGradient != null && m_SeverityGradient.colorKeys != null && m_SeverityGradient.colorKeys.Length > 0)
			{
				return m_SeverityGradient.Evaluate(severity);
			}
			// Default mapping: low damage = yellow, medium = orange, high = red
			if (severity < 0.33f) return new Color(1f, 0.9f, 0.2f, 1f);
			if (severity < 0.66f) return new Color(1f, 0.5f, 0.1f, 1f);
			return Color.red;
		}

		private void InternalSpawn(string text, Vector3 worldPosition, Color color, float lifetime, float floatSpeed)
		{
			var entry = Rent();
			Vector3 jitter = (Random.insideUnitSphere * m_SpawnJitterRadius);
			jitter.y = Mathf.Abs(jitter.y);
			entry.ConfigureAppearance(m_Font, m_FontSize);

			// If the entry uses UGUI, ensure it has a world-space Canvas parent
			if (entry.UsesUGUI)
			{
				var parentCanvas = GetOrCreateWorldSpaceCanvas();
				entry.transform.SetParent(parentCanvas.transform, worldPositionStays: false);
			}
			else
			{
				entry.transform.SetParent(transform, worldPositionStays: false);
			}

			entry.Play(text, color, lifetime, floatSpeed, worldPosition, jitter, UnityEngine.Camera.main, OnEntryComplete);
		}

		private Canvas GetOrCreateWorldSpaceCanvas()
		{
			const string canvasName = "DamageTextCanvas";
			var existing = transform.Find(canvasName);
			Canvas canvas;
			if (existing == null)
			{
				var go = new GameObject(canvasName);
				go.transform.SetParent(transform, false);
				canvas = go.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.WorldSpace;
				canvas.worldCamera = UnityEngine.Camera.main;
				var scaler = go.AddComponent<CanvasScaler>();
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
				var raycaster = go.AddComponent<GraphicRaycaster>();
				go.layer = gameObject.layer;
				// Reasonable default size (meters in world space)
				var rect = go.GetComponent<RectTransform>();
				rect.sizeDelta = new Vector2(10f, 10f);
			}
			else
			{
				canvas = existing.GetComponent<Canvas>();
			}
			return canvas;
		}

		private void OnEntryComplete(DamageText entry)
		{
			Return(entry);
		}
	}
}


