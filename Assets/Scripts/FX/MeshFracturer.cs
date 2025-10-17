using System.Collections.Generic;
using UnityEngine;

namespace BarelyMoved
{
	/// <summary>
	/// Runtime mesh fracturing using plane-based slicing.
	/// Creates realistic fractured pieces with proper UVs and physics.
	/// </summary>
	public static class MeshFracturer
	{
		private const float c_MinFragmentVolume = 0.001f;
		
		/// <summary>
		/// Fracture a mesh into multiple pieces using random plane cuts
		/// </summary>
		public static List<Mesh> FractureMesh(Mesh _originalMesh, int _pieceCount, Vector3 _impactPoint, Bounds _bounds)
		{
			List<Mesh> fragments = new List<Mesh>();
			
			// Start with the original mesh as a single fragment
			List<MeshFragment> workingFragments = new List<MeshFragment>();
			workingFragments.Add(new MeshFragment(_originalMesh));
			
			// Determine how many cuts we need (each cut roughly doubles fragments)
			int cutsNeeded = Mathf.CeilToInt(Mathf.Log(_pieceCount, 2f));
			cutsNeeded = Mathf.Clamp(cutsNeeded, 2, 5); // Limit to reasonable amount
			
			// Perform random cuts
			for (int cutIndex = 0; cutIndex < cutsNeeded && workingFragments.Count < _pieceCount * 2; cutIndex++)
			{
				List<MeshFragment> newFragments = new List<MeshFragment>();
				
				foreach (var fragment in workingFragments)
				{
					// Create a cutting plane
					Plane cutPlane = GenerateCuttingPlane(_impactPoint, _bounds, cutIndex);
					
					// Try to split this fragment
					var splitResult = SplitMeshFragment(fragment, cutPlane);
					
					if (splitResult.positive != null && splitResult.negative != null)
					{
						// Successfully split
						newFragments.Add(splitResult.positive);
						newFragments.Add(splitResult.negative);
					}
					else
					{
						// Couldn't split, keep original
						newFragments.Add(fragment);
					}
				}
				
				workingFragments = newFragments;
			}
			
			// Convert fragments to meshes, limiting to desired count
			int count = Mathf.Min(workingFragments.Count, _pieceCount);
			for (int i = 0; i < count; i++)
			{
				Mesh fragmentMesh = workingFragments[i].ToMesh($"Fragment_{i}");
				if (fragmentMesh != null && fragmentMesh.vertexCount > 0)
				{
					fragments.Add(fragmentMesh);
				}
			}
			
			return fragments;
		}
		
		private static Plane GenerateCuttingPlane(Vector3 _impactPoint, Bounds _bounds, int _cutIndex)
		{
			// Bias cuts to go through or near the impact point for realistic fracture
			Vector3 planePoint = Vector3.Lerp(_bounds.center, _impactPoint, Random.Range(0.3f, 0.9f));
			planePoint += Random.insideUnitSphere * _bounds.extents.magnitude * 0.3f;
			
			// Random orientation with slight bias toward radial from impact
			Vector3 radialDir = (_bounds.center - _impactPoint).normalized;
			Vector3 randomDir = Random.onUnitSphere;
			Vector3 normal = Vector3.Lerp(randomDir, radialDir, 0.3f).normalized;
			
			return new Plane(normal, planePoint);
		}
		
