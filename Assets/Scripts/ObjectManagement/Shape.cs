using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape : PersistableObject {

    public int ShapeId {
        get {
            return shapeId;
        }
        set {
            if (shapeId == int.MinValue && value != int.MinValue) {
                shapeId = value;
            }
            else {
                Debug.LogError("Not allowed to change shapeId.");
            }
        }
    }

    public int MaterialId { get; private set; }

    int shapeId = int.MinValue;

    Color color;

    MeshRenderer meshRenderer;

    static int colorPropertyId = Shader.PropertyToID("_Color");

    static MaterialPropertyBlock sharedPropertyBlock;

    void Awake () {
        meshRenderer = GetComponent<MeshRenderer>();    
    }

    public void SetColor (Color color) {
        this.color = color;
        if (sharedPropertyBlock == null) {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public void SetMaterial (Material material, int materialId) {
        meshRenderer.material = material;
        MaterialId = materialId;
    }

    public override void Save (GameDataWriter writer) {
        base.Save(writer);
        writer.Write(color);
    }

    public override void Load (GameDataReader reader) {
        base.Load(reader);
        SetColor(reader.Version > 0 ? reader.ReadColor() : Color.white);
    }
}
