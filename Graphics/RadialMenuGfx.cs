using System.Collections.Generic;
using System.Globalization;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppQuantum;
using JPInstaller.Custom;
using UnityEngine;

namespace AfuSandbox;

public partial class Sandbox
{
    internal static readonly Dictionary<EntityRef, RadialMenuGfx> radialMenuGfx = [];

    partial void RegisterTypes()
    {
        ClassInjector.RegisterTypeInIl2Cpp<RadialMenuGfx>();
    }

    public static void RemoveRadialMenu(Frame f, EntityRef humanoid)
    {
        f.CustomRemove<RadialMenuSelector>(humanoid);
        UnityEngine.GameObject.Destroy(Sandbox.radialMenuGfx[humanoid].gameObject);
        Sandbox.radialMenuGfx.Remove(humanoid);
    }
}

public class RadialMenuGfx : MonoBehaviour
{
    MeshFilter mF;

    const int VERTICES_PER_SECTION = 7;
    const int INDICES_PER_SECTION = 5 * 3;

    const int SELECTOR_VERTICES = 4;
    const int SELECTOR_INDICES = 2 * 3;

    const float VISUAL_PADDING = 0.06f;


    public Vector3 Position;
    public Vector3 Forward;

    private Section[] Sections;
    private float timeCreated = 0f;

    public int SelectedSection;
    private Section _cursorAnimation;
    public Vector2 CursorPosition;

    public static RadialMenuGfx Create(EntityRef humanoid)
    {
        GameObject gameObject = new($"{humanoid} RadialMenu");

        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        var rMG = gameObject.AddComponent<RadialMenuGfx>();

        return rMG;
    }

    public void Start()
    {
        mF = this.GetComponent<MeshFilter>();
        var mR = this.GetComponent<MeshRenderer>();

        mF.sharedMesh = new Mesh();
        mR.sharedMaterial = Material.GetDefaultParticleMaterial(); // Shader.Find("Unlit")
        mR.sharedMaterial.mainTexture = Texture2D.whiteTexture;

        _cursorAnimation.Position = 0.0f;
        _cursorAnimation.Color = Color.green;
    }

    int createLater;
    public void QueueCreateSections(int sections)
        => createLater = sections;

    private void CreateSections(int sections)
    {
        this.timeCreated = Time.time;

        this.Sections = new Section[sections];

        mF.sharedMesh.vertices = new Il2CppStructArray<Vector3>(Sections.Length * VERTICES_PER_SECTION + SELECTOR_VERTICES);
        mF.sharedMesh.colors = new Il2CppStructArray<Color>(Sections.Length * VERTICES_PER_SECTION + SELECTOR_VERTICES);

        Sandbox .Log.Msg($"len {mF.sharedMesh.vertices.Length}, {Sections.Length}");

        mF.sharedMesh.triangles = new Il2CppStructArray<int>(Sections.Length * INDICES_PER_SECTION + SELECTOR_INDICES);

        Il2CppSystem.Collections.Generic.List<int> triangles = new();
        for (int i = 0; i < Sections.Length; i++)
        {
            int index = i * VERTICES_PER_SECTION;

            triangles.Add(index + 6);
            triangles.Add(index + 3);
            triangles.Add(index + 5);

            triangles.Add(index + 3);
            triangles.Add(index + 2);
            triangles.Add(index + 5);

            triangles.Add(index + 1);
            triangles.Add(index + 5);
            triangles.Add(index + 2);

            triangles.Add(index + 5);
            triangles.Add(index + 1);
            triangles.Add(index + 0);

            triangles.Add(index + 4);
            triangles.Add(index + 5);
            triangles.Add(index + 0);
        }

        triangles.Add(mF.sharedMesh.vertices.Length - 1);
        triangles.Add(mF.sharedMesh.vertices.Length - 2);
        triangles.Add(mF.sharedMesh.vertices.Length - 3);

        triangles.Add(mF.sharedMesh.vertices.Length - 4);
        triangles.Add(mF.sharedMesh.vertices.Length - 2);
        triangles.Add(mF.sharedMesh.vertices.Length - 1);

        mF.sharedMesh.SetTriangles(triangles, 0, true);

        Vector3[] verts = new Vector3[mF.sharedMesh.vertices.Length];
        Color[] vertColors = new Color[mF.sharedMesh.vertices.Length];

        for (int i = 0; i < this.Sections.Length; i++)
        {
            int vi = i * VERTICES_PER_SECTION;

            for (int v = 0; v < VERTICES_PER_SECTION; v++)
            {
                verts[vi + v] = Vector3.zero;
                vertColors[vi + v] = Color.yellow;
            }
        }
    }

