using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DebugDrawManager : SingletonNode3D<DebugDrawManager>
{
	public const float DEBUG_POINT_SIZE = 10f;

	public const int DEBUG_STRING_FONT_SIZE = 12;
	public const int DEBUG_STRING_OUTLINE_SIZE = 4;

	public const int DEBUG_STRING_LINE_SPACING = -5;
	
	public const int NUM_CIRCLE_SEGMENTS = 12;

	private class DebugDrawBaseData
	{
		public Color Color;
		public double DurationSeconds;
	}

	private class DebugDrawPointData : DebugDrawBaseData
	{
		public Vector3 Location;
	}

	private class DebugDrawLineData : DebugDrawBaseData
	{
		public Vector3 Start;
		public Vector3 End;
	}

	private class DebugDrawSphereData : DebugDrawBaseData
	{
		public Vector3 Center;
		public float Radius;
	}

	private class DebugDrawBoxData : DebugDrawBaseData
	{
		public Vector3 Center;
		public Vector3 Extend;
		public Quaternion Rotation;
	}

	private class DebugDrawConeData : DebugDrawBaseData
	{
		public Vector3 Center;
		public Vector3 Direction;
		public float Length;
		public float AngleDegrees;
	}

	private class DebugDrawCylinderData : DebugDrawBaseData
	{
		public Vector3 Start;
		public Vector3 End;
		public float Radius;
	}

	private class DebugDrawStringData : DebugDrawBaseData
	{
		public Vector3 Location;
		public Label Label;
	}

	private List<DebugDrawPointData> _debugPoints = new();
	private List<DebugDrawLineData> _debugLines = new();
	private List<DebugDrawSphereData> _debugSpheres = new();
	private List<DebugDrawBoxData> _debugBoxes = new();
	private List<DebugDrawConeData> _debugCones = new();
	private List<DebugDrawCylinderData> _debugCylinders = new();
	private List<DebugDrawStringData> _debugStrings = new();

	private MeshInstance3D _meshInstance3D = new();
	private ImmediateMesh _immediateMesh = new();

	private StandardMaterial3D _primitivePointMaterial = new();
	private StandardMaterial3D _primitiveLineMaterial = new();

	private Vector3[] _normalizedCircleVertices;

	public override void _EnterTree()
	{
		base._EnterTree();


		AddChild(_meshInstance3D);
		_meshInstance3D.Mesh = _immediateMesh;
		_meshInstance3D.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		_primitivePointMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		_primitivePointMaterial.VertexColorUseAsAlbedo = true;
		_primitivePointMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		_primitivePointMaterial.UsePointSize = true;
		_primitivePointMaterial.PointSize = DEBUG_POINT_SIZE;

		_primitiveLineMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		_primitiveLineMaterial.VertexColorUseAsAlbedo = true;
		_primitiveLineMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		_primitiveLineMaterial.UsePointSize = false;

		// Generate circle vertices
		{
			_normalizedCircleVertices = new Vector3[NUM_CIRCLE_SEGMENTS];
			float angleStep = Mathf.Tau / NUM_CIRCLE_SEGMENTS;

			for (int i = 0; i <  NUM_CIRCLE_SEGMENTS; i++)
			{
				float angle = i * angleStep;
				float x = Mathf.Cos(angle);
				float z = Mathf.Sin(angle);

				_normalizedCircleVertices[i] = new Vector3(x, 0f, z);
			}
		}
	}

	public override void _Process(double deltaSeconds)
	{
		base._Process(deltaSeconds);

		_meshInstance3D.GlobalTransform = Transform3D.Identity;
		_immediateMesh.ClearSurfaces();

		InternalDrawPoints(deltaSeconds);
		InternalDrawLines(deltaSeconds);
		InternalDrawSpheres(deltaSeconds);
		InternalDrawBoxes(deltaSeconds);
		InternalDrawCones(deltaSeconds);
		InternalDrawCylinders(deltaSeconds);
		InternalDrawStrings(deltaSeconds);
	}

	public void Reset()
	{
		_debugPoints.Clear();
		_debugLines.Clear();
		_debugSpheres.Clear();
		_debugBoxes.Clear();
		_debugCones.Clear();
		_debugCylinders.Clear();

		for (int i = 0; i < _debugStrings.Count; i++)
			_debugStrings[i].Label.QueueFree();

		_debugStrings.Clear();
	}

	public void DrawDebugPoint(Vector3 worldLocation, Color color, double durationSeconds = 1f)
	{
		DebugDrawPointData point = new();
		point.Location = worldLocation;
		point.Color = color;
		point.DurationSeconds = durationSeconds;
		_debugPoints.Add(point);
	}

	public void DrawDebugLine(Vector3 worldStart, Vector3 worldEnd, Color color, double durationSeconds = 1f)
	{
		if (worldStart.IsEqualApprox(worldEnd))
			return;

		DebugDrawLineData line = new();
		line.Start = worldStart;
		line.End = worldEnd;
		line.Color = color;
		line.DurationSeconds = durationSeconds;
		_debugLines.Add(line);
	}
	public void DrawDebugSphere(Vector3 worldCenter, float radius, Color color, double durationSeconds = 1f)
	{
		if (Mathf.IsZeroApprox(radius))
			return;

		DebugDrawSphereData sphere = new();
		sphere.Center = worldCenter;
		sphere.Radius = radius;
		sphere.Color = color;
		sphere.DurationSeconds = durationSeconds;
		_debugSpheres.Add(sphere);
	}

	public void DrawDebugBox(Vector3 worldCenter, Vector3 extend, Vector3 eulerRotation, Color color, double durationSeconds = 1f)
	{
		if (extend.IsZeroApprox())
			return;

		DebugDrawBoxData box = new();
		box.Center = worldCenter;
		box.Extend = extend;
		box.Rotation = Quaternion.FromEuler(eulerRotation);
		box.Color = color;
		box.DurationSeconds = durationSeconds;
		_debugBoxes.Add(box);
	}

	public void DrawDebugCone(Vector3 worldCenter, Vector3 direction, float length, float angleDegrees, Color color, double durationSeconds = 1f)
	{
		if (Mathf.IsZeroApprox(length))
			return;

		DebugDrawConeData cone = new();
		cone.Center = worldCenter;
		cone.Direction = direction.Normalized();
		cone.Length = length;
		cone.AngleDegrees = angleDegrees;
		cone.Color = color;
		cone.DurationSeconds = durationSeconds;
		_debugCones.Add(cone);
	}

	public void DrawDebugCylinder(Vector3 worldStart, Vector3 worldEnd, float radius, Color color, double durationSeconds = 1f)
	{
		if (Mathf.IsZeroApprox(radius))
			return;

		DebugDrawCylinderData cylinder = new();
		cylinder.Start = worldStart;
		cylinder.End = worldEnd;
		cylinder.Radius = radius;
		cylinder.Color = color;
		cylinder.DurationSeconds = durationSeconds;
		_debugCylinders.Add(cylinder);
	}

	public void DrawDebugString(Vector3 worldLocation, string str, Color color, double durationSeconds = 1f)
	{
		if (string.IsNullOrEmpty(str)) 
			return;

		DebugDrawStringData stringData = new();
		stringData.Location = worldLocation;
		stringData.Color = color;
		stringData.DurationSeconds = durationSeconds;

		Label label = new();
		label.Text = str;
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeFontSizeOverride("font_size", DEBUG_STRING_FONT_SIZE);
		label.AddThemeConstantOverride("outline_size", DEBUG_STRING_OUTLINE_SIZE);
		label.AddThemeConstantOverride("line_spacing", DEBUG_STRING_LINE_SPACING);
		label.ZIndex = -1000;
		this.AddChild(label);

		stringData.Label = label;
		_debugStrings.Add(stringData);
	}

	private void InternalDrawPoints(double deltaSeconds)
	{
		if (_debugPoints.Count == 0)
			return;

		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Points, _primitivePointMaterial);

		for (int i = _debugPoints.Count - 1; i >= 0; i--)
		{
			DebugDrawPointData point = _debugPoints[i];

			_immediateMesh.SurfaceSetColor(point.Color);
			_immediateMesh.SurfaceAddVertex(point.Location);

			point.DurationSeconds -= deltaSeconds;
			if (point.DurationSeconds <= 0f)
				_debugPoints.RemoveAt(i);
		}

		_immediateMesh.SurfaceEnd();
	}

	private void InternalDrawLines(double deltaSeconds)
	{
		if (_debugLines.Count == 0)
			return;

		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _primitiveLineMaterial);

		for (int i = _debugLines.Count - 1; i >= 0; i--)
		{
			DebugDrawLineData line = _debugLines[i];

			_immediateMesh.SurfaceSetColor(line.Color);
			_immediateMesh.SurfaceAddVertex(line.Start);
			_immediateMesh.SurfaceAddVertex(line.End);

			line.DurationSeconds -= deltaSeconds;
			if (line.DurationSeconds <= 0f)
				_debugLines.RemoveAt(i);
		}

		_immediateMesh.SurfaceEnd();
	}

	private void InternalDrawSpheres(double deltaSeconds)
	{
		if (_debugSpheres.Count == 0)
			return;

		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _primitiveLineMaterial);

		for (int i = _debugSpheres.Count - 1; i >= 0; i--)
		{
			DebugDrawSphereData sphere = _debugSpheres[i];

			_immediateMesh.SurfaceSetColor(_debugSpheres[i].Color);

			Vector3 top = sphere.Center + Vector3.Up * sphere.Radius;
			Vector3 bottom = sphere.Center + Vector3.Down * sphere.Radius;

			Vector3[] mid_equator = (Vector3[])_normalizedCircleVertices.Clone();
			Vector3[] upper_equator = (Vector3[])_normalizedCircleVertices.Clone();
			Vector3[] lower_equator = (Vector3[])_normalizedCircleVertices.Clone();

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				mid_equator[j] *= sphere.Radius;
				mid_equator[j] += sphere.Center;

				upper_equator[j] *= sphere.Radius * 0.7f;
				lower_equator[j] *= sphere.Radius * 0.7f;

				upper_equator[j] += sphere.Center + Vector3.Up * sphere.Radius * 0.7f;
				lower_equator[j] += sphere.Center + Vector3.Down * sphere.Radius * 0.7f;
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(top);
				_immediateMesh.SurfaceAddVertex(upper_equator[j]);
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(upper_equator[j]);
				_immediateMesh.SurfaceAddVertex(mid_equator[j]);

				_immediateMesh.SurfaceAddVertex(mid_equator[j]);
				_immediateMesh.SurfaceAddVertex(lower_equator[j]);
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(lower_equator[j]);
				_immediateMesh.SurfaceAddVertex(bottom);
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(upper_equator[j]);
				_immediateMesh.SurfaceAddVertex(upper_equator[(j + 1) % NUM_CIRCLE_SEGMENTS]);

				_immediateMesh.SurfaceAddVertex(mid_equator[j]);
				_immediateMesh.SurfaceAddVertex(mid_equator[(j + 1) % NUM_CIRCLE_SEGMENTS]);

				_immediateMesh.SurfaceAddVertex(lower_equator[j]);
				_immediateMesh.SurfaceAddVertex(lower_equator[(j + 1) % NUM_CIRCLE_SEGMENTS]);
			}

			sphere.DurationSeconds -= deltaSeconds;
			if (sphere.DurationSeconds <= 0f)
				_debugSpheres.RemoveAt(i);
		}

		_immediateMesh.SurfaceEnd();
	}

	private void InternalDrawBoxes(double deltaSeconds)
	{
		if (_debugBoxes.Count == 0)
			return;

		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _primitiveLineMaterial);

		for (int i = _debugBoxes.Count - 1; i >= 0; i--)
		{
			DebugDrawBoxData box = _debugBoxes[i];

			_immediateMesh.SurfaceSetColor(box.Color);

			Vector3[] vertices = new Vector3[8];
			vertices[0] = new Vector3(-box.Extend.X, box.Extend.Y, -box.Extend.Z);
			vertices[1] = new Vector3(box.Extend.X, box.Extend.Y, -box.Extend.Z);
			vertices[2] = new Vector3(box.Extend.X, box.Extend.Y, box.Extend.Z);
			vertices[3] = new Vector3(-box.Extend.X, box.Extend.Y, box.Extend.Z);
			vertices[4] = new Vector3(-box.Extend.X, -box.Extend.Y, -box.Extend.Z);
			vertices[5] = new Vector3(box.Extend.X, -box.Extend.Y, -box.Extend.Z);
			vertices[6] = new Vector3(box.Extend.X, -box.Extend.Y, box.Extend.Z);
			vertices[7] = new Vector3(-box.Extend.X, -box.Extend.Y, box.Extend.Z);

			for (int j = 0; j < 8; j++)
			{
				vertices[j] = box.Rotation * vertices[j];
				vertices[j] += box.Center;
			}

			for (int j = 0; j < 4; j++)
			{
				_immediateMesh.SurfaceAddVertex(vertices[j]);
				_immediateMesh.SurfaceAddVertex(vertices[(j + 1) % 4]);
			}

			for (int j = 4; j < 8; j++)
			{
				_immediateMesh.SurfaceAddVertex(vertices[j]);
				_immediateMesh.SurfaceAddVertex(vertices[(j + 1) % 4 + 4]);
			}

			for (int j = 0; j < 4; j++)
			{
				_immediateMesh.SurfaceAddVertex(vertices[j]);
				_immediateMesh.SurfaceAddVertex(vertices[j + 4]);
			}

			box.DurationSeconds -= deltaSeconds;
			if (box.DurationSeconds <= 0f)
				_debugBoxes.RemoveAt(i);
		}

		_immediateMesh.SurfaceEnd();
	}

	private void InternalDrawCones(double deltaSeconds)
	{
		if (_debugCones.Count == 0)
			return;

		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _primitiveLineMaterial);

		for (int i = _debugCones.Count - 1; i >= 0; i--)
		{
			DebugDrawConeData cone = _debugCones[i];

			_immediateMesh.SurfaceSetColor(cone.Color);

			Vector3[] baseVertices = (Vector3[])_normalizedCircleVertices.Clone();

			float baseRadius = cone.Length * Mathf.Sin(0.5f * Mathf.DegToRad(cone.AngleDegrees));
			Vector3 baseWorldCenter = cone.Center + cone.Direction * cone.Length;
			Quaternion baseVerticesRotation = new Quaternion(Vector3.Up, cone.Direction);

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				baseVertices[j] *= baseRadius;
				baseVertices[j] = baseVerticesRotation * baseVertices[j];
				baseVertices[j] += baseWorldCenter;
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(cone.Center);
				_immediateMesh.SurfaceAddVertex(baseVertices[j]);
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(baseVertices[j]);
				_immediateMesh.SurfaceAddVertex(baseVertices[(j + 1) % NUM_CIRCLE_SEGMENTS]);
			}

			cone.DurationSeconds -= deltaSeconds;
			if (cone.DurationSeconds <= 0f)
				_debugCones.RemoveAt(i);
		}

		_immediateMesh.SurfaceEnd();
	}

	private void InternalDrawCylinders(double deltaSeconds)
	{
		if (_debugCylinders.Count == 0)
			return;

		_immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _primitiveLineMaterial);

		for (int i = _debugCylinders.Count - 1; i >= 0; i--)
		{
			DebugDrawCylinderData cylinder = _debugCylinders[i];

			_immediateMesh.SurfaceSetColor(cylinder.Color);

			Vector3[] topVertices = (Vector3[])_normalizedCircleVertices.Clone();
			Vector3[] bottomVertices = (Vector3[])_normalizedCircleVertices.Clone();

			Quaternion verticesRotation = new Quaternion(Vector3.Up, (cylinder.End - cylinder.Start).Normalized());

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				topVertices[j] *= cylinder.Radius;
				topVertices[j] = verticesRotation * topVertices[j];
				topVertices[j] += cylinder.Start;

				bottomVertices[j] *= cylinder.Radius;
				bottomVertices[j] = verticesRotation * bottomVertices[j];
				bottomVertices[j] += cylinder.End;
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(topVertices[j]);
				_immediateMesh.SurfaceAddVertex(bottomVertices[j]);
			}

			for (int j = 0; j < NUM_CIRCLE_SEGMENTS; j++)
			{
				_immediateMesh.SurfaceAddVertex(topVertices[j]);
				_immediateMesh.SurfaceAddVertex(topVertices[(j + 1) % NUM_CIRCLE_SEGMENTS]);

				_immediateMesh.SurfaceAddVertex(bottomVertices[j]);
				_immediateMesh.SurfaceAddVertex(bottomVertices[(j + 1) % NUM_CIRCLE_SEGMENTS]);
			}

			cylinder.DurationSeconds -= deltaSeconds;
			if (cylinder.DurationSeconds <= 0f)
				_debugCylinders.RemoveAt(i);
		}

		_immediateMesh.SurfaceEnd();
	}

	private void InternalDrawStrings(double deltaSeconds)
	{
		if (_debugStrings.Count == 0)
			return;

		for (int i = _debugStrings.Count - 1; i >= 0; i--)
		{
			DebugDrawStringData stringData = _debugStrings[i];

			if (GetViewport().GetCamera3D().IsPositionBehind(stringData.Location))
				stringData.Label.Hide();
			else
				stringData.Label.Show();

			Vector2 newScreenLocation = GetViewport().GetCamera3D().UnprojectPosition(stringData.Location);
			stringData.Label.Position = newScreenLocation;

			stringData.DurationSeconds -= deltaSeconds;
			if (stringData.DurationSeconds <= 0f)
			{
				_debugStrings.RemoveAt(i);
				stringData.Label.QueueFree();
			}
		}
	}

}
