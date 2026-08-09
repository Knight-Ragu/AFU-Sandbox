using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using Il2CppInterop.Generator.Passes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppQuantum;
using Il2CppQuantum_Weapons;
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

    const int VERTICES_PER_SECTION = 14;
    const int INDICES_PER_SECTION = 24 * 3;

    const int SELECTOR_VERTICES = 4 + 3;
    const int SELECTOR_INDICES = 3 * 3;

    const float VISUAL_PADDING = 0.016f;

    static Color PrimaryColor = new(72f / 255f, 189f / 255f, 162f / 255f);
    static Color SecondaryColor = new(80f / 255f, 69f / 255f, 108f / 255f);
    static Color TertiaryColor = new(235f / 255f, 83f / 255f, 77f / 255f);


    public Vector3 Position;
    public Vector3 Forward;

    private Section[] Sections;
    private float timeCreated = 0f;

    public int SelectedSection;
    public int lastSelectedSection;
    private Section _cursorAnimation;
    public Vector2 CursorPosition;

    public static RadialMenuGfx Create(EntityRef humanoid)
    {
        GameObject radialMenu = new($"{humanoid} RadialMenu");

        radialMenu.AddComponent<MeshFilter>();
        radialMenu.AddComponent<MeshRenderer>();
        var rMG = radialMenu.AddComponent<RadialMenuGfx>();

        return rMG;
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

        // Sandbox .Log.Msg($"len {mF.sharedMesh.vertices.Length}, {Sections.Length}");

        mF.sharedMesh.triangles = new Il2CppStructArray<int>(Sections.Length * INDICES_PER_SECTION + SELECTOR_INDICES);

        Il2CppSystem.Collections.Generic.List<int> triangles = new();
        for (int i = 0; i < Sections.Length; i++)
        {
            Sections[i] = new Section().Init(120f, 0f, SecondaryColor, SecondaryColor);

            int index = i * VERTICES_PER_SECTION;

            // Front faces

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

            // Back faces
            
            triangles.Add(index + 7 + 6);
            triangles.Add(index + 7 + 5);
            triangles.Add(index + 7 + 3);

            triangles.Add(index + 7 + 3);
            triangles.Add(index + 7 + 5);
            triangles.Add(index + 7 + 2);

            triangles.Add(index + 7 + 1);
            triangles.Add(index + 7 + 2);
            triangles.Add(index + 7 + 5);

            triangles.Add(index + 7 + 5);
            triangles.Add(index + 7 + 0);
            triangles.Add(index + 7 + 1);

            triangles.Add(index + 7 + 4);
            triangles.Add(index + 7 + 0);
            triangles.Add(index + 7 + 5);

            // Side faces

            triangles.Add(index + 6);
            triangles.Add(index + 7 + 6);
            triangles.Add(index + 3);

            triangles.Add(index + 7 + 6);
            triangles.Add(index + 7 + 3);
            triangles.Add(index + 3);

            triangles.Add(index + 0);
            triangles.Add(index + 7 + 0);
            triangles.Add(index + 4);

            triangles.Add(index + 7 + 0);
            triangles.Add(index + 7 + 4);
            triangles.Add(index + 4);

            triangles.Add(index + 3);
            triangles.Add(index + 7 + 3);
            triangles.Add(index + 2);

            triangles.Add(index + 7 + 3);
            triangles.Add(index + 7 + 2);
            triangles.Add(index + 2);

            triangles.Add(index + 1);
            triangles.Add(index + 7 + 1);
            triangles.Add(index + 0);

            triangles.Add(index + 7 + 1);
            triangles.Add(index + 7 + 0);
            triangles.Add(index + 0);

            triangles.Add(index + 5);
            triangles.Add(index + 7 + 5);
            triangles.Add(index + 6);

            triangles.Add(index + 7 + 5);
            triangles.Add(index + 7 + 6);
            triangles.Add(index + 6);

            triangles.Add(index + 4);
            triangles.Add(index + 7 + 4);
            triangles.Add(index + 5);

            triangles.Add(index + 7 + 4);
            triangles.Add(index + 7 + 5);
            triangles.Add(index + 5);

            triangles.Add(index + 2);
            triangles.Add(index + 7 + 2);
            triangles.Add(index + 1);

            triangles.Add(index + 7 + 2);
            triangles.Add(index + 7 + 1);
            triangles.Add(index + 1);
        }

        triangles.Add(mF.sharedMesh.vertices.Length - 1);
        triangles.Add(mF.sharedMesh.vertices.Length - 2);
        triangles.Add(mF.sharedMesh.vertices.Length - 3);

        triangles.Add(mF.sharedMesh.vertices.Length - 4);
        triangles.Add(mF.sharedMesh.vertices.Length - 2);
        triangles.Add(mF.sharedMesh.vertices.Length - 1);

        triangles.Add(mF.sharedMesh.vertices.Length - 5);
        triangles.Add(mF.sharedMesh.vertices.Length - 6);
        triangles.Add(mF.sharedMesh.vertices.Length - 7);

        mF.sharedMesh.SetTriangles(triangles, 0, true);

        Vector3[] verts = new Vector3[mF.sharedMesh.vertices.Length];
        Color[] vertColors = new Color[mF.sharedMesh.vertices.Length];

        for (int i = 0; i < this.Sections.Length; i++)
        {
            int vi = i * VERTICES_PER_SECTION;

            for (int v = 0; v < VERTICES_PER_SECTION; v++)
            {
                verts[vi + v] = Vector3.zero;
                vertColors[vi + v] = PrimaryColor;
            }
        }
    }

    public void Start()
    {
        mF = this.GetComponent<MeshFilter>();
        var mR = this.GetComponent<MeshRenderer>();

        mF.sharedMesh = new Mesh();
        mR.sharedMaterial = Material.GetDefaultParticleMaterial(); // Shader.Find("Unlit")
        mR.sharedMaterial.mainTexture = Texture2D.whiteTexture;

        _cursorAnimation = _cursorAnimation.Init(0.0f, 0f, PrimaryColor, SecondaryColor);
    }

    public void LateUpdate()
    {
        transform.position =
            RadialMenuGfx.Section.ExpDecay(transform.position, this.Position, 12f, Time.deltaTime);

        transform.forward =
            RadialMenuGfx.Section.ExpDecay(transform.forward, this.Forward, 7f, Time.deltaTime);
        
        if (lastSelectedSection != SelectedSection)
        {
            // Add sound effect later
        }

        if (createLater != 256)
        {
            CreateSections(createLater);
            createLater = 256;
        }

        for (int i = 0; i < this.Sections.Length; i++)
        {
            if (Time.time - this.timeCreated < i * 0.036f) continue;

            var section = Sections[i];

            section.Position = 1f;
            section.SizeMult = 1f;
            section.FrontColor = PrimaryColor;
            section.BackColor = SecondaryColor;

            if (SelectedSection == i)
            {
                section.SizeMult = 1.25f;
                section.FrontColor = SecondaryColor;
                section.BackColor = TertiaryColor;
            }

            section.Decay(Time.deltaTime);

            Sections[i] = section;
        }

        _cursorAnimation.Position = SelectedSection == RadialMenuSelector.NO_SELECTION ? 0.095f : 0.12f;
        _cursorAnimation.BackColor = SecondaryColor;
        _cursorAnimation.FrontColor = PrimaryColor;

        _cursorAnimation.Decay(Time.deltaTime);

        this.Draw();

        lastSelectedSection = SelectedSection;
    }

    void Draw()
    {
        if (mF.sharedMesh.vertices.Length == 0) return;

        Vector3[] verts = new Vector3[mF.sharedMesh.vertices.Length];
        Color[] vertColors = new Color[mF.sharedMesh.vertices.Length];

        // Section vertices

        for (int i = 0; i < this.Sections.Length; i++)
        {
            var (bottomHeight, topHeight, frontColor, backColor) = Sections[i].Info();
            var (_, topBackHeight, _, _) = Sections[i].Info(0.45f);

            // This angle is in Turns (1.0 Turn == 360.0 Degrees)
            float startingAngle = i / (float)Sections.Length;
            float oneSection = 1.0f / (float)Sections.Length;

            int vi = i * VERTICES_PER_SECTION;

            float a = (startingAngle + (oneSection * 0.5f)) * (Mathf.PI * 2.0f);
            Vector3 paddingVec = new(Mathf.Sin(a), Mathf.Cos(a), 0.0f);

            // Position the top row of vertices

            for (int v = 0; v < 4; v++)
            {
                float angle = startingAngle + (v / 3.0f * oneSection);
                // Convert to Radians before using
                angle *= Mathf.PI * 2.0f;

                Vector3 position = new(Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
 
                // Sandbox .Log.Msg($"len {mF.sharedMesh.vertices.Length}, in: {vi + v}");

                verts[vi + v] = (position * topBackHeight) + new Vector3(0f, 0f, -0.10f);
                verts[vi + 7 + v] = position * topHeight;
                vertColors[vi + v] = frontColor;
                vertColors[vi + 7 + v] = backColor;
            }

            // Position the bottom row of vertices

            for (int v = 0; v < 3; v++)
            {
                float angle = startingAngle + (v / 2.0f * oneSection);
                // Convert to Radians before using
                angle *= Mathf.PI * 2.0f;

                Vector3 position = new(Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);

                // Sandbox .Log.Msg($"len {mF.sharedMesh.vertices.Length}, in: {vi + 4 + v}");
 
                verts[vi + 4 + v] = (position * bottomHeight) + new Vector3(0f, 0f, -0.10f);
                verts[vi + 4 + 7 + v] = position * bottomHeight;
                vertColors[vi + 4 + v] = frontColor;
                vertColors[vi + 4 + 7 + v] = backColor;
            }

            for (int v = 0; v < 14; v++)
                verts[vi + v] += paddingVec * VISUAL_PADDING;
        }

        // Cursor vertices

        var (cursorSize, _, frontCursorColor, backCursorColor) = _cursorAnimation.Info();

        var pos = CursorPosition * 0.24f;
        pos.x = -pos.x;

        if (CursorPosition.magnitude > 0.1f)
        {
            var posNorm = pos.normalized;
            var right = new Vector2(posNorm.y, -posNorm.x);

            verts[^1] = right * cursorSize * 0.5f;
            verts[^2] = pos + -right * cursorSize * 0.5f;
            verts[^3] = -right * cursorSize * 0.5f;
            verts[^4] = pos + right * cursorSize * 0.5f;

            verts[^1] += new Vector3(0f, 0f, 0.02f);
            verts[^2] += new Vector3(0f, 0f, 0.02f);
            verts[^3] += new Vector3(0f, 0f, 0.02f);
            verts[^4] += new Vector3(0f, 0f, 0.02f);

            verts[^5] = pos + -right * cursorSize * 1.75f;
            verts[^6] = pos + right * cursorSize * 1.75f;
            verts[^7] = pos + posNorm * cursorSize * 1.75f;

            verts[^5] += new Vector3(0f, 0f, 0.02f);
            verts[^6] += new Vector3(0f, 0f, 0.02f);
            verts[^7] += new Vector3(0f, 0f, 0.02f);

            vertColors[^1] = backCursorColor;
            vertColors[^2] = frontCursorColor;
            vertColors[^3] = backCursorColor;
            vertColors[^4] = frontCursorColor;
        }
        else
        {
            verts[^1] = (Vector3)pos + new Vector3(-cursorSize, cursorSize, 0.02f);
            verts[^2] = (Vector3)pos + new Vector3(cursorSize, -cursorSize, 0.02f);
            verts[^3] = (Vector3)pos + new Vector3(cursorSize, cursorSize, 0.02f);
            verts[^4] = (Vector3)pos + new Vector3(-cursorSize, -cursorSize, 0.02f);

            verts[^5] = Vector2.zero;
            verts[^6] = Vector2.zero;
            verts[^7] = Vector2.zero;

            vertColors[^1] = backCursorColor;
            vertColors[^2] = backCursorColor;
            vertColors[^3] = backCursorColor;
            vertColors[^4] = backCursorColor;
        }

        vertColors[^5] = frontCursorColor;
        vertColors[^6] = frontCursorColor;
        vertColors[^7] = frontCursorColor;


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

        public Color _frontColor;
        public Color FrontColor = PrimaryColor;

        public Color _backColor;
        public Color BackColor = PrimaryColor;
        
        public readonly (float bottom, float top, Color frontColor, Color backColor) Info(float scale = 1.0f)
            => (_position * POSITION_SCALE, (_position * POSITION_SCALE) + (SECTION_SIZE * _sizeMult * scale), _frontColor, _backColor);
        
        public Section Init(float position, float size, Color frontColor, Color backColor)
        {
            _position = position;
            _sizeMult = size;
            _frontColor = frontColor;
            _backColor = backColor;

            return this;
        }
        public void Decay(float dt)
        {
            _sizeMult = ExpDecay(_sizeMult, SizeMult, 17f, dt);
            _position = ExpDecay(_position, Position, 14f, dt);
            _frontColor = ExpDecay(_frontColor, FrontColor, 8f, dt);
            _backColor = ExpDecay(_backColor, BackColor, 10f, dt);

            _backColor.a = 0.5f;
        }
        
        public static dynamic ExpDecay(dynamic a, dynamic b, float decay, float dt)
            => b+(a-b)*Mathf.Exp(-decay*dt);
    }
}