    public void LateUpdate()
    {
        transform.position = RadialMenuGfx.Section.ExpDecay(
            transform.position,
            this.Position,
            11f,
            Time.deltaTime
        );

        transform.forward = RadialMenuGfx.Section.ExpDecay(
            transform.forward,
            this.Forward,
            15f,
            UnityEngine.Time.deltaTime
        );

        if (createLater != 256)
        {
            CreateSections(createLater);
            createLater = 256;
        }

        for (int i = 0; i < this.Sections.Length; i++)
        {
            var section = Sections[i];

            if (Time.time - this.timeCreated < 0.04f * i) continue;

            section.Position = 1f;
            section.SizeMult = 1f;
            section.Color = Color.yellow;

            if (SelectedSection == i)
            {
                section.SizeMult = 1.27f;
                section.Color = Color.white;
            }

            section.Decay(Time.deltaTime);

            Sections[i] = section;
        }

        this.Draw();
    }

    void Draw()
    {
        if (mF.sharedMesh.vertices.Length == 0) return;

        Vector3[] verts = new Vector3[mF.sharedMesh.vertices.Length];
        Color[] vertColors = new Color[mF.sharedMesh.vertices.Length];

        for (int i = 0; i < this.Sections.Length; i++)
        {
            var (bottomHeight, topHeight, color) = Sections[i].Info();

            float halfPadding = VISUAL_PADDING / 2f;

            // This angle is in Turns (1.0 Turn == 360.0 Degrees)
            float startingAngle = (i / (float)Sections.Length) + halfPadding;
            float oneSection = 1.0f / (float)Sections.Length;

            int vi = i * VERTICES_PER_SECTION;

            // Position the top row of vertices

            for (int v = 0; v < 4; v++)
            {
                float angle = startingAngle + (v / 3.0f * oneSection);
                // Convert to Radians before using
                angle *= Mathf.PI * 2.0f;
                angle -= VISUAL_PADDING * (v / 3.0f);

                Vector3 position = new(Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
 
                // Sandbox .Log.Msg($"len {mF.sharedMesh.vertices.Length}, in: {vi + v}");

                verts[vi + v] = position * topHeight;
                vertColors[vi + v] = color;
            }

            // Position the bottom row of vertices

            for (int v = 0; v < 3; v++)
            {
                float angle = startingAngle + (v / 2.0f * oneSection);
                angle *= Mathf.PI * 2.0f;
                angle -= VISUAL_PADDING * (v / 2.0f);

                Vector3 position = new(Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);

                // Sandbox .Log.Msg($"len {mF.sharedMesh.vertices.Length}, in: {vi + 4 + v}");
 
                verts[vi + 4 + v] = position * bottomHeight;
                vertColors[vi + 4 + v] = color;
            }
        }

        _cursorAnimation.Position = SelectedSection == RadialMenuSelector.NO_SELECTION ? 0.06f : 0.13f;
        _cursorAnimation.Decay(Time.deltaTime);

        var (cursorSize, _, cursorColor) = _cursorAnimation.Info();
        var pos = CursorPosition * 0.24f;
        pos.x = -pos.x;

        verts[^1] = (Vector3)pos + new Vector3(-cursorSize, cursorSize, 0.025f);
        verts[^2] = (Vector3)pos + new Vector3(cursorSize, -cursorSize, 0.025f);
        verts[^3] = (Vector3)pos + new Vector3(cursorSize, cursorSize, 0.025f);
        verts[^4] = (Vector3)pos + new Vector3(-cursorSize, -cursorSize, 0.025f);

        vertColors[^1] = cursorColor;
        vertColors[^2] = cursorColor;
        vertColors[^3] = cursorColor;
        vertColors[^4] = cursorColor;


        mF.sharedMesh.SetVertices(verts);
        mF.sharedMesh.SetColors(vertColors);
    }

    struct Section() {
        public float Index = 0;

        const float POSITION_SCALE = 0.12f;
        private float _position = 10f;
        public float Position = 1f;

        const float SECTION_SIZE = 0.18f;
        private float _sizeMult = 0f;
        public float SizeMult = 1f;

        public Color _color = Color.white;
        public Color Color = Color.yellow;

        public readonly (float bottom, float top, Color color) Info()
            => (_position * POSITION_SCALE, (_position * POSITION_SCALE) + (SECTION_SIZE * _sizeMult), _color);
        
        public void Decay(float dt)
        {
            _sizeMult = ExpDecay(_sizeMult, SizeMult, 17f, dt);
            _position = ExpDecay(_position, Position, 14f, dt);
            _color = ExpDecay(_color, Color, 5f, dt);

            _color.a = 0.5f;
        }
        
        public static dynamic ExpDecay(dynamic a, dynamic b, float decay, float dt)
            => b+(a-b)*Mathf.Exp(-decay*dt);
    }
}