		private static (MeshFragment positive, MeshFragment negative) SplitMeshFragment(MeshFragment _fragment, Plane _plane)
		{
			List<Vector3> posVertices = new List<Vector3>();
			List<Vector2> posUVs = new List<Vector2>();
			List<int> posTriangles = new List<int>();
			
			List<Vector3> negVertices = new List<Vector3>();
			List<Vector2> negUVs = new List<Vector2>();
			List<int> negTriangles = new List<int>();
			
			List<Vector3> capVertices = new List<Vector3>();
			
			// Process each triangle
			for (int i = 0; i < _fragment.triangles.Count; i += 3)
			{
				int i0 = _fragment.triangles[i];
				int i1 = _fragment.triangles[i + 1];
				int i2 = _fragment.triangles[i + 2];
				
				Vector3 v0 = _fragment.vertices[i0];
				Vector3 v1 = _fragment.vertices[i1];
				Vector3 v2 = _fragment.vertices[i2];
				
				Vector2 uv0 = _fragment.uvs[i0];
				Vector2 uv1 = _fragment.uvs[i1];
				Vector2 uv2 = _fragment.uvs[i2];
				
				// Classify vertices relative to plane
				bool side0 = _plane.GetSide(v0);
				bool side1 = _plane.GetSide(v1);
				bool side2 = _plane.GetSide(v2);
				
				int posCount = (side0 ? 1 : 0) + (side1 ? 1 : 0) + (side2 ? 1 : 0);
				
				if (posCount == 3)
				{
					// All on positive side
					AddTriangle(posVertices, posUVs, posTriangles, v0, v1, v2, uv0, uv1, uv2);
				}
				else if (posCount == 0)
				{
					// All on negative side
					AddTriangle(negVertices, negUVs, negTriangles, v0, v1, v2, uv0, uv1, uv2);
				}
				else
				{
					// Triangle crosses plane - need to split it
					SplitTriangle(_plane, 
						v0, v1, v2, uv0, uv1, uv2, 
						side0, side1, side2,
						posVertices, posUVs, posTriangles,
						negVertices, negUVs, negTriangles,
						capVertices);
				}
			}
			
			// Create cap geometry to seal the cut
			if (capVertices.Count >= 3)
			{
				FillCapHole(capVertices, _plane, posVertices, posUVs, posTriangles);
				FillCapHole(capVertices, _plane, negVertices, negUVs, negTriangles, true);
			}
			
			// Only return valid fragments
			MeshFragment posFrag = (posVertices.Count >= 3) ? new MeshFragment(posVertices, posUVs, posTriangles) : null;
			MeshFragment negFrag = (negVertices.Count >= 3) ? new MeshFragment(negVertices, negUVs, negTriangles) : null;
			
			return (posFrag, negFrag);
		}
		
		private static void AddTriangle(List<Vector3> _verts, List<Vector2> _uvs, List<int> _tris,
			Vector3 _v0, Vector3 _v1, Vector3 _v2, Vector2 _uv0, Vector2 _uv1, Vector2 _uv2)
		{
			int startIndex = _verts.Count;
			_verts.Add(_v0);
			_verts.Add(_v1);
			_verts.Add(_v2);
			_uvs.Add(_uv0);
			_uvs.Add(_uv1);
			_uvs.Add(_uv2);
			_tris.Add(startIndex);
			_tris.Add(startIndex + 1);
			_tris.Add(startIndex + 2);
		}
		
		private static void SplitTriangle(Plane _plane,
			Vector3 _v0, Vector3 _v1, Vector3 _v2,
			Vector2 _uv0, Vector2 _uv1, Vector2 _uv2,
			bool _s0, bool _s1, bool _s2,
			List<Vector3> _posVerts, List<Vector2> _posUVs, List<int> _posTris,
			List<Vector3> _negVerts, List<Vector2> _negUVs, List<int> _negTris,
			List<Vector3> _capVerts)
		{
			// Find the two intersection points where triangle edges cross the plane
			List<Vector3> posVerts = new List<Vector3>();
			List<Vector2> posUVs = new List<Vector2>();
			List<Vector3> negVerts = new List<Vector3>();
			List<Vector2> negUVs = new List<Vector2>();
			List<Vector3> intersections = new List<Vector3>();
			
			// Helper to process each edge
			void ProcessEdge(Vector3 v1, Vector3 v2, Vector2 uv1, Vector2 uv2, bool s1, bool s2)
			{
				if (s1) { posVerts.Add(v1); posUVs.Add(uv1); }
				else { negVerts.Add(v1); negUVs.Add(uv1); }
				
				if (s1 != s2) // Edge crosses plane
				{
					float distance1 = _plane.GetDistanceToPoint(v1);
					float distance2 = _plane.GetDistanceToPoint(v2);
					float t = Mathf.Abs(distance1) / (Mathf.Abs(distance1) + Mathf.Abs(distance2));
					
					Vector3 intersection = Vector3.Lerp(v1, v2, t);
					Vector2 uvIntersection = Vector2.Lerp(uv1, uv2, t);
					
					posVerts.Add(intersection);
					posUVs.Add(uvIntersection);
					negVerts.Add(intersection);
					negUVs.Add(uvIntersection);
					intersections.Add(intersection);
				}
			}
			
			ProcessEdge(_v0, _v1, _uv0, _uv1, _s0, _s1);
			ProcessEdge(_v1, _v2, _uv1, _uv2, _s1, _s2);
			ProcessEdge(_v2, _v0, _uv2, _uv0, _s2, _s0);
			
			// Triangulate the resulting polygons
			TriangulatePoly(posVerts, posUVs, _posVerts, _posUVs, _posTris);
			TriangulatePoly(negVerts, negUVs, _negVerts, _negUVs, _negTris);
			
			// Add intersection points for cap
			foreach (var point in intersections)
			{
				_capVerts.Add(point);
			}
		}
		
		private static void TriangulatePoly(List<Vector3> _polyVerts, List<Vector2> _polyUVs,
			List<Vector3> _outVerts, List<Vector2> _outUVs, List<int> _outTris)
		{
			if (_polyVerts.Count < 3) return;
			
			int startIdx = _outVerts.Count;
			_outVerts.AddRange(_polyVerts);
			_outUVs.AddRange(_polyUVs);
			
			// Simple fan triangulation
			for (int i = 1; i < _polyVerts.Count - 1; i++)
			{
				_outTris.Add(startIdx);
				_outTris.Add(startIdx + i);
				_outTris.Add(startIdx + i + 1);
			}
		}
		
		private static void FillCapHole(List<Vector3> _capVerts, Plane _plane,
			List<Vector3> _outVerts, List<Vector2> _outUVs, List<int> _outTris, bool _reverse = false)
		{
			if (_capVerts.Count < 3) return;
			
			// Project cap vertices onto plane and triangulate
			Vector3 center = Vector3.zero;
			foreach (var v in _capVerts) center += v;
			center /= _capVerts.Count;
			
			// Sort vertices around center for proper triangulation
			List<Vector3> sortedVerts = new List<Vector3>(_capVerts);
			Vector3 refDir = (sortedVerts[0] - center).normalized;
			Vector3 normal = _plane.normal;
			
			sortedVerts.Sort((a, b) =>
			{
				Vector3 dirA = (a - center).normalized;
				Vector3 dirB = (b - center).normalized;
				float angleA = Vector3.SignedAngle(refDir, dirA, normal);
				float angleB = Vector3.SignedAngle(refDir, dirB, normal);
				return angleA.CompareTo(angleB);
			});
			
			// Create cap triangles
			int startIdx = _outVerts.Count;
			foreach (var v in sortedVerts)
			{
				_outVerts.Add(v);
				_outUVs.Add(new Vector2(v.x, v.z)); // Simple planar UV
			}
			
			for (int i = 1; i < sortedVerts.Count - 1; i++)
			{
				if (_reverse)
				{
					_outTris.Add(startIdx);
					_outTris.Add(startIdx + i + 1);
					_outTris.Add(startIdx + i);
				}
				else
				{
					_outTris.Add(startIdx);
					_outTris.Add(startIdx + i);
					_outTris.Add(startIdx + i + 1);
				}
			}
		}
		
		private class MeshFragment
		{
			public List<Vector3> vertices;
			public List<Vector2> uvs;
			public List<int> triangles;
			
			public MeshFragment(Mesh _mesh)
			{
				vertices = new List<Vector3>(_mesh.vertices);
				uvs = new List<Vector2>(_mesh.uv.Length > 0 ? _mesh.uv : new Vector2[_mesh.vertexCount]);
				triangles = new List<int>(_mesh.triangles);
				
				// Fill UVs if empty
				if (_mesh.uv.Length == 0)
				{
					for (int i = 0; i < vertices.Count; i++)
					{
						uvs.Add(new Vector2(vertices[i].x, vertices[i].z));
					}
				}
			}
			
			public MeshFragment(List<Vector3> _verts, List<Vector2> _uvs, List<int> _tris)
			{
				vertices = new List<Vector3>(_verts);
				uvs = new List<Vector2>(_uvs);
				triangles = new List<int>(_tris);
			}
			
			public Mesh ToMesh(string _name)
			{
				if (vertices.Count < 3 || triangles.Count < 3) return null;
				
				Mesh mesh = new Mesh();
				mesh.name = _name;
				mesh.SetVertices(vertices);
				mesh.SetUVs(0, uvs);
				mesh.SetTriangles(triangles, 0);
				mesh.RecalculateNormals();
				mesh.RecalculateBounds();
				return mesh;
			}
		}
	}
}

